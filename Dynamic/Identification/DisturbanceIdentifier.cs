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
        SetpointWasDetected=1
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

        public double[] d_est;
        public double f1EstProcessGain;
        public double[] d_HF, d_u;
        
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
            f1EstProcessGain = 0;
            isAllZero = true;
            d_HF = Vec<double>.Fill(0, N);
            d_u = Vec<double>.Fill(0, N);

        }

        public DisturbanceIdResult Copy()
        {
            DisturbanceIdResult returnCopy = new DisturbanceIdResult(N);

            returnCopy.d_HF = d_HF;
            returnCopy.d_u = d_u;
            returnCopy.d_est = d_est;
            returnCopy.f1EstProcessGain = f1EstProcessGain;

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
        // TODO: issue
        // would ideally like tryToModelDisturbanceIfSetpointChangesInDataset = true to be set 
        //  BUT: 
        // unit tests where there is a step in yset fail if  tryToModelDisturbanceIfSetpointChangesInDataset=true
        // but unit tests 
        //  AND:
        // unit tests whene there is both a yset step and a disturbance step fail and sinus disturbances also fail. 


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
        /// Estimates the disturbance time-series over a given unit data set 
        /// given an estimate of the unit model (reference unit model) for a closed loop system.
        /// </summary>
        /// <param name="unitDataSet">the dataset descrbing the unit, over which the disturbance is to be found, datset must specify Y_setpoint,Y_meas and U</param>
        /// <param name="unitModel">the estimate of the unit</param>
        /// <returns></returns>
        public static DisturbanceIdResult EstimateDisturbance(UnitDataSet unitDataSet,  
            UnitModel unitModel, int inputIdx =0, PidParameters pidParams = null)
        {
            const bool tryToModelDisturbanceIfSetpointChangesInDataset = true;
            var vec = new Vec();


            DisturbanceIdResult result = new DisturbanceIdResult(unitDataSet);
            if (unitDataSet.Y_setpoint == null || unitDataSet.Y_meas == null || unitDataSet.U == null)
            {
                return result;
            }
            double[] e_minusSetpointTransients;// part of disturbance as oberved by deviations (ymeas-yset)
            double estProcessGain = 0;
            bool doesSetpointChange = !(vec.Max(unitDataSet.Y_setpoint) == vec.Min(unitDataSet.Y_setpoint));
            if (!tryToModelDisturbanceIfSetpointChangesInDataset && doesSetpointChange)
            {
                result.SetToZero();//the default anyway,added for clarity.
                return result;
            }
            e_minusSetpointTransients = vec.Subtract(unitDataSet.Y_meas, unitDataSet.Y_setpoint);// this under-estimates disturbance because the controller has rejected some of it.
            // subtract setpoint transients from e_minusSetpointTransients
            if (doesSetpointChange && unitModel!= null && pidParams != null)
            {
                double[] e_setpointTransients = null;
                var pidModel1 = new PidModel(pidParams,"PID");
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
                    e_setpointTransients = vec.Subtract(simData.GetValues(unitModel.GetID(),SignalType.Output_Y), unitDataSet.Y_setpoint);
                    e_minusSetpointTransients = vec.Subtract(e_minusSetpointTransients, e_setpointTransients);
                }
            }
            
            
            // d_u : (low-pass) back-estimation of disturbances by the effect that they have on u as the pid-controller integrates to 
            // counteract them
            // d_y : (high-pass) disturbances appear for a short while on the output y before they can be counter-acted by the pid-controller 
            // nb!candiateGainD is an estimate for the process gain, and the value chosen in this class 
            // will influence the process model identification afterwards.
            double[] deltaU  = null;
            //
            // knowing the sign of the process gain is quite important!
            // if a system has negative gain and is given a positive process disturbance, then y and u will both increase in a way that is 
            // correlated 
            double processGainSign = 1;
            // look at the correlation between u and y.
            // assuming that the sign of the Kp in PID controller is set correctly so that the process is not unstable: 
            // If an increase in _y(by means of a disturbance)_ causes PID-controller to _increase_ u then the processGainSign is negative
            // If an increase in y causes PID to _decrease_ u, then processGainSign is positive!
            var indGreaterThanZeroE = vec.FindValues(e_minusSetpointTransients,0,VectorFindValueType.BiggerOrEqual);
            var indLessThanZeroE = vec.FindValues(e_minusSetpointTransients, 0, VectorFindValueType.SmallerOrEqual);
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

            // just use first value as "u0", just perturbing this value 
            // a little will cause unit test to fail ie.algorithm is sensitive to its choice.
            double[] u0 = Vec<double>.Fill(unitDataSet.U[inputIdx,0], unitDataSet.GetNumDataPoints());//NB! algorithm is sensitive to choice of u0!!!
            double yset0 = unitDataSet.Y_setpoint[0];
            deltaU          = vec.Subtract(unitDataSet.U.GetColumn(inputIdx), u0);//TODO : U including feed-forward?

            // y0,u0 is at the first data point
            // disadvantage, is that you are not sure that the time series starts at steady state
            // but works better than candiate 2 when disturbance is a step
            bool candidateGainSet = false;
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
                        candidateGainSet = true;
                    }
                }
            }
            LowPass lowPass = new LowPass(unitDataSet.GetTimeBase());
            double FilterTc_s = 0;
            // initalizaing(rough estimate): this should only be used as an inital guess on the first
            // run when no process model exists!
            if (!candidateGainSet)
            {
                double[] eFiltered = lowPass.Filter(e_minusSetpointTransients, FilterTc_s, 2);
                double maxDE = vec.Max(vec.Abs(eFiltered));       // this has to be sensitive to noise?
                double[] uFiltered = lowPass.Filter(deltaU, FilterTc_s, 2);
                double maxU = vec.Max(vec.Abs(uFiltered));        // sensitive to output noise/controller overshoot
                double minU = vec.Min(vec.Abs(uFiltered));        // sensitive to output noise/controller overshoot  
                estProcessGain = processGainSign * maxDE / (maxU - minU);
            }

            //
            // default version of disturbance estimate that looks at deviation from yset
            // and uses no reference model
            // 
            // too simple, only works for static processes when setpoint does not change
            // for dynamic processes "e" will include process transients that should be 
            // subtracted.
            double[]  d_HF = e_minusSetpointTransients;

            bool isFittedButFittingFailed = false;
            if (unitModel != null)
                 if (unitModel.GetModelParameters().Fitting != null)
                     if (unitModel.GetModelParameters().Fitting.WasAbleToIdentify == false)
                         isFittedButFittingFailed = true;
            double[] d_LF;
            if (unitModel == null|| isFittedButFittingFailed)
            {
                double[] deltaU_lp = lowPass.Filter(deltaU, FilterTc_s, 2);
                double[] deltaYset = vec.Subtract(unitDataSet.Y_setpoint, yset0);
                double[] du_contributionFromU = deltaU_lp;

                double[] du_contributionFromYset = vec.Multiply(deltaYset, 1 / -estProcessGain);// will be zero if yset is constant.
                double[] d_LF_internal = vec.Add(du_contributionFromU, du_contributionFromYset);
                d_LF = vec.Multiply(d_LF_internal, -estProcessGain);
            }
            else
            {
                unitModel.WarmStart();
                var sim = new UnitSimulator(unitModel);
                unitDataSet.D = null;
                double[] y_sim = sim.Simulate(ref unitDataSet);

                d_LF = vec.Multiply(vec.Subtract(y_sim,y_sim[0]),-1);
            }

            // d = d_HF+d_LF 
            double[] d_est = vec.Add(d_HF, d_LF);       

            // copy result to result class
            result.f1EstProcessGain     = estProcessGain;
            result.d_est              = d_est;
            result.d_u = d_LF;
            result.d_HF = d_HF;
            // NB! minus!
          //  double[] dest_test = vec.Add(vec.Multiply(result.dest_f2_proptoGain,-result.f1EstProcessGain),result.dest_f2_constTerm);
            return result;
        }

    }
}
