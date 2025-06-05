using Accord.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Class that attempts to identify the parameters (such as Kp and Ti) of a PID-controller from a 
    /// given set of time-series of the input and output of said controller.
    /// </summary>
    public class PidIdentifier
    {
        private const double CUTOFF_FOR_GUESSING_PID_IN_MANUAL_FRAC = 0.005;
        const double rSquaredCutoffForInTrackingWarning = 0.02;//must be between 0 and 1
        private const double MAX_RSQUARED_DIFF_BEFORE_COMPARING_FITSCORE = 99.75; // Must be below 100
        private const double MIN_FITSCORE_DIFF_BEFORE_COMPARING_FITSCORE = 10; // Must be positive
        private const int MAX_ESTIMATIONS_PER_DATASET = 1;
        private const double MIN_DATASUBSET_URANGE_PRC = 0;
        private double badValueIndicatingValue;

        private PidScaling pidScaling;
        private PidControllerType type;
        private PidFilter pidFilter;
        private Vec vec;
        private double timeBase_s;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="pidScaling"></param>
        /// <param name="maxExpectedTc_s"></param>
        /// <param name="badValueIndicatingValue"></param>
        /// <param name="type"></param>
        public PidIdentifier(PidScaling pidScaling=null,  double badValueIndicatingValue=-9999,
            PidControllerType type = PidControllerType.Unset)
        {
            this.badValueIndicatingValue = badValueIndicatingValue;
            this.pidScaling = pidScaling;
            this.type = type;
            this.vec = new Vec();
        }


        private bool IsFirstModelBetterThanSecondModel(PidParameters firstModel, PidParameters secondModel)
        {
            // If both models show a very high R-Squared-diff, look at fitscore instead if there is a significant difference
            if (firstModel.Fitting.RsqDiff > MAX_RSQUARED_DIFF_BEFORE_COMPARING_FITSCORE
                && secondModel.Fitting.RsqDiff > MAX_RSQUARED_DIFF_BEFORE_COMPARING_FITSCORE)
            //     && (Math.Abs(firstModel.Fitting.FitScorePrc - secondModel.Fitting.FitScorePrc) > Math.Abs(firstModel.Fitting.RsqDiff - secondModel.Fitting.RsqDiff)))
            //     && Math.Abs(firstModel.Fitting.FitScorePrc - secondModel.Fitting.FitScorePrc) > MIN_FITSCORE_DIFF_BEFORE_COMPARING_FITSCORE)
            {
                if (firstModel.Fitting.FitScorePrc > secondModel.Fitting.FitScorePrc)
                    return true;
                else
                    return false;
            }
            else if (firstModel.Fitting.RsqDiff > secondModel.Fitting.RsqDiff)
                return true;
            else
                return false;

        }

        /// <summary>
        /// Identifies a PID-controller from a UnitDataSet
        /// </summary>
        /// <param name="dataSet">a UnitDataSet, where .Y_meas, .Y_setpoint and .U are analyzed</param>
        /// <param name="downsampleOversampledData">Boolean, whether to do internal oversample identification and attempt downsampling. Defaults to true.</param>
        /// <param name="ignoreFlatLines">Boolean, whether to do internal flatline identification and ignore their indices. Defaults to true.</param>
        /// <returns>the identified parameters of the PID-controller</returns>
        public PidParameters Identify(ref UnitDataSet dataSet, bool downsampleOversampledData = true/*, bool ignoreFlatLines = true*/)
        {
         //   const bool doOnlyWithDelay = false;// should be false unless debugging something
            const bool DoFiltering = true; // default is true (this improves performance significantly)
            const bool returnFilterParameters = false; // even if filtering helps improve estimates, maybe the filter should not be returned?

            // Find the oversampled factor if relevant
          /*  bool ignoreFlatLinesFirst = ignoreFlatLines;
            if (downsampleOversampledData & ignoreFlatLinesFirst)
            {
                double oversampledFactor = dataSet.GetOversampledFactor(out int keyIndex);
                // Oversamples of less than 20 percent can be handled by the flatline index ignoration.
                if (oversampledFactor > 1.2)
                {
                    ignoreFlatLinesFirst = false;
                }
            }*/

            // 1. try identification with delay of one sample but without filtering
            (PidParameters results_withDelay, double[,] U_withDelay, List<int> indicesToIgnore_withDelay) = IdentifyInternal(dataSet, true);
   
   /*         if (doOnlyWithDelay)
            {
                dataSet.U_sim = U_withDelay;
                return results_withDelay;
            }*/

            // 2. try identification without delay of one sample (yields better results often if dataset is downsampled
            //    relative to the clock that the pid algorithm ran on originally)
            (PidParameters results_withoutDelay, double[,] U_withoutDelay, List<int> indicesToIgnore_withoutDelay) = 
                IdentifyInternal(dataSet, false);

            // save which is the "best" estimate for comparison 
            bool doDelay = true;
            PidParameters bestPidParameters= results_withoutDelay;
            double[,] bestU = U_withoutDelay;
            List<int> bestIndicesToIgnore = indicesToIgnore_withoutDelay;
            if (IsFirstModelBetterThanSecondModel(results_withDelay, results_withoutDelay))
            {
                doDelay = true;
                bestPidParameters = results_withDelay;
                bestU = U_withDelay;
                bestIndicesToIgnore = indicesToIgnore_withDelay;

            }
            else
            {
                doDelay = false;
                bestPidParameters = results_withoutDelay;
                bestU = U_withoutDelay;
                bestIndicesToIgnore = indicesToIgnore_withoutDelay;
            }

            if (DoFiltering)
            {
                // 3. try filtering y_meas and see if this improves fit 
                // if there is noise on y_meas that is not filtered, this may cause too small Kp/Ti
                double maxFilterTime_s = 6 * timeBase_s;
                for (double filterTime_s = timeBase_s; filterTime_s < maxFilterTime_s; filterTime_s += timeBase_s)
                {
                    var pidFilterParams = new PidFilterParams(true, 1, filterTime_s);
                    (PidParameters results_withFilter, double[,] U_withFilter, List<int> indicesToIgnore_withFilter) = 
                        IdentifyInternal(dataSet, doDelay, pidFilterParams/*, ignoreFlatLines: ignoreFlatLinesFirst*/);

                    if (IsFirstModelBetterThanSecondModel(results_withFilter, bestPidParameters))
                    {
                        bestU = U_withFilter;
                        bestPidParameters = results_withFilter;
                        bestIndicesToIgnore = indicesToIgnore_withFilter;
                    }
                }

                // 4. try filtering the input u_meas
                // this is experimental, but if downsampled, Kp/Ti often seems too low, and the hypothesis is that this is because
                // small variations in u_meas/y_meas are no longer tightly correlated, so identification should perhaps focus on fitting
                // to only "larger" changes. 
                bool filterUmeas = true;
                for (double filterTime_s = timeBase_s; filterTime_s < maxFilterTime_s; filterTime_s += timeBase_s)
                {
                    var pidFilterParams = new PidFilterParams(true, 1, filterTime_s);
                    (PidParameters results_withFilter, double[,] U_withFilter, List<int> indicesToIgnore_withFilter) =
                        IdentifyInternal(dataSet, doDelay, pidFilterParams, filterUmeas/*, ignoreFlatLines: ignoreFlatLinesFirst*/);

                    if (IsFirstModelBetterThanSecondModel(results_withFilter, bestPidParameters))
                    {
                        bestU = U_withFilter;
                        bestPidParameters = results_withFilter;
                        bestIndicesToIgnore = indicesToIgnore_withFilter;
                    }
                }
            }

            // 5. Try identifying if the data is oversampled, and if so check whether downsampled identification yields better results.
            if (downsampleOversampledData)
            {
                double oversampledFactor = dataSet.GetOversampledFactor(out int keyIndex);
                // Oversamples of less than 20 percent can be handled by the flatline index ignoration.
                if (oversampledFactor > 1.2)
                {
                    UnitDataSet dataSetDownsampled = new UnitDataSet(dataSet, oversampledFactor, keyIndex);
                    if (dataSet.Times.Count() != dataSetDownsampled.Times.Count())
                    {
                        pidFilter = null;
                        PidParameters results_downsampled = Identify(ref dataSetDownsampled/*, ignoreFlatLines: ignoreFlatLines*/);
                        if (IsFirstModelBetterThanSecondModel(results_downsampled, bestPidParameters))
                        {
                            bestU = dataSetDownsampled.U_sim;
                            dataSet = dataSetDownsampled;
                            bestPidParameters = results_downsampled;
                            bestIndicesToIgnore = dataSetDownsampled.IndicesToIgnore;
                        }
                    }
                }
            }
            
            // 6. finally return the "best" result
            dataSet.U_sim = bestU;
            dataSet.IndicesToIgnore = bestIndicesToIgnore;
            // consider if the the filter parameters maybe do not need to be returned
            if (!returnFilterParameters)
            {
                bestPidParameters.Filtering = new PidFilterParams();
            }
            return bestPidParameters;
        }

        /// <summary>
        /// a steady-state offset between U_sim and U is an indication of noise in Y_meas getting into controller and 
        /// "biasing" estimates
        /// </summary>
        /// <returns></returns>

        private bool HasSignificantSteadyStateOffset(double[,] U_sim, double[,] U_meas)
        {
            const int cutoff_percent = 10;
            const double avgOffsetRelativeToRange_cutoff_prc = 0.1;
            int nLower = 0;
            int nHigher = 0;
            double valLower = 0;
            double valHigher = 0;


            double[] u_sim = U_sim.GetColumn(0);
            double[] u_meas = U_meas.GetColumn(0);
            int N = u_sim.GetLength(0); 

            for (int i = 0; i < u_sim.Length; i++)
            {
                if (u_sim[i] < u_meas[i])
                {
                    nLower++;
                    valLower = Math.Abs(u_sim[i] - u_meas[i]);
                }
                else if (u_sim[i] > u_meas[i])
                {
                    nHigher++;
                    valHigher = Math.Abs(u_sim[i] - u_meas[i]);
                }
            }
            valLower = valLower / nLower;
            valHigher = valHigher / nHigher;
            double u_Range = (new Vec()).Max(u_meas)- (new Vec()).Min(u_meas);
            // protect from divide-by-zero
            if (u_Range == 0)
                return false;
            // see if average max deviation is significant in amplitude
            double avgOffsetRelativeToRange_prc = (double)Math.Max(valLower, valHigher) / u_Range * 100;
            if (avgOffsetRelativeToRange_prc < avgOffsetRelativeToRange_cutoff_prc)
            {
                return false;
            }

            //protect from divide-by-zero
            if (Math.Min(valLower, valHigher) == 0)
            {
                return true;
            }
            // see if there is a big deviation between amplitude of overshoot and undershoot.
            float percentDiffBetweenLowerAndHigherValues = 
                (float)((Math.Max(valLower,valHigher)/Math.Min(valLower, valHigher)) -1) * 100;
            if (percentDiffBetweenLowerAndHigherValues> cutoff_percent)
            {
                return true;
            }
            return false;
        }

        private double[] GetUMinusFF(UnitDataSet dataSet)
        {
            // todo: how to treat/remove feedforward term here???
            if (dataSet.U.GetNColumns() == 1)
                return dataSet.U.GetColumn(0);
            else if (dataSet.U.GetNColumns() == 2)
            {
                return vec.Subtract(dataSet.U.GetColumn(0), dataSet.U.GetColumn(1));
            }
            else
            {
                return null;
            }
        }

        private double[] GetErrorTerm(UnitDataSet dataSet, PidFilter pidFilter)
        {
            if (pidFilter == null)
                return vec.Subtract(dataSet.Y_meas, dataSet.Y_setpoint);
            else
            {
                double[] y_filt = new double[dataSet.Y_meas.Length];
                /*
                y_filt[0] = dataSet.Y_meas[0];
                for (int i = 1; i < dataSet.Y_meas.Length; i++)
                {
                    y_filt[i] = pidFilter.Filter(dataSet.Y_meas[i]);
                }
                */

                int kernelLength = (int)Math.Floor(pidFilter.GetParams().TimeConstant_s / timeBase_s);
                y_filt = (new Vec()).NonCausalSmooth(dataSet.Y_meas, kernelLength);

                /*
                if (kernelLength > 0)
                {
                    Shared.EnablePlots();
                    Plot.FromList(new List<double[]>{ dataSet.Y_meas,
                    y_filt },
                        new List<string> { "y1=y_meas", "y1=yfilt" },
                        timeBase_s, "test_3");
                    Shared.DisablePlots();
                }*/
                return vec.Subtract(y_filt, dataSet.Y_setpoint);
            }

        }


        /// <summary>
        /// Internal Pid-identification for a given dataset, sampling and with a given filter on y.
        /// Note that if there is noise on y, then this identification algorithm is observed to underestimate Kp and Ti.
        /// This is the motivation for including the ability to include a filter on y
        /// This method also takes pidScaling into account!
        /// </summary>
        /// <param name="dataSet">dataset to filter over</param>
        /// <param name="isPIDoutputDelayOneSample"></param>
        /// <param name="pidFilterParams">optional filter to apply to y</param>
        /// <param name="doFilterUmeas"> if set to true, the measurement of the manipulated variable will be filtered</param>
        /// <param name="ignoreFlatLines">if set to true, indices with oversampled data will be ignored.
        /// This helps identification if there are periods with flatlined data that are oversampled,
        /// but will give incorrect Ti if the entire dataset is oversampled.</param>
        /// 
        /// <returns></returns>
        private (PidParameters, double[,], List<int>) IdentifyInternal(UnitDataSet dataSet, bool isPIDoutputDelayOneSample,
            PidFilterParams pidFilterParams = null, bool doFilterUmeas=false/*, bool ignoreFlatLines=true*/)
        {
            this.timeBase_s = dataSet.GetTimeBase();
            PidParameters pidParam = new PidParameters();
            if (pidFilterParams != null)
            {
                pidFilter = new PidFilter(pidFilterParams, timeBase_s);
                pidParam.Filtering = pidFilterParams;
            }
           
            pidParam.Fitting = new FittingInfo();
            if (pidScaling!=null)
                pidParam.Scaling = pidScaling;
            else
                pidParam.Scaling = new PidScaling();//default scaling
            if (dataSet.Y_setpoint == null)
            {
                pidParam.Fitting.WasAbleToIdentify = false;
                pidParam.AddWarning(PidIdentWarning.NotPossibleToIdentifyPIDcontroller_YsetIsBad);
                return (pidParam, null, null);
            }
            else if (vec.IsAllNaN(dataSet.Y_setpoint))
            {
                pidParam.Fitting.WasAbleToIdentify = false;
                pidParam.AddWarning(PidIdentWarning.NotPossibleToIdentifyPIDcontroller_YsetIsBad);
                return (pidParam, null, null);
            }

            double[] e_unscaled = GetErrorTerm(dataSet, pidFilter);
            /*
            if (pidParam.Scaling.IsDefault())
            {
                double umin, umax;
                DetermineUminUmax(dataSet, out umin, out umax);
                // TODO: the check for this should be better.
                bool foundEstimatedUmax = false;
                if (umax < 100)
                {
                    pidParam.AddWarning(PidIdentWarning.InputSaturation_UcloseToUmax);
                    foundEstimatedUmax = true;
                }
                if (umin > 0)
                {
                    pidParam.AddWarning(PidIdentWarning.InputSaturation_UcloseToUmin);
                    foundEstimatedUmax = true;
                }
                if (foundEstimatedUmax)
                {
                    pidScaling.SetEstimatedUminUmax(umin, umax);
                }
            }
            */
            int bufferLength = dataSet.GetNumDataPoints() - 1;

            double[] uMinusFF = GetUMinusFF(dataSet);
            if (doFilterUmeas && pidFilter != null)
            {
                int kernelLength = (int)Math.Floor(pidFilter.GetParams().TimeConstant_s / timeBase_s);
                uMinusFF = (new Vec()).NonCausalSmooth(uMinusFF, kernelLength);
            }

            double uRange = vec.Max(uMinusFF) - vec.Min(uMinusFF);
            double[] X1_ols = new double[bufferLength];
            double[] X2_ols = new double[bufferLength];
            double[] Y_ols = new double[bufferLength];

            int nIndexesBetweenWindows = (int)Math.Floor((double)dataSet.GetNumDataPoints() / 1);
            double Kpest, Tiest, objFunEst, Rsq;
            DateTime t_result ;
            int ind_result ;
            double[] yMod ;

             RegressionResults regressResults = null;
            List<int> indicesToIgnore = new List<int>();

            int nSamplesToLookBack = 0;
            if (isPIDoutputDelayOneSample)
            {
                nSamplesToLookBack = 1;
            }

            {
                int nIterationsToLookBack = 2;//since eprev and eprevprev are needed, look two iterations back.

                int idxStart = nIterationsToLookBack;
                int idxEnd = dataSet.GetNumDataPoints() - 1;
             
                double[] ucur, uprev, ecur, eprev;

                double[] e_scaled;
                double yScaleFactor = 1;
                if (pidParam.Scaling.IsKpScalingOn())
                {
                    yScaleFactor = 1 / pidScaling.GetKpScalingFactor();
                }
                if (yScaleFactor == 1)
                {
                    e_scaled = e_unscaled;
                }
                else
                {
                    e_scaled = vec.Multiply(e_unscaled, yScaleFactor);
                }
                ucur = Vec<double>.SubArray(uMinusFF, idxStart, idxEnd);
                uprev = Vec<double>.SubArray(uMinusFF, idxStart - 1, idxEnd - 1);
                
                ecur = Vec<double>.SubArray(e_scaled, idxStart - nSamplesToLookBack, idxEnd - nSamplesToLookBack);
                eprev = Vec<double>.SubArray(e_scaled, idxStart - 1 - nSamplesToLookBack, idxEnd - 1 - nSamplesToLookBack);

                // replace -9999 in dataset
                List<int> indBadUcur = SysIdBadDataFinder.GetAllBadIndicesPlussNext(ucur,dataSet.BadDataID);
                List<int> indBadEcur = vec.FindValues(ecur, badValueIndicatingValue, VectorFindValueType.Equal);
                List<int> indBadEprev = Index.Subtract(indBadEcur.ToArray(), 1).ToList();

                List<int> umaxInd = Index.AppendTrailingIndices(vec.FindValues(ucur, pidParam.Scaling.GetUmax(), VectorFindValueType.BiggerOrEqual));
                List<int> uminInd = Index.AppendTrailingIndices(vec.FindValues(ucur, pidParam.Scaling.GetUmin(), VectorFindValueType.SmallerOrEqual));

                // Anti-surge controllers: in PI-control only if the compressor operates between the control line and the surge line.
                /*
                if (type == ControllerType.AntiSurge)
                {
                    List<int> indAntiSurgeKicks = PIDantiSurgeID.FindKicks(ucur, indicesToIgnore, TimeBase_s,
                        out int nKicksFound, out double? estFFdownRampLimit_prcPerMin);
                    indicesToIgnore = indicesToIgnore.Union(indAntiSurgeKicks).ToList();
                    //   result.antiSurgeParams      = PIDantiSurgeID.Ident(ecur, ucur, indicesToIgnore, TimeBase_s);//TODO: find these values!
                    result.antiSurgeParams = new AntiSurgePIDParams(50 / TimeBase_s,
                        estFFdownRampLimit_prcPerMin, nKicksFound);
                    // for anti-surge "ymeas" contains "CtrlDev" and yset=0.
                    //   List<int> indAntiSurge = Vec.FindValues(ecur, result.antiSurgeParams.kickBelowThresholdE, FindValues.SmallerThan);
                }
                */
                indicesToIgnore = indicesToIgnore.Union(indBadUcur).ToList();
                indicesToIgnore = indicesToIgnore.Union(indBadEcur).ToList();
                indicesToIgnore = indicesToIgnore.Union(indBadEprev).ToList();
                //TODO: need to remove indice if _previous_ value was umax or umin
                indicesToIgnore = indicesToIgnore.Union(umaxInd).ToList();
                indicesToIgnore = indicesToIgnore.Union(uminInd).ToList();
                indicesToIgnore.Sort();

             /*   if (indicesToIgnore.Count() > ucur.Count() * 0.5)
                {
                    pidParam.Fitting.WasAbleToIdentify = false;
                    pidParam.AddWarning(PidIdentWarning.NotPossibleToIdentifyPIDcontroller_BadInputData);
                    return (pidParam, null, null);
                }*/

             /*   if (ignoreFlatLines)
                {
                    // Identify oversampled data
                    List<int> indSameUcur = vec.FindValues(ucur, badValueIndicatingValue, VectorFindValueType.SameAsPrevious);
                    List<int> indSameEcur = vec.FindValues(ecur, badValueIndicatingValue, VectorFindValueType.SameAsPrevious);
                    // A delay in one of the signals causes more indices to need filtering out
                    if (nSamplesToLookBack != 0)
                    {
                        var moreIndSameUcur = new List<int>();
                        for (int i = 0; i < indSameUcur.Count; i++)
                        {
                            if (indSameUcur[i] + nSamplesToLookBack < ucur.Count())
                            {
                                moreIndSameUcur.Add(indSameUcur[i] + nSamplesToLookBack);
                            }
                        }
                        indSameUcur = indSameUcur.Union(moreIndSameUcur).ToList();
                        var moreIndSameEcur = new List<int>();
                        for (int i = 0; i < indSameEcur.Count; i++)
                        {
                            if (indSameEcur[i] - nSamplesToLookBack >= 0)
                            {
                                moreIndSameEcur.Add(indSameEcur[i] - nSamplesToLookBack);
                            }
                        }
                        indSameEcur = indSameEcur.Union(moreIndSameEcur).ToList();
                    }
                    List<int> indOversampled = indSameUcur.Intersect(indSameEcur).ToList();

                    // ignore oversampled indices as well.
                    indicesToIgnore = indicesToIgnore.Union(indOversampled).ToList();
                    indicesToIgnore.Sort();
                }*/
                Y_ols = vec.Subtract(ucur, uprev);
                X1_ols = vec.Subtract(ecur, eprev);
                X2_ols = vec.Multiply(ecur, timeBase_s);

                double[][] inputs = { X1_ols, X2_ols };
                // important: use the un-regularized solver here!!
                regressResults = vec.RegressUnRegularized(Y_ols, inputs, indicesToIgnore.ToArray());
                //  out double[] notUsed, out double[] Y_mod, out double Rsq_cur;
                var b = regressResults.Param;

                Rsq = regressResults.Rsq;
                yMod = regressResults.Y_modelled;

                if (b == null)
                {
                    Tiest = badValueIndicatingValue;
                    Kpest = badValueIndicatingValue;
                    pidParam.Fitting.WasAbleToIdentify = false;
                    pidParam.AddWarning(PidIdentWarning.RegressionProblemFailedToYieldSolution);
                    return (pidParam, null, null);
                }
                else
                {
                    Kpest = -b[0];
                }

                double eMax = vec.Max(ecur);
                double eMin = vec.Min(ecur);
                double uMax = vec.Max(ucur);
                double uMin = vec.Min(ucur);
                
                double expectedRoughtEstimateForKp = Math.Abs((uMax - uMin) / (eMax - eMin));
                if (Math.Abs(Kpest) < expectedRoughtEstimateForKp * CUTOFF_FOR_GUESSING_PID_IN_MANUAL_FRAC)
                {
                    pidParam.AddWarning(PidIdentWarning.PIDControllerDoesNotAppearToBeInAuto);
                    pidParam.Fitting.WasAbleToIdentify = false;
                    return (pidParam, null, null);
                }

                if (b[1] == 0)
                {
                    // this seems to happen if for instance the entire Y is constant, for instance in case 
                    // of input saturation, or if the controller is in manual the entire time or it is the wrong signal
                    pidParam.AddWarning(PidIdentWarning.NotPossibleToIdentifyPIDcontroller_UAppearsUncorrelatedWithY);
                    Tiest = badValueIndicatingValue;
                }
                else
                    Tiest = Math.Abs( b[0] / b[1]);

                if (dataSet.Times != null)
                {
                    t_result = dataSet.Times[idxEnd];
                }
                if (Double.IsNaN(Kpest) || Double.IsNaN(Tiest))
                {
                    pidParam.Fitting.WasAbleToIdentify = false;
                    pidParam.AddWarning(PidIdentWarning.RegressionProblemFailedToYieldSolution);
                    return (pidParam, null, null);
                }

            }
            // The ignored indices above are for the arrays used for identification.
            // For evaluation and simulation, slightly larger arrays are used, and other indices must be found.
            List<int> indicesToIgnoreForEvalSim = new List<int>();

            List<int> indBadU = new List<int>();
            for (int i = 0; i < dataSet.U.GetNColumns(); i++)
            {
                indBadU = indBadU.Union(vec.FindValues(dataSet.U.GetColumn(i),dataSet.BadDataID, VectorFindValueType.Equal)).ToList();
            }
            List<int> indBadY_meas = vec.FindValues(dataSet.Y_meas,dataSet.BadDataID, VectorFindValueType.Equal).ToList();

            indicesToIgnoreForEvalSim = indicesToIgnoreForEvalSim.Union(indBadU).ToList();
            indicesToIgnoreForEvalSim = indicesToIgnoreForEvalSim.Union(indBadY_meas).ToList();
            
           /* if (ignoreFlatLines)
            {
                // Identify oversampled data
                List<int> indSameU = new List<int>();
                for (int i = 0; i < dataSet.U.GetNColumns(); i++)
                {
                   indSameU = indSameU.Union(vec.FindValues(dataSet.U.GetColumn(i), badValueIndicatingValue, VectorFindValueType.SameAsPrevious)).ToList();
                }
                List<int> indSameYmeas = vec.FindValues(dataSet.Y_meas, badValueIndicatingValue, VectorFindValueType.SameAsPrevious);
                List<int> indSameYsetpoint = vec.FindValues(dataSet.Y_setpoint, badValueIndicatingValue, VectorFindValueType.SameAsPrevious);
                List<int> indOversampled = indSameU.Intersect(indSameYmeas).ToList();
                indOversampled = indOversampled.Intersect(indSameYsetpoint).ToList();

                indicesToIgnoreForEvalSim = indicesToIgnoreForEvalSim.Union(indOversampled).ToList();
                indicesToIgnoreForEvalSim.Sort();
            }*/

            // see if using "next value= last value" gives better objective function than the model found"
            // if so it is an indication that something is wrong
            // if the data is "only noise" then the two can be very close even if the model is good, for that reason add
            // a little margin 
            // if there is too little variation in U, then Kp tends to be close to zero.

            const int nDigits = 7;
            const int nDigitsParams = 4;

            pidParam.Kp = Kpest;
            pidParam.Ti_s = Tiest;
            pidParam.Fitting.SolverID = "PidIdentifier v1.0";

            if (dataSet.Times.Count() > 0)
            {
                pidParam.Fitting.StartTime = dataSet.Times.First();
                pidParam.Fitting.EndTime = dataSet.Times.Last();
            }
            if (regressResults == null)
            {
                pidParam.Fitting.WasAbleToIdentify = false;
                return (pidParam, null, null);
            }

            double[,] U_sim = Array2D<double>.Create(GetSimulatedU(pidParam, dataSet, isPIDoutputDelayOneSample, indicesToIgnoreForEvalSim));
            pidParam.Fitting.WasAbleToIdentify = true;
            dataSet.U_sim = U_sim;

            // If the measured and simulated signals end up being inversely correlated, the sign of the Kp parameter
            // can be flipped to produce a simulated signal that is positively correlated with the measured signal.
         /*   if (vec.RSquared(dataSet.U.GetColumn(0), U_sim.GetColumn(0), indicesToIgnoreForEvalSim, 0) < -0.1)
            {
                double oldFitScore = FitScoreCalculator.Calc(dataSet.U.GetColumn(0), U_sim.GetColumn(0), indicesToIgnoreForEvalSim);
                pidParam.Kp = -pidParam.Kp;
                U_sim = Array2D<double>.Create(GetSimulatedU(pidParam, dataSet, isPIDoutputDelayOneSample, indicesToIgnoreForEvalSim));
                double newFitScore = FitScoreCalculator.Calc(dataSet.U.GetColumn(0), U_sim.GetColumn(0), indicesToIgnoreForEvalSim);
                //todo: find out why this sometimes fails and needs to be reverted
                if (oldFitScore > newFitScore)
                {
                    pidParam.Kp = -pidParam.Kp;
                    U_sim = Array2D<double>.Create(GetSimulatedU(pidParam, dataSet, isPIDoutputDelayOneSample, indicesToIgnoreForEvalSim));
                }
                dataSet.U_sim = U_sim;
            }*/

            pidParam.Kp = SignificantDigits.Format(pidParam.Kp, nDigitsParams);
            pidParam.Ti_s = SignificantDigits.Format(pidParam.Ti_s, nDigitsParams);
            pidParam.Td_s = SignificantDigits.Format(pidParam.Td_s, nDigitsParams);

            pidParam.Fitting.TimeBase_s = dataSet.GetTimeBase();
            pidParam.Fitting.StartTime = dataSet.Times.First();
            pidParam.Fitting.EndTime = dataSet.Times.Last();
            pidParam.Fitting.Umin = new double[] { SignificantDigits.Format(vec.Min(dataSet.U.GetColumn(0)), nDigitsParams) };
            pidParam.Fitting.Umax = new double[] { SignificantDigits.Format(vec.Max(dataSet.U.GetColumn(0)), nDigitsParams) };

            pidParam.Fitting.NFittingTotalDataPoints = regressResults.NfittingTotalDataPoints;
            pidParam.Fitting.NFittingBadDataPoints = regressResults.NfittingBadDataPoints;
      
            pidParam.Fitting.RsqDiff = regressResults.Rsq;
            pidParam.Fitting.ObjFunValDiff = regressResults.ObjectiveFunctionValue;
            pidParam.Fitting.FitScorePrc = SignificantDigits.Format(FitScoreCalculator.Calc(dataSet.U.GetColumn(0), dataSet.U_sim.GetColumn(0), indicesToIgnoreForEvalSim), nDigits);
            
            pidParam.Fitting.ObjFunValAbs  = vec.SumOfSquareErr(dataSet.U.GetColumn(0), dataSet.U_sim.GetColumn(0), 0);
            pidParam.Fitting.RsqAbs = vec.RSquared(dataSet.U.GetColumn(0), dataSet.U_sim.GetColumn(0), indicesToIgnoreForEvalSim, 0) * 100;

            pidParam.Fitting.RsqAbs = SignificantDigits.Format(pidParam.Fitting.RsqAbs, nDigits);
            pidParam.Fitting.RsqDiff = SignificantDigits.Format(pidParam.Fitting.RsqDiff, nDigits);
            pidParam.Fitting.ObjFunValDiff = SignificantDigits.Format(pidParam.Fitting.ObjFunValDiff, nDigits);
            pidParam.Fitting.ObjFunValAbs = SignificantDigits.Format(pidParam.Fitting.ObjFunValAbs, nDigits);

            pidParam.DelayOutputOneSample = isPIDoutputDelayOneSample;
            // fitting abs?
            return (pidParam, dataSet.U_sim, indicesToIgnoreForEvalSim);
        }


        /// <summary>
        /// Returns the simulated time series of the manipulated variable u as given by the PID-controller.
        /// </summary>
        /// <param name="pidParams"></param>
        /// <param name="dataset"></param>
        /// <param name="isPIDoutputDelayOneSample"></param>
        /// <param name="indToIgnore"></param>
        /// <returns></returns>
        public double[] GetSimulatedU(PidParameters pidParams, UnitDataSet dataset,bool isPIDoutputDelayOneSample, List<int> indToIgnore = null)
        {
            var pidModel = new PidModel(pidParams, "pid");
            (var isOk,var simulatedU) =  PlantSimulatorHelper.SimulateSingle(dataset, pidModel);
            dataset.U_sim = Array2D<double>.CreateFromList(new List<double[]> { simulatedU });
            return simulatedU;

            /*
            const bool DO_GAP_SKIPPING = false;

            int firstGoodDataPointToStartSimIdx = 0;
            while ((dataset.Y_setpoint[firstGoodDataPointToStartSimIdx] == badValueIndicatingValue ||
                dataset.Y_meas[firstGoodDataPointToStartSimIdx] == badValueIndicatingValue ||
                dataset.U[firstGoodDataPointToStartSimIdx,0] == badValueIndicatingValue ||
                indToIgnore.Contains(firstGoodDataPointToStartSimIdx)) &&
                 firstGoodDataPointToStartSimIdx < dataset.Y_meas.Length - 2)
            {
                firstGoodDataPointToStartSimIdx++;
            }
            double u_init = dataset.U[firstGoodDataPointToStartSimIdx,0];

            Double[] simulatedU = new Double[dataset.Y_setpoint.Length];

            PidController pid = new PidController(timeBase_s);
            pid.SetKp(pidParams.Kp);
            pid.SetTi(pidParams.Ti_s);
            pid.SetTd(pidParams.Td_s);
            pid.SetScaling(pidParams.Scaling);

            pid.WarmStart(dataset.Y_meas[firstGoodDataPointToStartSimIdx], 
                dataset.Y_setpoint[firstGoodDataPointToStartSimIdx], u_init);

            int samplesToDelayOutput = 0;
            if (isPIDoutputDelayOneSample)
            {
                simulatedU[0] = u_init;
                samplesToDelayOutput = 1;
            }
            if (firstGoodDataPointToStartSimIdx > 0)
            {
                for (int i = 0; i < firstGoodDataPointToStartSimIdx; i++)
                {
                    simulatedU[i] = u_init;
                }
            }
            if (DO_GAP_SKIPPING)
            {
                double lastGoodU = 0, nextU;
                int gapSize = 0;
                int gapSizeLimit = 1; // Number of consecutive indicies to ignore needed to trigger warmstarting the simulator.
                for (int i = firstGoodDataPointToStartSimIdx; i < Math.Min(simulatedU.Length - samplesToDelayOutput, dataset.Y_meas.Length); i++)
                {
                    // Check if there is bad data
                    if (dataset.Y_meas[i] == badValueIndicatingValue || Double.IsNaN(dataset.Y_meas[i]) 
                        || Double.IsInfinity(dataset.Y_meas[i]) || indToIgnore.Contains(i))
                    {
                        nextU = lastGoodU;
                        gapSize++;
                    }
                    else
                    {
                        // If there is a considerable consecutive data gap, the pid should be warm-started again with the conditions after the gap.
                        if (gapSize > gapSizeLimit)
                        {
                            nextU = dataset.U[i, 0];
                            // If the next index is not ignored, it can be used to better start the pid
                            if (i + 1 < simulatedU.Length)
                            {
                                if (!indToIgnore.Contains(i + 1))
                                {
                                    simulatedU[i] = nextU;
                                    pid.WarmStart(dataset.Y_meas[i + 1],
                                    dataset.Y_setpoint[i + 1], dataset.U[i + 1, 0],
                                    dataset.Y_meas[i], dataset.Y_setpoint[i]);
                                    nextU = dataset.U[i + 1, 0];
                                    simulatedU[i + 1] = nextU;
                                    lastGoodU = nextU;
                                    continue;
                                }
                            }
                            else
                            {
                                pid.WarmStart(dataset.Y_meas[i],
                                dataset.Y_setpoint[i], dataset.U[i, 0]);
                            }
                        }
                        else
                        {
                            nextU = pid.Iterate(dataset.Y_meas[i], dataset.Y_setpoint[i]);
                        }
                        // Handle special cases when there is a delayed ouput
                        if (samplesToDelayOutput > 0)
                        {
                            // Before bad data
                            if (i + samplesToDelayOutput < simulatedU.Length)
                            {
                                if (indToIgnore.Contains(i + samplesToDelayOutput))
                                {
                                    nextU = lastGoodU;
                                }
                            }
                        }
                        gapSize = 0;
                    }
                    lastGoodU = nextU;
                    simulatedU[i + samplesToDelayOutput] = nextU;
                }
                return simulatedU;
            }
            else
            {
                double lastGoodU = 0, nextU;
                for (int i = firstGoodDataPointToStartSimIdx; i < Math.Min(simulatedU.Length - samplesToDelayOutput, dataset.Y_meas.Length); i++)
                {
                    if (dataset.Y_meas[i] == -9999 || Double.IsNaN(dataset.Y_meas[i]) || Double.IsInfinity(dataset.Y_meas[i]))
                    {
                        nextU = lastGoodU;
                    }
                    else
                    {
                        nextU = pid.Iterate(dataset.Y_meas[i], dataset.Y_setpoint[i]);
                        lastGoodU = nextU;
                    }
                    simulatedU[i + samplesToDelayOutput] = nextU;
                }
                return simulatedU;
            }
           */
        }
    }

}


