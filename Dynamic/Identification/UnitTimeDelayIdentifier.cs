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
    /// Brute-force(trial and error) estimation of time delay 
    /// <para>
    /// This method is intended to be generic, so that it can be applied on different kinds of process models, and thus it uses
    /// interfaces and dependency injection.
    /// </para>
    /// <para>
    /// The idea of the method is to re-identify the model first with no time-delay, and then increasing the time-delay step-by-step.
    /// This process should continue so long as the model improves (as measured by uncertainty mangitude, objective function value or Rsquared).
    /// </para>
    /// <para>
    /// Thus, this method is a component in reducing the problem of determining continous paramters along with the integer time delay
    /// (a mixed-integer problem) to a sequential optimization approach where the integer and continous parts of the problem are solved
    /// sequentially.
    /// </para>
    /// <seealso cref="UnitIdentifier"/>
    /// </summary>
    class UnitTimeDelayIdentifier
    {
        private double TimeBase_s;

        private const int minTimeDelayIts = 5;
        private int maxExpectedTimeDelay_samples;

        private List<ModelParametersBaseClass> modelRuns;


        public UnitTimeDelayIdentifier(double TimeBase_s, double maxExpectedTc_s)
        {
            this.TimeBase_s = TimeBase_s;
            modelRuns = new List<ModelParametersBaseClass>();

            this.maxExpectedTimeDelay_samples = Math.Max((int)Math.Floor(maxExpectedTc_s / TimeBase_s), minTimeDelayIts);

        }

        public void AddRun(ModelParametersBaseClass modelParameters)
        {
            modelRuns.Add(modelParameters);
        }

        public bool DetermineIfContinueIncreasingTimeDelay(int timeDelayIdx)
        {
            if (timeDelayIdx < minTimeDelayIts)
                return true;

            // nb! note that time window decreases with one timestep for each increase in time delay of one timestep.
            // thus there is a chance of the object function decreasing without the model fit improving.
            // especially if the only information in the dataset is at the start of the dataset.
            var objFunVals = GetObjFunValList();
            bool isObjFunDecreasing = objFunVals.ElementAt(objFunVals.Length - 2) < objFunVals.ElementAt(objFunVals.Length-1);

            var r2Vals = GetR2List();
            bool isR2Decreasing = r2Vals.ElementAt(r2Vals.Length - 2) < r2Vals.ElementAt(r2Vals.Length - 1); ;

            bool continueIncreasingTimeDelayEst;// = true;

            if (isObjFunDecreasing || isR2Decreasing)
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
        /// Get R2 of all runs so far in an array, 
        /// </summary>
        /// <returns>array or R-squared values, with Nan if any runs have failed</returns>
        private double[] GetR2List()
        {
            List<double> objR2List = new List<double>();
            for (int i = 0; i < modelRuns.Count; i++)
            {
                if (modelRuns[i] == null)
                {
                    objR2List.Add(Double.NaN);
                }
                else
                {
                    if (modelRuns[i].Fitting.WasAbleToIdentify)
                    {
                        objR2List.Add(modelRuns[i].Fitting.RsqFittingDiff);
                    }
                    else
                    {
                        objR2List.Add(Double.NaN);
                    }
                }

            }
            return objR2List.ToArray();
        }

        /// <summary>
        /// Get objective functions valueof all runs so far in an array
        /// </summary>
        /// <returns>array of objective function values, nan for failed runs</returns>
        private double[] GetObjFunValList()
        {
            List<double> objFunValList = new List<double>();
            for (int i = 0; i < modelRuns.Count; i++)
            {
                if (modelRuns[i] == null)
                {
                    objFunValList.Add(Double.NaN);
                }
                else
                {
                    if (modelRuns[i].Fitting.WasAbleToIdentify)
                    {
                        objFunValList.Add(modelRuns[i].Fitting.ObjFunValFittingDiff);
                    }
                    else
                    {
                        objFunValList.Add(Double.NaN);
                    }
                }
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
                if (sortedIndices.Count() > 1)
                {
                   
                    int R2distanceToRunnerUp = Math.Abs(sortedIndices[1] - sortedIndices[0]);
                    if (R2distanceToRunnerUp > 1)
                    {
                        warnings.Add(ProcessTimeDelayIdentWarnings.NonConvexRsquaredSolutionSpace);
                    }
                    if (objR2List.Contains(Double.NaN))
                    {
                        warnings.Add(ProcessTimeDelayIdentWarnings.SomeModelRunsFailedToFindSolution);
                    }
                    double R2valueDiffToRunnerUp = objR2List[sortedIndices[0]] - objR2List[sortedIndices[1]];
                    if (R2valueDiffToRunnerUp < 0.1)
                    {
                        warnings.Add(ProcessTimeDelayIdentWarnings.NoUniqueRsquaredMinima);
                    }
                }
            }
            //
            // objective function value analysis
            //
            {
                var objObjFunList = GetObjFunValList();
                Vec<double>.Sort(objObjFunList.ToArray(), VectorSortType.Ascending, out int[] objFunSortedIndices);
                // int objFunBestTimeDelayIdx = objFunSortedIndices[0];
                // the solution space should be "concave", so there should not be several local minimia
                // as you compare R2list for different time delays-that has happended but indicates something
                // is wrong. 
                if (objFunSortedIndices.Count() > 1)
                {
                    int objFunDistanceToRunnerUp = Math.Abs(objFunSortedIndices[1] - objFunSortedIndices[0]);
                    if (objFunDistanceToRunnerUp > 1)
                    {
                        warnings.Add(ProcessTimeDelayIdentWarnings.NonConvexObjectiveFunctionSolutionSpace);
                    }
                    double ObjFunvalueDiffToRunnerUp = Math.Abs(objObjFunList[objFunSortedIndices[0]]
                        - objObjFunList[objFunSortedIndices[1]]);
                    if (ObjFunvalueDiffToRunnerUp <= 0.0001)
                    {
                        warnings.Add(ProcessTimeDelayIdentWarnings.NoUniqueObjectiveFunctionMinima);
                    }
                }
            }

            //
            // parameter uncertatinty value analysis
            //
            // TODO: consider re-introducing ranking by uncertainty in a generic way.

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

        public ModelParametersBaseClass GetRun(int runIndex)
        {
            return modelRuns.ElementAt(runIndex);
        }

    }
}
