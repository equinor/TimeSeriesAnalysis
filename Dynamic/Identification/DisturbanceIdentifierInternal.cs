using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;

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
    internal class DisturbanceIdResult
    {

        public int N = 0;
        public bool useFormulation1 = false;// false means use formulation 2
        public bool isAllZero = true;
        public DisturbanceSetToZeroReason zeroReason;

        //formulation 1:
        public double[] dest_f1;
        public double f1EstProcessGain;
        public double[] d_HF, d_LF;

        // formulation 2: divide disturbance into term that is proporational to process gain and constant term  
        public double[] dest_f2_proptoGain;
        public double[] dest_f2_constTerm;

        
        public DisturbanceIdResult(UnitDataSet dataSet)
        {
            N = dataSet.GetUMinusFF().Count();
            SetToZero();
        }

        public DisturbanceIdResult(int N)
        {
            this.N = N;
            SetToZero();
        }


        public void SetToZero()
        {
            dest_f1 = Vec<double>.Fill(0, N);
            f1EstProcessGain = 0;
            dest_f2_proptoGain = Vec<double>.Fill(0, N);
            dest_f2_constTerm = Vec<double>.Fill(0, N);

            isAllZero = true;
            d_HF = Vec<double>.Fill(0, N);
            d_LF = Vec<double>.Fill(0, N);

        }

        public DisturbanceIdResult Copy()
        {
            DisturbanceIdResult returnCopy = new DisturbanceIdResult(N);

            returnCopy.d_HF = d_HF;
            returnCopy.d_LF = d_LF;
            returnCopy.dest_f1 = dest_f1;
            returnCopy.f1EstProcessGain = f1EstProcessGain;

            return returnCopy;
        }

        public double GetMagnitude()
        {
            Vec vec = new Vec();
            if (useFormulation1)
                return vec.Max(dest_f1) - vec.Min(dest_f1);
            else // not sure about this one
                return vec.Max(dest_f2_proptoGain)- vec.Min(dest_f2_proptoGain);
        }
    }

    /// <summary>
    /// An algorithm that attempts to re-create the additive output disturbance acting on 
    /// a signal Y while PID-control attempts to counter-act the disturbance by adjusting its manipulated output u. 
    /// </summary>
    internal class DisturbanceIdentifierInternal
    {
        const double numberOfTiConstantsToWaitAfterSetpointChange = 5;
        // TODO: issue
        // would ideally like tryToModelDisturbanceIfSetpointChangesInDataset = true to be set 
        //  BUT: 
        // unit tests where there is a step in yset fail if  tryToModelDisturbanceIfSetpointChangesInDataset=true
        // but unit tests 
        //  AND:
        // unit tests whene there is both a yset step and a disturbance step fail and sinus disturbances also fail. 

        public static DisturbanceIdResult EstimateDisturbance(PIDDataSet tuningDataSet, PIDidResults pidid, 
            ProcessIdResults referenceProcessId, bool tryToModelDisturbanceIfSetpointChangesInDataset = false)
        {
            DisturbanceIdResult result = new DisturbanceIdResult(tuningDataSet);

            double[] e;// part of disturbance as oberved by deviations (ymeas-yset)
       //     double[] d_LF;// part of disturbance as observed by different u being required at get same ymeas at steady-state

            double candidateGainD = 0;

            bool doesSetpointChange = !(Vec.Max(tuningDataSet.yset) == Vec.Min(tuningDataSet.yset));
            if (!tryToModelDisturbanceIfSetpointChangesInDataset && doesSetpointChange)
            {
                result.SetToZero();//the default anyway,added for clarity.
                return result;
            }
            Vec vec = new Vec();

            e = vec.Subtract(tuningDataSet.ymeas, tuningDataSet.yset);// this under-estimates disturbance because the controller has rejected some of it.


            // d_u : (low-pass) back-estimation of disturbances by the effect that they have on u as the pid-controller integrates to 
            // counteract them
            // d_y : (high-pass) disturbances appear for a short while on the output y before they can be counter-acted by the pid-controller 

            // nb!candiateGainD is an estimate for the process gain, and the value chosen in this class 
            // will influence the process model identification afterwards.

            double[] deltaU  = null;
            double[] deltaE = null;
            // the way that the pid-controller is implemented pid Kp and process K should be same sign.
            // pid Kp is known when running this algo
            double processGainSign = 1;
            if (pidid.Kpest_val1.Value<0)
            {
                processGainSign = -1;
            }
   
            const int candidateGainVersion = 1;

            // v1: just use first value as "u0", just perturbing this value 
            // a little will cause unit test to fail ie.algorithm is sensitive to its choice.
            double[] u0 = Vec<double>.Fill(tuningDataSet.GetUinclFF()[0], tuningDataSet.GetLength());//NB! algorithm is sensitive to choice of u0!!!

            // v2: try to "highpass" u
            if (candidateGainVersion == 3)
            {
                LowPass lpFilt = new LowPass(tuningDataSet.timeBase_s);
                double[] uLPfilt = lpFilt.Filter(tuningDataSet.GetUMinusFF(), pidid.Tiest_val1.Value * 6);
                double[] uHPfilt = vec.Sub(tuningDataSet.GetUMinusFF(), uLPfilt);
                u0 = Vec.Add(uHPfilt, tuningDataSet.GetUMinusFF()[0]);
                double y0 = tuningDataSet.ymeas[0];
            }
            double yset0 = tuningDataSet.yset[0];
            deltaU          = vec.Subtract(tuningDataSet.GetUinclFF(), u0);
            LowPass lowPass = new LowPass(tuningDataSet.timeBase_s);
            // filter is experimental, consider removing if cannot be made to work.
            double FilterTc_s = 0;// tuningDataSet.timeBase_s;
            /*if (referenceProcessId != null)
            {
                FilterTc_s = referenceProcessId.TimeConstant_s;
            } */

            double[] deltaU_lp = lowPass.Filter(deltaU, FilterTc_s,2);

            double[] deltaYset = vec.Subtract(tuningDataSet.yset, yset0);
            double[] du_contributionFromU = deltaU_lp;
            double[] dest_f2_proptoGain = du_contributionFromU;
            double[] dest_f2_constTerm = vec.Add(deltaYset, e);

            // y0,u0 is at the first data point
            // disadvantage, is that you are not sure that the time series starts at steady state
            // but works better than candiate 2 when disturbance is a step
         
            if (candidateGainVersion ==1)
            {
                bool candidateGainSet = false;
                if (referenceProcessId != null)
                {
                    if (!Double.IsNaN(referenceProcessId.processGain))
                    {
                        candidateGainD = referenceProcessId.processGain;
                        candidateGainSet = true;
                    }
                }
                // fallback: this should only be used as an inital guess on the first
                // run when no process model exists!
                if (!candidateGainSet)
                {
                    double[] eFiltered = lowPass.Filter(e, FilterTc_s, 2);
                    double maxDE = vec.Max(vec.Abs(eFiltered));                      // this has to be sensitive to noise?
                    double[] uFiltered = lowPass.Filter(deltaU, FilterTc_s, 2);

                    double maxU = vec.Max(vec.Abs(uFiltered));        // sensitive to output noise/controller overshoot
                    double minU = vec.Min(vec.Abs(uFiltered));        // sensitive to output noise/controller overshoot  
                    candidateGainD = processGainSign * maxDE / (maxU - minU);
                }
            }

            //
            // default version of disturbance estimate that looks at deviation from yset
            // and uses no reference model
            // 
            // too simple, only works for static processes.
            // for dynamic processes "e" will include process transients that shoudl be 
            // subtracted.
            double[]  d_HF = e;
            // too simple, only works if setpoint does not change.
            double[] du_contributionFromYset = vec.Multiply(deltaYset, 1 / -candidateGainD);// will be zero if yset is constant.
            double[] d_LF_internal = vec.Add(du_contributionFromU, du_contributionFromYset);
            double[]  d_LF = vec.Multiply(d_LF_internal, -candidateGainD);
            // d = d_LF+d_HF 
            double[]  d_est = vec.Add(d_HF, d_LF);

            // currently the below code breaks disturbance estimtes in those cases
            // where setpoint is constant - wonder if it is possible to unify these two appraoches???
            // very bad form to have lots of "if" statements in this estiamtor.

            if (referenceProcessId != null && doesSetpointChange)
            {
                // if a process model is available, try to subtract the process transients
                // from e when estimating d_HF.
                if (referenceProcessId.modelledY != null)
                {
                    d_HF = null;
                    d_LF = null;
                    // be careful! the inital value of modelledY is often NaN.
                    double[] e_mod = vec.Subtract(referenceProcessId.modelledY, tuningDataSet.ymeas);
                    double filterTc_s = 200;
                    LowPass lp = new LowPass(tuningDataSet.timeBase_s);
                    double[] e_mod_LP = lp.Filter(e_mod, filterTc_s, 1);
                    double[] e_mod_HF = vec.Subtract(e_mod, e_mod_LP);
                    // because of time-delay e_mod is generally shorter than e if timeDelay_samples>0
                    double[] e_mod_padding = Vec<double>.Fill(0, e.Length - e_mod.Length);
                    double[] e_mod_padded = Vec<double>.Concat(e_mod_padding, e_mod_HF);

                    // d_HF 
                    //e = Vec.Sub(tuningDataSet.ymeas, referenceProcessId.modelledY);
                    /*d_HF = Vec.Sub(e, e_mod_padded);
                    List<int> nanIndices = Vec.FindValues(d_HF,Double.NaN, FindValues.NaN);
                    for (int i = 0; i < nanIndices.Count; i++)
                    {
                        d_HF[nanIndices[i]] = e[i];
                    }
                    */
                    d_HF = Vec<double>.Fill(0, tuningDataSet.ymeas.Length);

                    //d_LF
                    //   double[] du_contributionFromYset = Vec.Mult(/*deltaYset*/e_mod, 1 / -candidateGainD);// will be zero if yset is constant.
                    //double[] d_LF_internal = Vec.Add(du_contributionFromU, du_contributionFromYset);
                    // d_LF = Vec.Mult(d_LF_internal, -candidateGainD);
                    d_LF = vec.Subtract(tuningDataSet.ymeas, referenceProcessId.modelledY);
                    d_LF[0] = d_LF[1];//first modelledY is NaN
                    d_est = d_LF;
                }
            }
        //    d_est       = FreezeDisturbanceAfterSetpointChange(d_est, tuningDataSet, pidid);//TODO:do for formulation 2 as well!

            // copy result to result class
            result.f1EstProcessGain     = candidateGainD;
            result.dest_f1              = d_est;
            result.dest_f2_proptoGain   = dest_f2_proptoGain;
            result.dest_f2_constTerm    = dest_f2_constTerm;
            result.d_LF = d_LF;
            result.d_HF = d_HF;

            // NB! minus!
            double[] dest_test = vec.Add(vec.Multiply(result.dest_f2_proptoGain,-result.f1EstProcessGain),result.dest_f2_constTerm);

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
