using Accord.Math;
using Accord.Statistics;
using Accord.Statistics.Links;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using Newtonsoft.Json.Linq;


namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Enum describing reasons that a disturbance may be set by logic to exactly zero
    /// </summary>
    public enum DisturbanceEstimationError
    { 
        /// <summary>
        /// default, when disturbance estimation has not run yet
        /// </summary>
        None=0,
        /// <summary>
        /// Set disturbance to zero beacause simulator did not run
        /// </summary>
        UnitSimulatorUnableToRun =1,
    }


    // note that in the real-world the disturbance is not a completely steady disturbance
    // it can have phase-shift and can look different than a normal

    /// <summary>
    /// Internal class to store a single sub-run of the DisturnanceIdentifierInternal
    /// 
    /// </summary>
    public class DisturbanceIdResult
    {

        /// <summary>
        /// Number of datapoints
        /// </summary>
        public int N = 0;
        /// <summary>
        /// Boolean which is true if all disturbances are zero
        /// </summary>
        public bool isAllZero = true;
        /// <summary>
        /// The reason for setting the disturbance to zero
        /// </summary>
        public DisturbanceEstimationError ErrorType;
        /// <summary>
        /// 
        /// </summary>
        public UnitDataSet adjustedUnitDataSet;

        /// <summary>
        /// The estimated disturbance vector
        /// </summary>
        public double[] d_est;
        /// <summary>
        /// (For debugging:) an estimated process gain
        /// </summary>
        public double estPidProcessGain;
        
        /// <summary>
        /// Constuctor
        /// </summary>
        /// <param name="dataSet"></param>
        public DisturbanceIdResult(UnitDataSet dataSet)
        {
            N = dataSet.GetNumDataPoints();
            ErrorType = DisturbanceEstimationError.None;
            SetToZero();
        }

        /// <summary>
        /// Sets the disturbance to zero (used in case of identification failure)
        /// </summary>
        public void SetToZero()
        {
            d_est = Vec<double>.Fill(0, N);
            estPidProcessGain = 0;
            isAllZero = true;
            adjustedUnitDataSet = null;
        }

    }

    /// <summary>
    /// For a given process model and dataset, calculate the disturbance vector d by subtracting y_proc from y_meas
    /// </summary>
    public class DisturbanceCalculator
    {

        /// <summary>
        /// Removes the effect of setpoint and (if relevant any non-pid input) changes  from the dataset using the model of pid and unit provided 
        /// 
        /// If external inputs u and y_set are constant, the original dataset is returned. 
        /// </summary>
        /// <param name="unitDataSet"></param>
        /// <param name="unitModel"></param>
        /// <param name="pidInputIdx"></param>
        /// <param name="pidParams"></param>
        /// <returns> a scrubbed copy of unitDataSet</returns>
        private static UnitDataSet RemoveSetpointAndOtherInputChangeEffectsFromDataSet(UnitDataSet unitDataSet,
            UnitModel unitModel, int pidInputIdx = 0, PidParameters pidParams = null)
        {
            // BEGIN check if both y_setpoint and all U externals are constant, if so just return the original dataset 
            bool isYsetConstant = false;
            if (Vec<double>.IsConstant(unitDataSet.Y_setpoint)) // and: if all 
            {
                isYsetConstant = true;
            }
            bool areAllExternalInputsConstant = false;
            if (unitDataSet.U.GetNColumns() > 1)
            {
                areAllExternalInputsConstant = true;
                for (int curColIdx = 0; curColIdx < unitDataSet.U.GetNColumns(); curColIdx++)
                {
                    if (curColIdx == pidInputIdx)
                        continue;
                    bool isConstant = Vec<double>.IsConstant(unitDataSet.U.GetColumn(curColIdx));
                    if (!isConstant)
                        areAllExternalInputsConstant = false;
                }
            }
            if (areAllExternalInputsConstant && isYsetConstant)
                return unitDataSet;
            // END

            var unitDataSet_adjusted = new UnitDataSet(unitDataSet);
            if (unitModel != null && pidParams != null)
            {
                var pidModel1 = new PidModel(pidParams, "PID");
                // BEGIN "no_dist" process simulation = 
                // a simulation of the process that does not include any real Y_meas or u_pid, thus no effects of 
                // disturbances are visible in this simulation

                var unitModelCopy = (UnitModel)unitModel.Clone("RemoveSetpointAndOtherInputChangeEffectsFromDataSet");// make copy that has no additive output signals(disturbance)
                unitModelCopy.additiveInputIDs = null;

                // a time delay of one sample must be applied because by convention
                // y_meas[k] = y_proces[k-1] + d[k]
                unitModelCopy.modelParameters.TimeDelay_s = unitModelCopy.modelParameters.TimeDelay_s + unitDataSet.GetTimeBase();

               var processSim_noDist = new PlantSimulator(
                    new List<ISimulatableModel> { pidModel1, unitModelCopy });
                processSim_noDist.ConnectModels(unitModelCopy, pidModel1);
                processSim_noDist.ConnectModels(pidModel1, unitModelCopy, pidInputIdx);

                var inputData_noDist = new TimeSeriesDataSet();
                if (unitDataSet.U.GetNColumns()>1)
                {
                    for (int curColIdx = 0; curColIdx < unitDataSet.U.GetNColumns(); curColIdx++)
                    {
                        if (curColIdx == pidInputIdx)
                            continue;
                        inputData_noDist.Add(processSim_noDist.AddExternalSignal(unitModelCopy, SignalType.External_U, curColIdx), 
                            unitDataSet.U.GetColumn(curColIdx));
                    }
                }
                     
                inputData_noDist.Add(processSim_noDist.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), unitDataSet.Y_setpoint);
                inputData_noDist.CreateTimestamps(unitDataSet.GetTimeBase(),unitDataSet.GetNumDataPoints());
                inputData_noDist.SetIndicesToIgnore(unitDataSet.IndicesToIgnore);
               
                // rewrite:
               // (var processSim_noDist, var inputData_noDist) = PlantSimulatorHelper.CreateFeedbackLoop(unitDataSet, pidModel1, unitModel, pidInputIdx);
                var noDist_isOk = processSim_noDist.Simulate(inputData_noDist, out TimeSeriesDataSet simData_noDist);
                if (noDist_isOk)
                {
                    int idxFirstGoodValue = 0;
                    if (unitDataSet.IndicesToIgnore != null)
                    {
                        if (unitDataSet.GetNumDataPoints() > 0)
                        {
                            while (unitDataSet.IndicesToIgnore.Contains(idxFirstGoodValue) &&
                                idxFirstGoodValue < unitDataSet.GetNumDataPoints() - 1)
                            {
                                idxFirstGoodValue++;
                            }
                        }
                    }
                    var vec = new Vec();

                    // create a new Y_meas that excludes the influence of any disturbance using "no_Dist" simulation
                    // this is used to find d_HF
                    //var procOutputY = simData_noDist.GetValues(unitModel.GetID(), SignalType.Output_Y);
                    var procOutputY = simData_noDist.GetValues(unitModelCopy.GetID());
                    var deltaProcOutputY = vec.Subtract(procOutputY, procOutputY[idxFirstGoodValue]);
                    unitDataSet_adjusted.Y_meas = vec.Subtract(unitDataSet.Y_meas, deltaProcOutputY);

                    // Yset : setpoints also, used to find d_HF
                    unitDataSet_adjusted.Y_setpoint = Vec<double>.Fill(unitDataSet.Y_setpoint[idxFirstGoodValue], unitDataSet.Y_setpoint.Length);

                    // create a new "U" that will be usd to do a new simulation y_sim of the UnitModel that excludes
                    // effects of externalInputs U, which will go into creating d_LF
                    // it uses the "no_dist" simulation for the PID-

                    for (int inputIdx = 0; inputIdx < unitDataSet_adjusted.U.GetNColumns(); inputIdx++)
                    {
                        if (inputIdx == pidInputIdx)
                        {
                            var pidOutputU = simData_noDist.GetValues(pidModel1.GetID(), SignalType.PID_U);
                            var pidDeltaU = vec.Subtract(pidOutputU, pidOutputU[idxFirstGoodValue]);
                            var newU = vec.Subtract(unitDataSet.U.GetColumn(inputIdx), pidDeltaU);
                            unitDataSet_adjusted.U = Matrix.ReplaceColumn(unitDataSet_adjusted.U, inputIdx, newU);
                        }
                        else
                        {
                            int N = unitDataSet.GetNumDataPoints();
                            // set all unitmodle external inputsvalues to constant value equalt to initial value
                            var newU = Vec<double>.Fill(unitDataSet.U.GetColumn(inputIdx)[idxFirstGoodValue], N);
                            unitDataSet_adjusted.U = Matrix.ReplaceColumn(unitDataSet_adjusted.U, inputIdx, newU);
                        }
                    }
                    unitDataSet_adjusted.IndicesToIgnore = unitDataSet.IndicesToIgnore;

                    if (false) // debugging plots, normally set to false
                    {
                        var dEst = vec.Subtract(unitDataSet.Y_meas,unitDataSet_adjusted.Y_meas);
                        var u_pid_adjusted = unitDataSet_adjusted.U.GetColumn(pidInputIdx);
                        var uPidVariance = vec.Mean(vec.Abs(vec.Diff(u_pid_adjusted))).Value;
                         var covBtwUPidAdjustedAndDest = Math.Abs(Measures.Covariance(dEst, u_pid_adjusted, false));
                        var comment = "distIdent_setpointTest Kp=" +
                            unitModel.GetModelParameters().LinearGains.ElementAt(pidInputIdx).ToString("F3", CultureInfo.InvariantCulture) +
                             "uPidVariance= " + uPidVariance.ToString("F3", CultureInfo.InvariantCulture) +
                             "cov=" + covBtwUPidAdjustedAndDest.ToString("F3", CultureInfo.InvariantCulture);
                        Shared.EnablePlots();
                        Plot.FromList(
                        new List<double[]> {
                          unitDataSet_adjusted.Y_meas,
                          unitDataSet.Y_meas,
                          unitDataSet_adjusted.Y_setpoint,
                          unitDataSet.Y_setpoint,
                          unitDataSet_adjusted.U.GetColumn(pidInputIdx),
                          unitDataSet.U.GetColumn(pidInputIdx)
                        },
                        new List<string> { "y1=y_meas(new)", "y1=y_meas(old)", "y1=y_set(new)", "y1=y_set(old)", "y3=u_pid(new)", "y3=u_pid(old)" },
                        inputData_noDist.GetTimeBase(), comment);
                        Shared.DisablePlots();
                        Thread.Sleep(100);
                    }
                }
            }
            return unitDataSet_adjusted;

        }


        /// <summary>
        /// Estimates the disturbance time-series over a given unit data set 
        /// given an estimate of the unit model (reference unit model) for a closed loop system.
        /// ymeas[k] = y_proc[k-1] +d[k] by convention
        /// </summary>
        /// <param name="unitDataSet">the dataset describing the unit, over which the disturbance is to be found, datset must specify Y_setpoint,Y_meas and U</param>
        /// <param name="unitModel">the estimate of the unit</param>
        /// <param name="pidInputIdx">the index of the pid-input in the unitModel</param>
        /// <param name="pidParams">the parameters if known of the pid-controller in the closed loop</param>
        /// <returns>A DisturbanceIdResult object, that contains the estimated disturbance, as well as </returns>
        public static DisturbanceIdResult CalculateDisturbanceVector(UnitDataSet unitDataSet,
            UnitModel unitModel, int pidInputIdx = 0, PidParameters pidParams = null)
        {
            if (unitModel == null)
                return null;

            var result = new DisturbanceIdResult(unitDataSet);
            var vec = new Vec(unitDataSet.BadDataID);

            // using the pidParams and unitModel, and if relevant any given y_set and external U, try to subtract the effects of 
            // non-disturbance related changes in the dataset producing "unitDataSet_adjusted"
            var unitDataSet_adjusted = RemoveSetpointAndOtherInputChangeEffectsFromDataSet(unitDataSet, unitModel, pidInputIdx, pidParams);
            unitDataSet_adjusted.D = null;
            (bool isOk, double[] y_proc, int numSimRestarts) = PlantSimulatorHelper.SimulateSingle(unitDataSet_adjusted, unitModel);

            if (y_proc == null)
            {
                result.ErrorType = DisturbanceEstimationError.UnitSimulatorUnableToRun;
                return result;
            }

            int indexOfFirstGoodValue = 0;
            if (unitDataSet.IndicesToIgnore != null)
            {
                if (unitDataSet.GetNumDataPoints() > 0)
                {
                    while (unitDataSet.IndicesToIgnore.Contains(indexOfFirstGoodValue) && indexOfFirstGoodValue < 
                        unitDataSet.GetNumDataPoints() - 1)
                    {
                        indexOfFirstGoodValue++;
                    }
                }
            }

            bool IsNaN(double value)
            {
                if (double.IsNaN(value) || value == unitDataSet_adjusted.BadDataID)
                    return true;
                else
                    return false;
            }

            double[] d_est = new double[unitDataSet_adjusted.Y_meas.Length];
            for (int i = 1; i < d_est.Length; i++)
            {
                if (IsNaN(unitDataSet_adjusted.Y_meas[i]) || IsNaN(y_proc[i]))
                    d_est[i] = double.NaN;
                else
                    d_est[i] = unitDataSet_adjusted.Y_meas[i] - y_proc[i-1]; // NB!!!  note by definiton y_proc[i-1]
            }
            d_est[0] = unitDataSet_adjusted.Y_meas[0] - y_proc[0];

            result.d_est = d_est;
            result.estPidProcessGain = unitModel.GetModelParameters().LinearGains.ElementAt(pidInputIdx);
            result.adjustedUnitDataSet = unitDataSet_adjusted;
            if (false)// debugging plots, should normally be set to "false"
            {
                var variableList = new List<double[]> {
                    unitDataSet.Y_meas,
                   result.adjustedUnitDataSet.Y_meas
                };
                var variableNameList = new List<string> { "y1=y_meas", "y1=y_meas(extUrem)" };

                if (result.adjustedUnitDataSet.U.GetNColumns() == 2)
                {
                    var nonPidIdx = 0;
                    if (pidInputIdx == 0)
                        nonPidIdx = 1;
                    variableList.Add(result.adjustedUnitDataSet.U.GetColumn(nonPidIdx));
                    variableNameList.Add("y2=u_nonpid");
                }
                Shared.EnablePlots();
                Plot.FromList(
                    variableList, variableNameList, result.adjustedUnitDataSet.GetTimeBase(), "distIdent_dLF_est");
                Shared.DisablePlots();
            }
            return result;
        }

    }
}
