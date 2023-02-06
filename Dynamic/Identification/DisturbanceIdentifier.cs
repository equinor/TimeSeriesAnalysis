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
    //    public bool useFormulation1 = false;// false means use formulation 2
        public bool isAllZero = true;
        public DisturbanceSetToZeroReason zeroReason;

        //formulation 1:
        public double[] d_est;
        public double f1EstProcessGain;
        public double[] d_HF, d_u;

        // formulation 2: divide disturbance into term that is proporational to process gain and constant term  
    //    public double[] dest_f2_proptoGain;
     //   public double[] dest_f2_constTerm;

        
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
      //      dest_f2_proptoGain = Vec<double>.Fill(0, N);
      //      dest_f2_constTerm = Vec<double>.Fill(0, N);

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
        /*
        public double GetMagnitude()
        {
            Vec vec = new Vec();
            if (useFormulation1)
                return vec.Max(d_est) - vec.Min(d_est);
            else // not sure about this one
                return vec.Max(dest_f2_proptoGain)- vec.Min(dest_f2_proptoGain);
        }*/
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
        /// given an estimate of the unit model (reference unit model)
        /// </summary>
        /// <param name="unitDataSet">the dataset descrbing the unit, over which the disturbance is to be found</param>
        /// <param name="unitModel">the estimate of the unit</param>
        /// <returns></returns>
        public static DisturbanceIdResult EstimateDisturbance(UnitDataSet unitDataSet,  
            UnitModel unitModel, int inputIdx =0)
        {
            bool tryToModelDisturbanceIfSetpointChangesInDataset = false;
            var vec = new Vec();

            DisturbanceIdResult result = new DisturbanceIdResult(unitDataSet);

            double[] e;// part of disturbance as oberved by deviations (ymeas-yset)

            double estProcessGain = 0;

            bool doesSetpointChange = !(vec.Max(unitDataSet.Y_setpoint) == vec.Min(unitDataSet.Y_setpoint));
            if (!tryToModelDisturbanceIfSetpointChangesInDataset && doesSetpointChange)
            {
                result.SetToZero();//the default anyway,added for clarity.
                return result;
            }
            e = vec.Subtract(unitDataSet.Y_meas, unitDataSet.Y_setpoint);// this under-estimates disturbance because the controller has rejected some of it.

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

           // bool isSetpointConstant = true;

          //  if (isSetpointConstant)
            {
                // look at the correlation between u and y.
                // assuming that the sign of the Kp in PID controller is set correctly so that the process is not unstable: 
                // If an increase in _y(by means of a disturbance)_ causes PID-controller to _increase_ u then the processGainSign is negative
                // If an increase in y causes PID to _decrease_ u, then processGainSign is positive!
                var indGreaterThanZeroE = vec.FindValues(e,0,VectorFindValueType.BiggerOrEqual);
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

            // v1: just use first value as "u0", just perturbing this value 
            // a little will cause unit test to fail ie.algorithm is sensitive to its choice.
            double[] u0 = Vec<double>.Fill(unitDataSet.U[inputIdx,0], unitDataSet.GetNumDataPoints());//NB! algorithm is sensitive to choice of u0!!!

            // v2: try to "highpass" u
          /*  if (candidateGainVersion == 3)
            {
                LowPass lpFilt = new LowPass(tuningDataSet.timeBase_s);
                double[] uLPfilt = lpFilt.Filter(tuningDataSet.GetUMinusFF(), pidid.Tiest_val1.Value * 6);
                double[] uHPfilt = vec.Sub(tuningDataSet.GetUMinusFF(), uLPfilt);
                u0 = Vec.Add(uHPfilt, tuningDataSet.GetUMinusFF()[0]);
                double y0 = tuningDataSet.ymeas[0];
            }*/
            double yset0 = unitDataSet.Y_setpoint[0];
            deltaU          = vec.Subtract(unitDataSet.U.GetColumn(inputIdx), u0);//TODO : U including feed-forward?
            LowPass lowPass = new LowPass(unitDataSet.GetTimeBase());
            // filter is experimental, consider removing if cannot be made to work.
            double FilterTc_s = 0;// tuningDataSet.timeBase_s;
       /*     if (unitModel != null)
            {
                FilterTc_s = unitModel.modelParameters.TimeConstant_s;
            } 
         */   

            /*double[] dest_f2_proptoGain = du_contributionFromU;
            double[] dest_f2_constTerm = vec.Add(deltaYset, e);
            */
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
                    if (!Double.IsNaN(processGains[inputIdx]))
                    {
                        estProcessGain = processGains[inputIdx];
                        candidateGainSet = true;
                    }
                }
            }
            // initalizaing(rough estimate): this should only be used as an inital guess on the first
            // run when no process model exists!
            if (!candidateGainSet)
            {
                double[] eFiltered = lowPass.Filter(e, FilterTc_s, 2);
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
            double[]  d_HF = e;

            bool isFittedButFittingFailed = false;
            if (unitModel != null)
                 if (unitModel.GetModelParameters().Fitting != null)
                     if (unitModel.GetModelParameters().Fitting.WasAbleToIdentify == false)
                         isFittedButFittingFailed = true;

            double[] d_u;
            if (unitModel == null|| isFittedButFittingFailed)
            {
                double[] deltaU_lp = lowPass.Filter(deltaU, FilterTc_s, 2);
                double[] deltaYset = vec.Subtract(unitDataSet.Y_setpoint, yset0);
                double[] du_contributionFromU = deltaU_lp;

                double[] du_contributionFromYset = vec.Multiply(deltaYset, 1 / -estProcessGain);// will be zero if yset is constant.
                double[] d_LF_internal = vec.Add(du_contributionFromU, du_contributionFromYset);
                d_u = vec.Multiply(d_LF_internal, -estProcessGain);
            }
            else
            {
                unitModel.WarmStart();
                var sim = new UnitSimulator(unitModel);
                unitDataSet.D = null;
                double[] y_sim = sim.Simulate(ref unitDataSet);

                d_u = vec.Multiply(vec.Subtract(y_sim,y_sim[0]),-1);
            }

            double[] d_est = vec.Add(d_HF, d_u);       // d = d_LF+d_HF 
            
            // currently the below code breaks disturbance estimtes in those cases
            // where setpoint is constant - wonder if it is possible to unify these two appraoches???
            // very bad form to have lots of "if" statements in this estiamtor.

     /*       if (referenceUnitModel != null && doesSetpointChange)
            {
                // if a process model is available, try to subtract the process transients
                // from e when estimating d_HF.
                if (unitDataSet.Y_sim != null)
                {
                    d_HF = null;
                    d_LF = null;
                    // be careful! the inital value of modelledY is often NaN.
                    double[] e_mod = vec.Subtract(unitDataSet.Y_sim, unitDataSet.Y_meas);
                    double filterTc_s = 200;//whaatt?
                    LowPass lp = new LowPass(unitDataSet.GetTimeBase());
                    double[] e_mod_LP = lp.Filter(e_mod, filterTc_s, 1);
                    double[] e_mod_HF = vec.Subtract(e_mod, e_mod_LP);
                    // because of time-delay e_mod is generally shorter than e if timeDelay_samples>0
                    double[] e_mod_padding = Vec<double>.Fill(0, e.Length - e_mod.Length);
                    double[] e_mod_padded = Vec<double>.Concat(e_mod_padding, e_mod_HF);

                    // d_HF 
                    //e = Vec.Sub(tuningDataSet.ymeas, referenceProcessId.modelledY);
                    //d_HF = Vec.Sub(e, e_mod_padded);
                    //List<int> nanIndices = Vec.FindValues(d_HF,Double.NaN, FindValues.NaN);
                    //for (int i = 0; i < nanIndices.Count; i++)
                    //{
                    //    d_HF[nanIndices[i]] = e[i];
                   // }
                    
                    d_HF = Vec<double>.Fill(0, unitDataSet.Y_meas.Length);

                    //d_LF
                    //   double[] du_contributionFromYset = Vec.Mult(e_mod, 1 / -candidateGainD);// will be zero if yset is constant.
                    //double[] d_LF_internal = Vec.Add(du_contributionFromU, du_contributionFromYset);
                    // d_LF = Vec.Mult(d_LF_internal, -candidateGainD);
                    d_LF = vec.Subtract(unitDataSet.Y_meas, unitDataSet.Y_sim);
                    d_LF[0] = d_LF[1];//first modelledY is NaN
                    d_est = d_LF;
                }
            }*/

        //    d_est       = FreezeDisturbanceAfterSetpointChange(d_est, tuningDataSet, pidid);//TODO:do for formulation 2 as well!

            // copy result to result class
            result.f1EstProcessGain     = estProcessGain;
            result.d_est              = d_est;
          //  result.dest_f2_proptoGain   = dest_f2_proptoGain;
           // result.dest_f2_constTerm    = dest_f2_constTerm;
            result.d_u = d_u;
            result.d_HF = d_HF;

            // NB! minus!
          //  double[] dest_test = vec.Add(vec.Multiply(result.dest_f2_proptoGain,-result.f1EstProcessGain),result.dest_f2_constTerm);

            return result;

        }
        /*
        static double[] FreezeDisturbanceAfterSetpointChange(double[] d_in,PIDDataSet tuningDataSet, PIDidResults pidid )
        {
            bool doesSetpointChange = !(Vec.Max(tuningDataSet.yset) == Vec.Min(tuningDataSet.yset));

            // if there is setpoint changes, set disturbance to zero in this "step response" time

            int numberOfSamplesToWaitAfterSetpointChange = (int)Math.Floor(pidid.Tiest_val1.Value /
                tuningDataSet.timeBase_s * numberOfTiConstantsToWaitAfterSetpointChange);// NB!
            if (doesSetpointChange)
            {
                int idxLastSetpointChange = 0;
                for (int i = 1; i < tuningDataSet.yset.Length; i++)
                {
                    if (tuningDataSet.yset[i] != tuningDataSet.yset[i - 1])
                    {
                        idxLastSetpointChange = i;
                    }

                    if (i - idxLastSetpointChange < numberOfSamplesToWaitAfterSetpointChange)
                    {
                        d_in [i] = d_in[i - 1];
                    }
                }
            }
            return d_in;
        }
        */





    }
}
