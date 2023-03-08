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
    public enum DisturbanceSetToZeroReason
    { 
        NotRunYet=0,
        SetpointWasDetected=1,
        UnitSimulatorUnableToRun =2,
    }


    // note that in the real-world the disturbance is not a completely steady disturbance
    // it can have phase-shift and can look different than a normal

    /// <summary>
    /// Internal class to store a single sub-run of the DisturnanceIdentifierInternal
    /// 
    /// </summary>
    public class DisturbanceIdResult
    {

        public int N = 0;
        public bool isAllZero = true;
        public DisturbanceSetToZeroReason zeroReason;
        public UnitDataSet adjustedUnitDataSet;


        public double[] d_est;
        public double estProcessGain;
        public double[] d_HF, d_LF;
        
        public DisturbanceIdResult(UnitDataSet dataSet)
        {
            N = dataSet.GetNumDataPoints();
            SetToZero();
        }

        public DisturbanceIdResult(int N)
        {
            this.N = N;
            SetToZero();
        }

        public void SetToZero()
        {
            d_est = Vec<double>.Fill(0, N);
            estProcessGain = 0;
            isAllZero = true;
            d_HF = Vec<double>.Fill(0, N);
            d_LF = Vec<double>.Fill(0, N);
            adjustedUnitDataSet = null;

        }

        public DisturbanceIdResult Copy()
        {
            DisturbanceIdResult returnCopy = new DisturbanceIdResult(N);

            returnCopy.d_HF = d_HF;
            returnCopy.d_LF = d_LF;
            returnCopy.d_est = d_est;
            returnCopy.estProcessGain = estProcessGain;
            returnCopy.adjustedUnitDataSet = adjustedUnitDataSet;

            return returnCopy;
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
        /// Only uses Y_meas and U in unitDataSet, i.e. does not consider feedback 
        /// </summary>
        /// <param name="unitDataSet"></param>
        /// <param name="unitModel"></param>
        /// <param name="inputIdx"></param>
        /// <returns></returns>
        public static DisturbanceIdResult EstDisturbanceBasedOnProcessModel(UnitDataSet unitDataSet,
            UnitModel unitModel, int inputIdx = 0)
        {
            unitModel.WarmStart();
            var sim = new UnitSimulator(unitModel);
            unitDataSet.D = null;
            double[] y_sim = sim.Simulate(ref unitDataSet);

            DisturbanceIdResult result = new DisturbanceIdResult(unitDataSet);
            result.d_est = (new Vec()).Subtract(unitDataSet.Y_meas, y_sim);

            return result;
        }


        /// <summary>
        /// Removes the effect of setpoint changes from the dataset using the model of pid and unit provided 
        /// </summary>
        /// <param name="unitDataSet"></param>
        /// <param name="unitModel"></param>
        /// <param name="inputIdx"></param>
        /// <param name="pidParams"></param>
        /// <returns> a scrubbed copy of unitDataSet</returns>
        private static UnitDataSet RemoveSetpointEffectFromDataSet(UnitDataSet unitDataSet,
             UnitModel unitModel, int inputIdx = 0, PidParameters pidParams = null)
        {
            if (Vec<double>.IsConstant(unitDataSet.Y_setpoint))
            {
                return unitDataSet;
            }

            var unitDataSet_setpointEffectsRemoved = new UnitDataSet(unitDataSet);
            if (unitModel != null && pidParams != null)
            {
                var pidModel1 = new PidModel(pidParams, "PID");
                var processSim = new PlantSimulator(
                    new List<ISimulatableModel> { pidModel1, unitModel });
                processSim.ConnectModels(unitModel, pidModel1);
                processSim.ConnectModels(pidModel1, unitModel);
                var inputData = new TimeSeriesDataSet();
                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), unitDataSet.Y_setpoint);
                inputData.CreateTimestamps(unitDataSet.GetTimeBase());
                var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);

                if (isOk)
                {
                    var vec = new Vec();
                    var procOutputY = simData.GetValues(unitModel.GetID(), SignalType.Output_Y);
                    var pidOutputU = simData.GetValues(pidModel1.GetID(), SignalType.PID_U);
                    var pidDeltaU = vec.Subtract(pidOutputU, pidOutputU.First());
                    var deltaProcOutputY = vec.Subtract(procOutputY, procOutputY.First());
                    var newU = vec.Subtract(unitDataSet.U.GetColumn(inputIdx), pidDeltaU);

                    unitDataSet_setpointEffectsRemoved.Y_meas = vec.Subtract(unitDataSet.Y_meas, deltaProcOutputY);
                    unitDataSet_setpointEffectsRemoved.Y_setpoint = Vec<double>.Fill(unitDataSet.Y_setpoint.First(), unitDataSet.Y_setpoint.Length);
                    unitDataSet_setpointEffectsRemoved.U = Matrix.ReplaceColumn(unitDataSet_setpointEffectsRemoved.U, inputIdx, newU);

  /*                  Shared.EnablePlots();
                    Plot.FromList(
                    new List<double[]> {
                          unitDataSet_setpointEffectsRemoved.Y_meas,
                          unitDataSet.Y_meas,
                          unitDataSet_setpointEffectsRemoved.Y_setpoint,
                          unitDataSet.Y_setpoint,
                          unitDataSet_setpointEffectsRemoved.U.GetColumn(inputIdx),
                          unitDataSet.U.GetColumn(inputIdx)
                    },
                    new List<string> { "y1=y_meas(new)", "y1=y_meas(old)", "y1=y_set(new)", "y1=y_set(old)", "y3=u(new)", "y3=u(old)" },
                    inputData.GetTimeBase(), "distIdent_setpointTest");
                    Shared.DisablePlots();
  */
                }
            }
            return unitDataSet_setpointEffectsRemoved;

        }



        /// <summary>
        /// Estimates the disturbance time-series over a given unit data set 
        /// given an estimate of the unit model (reference unit model) for a closed loop system.
        /// </summary>
        /// <param name="unitDataSet">the dataset descrbing the unit, over which the disturbance is to be found, datset must specify Y_setpoint,Y_meas and U</param>
        /// <param name="unitModel">the estimate of the unit</param>
        /// <returns></returns>
        public static DisturbanceIdResult EstimateDisturbance(UnitDataSet unitDataSet_raw,  
            UnitModel unitModel, int inputIdx =0, PidParameters pidParams = null)
        {
            const bool tryToModelDisturbanceIfSetpointChangesInDataset = true;
            var vec = new Vec();

            DisturbanceIdResult result = new DisturbanceIdResult(unitDataSet_raw);
            if (unitDataSet_raw.Y_setpoint == null || unitDataSet_raw.Y_meas == null || unitDataSet_raw.U == null)
            {
                return result;
            }

            bool doesSetpointChange = !(vec.Max(unitDataSet_raw.Y_setpoint) == vec.Min(unitDataSet_raw.Y_setpoint));
            if (!tryToModelDisturbanceIfSetpointChangesInDataset && doesSetpointChange)
            {
                result.SetToZero();//the default anyway,added for clarity.
                return result;
            }
            //
            // if a both a pidmodel and a unitmodel is provided, the effects of any setpoint changes on the dataset
            // are attempted "scrubbed" from unitdataset before attempting to estimate the disturbane
            // NOTE: that if unitMOdel == null like it is on the first iteration, this will do nothing!!!!
            var unitDataSet = RemoveSetpointEffectFromDataSet(unitDataSet_raw,unitModel, inputIdx, pidParams);

            double[] e = vec.Subtract(unitDataSet.Y_meas, unitDataSet.Y_setpoint);
            double[] u0 = Vec<double>.Fill(unitDataSet.U[inputIdx, 0], unitDataSet.GetNumDataPoints());//NB! algorithm is sensitive to choice of u0!!!
            double[]  deltaU = vec.Subtract(unitDataSet.U.GetColumn(inputIdx), u0);//TODO : U including feed-forward?
            
            // d_u : (low-pass) back-estimation of disturbances by the effect that they have on u as the pid-controller integrates to 
            // counteract them
            // d_y : (high-pass) disturbances appear for a short while on the output y before they can be counter-acted by the pid-controller 
            // nb!candiateGainD is an estimate for the process gain, and the value chosen in this class 
            // will influence the process model identification afterwards.
            //
            // knowing the sign of the process gain is quite important!
            // if a system has negative gain and is given a positive process disturbance, then y and u will both increase in a way that is 
            // correlated 
            double processGainSign = 1;
            // look at the correlation between u and y.
            // assuming that the sign of the Kp in PID controller is set correctly so that the process is not unstable: 
            // If an increase in _y(by means of a disturbance)_ causes PID-controller to _increase_ u then the processGainSign is negative
            // If an increase in y causes PID to _decrease_ u, then processGainSign is positive!
            {
                var indGreaterThanZeroE = vec.FindValues(e, 0, VectorFindValueType.BiggerOrEqual);
                var indLessThanZeroE = vec.FindValues(e, 0, VectorFindValueType.SmallerOrEqual);
                var u = unitDataSet.U.GetColumn(0);
                var uAvgWhenEgreatherThanZero = vec.Mean(Vec<double>.GetValuesAtIndices(u, indGreaterThanZeroE));
                var uAvgWhenElessThanZero = vec.Mean(Vec<double>.GetValuesAtIndices(u, indLessThanZeroE));

                if (uAvgWhenEgreatherThanZero != null && uAvgWhenElessThanZero != 0)
                {
                    if (uAvgWhenElessThanZero >= uAvgWhenEgreatherThanZero)
                    {
                        processGainSign = 1;
                    }
                    else
                    {
                        processGainSign = -1;
                    }
                }
            }
            // just use first value as "u0", just perturbing this value 
            // a little will cause unit test to fail ie.algorithm is sensitive to its choice.
            double yset0 = unitDataSet.Y_setpoint[0];

            // y0,u0 is at the first data point
            // disadvantage, is that you are not sure that the time series starts at steady state
            // but works better than candiate 2 when disturbance is a step
            bool isProcessGainSet = false;
            double estProcessGain = 0;
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
                    if (!Double.IsNaN(processGains[inputIdx]))
                    {
                        estProcessGain = processGains[inputIdx];
                        isProcessGainSet = true;
                    }
                }
            }
            LowPass lowPass = new LowPass(unitDataSet.GetTimeBase());
            double FilterTc_s = 0;
            // initalizaing(rough estimate): this should only be used as an inital guess on the first
            // run when no process model exists!
            if (!isProcessGainSet)
            {
                double[] eFiltered = lowPass.Filter(e, FilterTc_s, 2);
                double maxDE = vec.Max(vec.Abs(eFiltered));       // this has to be sensitive to noise?
                double[] uFiltered = lowPass.Filter(deltaU, FilterTc_s, 2);
                double maxU = vec.Max(vec.Abs(uFiltered));        // sensitive to output noise/controller overshoot
                double minU = vec.Min(vec.Abs(uFiltered));        // sensitive to output noise/controller overshoot  
                estProcessGain = processGainSign * maxDE / (maxU - minU);

                // temporary:(setting correct process gain should give the corred disturbance out!)
                 //estProcessGain = 1.2;
            }

            bool isFittedButFittingFailed = false;
            if (unitModel != null)
                 if (unitModel.GetModelParameters().Fitting != null)
                     if (unitModel.GetModelParameters().Fitting.WasAbleToIdentify == false)
                         isFittedButFittingFailed = true;

            // if no unit model from regression, create on useing a "guesstimated" process gain
            if (unitModel == null|| isFittedButFittingFailed)
            {
                var unitParamters = new UnitParameters();
                unitParamters.LinearGains = new double[] { estProcessGain };
                unitParamters.U0 = new double[] { u0[0] };
                unitParamters.UNorm = new double[] { 1 };
                unitParamters.Bias = unitDataSet.Y_meas[0];
                unitModel = new UnitModel(unitParamters) ;
            }

            var unitDataSet_setpointRemoved = RemoveSetpointEffectFromDataSet(unitDataSet, unitModel, inputIdx, pidParams);
            unitModel.WarmStart();
            var sim = new UnitSimulator(unitModel);
            unitDataSet_setpointRemoved.D = null;
            double[] y_sim = sim.Simulate(ref unitDataSet_setpointRemoved);
            if (y_sim == null)
            {
                result.zeroReason = DisturbanceSetToZeroReason.UnitSimulatorUnableToRun;
                return result;
            }
            double[] d_LF = vec.Multiply(vec.Subtract(y_sim, y_sim[0]), -1);
            double[] d_HF = vec.Subtract(unitDataSet_setpointRemoved.Y_meas, unitDataSet_setpointRemoved.Y_setpoint);
            // d = d_HF+d_LF 
            double[] d_est = vec.Add(d_HF, d_LF);

           /*    Shared.EnablePlots();
               Plot.FromList(
               new List<double[]> {
                   unitDataSet_setpointRemoved.Y_meas,
                   unitDataSet_setpointRemoved.Y_setpoint,
                   y_sim,
                   d_LF,
                   d_HF,
                   d_est,
                   unitDataSet_setpointRemoved.U.GetColumn(inputIdx),
               },
               new List<string> { "y1=y_meas", "y1=y_set", "y1=y_sim", "y2=d_LF", "y2=d_HF", "y2=d_est", "y3=u" },
               unitDataSet.GetTimeBase(), "distIdent_dLF_est");
               Shared.DisablePlots();
           */

            // copy result to result class
            result.estProcessGain = estProcessGain;
            result.d_est            = d_est;
            result.d_LF             = d_LF;
            result.d_HF             = d_HF;
            result.adjustedUnitDataSet = unitDataSet;
            // NB! minus!
            //  double[] dest_test = vec.Add(vec.Multiply(result.dest_f2_proptoGain,-result.f1EstProcessGain),result.dest_f2_constTerm);
            return result;
        }

    }
}
