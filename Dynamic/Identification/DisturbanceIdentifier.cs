using Accord.Math;
using Accord.Statistics.Links;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;


namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Enum describing reasons that a disturbance may be set by logic to exactly zero
    /// </summary>
    public enum DisturbanceSetToZeroReason
    { 
        /// <summary>
        /// default, when disturbance estimation has not run yet
        /// </summary>
        NotRunYet=0,
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
        public DisturbanceSetToZeroReason zeroReason;
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
        /// (for debugging) the high-frequency component of the disturbance that is determined by observing changes in process variable y
        /// </summary>
        public double[] d_HF;
        /// <summary>
        /// (for debugging) the low-frequency component of the disturbance that is determined by observing changes in manipulated variable u
        /// </summary>
        public double[] d_LF;
        
        /// <summary>
        /// Constuctor
        /// </summary>
        /// <param name="dataSet"></param>
        public DisturbanceIdResult(UnitDataSet dataSet)
        {
            N = dataSet.GetNumDataPoints();
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
            d_HF = Vec<double>.Fill(0, N);
            d_LF = Vec<double>.Fill(0, N);
            adjustedUnitDataSet = null;

        }


    }

    /// <summary>
    /// An algorithm that attempts to re-create the additive output disturbance acting on 
    /// a signal Y while PID-control attempts to counter-act the disturbance by adjusting its manipulated output u. 
    /// </summary>
    public class DisturbanceIdentifier
    {
        const double numberOfTiConstantsToWaitAfterSetpointChange = 5;

        /// <summary>
        /// Removes the effect of setpoint and (if relevant any non-pid input) changes  from the dataset using the model of pid and unit provided 
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
            if (Vec<double>.IsConstant(unitDataSet.Y_setpoint) ) // and: if all 
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
                // BEGIN "no_dist" process simulation = 
                // a simulation of the process that does not include any real Y_meas or u_pid, thus no effects of 
                // disturbances are visible in this simulation

                var pidModel1 = new PidModel(pidParams, "PID");
                var processSim_noDist = new PlantSimulator(
                    new List<ISimulatableModel> { pidModel1, unitModel });
                processSim_noDist.ConnectModels(unitModel, pidModel1);
                processSim_noDist.ConnectModels(pidModel1, unitModel,pidInputIdx);

                var inputData_noDist = new TimeSeriesDataSet();
                if (unitDataSet.U.GetNColumns()>1)
                {
                    for (int curColIdx = 0; curColIdx < unitDataSet.U.GetNColumns(); curColIdx++)
                    {
                        if (curColIdx == pidInputIdx)
                            continue;
                        inputData_noDist.Add(processSim_noDist.AddExternalSignal(unitModel, SignalType.External_U, curColIdx), 
                            unitDataSet.U.GetColumn(curColIdx));
                    }
                }
                     
                inputData_noDist.Add(processSim_noDist.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), unitDataSet.Y_setpoint);
                inputData_noDist.CreateTimestamps(unitDataSet.GetTimeBase());
                inputData_noDist.SetIndicesToIgnore(unitDataSet.IndicesToIgnore);
                var noDist_isOk = processSim_noDist.Simulate(inputData_noDist, out TimeSeriesDataSet simData_noDist);

                if (noDist_isOk)
                {
                    int idxFirstGoodValue = 0;
                    if (unitDataSet.IndicesToIgnore != null)
                    {
                        if (unitDataSet.GetNumDataPoints() > 0)
                        {
                            while (unitDataSet.IndicesToIgnore.Contains(idxFirstGoodValue) && 
                                idxFirstGoodValue < unitDataSet.GetNumDataPoints()-1)
                            {
                                idxFirstGoodValue++;
                            }
                        }
                    }
                    var vec = new Vec();
       
                    // create a new Y_meas that excludes the influence of any disturabnce using "no_Dist" simulation
                    // this is used to find d_HF
                    var procOutputY = simData_noDist.GetValues(unitModel.GetID(), SignalType.Output_Y);
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


                    // original code, works only for SISO systems
                    /*                    {
                                            var pidOutputU = simData_noDist.GetValues(pidModel1.GetID(), SignalType.PID_U);
                                            var pidDeltaU = vec.Subtract(pidOutputU, pidOutputU[idxFirstGoodValue]);
                                            var newU = vec.Subtract(unitDataSet.U.GetColumn(pidInputIdx), pidDeltaU);
                                            unitDataSet_setpointEffectsAndExternalEffectsRemoved.U = Matrix.ReplaceColumn(unitDataSet_setpointEffectsAndExternalEffectsRemoved.U, pidInputIdx, newU);
                                        }
                    */
                    unitDataSet_adjusted.IndicesToIgnore = unitDataSet.IndicesToIgnore;

                    bool doDebugPlot = false;
                    if (doDebugPlot)
                    {

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
                        inputData_noDist.GetTimeBase(), "distIdent_setpointTest");
                        Shared.DisablePlots();
                    }  
                }
            }
            return unitDataSet_adjusted;

        }



        /// <summary>
        /// Estimates the disturbance time-series over a given unit data set 
        /// given an estimate of the unit model (reference unit model) for a closed loop system.
        /// </summary>
        /// <param name="unitDataSet_raw">the dataset descrbing the unit, over which the disturbance is to be found, datset must specify Y_setpoint,Y_meas and U</param>
        /// <param name="unitModel">the estimate of the unit</param>
        /// <param name="pidInputIdx">the index of the pid-input in the unitModel</param>
        /// <param name="pidParams">the parameters if known of the pid-controller in the closed loop</param>
        /// <returns></returns>
        public static DisturbanceIdResult EstimateDisturbance(UnitDataSet unitDataSet_raw,  
            UnitModel unitModel, int pidInputIdx =0, PidParameters pidParams = null)
        {
            const bool tryToModelDisturbanceIfSetpointChangesInDataset = true;
            var vec = new Vec();

            DisturbanceIdResult result = new DisturbanceIdResult(unitDataSet_raw);
            if (unitDataSet_raw.Y_setpoint == null || unitDataSet_raw.Y_meas == null || unitDataSet_raw.U == null)
            {
                return result;
            }

            bool doesSetpointChange = !(vec.Max(unitDataSet_raw.Y_setpoint, unitDataSet_raw.IndicesToIgnore) 
                == vec.Min(unitDataSet_raw.Y_setpoint, unitDataSet_raw.IndicesToIgnore));
            if (!tryToModelDisturbanceIfSetpointChangesInDataset && doesSetpointChange)
            {
                result.SetToZero();//the default anyway,added for clarity.
                return result;
            }

            // determine if process gains are given, otherwise the algorithm will need to make a rough estimate
            bool isProcessGainSet = false;
            double estPidInputProcessGain = 0;
            if (unitModel != null)
            {
                bool updateEstGain = false;
                if (unitModel.modelParameters.Fitting == null)// a priori model
                {
                    updateEstGain = true;
                }
                else if (unitModel.modelParameters.Fitting.WasAbleToIdentify == true)
                {
                    updateEstGain = true;
                }
                if (updateEstGain == true)
                {
                    var processGains = unitModel.modelParameters.GetProcessGains();
                    if (processGains == null)
                    {
                        return result;
                    }
                    if (!Double.IsNaN(processGains[pidInputIdx]))
                    {
                        estPidInputProcessGain = processGains[pidInputIdx];
                        isProcessGainSet = true;
                    }
                }
            }

            // Instead of using index 0 use the first index that is not "bad".
            int indexOfFirstGoodValue = 0;
            if (unitDataSet_raw.IndicesToIgnore != null)
            {
                if (unitDataSet_raw.GetNumDataPoints() > 0)
                {
                    while (unitDataSet_raw.IndicesToIgnore.Contains(indexOfFirstGoodValue) && indexOfFirstGoodValue < unitDataSet_raw.GetNumDataPoints() - 1)
                    {
                        indexOfFirstGoodValue++;
                    }
                }
            }

            // if process gains are not given, then try to find a rough first estimate by heuristics
            // TODO: consider if this should code should be moved out of this method?
            if (!isProcessGainSet)
            {
                double[] pidInput_u0 = Vec<double>.Fill(unitDataSet_raw.U[pidInputIdx, 0],
                    unitDataSet_raw.GetNumDataPoints());
                double yset0 = unitDataSet_raw.Y_setpoint[0];

                // y0,u0 is at the first data point
                // disadvantage, is that you are not sure that the time series starts at steady state
                // but works better than candiate 2 when disturbance is a step

                double FilterTc_s = 0;
                // initalizaing(rough estimate): this should only be used as an inital guess on the first
                // run when no process model exists!
                if (!isProcessGainSet)
                {
                    LowPass lowPass = new LowPass(unitDataSet_raw.GetTimeBase());
                    double[] e = vec.Subtract(unitDataSet_raw.Y_meas, unitDataSet_raw.Y_setpoint);
                    // knowing the sign of the process gain is quite important!
                    // if a system has negative gain and is given a positive process disturbance, then y and u will both increase in a way that is 
                    // correlated 
                    double pidInput_processGainSign = 1;
                    // look at the correlation between u and y.
                    // assuming that the sign of the Kp in PID controller is set correctly so that the process is not unstable: 
                    // If an increase in _y(by means of a disturbance)_ causes PID-controller to _increase_ u then the processGainSign is negative
                    // If an increase in y causes PID to _decrease_ u, then processGainSign is positive!
                    {
                        var indGreaterThanZeroE = vec.FindValues(e, 0, VectorFindValueType.BiggerOrEqual, unitDataSet_raw.IndicesToIgnore);
                        var indLessThanZeroE = vec.FindValues(e, 0, VectorFindValueType.SmallerOrEqual, unitDataSet_raw.IndicesToIgnore);

                        var u_pid = unitDataSet_raw.U.GetColumn(pidInputIdx);
                        var uAvgWhenEgreatherThanZero = vec.Mean(Vec<double>.GetValuesAtIndices(u_pid, indGreaterThanZeroE));
                        var uAvgWhenElessThanZero = vec.Mean(Vec<double>.GetValuesAtIndices(u_pid, indLessThanZeroE));

                        if (uAvgWhenEgreatherThanZero != null && uAvgWhenElessThanZero != 0)
                        {
                            if (uAvgWhenElessThanZero >= uAvgWhenEgreatherThanZero)
                            {
                                pidInput_processGainSign = 1;
                            }
                            else
                            {
                                pidInput_processGainSign = -1;
                            }
                        }
                    }
                    double[] pidInput_deltaU = vec.Subtract(unitDataSet_raw.U.GetColumn(pidInputIdx), pidInput_u0);//TODO : U including feed-forward?
                    double[] eFiltered = lowPass.Filter(e, FilterTc_s, 2, unitDataSet_raw.IndicesToIgnore);
                    double maxDE = vec.Max(vec.Abs(eFiltered), unitDataSet_raw.IndicesToIgnore);       // this has to be sensitive to noise?
                    double[] uFiltered = lowPass.Filter(pidInput_deltaU, FilterTc_s, 2, unitDataSet_raw.IndicesToIgnore);
                    double maxU = vec.Max(vec.Abs(uFiltered), unitDataSet_raw.IndicesToIgnore);        // sensitive to output noise/controller overshoot
                    double minU = vec.Min(vec.Abs(uFiltered), unitDataSet_raw.IndicesToIgnore);        // sensitive to output noise/controller overshoot  
                    estPidInputProcessGain = pidInput_processGainSign * maxDE / (maxU - minU);
                }
                bool isFittedButFittingFailed = false;
                if (unitModel != null)
                    if (unitModel.GetModelParameters().Fitting != null)
                        if (unitModel.GetModelParameters().Fitting.WasAbleToIdentify == false)
                            isFittedButFittingFailed = true;

                // if no unit model from regression, create on useing a "guesstimated" process gain
                if (unitModel == null || isFittedButFittingFailed)
                {
                    int nGains = unitDataSet_raw.U.GetNColumns();
                    if (nGains == 1)
                    {
                        var unitParamters = new UnitParameters();
                        unitParamters.LinearGains = new double[nGains];
                        unitParamters.LinearGains[pidInputIdx] = estPidInputProcessGain;
                        // TODO: first guess of linear gains and u0 for non-pid inputs if more than one input ??
                        unitParamters.U0 = new double[nGains];
                        unitParamters.U0[pidInputIdx] = pidInput_u0[indexOfFirstGoodValue];
                        unitParamters.UNorm = Vec<double>.Fill(1, nGains);
                        unitParamters.Bias = unitDataSet_raw.Y_meas[indexOfFirstGoodValue];
                        unitModel = new UnitModel(unitParamters);
                    }
                    else
                    {
                        unitModel = UnitIdentifier.IdentifyLinearAndStaticWhileKeepingLinearGainFixed(unitDataSet_raw, pidInputIdx, estPidInputProcessGain,
                            pidInput_u0[indexOfFirstGoodValue], 1);
                    }
                }
            }

            // using the pidParams and unitModel, and if relevant any given y_set and external U, try to subtract the effects of 
            // non-disturbance related changes in the dataset producing "unitDataSet_adjusted"
            var unitDataSet_adjusted = RemoveSetpointAndOtherInputChangeEffectsFromDataSet(unitDataSet_raw, unitModel, pidInputIdx, pidParams);
            unitModel.WarmStart();
           // var sim = new UnitSimulator(unitModel);
            unitDataSet_adjusted.D = null;
            // double[] y_sim = sim.Simulate(ref unitDataSet_adjusted);
            (var isOk, var y_sim) = PlantSimulator.SimulateSingle(unitDataSet_adjusted, unitModel, false, 0, false);

            if (y_sim == null)
            {
                result.zeroReason = DisturbanceSetToZeroReason.UnitSimulatorUnableToRun;
                return result;
            }

            // d_u : (low-pass) back-estimation of disturbances by the effect that they have on u as the pid-controller integrates to 
            // counteract them
            // d_y : (high-pass) disturbances appear for a short while on the output y before they can be counter-acted by the pid-controller 
            // nb!candiateGainD is an estimate for the process gain, and the value chosen in this class 
            // will influence the process model identification afterwards.

            double[] d_LF = vec.Multiply(vec.Subtract(y_sim, y_sim[indexOfFirstGoodValue]), -1);
            double[] d_HF = vec.Subtract(unitDataSet_adjusted.Y_meas, unitDataSet_adjusted.Y_setpoint);
            // d = d_HF+d_LF 
            double[] d_est = vec.Add(d_HF, d_LF);

            bool doDebugPlot = false;

            if (doDebugPlot)
            {
                var variableList = new List<double[]> {
                    unitDataSet_raw.Y_meas,
                   unitDataSet_adjusted.Y_meas/*,
                   unitDataSet_setpointAndExternalUChangeEffectsRemoved.Y_setpoint,
                   y_sim,
                   d_LF,
                   d_HF,
                   d_est,
                   unitDataSet_setpointAndExternalUChangeEffectsRemoved.U.GetColumn(pidInputIdx),*/
                };
                var variableNameList = new List<string> { "y1=y_meas", "y1=y_meas(extUrem)"/*, "y1=y_set", "y1=y_sim", "y3=d_LF", "y3=d_HF", "y3=d_est", "y2=u_pid"*/ };

                if (unitDataSet_adjusted.U.GetNColumns() == 2)
                {
                    var nonPidIdx = 0;
                    if (pidInputIdx == 0)
                        nonPidIdx = 1;
                    variableList.Add(unitDataSet_adjusted.U.GetColumn(nonPidIdx));
                    variableNameList.Add("y2=u_nonpid");
                }
                Shared.EnablePlots();
                Plot.FromList(
                    variableList, variableNameList, unitDataSet_adjusted.GetTimeBase(), "distIdent_dLF_est");
                Shared.DisablePlots();
            }
            //

            // copy result to result class
            result.estPidProcessGain = estPidInputProcessGain;
            result.d_est            = d_est;
            result.d_LF             = d_LF;
            result.d_HF             = d_HF;
            result.adjustedUnitDataSet = unitDataSet_adjusted;
            return result;
        }

    }
}
