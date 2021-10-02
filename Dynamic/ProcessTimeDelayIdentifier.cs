using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{



    /// <summary>
    /// Brute-force estimation of time delay 
    /// 
    /// This method is intended to be generic, so that it can be applied on different kinds of process models, and thus it uses
    /// interfaces and dependency injection.
    /// 
    /// The idea of the method is to re-identify the model first with no time-delay, and then increasing the time-delay step-by-step.
    /// This process should continue so long as the model improves (as measured by uncertainty mangitude, objective function value or Rsquared).
    /// 
    /// Thus, this method is a component in reducing the problem of determining continous paramters along with the integer time delay
    /// (a mixed-integer problem) to a sequential optimization approach where the integer and continous parts of the problem are solved
    /// sequentially.
    ///
    /// </summary>
    class ProcessTimeDelayIdentifier
    {
        private double TimeBase_s;

        private const int minTimeDelayIts = 5;
        private int maxExpectedTimeDelay_samples;

        private List<IFittedProcessModelParameters> modelRuns;


        public ProcessTimeDelayIdentifier(double TimeBase_s, double maxExpectedTc_s)
        {
            this.TimeBase_s = TimeBase_s;
            modelRuns = new List<IFittedProcessModelParameters>();

            this.maxExpectedTimeDelay_samples = Math.Max((int)Math.Floor(maxExpectedTc_s / TimeBase_s), minTimeDelayIts);

        }


        public void AddRun(IFittedProcessModelParameters modelParameters)
        {
            modelRuns.Add(modelParameters);
        }


        /*
        public void AddUncertaintyEstiamtes(bool useDynamicModel, double[] param,
            double[] param_95prcUnc, double[][] varCovarMatrix,
            int Ndataset)
        {
            if (param_95prcUnc != null && varCovarMatrix != null)
            {
                if (useDynamicModel)
                {
                    if (varCovarMatrix[2] != null)
                        BiasEstUnc.Add(varCovarMatrix[2][2]);
                    else
                        BiasEstUnc.Add(Double.NaN);

                    //   TimeConstantEstUnc.Add(varCovarMatrix[0][0] * 1.96);
                    double varb0 = varCovarMatrix[0][0];
                    double varb1 = varCovarMatrix[1][1];
                    double covb1b2 = varCovarMatrix[0][1];

                    // time constant uncertainty
                    if (param_95prcUnc[0] != 0)
                    {
                        // http://dept.stat.lsa.umich.edu/~kshedden/Courses/Stat406_2004/Notes/variance.pdf

                        //https://stats.stackexchange.com/questions/41896/varx-is-known-how-to-calculate-var1-x

                        // the standard error of y = b1/b0
                        // note that var (cX) = c^2*var(X)

                        // g(b0) = 1/(1/b0-1)

                        // Var(Tc) = Ts^2* Var(g(b0))
                        // Var(g(b0)) = Var(1/(1/b0)-1) = is difficult to find in general
                        // but often this can be approximated by a first or second order Taylor approximation
                        // 
                        // first order: var(g(x)) approx dgdx^2*varx
                        // d(1/(1/a-1))/da = (apply chain rule)
                        // 
                        double a = param[0] + 1;
                        double dTc_db0 = a * Math.Pow(1 / a - 1, -2);
                        double c_square = Math.Pow(TimeBase_s, 2);
                        double varTc = c_square * Math.Pow(dTc_db0, 2) * varb0;
                        // standard error of the mean is SE_x = var_X/(sqrt(N));
                        // if the 95 prc confidence interval of x is 1.96*SE_x if the X is normally distributed.
                        double SE_Tc = varTc / Math.Sqrt(Ndataset);
                        TimeConstantEstUnc.Add(SE_Tc * 1.96);
                    }
                    else
                        TimeConstantEstUnc.Add(Double.NaN);

                    // process gain uncertainty 
                    // process gain: b[1] /(1- b[0])
                    // idea to take first order taylor:
                    //var(g(x)) approx (dgdx)^2 * varx

                    // var(x1+x2) = var(x1) + var(x2) +2*cov(x1,x2)
                    // g(x1,x2) = b1/(1-b0)

                    //g(x1,x2) = (dg/dx1)^2 *var(x1) + (dg/dx2)^2 *var(x2) +dg/dx1dx2 *cov() 

                    double dg_db0 = param[1] * param[0] * Math.Pow(1 - param[0], -2);
                    double dg_db1 = 1 / (1 - param[0]);
                    double covTerm = param[0] * Math.Pow(1 - param[0], -2);
                    double SE_Kc = (Math.Pow(dg_db0, 2) * varb0 + Math.Pow(dg_db1, 2) * varb1 + covTerm * covb1b2) / Math.Sqrt(Ndataset);//     / ;
                    ProcessGainEstUnc.Add(SE_Kc * 1.96);
                }
                else
                {
                    TimeConstantEstUnc.Add(Double.NaN);
                    ProcessGainEstUnc.Add(varCovarMatrix[0][0] * 1.96);
                }
            }
            else
            {
                TimeConstantEstUnc.Add(Double.NaN);
                ProcessGainEstUnc.Add(Double.NaN);
            }

            // calculate the uncertainty as a percentage of the estimated value for comparison
            if (ProcessGainEst.Last() > 0)
            {
                ProcessGainEstUnc_prc.Add(ProcessGainEstUnc.Last() / ProcessGainEst.Last());
            }
            else
            {
                ProcessGainEstUnc_prc.Add(Double.NaN);
            }
            if (TimeConstantEst.Last() > 0)
            {
                TimeConstantEstUnc_prc.Add(TimeConstantEstUnc.Last() / TimeConstantEst.Last());
            }
            else
            {
                TimeConstantEstUnc_prc.Add(Double.NaN);
            }
        }
        */



        public bool DetermineIfContinueIncreasingTimeDelay(int timeDelayIdx)
        {
            if (timeDelayIdx < minTimeDelayIts)
                return true;
            /*
            //indicates a rank deficient problem
            if (objctFunVal.Count() < timeDelayIdx)
            {
                return false;
            }

            bool continueIncreasingTimeDelayEst = true;
            bool isObjFunDecreasing = objctFunVal.ElementAt(timeDelayIdx - 2) > objctFunVal.ElementAt(timeDelayIdx - 1);

            double prevProcessGainUncPrc = ProcessGainEstUnc.ElementAt(timeDelayIdx - 2) / ProcessGainEst.ElementAt(timeDelayIdx - 2);
            double curProcessGainUncPrc = ProcessGainEstUnc.ElementAt(timeDelayIdx - 1) / ProcessGainEst.ElementAt(timeDelayIdx - 1);
            bool isProcessGainUncDecreasing = prevProcessGainUncPrc > curProcessGainUncPrc;

            double prevProcessTimeConstncPrc = TimeConstantEstUnc.ElementAt(timeDelayIdx - 2) / TimeConstantEst.ElementAt(timeDelayIdx - 2);
            double curProcessTimeConstUncPrc = TimeConstantEstUnc.ElementAt(timeDelayIdx - 1) / TimeConstantEst.ElementAt(timeDelayIdx - 1);
            bool isProcessTimeConstantnUncDecreasing = prevProcessTimeConstncPrc > curProcessTimeConstUncPrc;


            if (isObjFunDecreasing || isProcessGainUncDecreasing || isProcessTimeConstantnUncDecreasing)
                */


            var objFunVals = GetObjFunValList();
            bool isObjFunDecreasing = objFunVals.ElementAt(objFunVals.Length - 2) < objFunVals.ElementAt(objFunVals.Length-1);

            var r2Vals = GetR2List();
            bool isParameterUncertaintyDecreasing = r2Vals.ElementAt(r2Vals.Length - 2) < r2Vals.ElementAt(r2Vals.Length - 1); ;

            bool continueIncreasingTimeDelayEst;// = true;

            if (isObjFunDecreasing || isParameterUncertaintyDecreasing)
            {
                if (timeDelayIdx < maxExpectedTimeDelay_samples)
                {
                    continueIncreasingTimeDelayEst = true;
                }
                else
                {
                    continueIncreasingTimeDelayEst = false;
                }
            }
            else
                continueIncreasingTimeDelayEst = false;


            return continueIncreasingTimeDelayEst;
        }

        /// <summary>
        /// Get R2 of all runs so far in an array
        /// </summary>
        /// <returns></returns>
        private double[] GetR2List()
        {
            List<double> objR2List = new List<double>();
            for (int i = 0; i < modelRuns.Count; i++)
            {
                objR2List.Add(modelRuns[i].GetFittingR2());
            }
            return objR2List.ToArray();
        }

        /// <summary>
        /// Get objective functions valueof all runs so far in an array
        /// </summary>
        /// <returns></returns>
        private double[] GetObjFunValList()
        {
            List<double> objFunValList = new List<double>();
            for (int i = 0; i < modelRuns.Count; i++)
            {
                objFunValList.Add(modelRuns[i].GetFittingObjFunVal());
            }
            return objFunValList.ToArray();
        }





        /// <summary>
        /// Chooses between all the stored model runs,the model which seems the best.
        /// </summary>
        /// <returns>Index of best time delay model</returns>
        public int ChooseBestTimeDelay(out List<ProcessTimeDelayIdentWarnings> warnings)
        {
            int bestTimeDelayIdx;
            warnings = new List<ProcessTimeDelayIdentWarnings>();

            // one issue is that if time delay is increasing, the ymod becomes shorter,
            // so this needs to be objective function either normalized or excluding the first few points.

            //
            // R-squared analysis
            //
            int R2BestTimeDelayIdx;
            { 
                var objR2List = GetR2List();
                Vec<double>.Sort(objR2List.ToArray(), VectorSortType.Descending, out int[] sortedIndices);
                R2BestTimeDelayIdx = sortedIndices[0];
                // the solution space should be "concave", so there should not be several local minimia
                // as you compare R2list for different time delays-that has happended but indicates something
                // is wrong. 
                int R2distanceToRunnerUp = Math.Abs(sortedIndices[1] - sortedIndices[0]);
                if (R2distanceToRunnerUp > 1)
                {
                    warnings.Add(ProcessTimeDelayIdentWarnings.NonConvexRsquaredSolutionSpace);
                }

                double R2valueDiffToRunnerUp = objR2List[sortedIndices[0]] - objR2List[sortedIndices[1]];
                if (R2valueDiffToRunnerUp < 0.1)
                {
                    warnings.Add(ProcessTimeDelayIdentWarnings.NoUniqueRsquaredMinima);
                }
            }
            //
            // objective function value analysis
            //
            {
                var objObjFunList = GetObjFunValList();
                Vec<double>.Sort(objObjFunList.ToArray(), VectorSortType.Descending, out int[] objFunSortedIndices);
                // int objFunBestTimeDelayIdx = objFunSortedIndices[0];
                // the solution space should be "concave", so there should not be several local minimia
                // as you compare R2list for different time delays-that has happended but indicates something
                // is wrong. 
                int objFunDistanceToRunnerUp = Math.Abs(objFunSortedIndices[1] - objFunSortedIndices[0]);
                if (objFunDistanceToRunnerUp > 1)
                {
                    warnings.Add(ProcessTimeDelayIdentWarnings.NonConvexObjectiveFunctionSolutionSpace);
                }
                double ObjFunvalueDiffToRunnerUp = objObjFunList[objFunSortedIndices[0]] 
                    - objObjFunList[objFunSortedIndices[1]];
                if (ObjFunvalueDiffToRunnerUp <= 0.0001)
                {
                    warnings.Add(ProcessTimeDelayIdentWarnings.NoUniqueObjectiveFunctionMinima);
                }
            }

            //
            // parameter uncertatinty value analysis
            //
            // TODO: consider re-introducing rankng by uncertainty in a generic way.

            /*
            // v1: use uncertainty to choose time delay
            if (!ProcessGainEstUnc_prc.ToArray().Contains(Double.NaN) &&
                !(BiasEstUnc.ToArray().Contains(Double.NaN)))
            {
                Vec.Min(ProcessGainEstUnc_prc.ToArray(), out int processGainUncBestTimeDelayIdx);
                Vec.Min(TimeConstantEstUnc_prc.ToArray(), out int processTimeConstantUncBestTimeDelayIdx);
                Vec.Min(BiasEstUnc.ToArray(), out int biasUncBestTimeDelayIdx);
                bestTimeDelayIdx = Math.Min(biasUncBestTimeDelayIdx, processGainUncBestTimeDelayIdx);
            }
            else*/// fallback: use just lowest objective function
            {
                bestTimeDelayIdx = R2BestTimeDelayIdx;
            }
            return bestTimeDelayIdx;
        }

        public IFittedProcessModelParameters GetRun(int runIndex)
        {
            return modelRuns.ElementAt(runIndex);
        }




        /*
        public ProcessIdResults ExtractResult(int bestTimeDelayIdx, ProcessDataSet dataSet, ref ProcessIdResults result)
        {
            result.SetU0(u0);

            // ProcessIdResults result = new ProcessIdResults(TimeBase_s);
            // fill result in full-size vector
            result.processGain = ProcessGainEst.ElementAt(bestTimeDelayIdx);
            result.timeConstant_s = TimeConstantEst.ElementAt(bestTimeDelayIdx);
            result.timeDelay_s = bestTimeDelayIdx * TimeBase_s;
            result.timeDelay_samples = bestTimeDelayIdx;
            result.ProcessGainUnc = ProcessGainEstUnc.ElementAt(bestTimeDelayIdx);
            result.TimeConstantUnc_s = TimeConstantEstUnc.ElementAt(bestTimeDelayIdx);
            result.bias = BiasEst.ElementAt(bestTimeDelayIdx);
            result.TimeDelayUnc_s = 0;
            result.objFunVal = objctFunVal.ElementAt(bestTimeDelayIdx);
            if (y_mod.ElementAt(bestTimeDelayIdx) == null)
            {
                result.modelledY = null;
                result.modelledD = null;
            }
            else
            {
                int nInitalValuesToFill = dataSet.y_meas.Length - y_mod.ElementAt(bestTimeDelayIdx).Length;// TODO:-1?
                result.modelledY = Vec<double>.Concat(Vec<double>.Fill(Double.NaN, nInitalValuesToFill), y_mod.ElementAt(bestTimeDelayIdx));//nb! this will only work if only a single model is found.
                nInitalValuesToFill = dataSet.y_meas.Length - d_mod.ElementAt(bestTimeDelayIdx).Length;
                double modelledDYFillValue = d_mod.ElementAt(bestTimeDelayIdx)[nInitalValuesToFill];
                result.modelledD = Vec<double>.Concat(Vec<double>.Fill(modelledDYFillValue, nInitalValuesToFill), d_mod.ElementAt(bestTimeDelayIdx));
                double modelledXYFillValue = x_mod.ElementAt(bestTimeDelayIdx)[nInitalValuesToFill];
                result.modelledX = Vec<double>.Concat(Vec<double>.Fill(modelledXYFillValue, nInitalValuesToFill), x_mod.ElementAt(bestTimeDelayIdx));
            }

            result.GoodnessOfFit_prc = Vec.RSquared(result.modelledY, dataSet.y_meas) * 100;

            return result;

        }
        */




    }
}
