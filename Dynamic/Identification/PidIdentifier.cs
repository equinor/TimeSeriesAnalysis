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
            // TODO: would ideally like to remove reliance on other scores than FitScore, but setting the below to true 
            // causes some unit tests to fail
            const bool useOnlyFitScore = false; //would like: true

            if (useOnlyFitScore)
            {
                if (firstModel.Fitting.FitScorePrc > secondModel.Fitting.FitScorePrc)
                    return true;
                else
                    return false;
            }
            else
            {
                    // If both models show a very high R-Squared-diff, look at fitscore instead if there is a significant difference
                    if (firstModel.Fitting.RsqDiff > MAX_RSQUARED_DIFF_BEFORE_COMPARING_FITSCORE
                        && secondModel.Fitting.RsqDiff > MAX_RSQUARED_DIFF_BEFORE_COMPARING_FITSCORE)
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
        }

        /// <summary>
        /// Identifies a PID-controller from a UnitDataSet
        /// </summary>
        /// <param name="dataSet">a UnitDataSet, where .Y_meas, .Y_setpoint and .U are analyzed</param>
        /// <param name="downsampleOversampledData">Boolean, whether to do internal oversample identification and attempt downsampling. Defaults to true.</param>
        /// <returns>the identified parameters of the PID-controller</returns>
        public PidParameters Identify(ref UnitDataSet dataSet, bool downsampleOversampledData = true)
        {
         //   const bool doOnlyWithDelay = false;// should be false unless debugging something
            const bool doFiltering = true; // default is true (this improves performance significantly)
            const bool returnFilterParameters = false; // even if filtering helps improve estimates, maybe the filter should not be returned?


            // 1. try identification with delay of one sample but without filtering
            (var idParamsWithDelay, var U_withDelay, var indicesToIgnore_withDelay)
                = IdentifyInternal(dataSet, true);
   
   /*         if (doOnlyWithDelay)
            {
                dataSet.U_sim = U_withDelay;
                return results_withDelay;
            }*/

            // 2. try identification without delay of one sample (yields better results often if dataset is downsampled
            //    relative to the clock that the pid algorithm ran on originally)
            (var idParamsWithoutDelay, var U_withoutDelay, var indicesToIgnore_withoutDelay) = 
                IdentifyInternal(dataSet, false);

            // save which is the "best" estimate for comparison 
            bool doDelay = true;
            var bestPidParameters= idParamsWithoutDelay;
            double[,] bestU = U_withoutDelay;
            var  bestIndicesToIgnore = indicesToIgnore_withoutDelay;
            if (IsFirstModelBetterThanSecondModel(idParamsWithDelay, idParamsWithoutDelay))
            {
                doDelay = true;
                bestPidParameters = idParamsWithDelay;
                bestU = U_withDelay;
                bestIndicesToIgnore = indicesToIgnore_withDelay;

            }
            else
            {
                doDelay = false;
                bestPidParameters = idParamsWithoutDelay;
                bestU = U_withoutDelay;
                bestIndicesToIgnore = indicesToIgnore_withoutDelay;
            }

            if (doFiltering)
            {
                // 3. try filtering y_meas and see if this improves fit 
                // if there is noise on y_meas that is not filtered, this may cause too small Kp/Ti
                double maxFilterTime_s = 6 * timeBase_s;
                for (double filterTime_s = timeBase_s; filterTime_s < maxFilterTime_s; filterTime_s += timeBase_s)
                {
                    var pidFilterParams = new PidFilterParams(true, 1, filterTime_s);
                    (var idParamsWithFilter, var U_withFilter, var indicesToIgnoreWithFilter) = 
                        IdentifyInternal(dataSet, doDelay, pidFilterParams/*, ignoreFlatLines: ignoreFlatLinesFirst*/);

                    if (IsFirstModelBetterThanSecondModel(idParamsWithFilter, bestPidParameters))
                    {
                        bestU = U_withFilter;
                        bestPidParameters = idParamsWithFilter;
                        bestIndicesToIgnore = indicesToIgnoreWithFilter;
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
                    (var idParamsWithFilter, var UwithFilter, var indicesToIgnore_withFilter) =
                        IdentifyInternal(dataSet, doDelay, pidFilterParams, filterUmeas);

                    if (IsFirstModelBetterThanSecondModel(idParamsWithFilter, bestPidParameters))
                    {
                        bestU = UwithFilter;
                        bestPidParameters = idParamsWithFilter;
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
                        var idParamsDownsampled = Identify(ref dataSetDownsampled);
                        if (IsFirstModelBetterThanSecondModel(idParamsDownsampled, bestPidParameters))
                        {
                            bestU = dataSetDownsampled.U_sim;
                            dataSet = dataSetDownsampled;
                            bestPidParameters = idParamsDownsampled;
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
        /// This helps identification if there are periods with flatlined data that are oversampled,
        /// but will give incorrect Ti if the entire dataset is oversampled.</param>
        /// 
        /// <returns></returns>
        private (PidParameters, double[,], List<int>) IdentifyInternal(UnitDataSet dataSet, bool isPIDoutputDelayOneSample,
            PidFilterParams pidFilterParams = null, bool doFilterUmeas=false)
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

            double Kpest, Tiest, Rsq; 
            DateTime t_result ;
            double[] yMod ;

            RegressionResults regressResults = null;
            
            //
            // Note that these are the indices to be fed to the regression, and does not 1-to-1 conicide with indices numbering of the
            // given dataset without conversion!
            // 

            var  indicesToIgnoreInternal = new List<int>();

            int nSamplesToLookBack = 0;
            if (isPIDoutputDelayOneSample)
            {
                nSamplesToLookBack = 1;
            }

            int nIterationsToLookBack = 2;//since eprev and eprevprev are needed, look two iterations back.
            {
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

                // replace -9999 or NaNs in dataset
                var indBadUcur = BadDataFinder.GetAllBadIndicesPlussNext(ucur,dataSet.BadDataID);
                var indBadEcur = Index.Shift(BadDataFinder.GetAllBadIndicesPlussNext(ecur, dataSet.BadDataID).ToArray(), -nSamplesToLookBack);

                var umaxInd = Index.AppendTrailingIndices(vec.FindValues(ucur, pidParam.Scaling.GetUmax(), VectorFindValueType.BiggerOrEqual));
                var uminInd = Index.AppendTrailingIndices(vec.FindValues(ucur, pidParam.Scaling.GetUmin(), VectorFindValueType.SmallerOrEqual));

                // var indToIgnoreFromChooser =  Index.Shift(DataIndicesToIgnoreChooser.ChooseIndicesToIgnore(dataSet, detectBadData: false,detectFrozenData: true).ToArray(), -nIterationsToLookBack);
                var indToIgnoreFromChooser = DataIndicesToIgnoreChooser.ChooseIndicesToIgnore(dataSet, detectBadData: false, detectFrozenData: true).ToArray();

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
                indicesToIgnoreInternal = indicesToIgnoreInternal.Union(indBadUcur).ToList();
                indicesToIgnoreInternal = indicesToIgnoreInternal.Union(indBadEcur).ToList();
                //TODO: need to remove indice if _previous_ value was umax or umin
                indicesToIgnoreInternal = indicesToIgnoreInternal.Union(umaxInd).ToList();
                indicesToIgnoreInternal = indicesToIgnoreInternal.Union(uminInd).ToList();
                indicesToIgnoreInternal = indicesToIgnoreInternal.Union(indToIgnoreFromChooser).ToList();
                indicesToIgnoreInternal.Sort();

                Y_ols = vec.Subtract(ucur, uprev);
                X1_ols = vec.Subtract(ecur, eprev);
                X2_ols = vec.Multiply(ecur, timeBase_s);

                double[][] inputs = { X1_ols, X2_ols };
                // important: use the un-regularized solver here!!
                regressResults = vec.RegressUnRegularized(Y_ols, inputs, indicesToIgnoreInternal.ToArray());
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
       /*     List<int> indicesToIgnoreForEvalSim = new List<int>();

            List<int> indBadU = new List<int>();
            for (int i = 0; i < dataSet.U.GetNColumns(); i++)
            {
                indBadU = indBadU.Union(vec.FindValues(dataSet.U.GetColumn(i),dataSet.BadDataID, VectorFindValueType.Equal)).ToList();
            }
            List<int> indBadY_meas = vec.FindValues(dataSet.Y_meas,dataSet.BadDataID, VectorFindValueType.Equal).ToList();

            indicesToIgnoreForEvalSim = indicesToIgnoreForEvalSim.Union(indBadU).ToList();
            indicesToIgnoreForEvalSim = indicesToIgnoreForEvalSim.Union(indBadY_meas).ToList();
            */
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

            dataSet.IndicesToIgnore = Index.Shift(indicesToIgnoreInternal.ToArray(), nIterationsToLookBack).ToList();
            (var u_sim, int numSimRestarts) = GetSimulatedU(pidParam, dataSet, isPIDoutputDelayOneSample);
            double[,] U_sim = Array2D<double>.Create(u_sim);

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
            pidParam.Fitting.ObjFunValDiff = regressResults.ObjectiveFunctionValue;//remove? does not include indicesToIgnore?

    
            pidParam.Fitting.FitScorePrc = SignificantDigits.Format(FitScoreCalculator.Calc(dataSet.U.GetColumn(0), dataSet.U_sim.GetColumn(0),
                dataSet.IndicesToIgnore,nIterationsToLookBack), nDigits);
            
            pidParam.Fitting.ObjFunValAbs  = vec.SumOfSquareErr(dataSet.U.GetColumn(0), dataSet.U_sim.GetColumn(0), 0);//remove? does not include indicesToIgnore?
            pidParam.Fitting.RsqAbs = vec.RSquared(dataSet.U.GetColumn(0), dataSet.U_sim.GetColumn(0), indicesToIgnoreInternal, 0) * 100;

            pidParam.Fitting.RsqAbs = SignificantDigits.Format(pidParam.Fitting.RsqAbs, nDigits);
            pidParam.Fitting.RsqDiff = SignificantDigits.Format(pidParam.Fitting.RsqDiff, nDigits);
            pidParam.Fitting.ObjFunValDiff = SignificantDigits.Format(pidParam.Fitting.ObjFunValDiff, nDigits);
            pidParam.Fitting.ObjFunValAbs = SignificantDigits.Format(pidParam.Fitting.ObjFunValAbs, nDigits);
            pidParam.Fitting.NumSimulatorRestarts = numSimRestarts;

            pidParam.DelayOutputOneSample = isPIDoutputDelayOneSample;
            // fitting abs?
            return (pidParam, dataSet.U_sim, dataSet.IndicesToIgnore);
        }


        /// <summary>
        /// Returns the simulated time series of the manipulated variable u as given by the PID-controller.
        /// </summary>
        /// <param name="pidParams"></param>
        /// <param name="dataset"> dataset, including including the "indicesToIgnore"</param>
        /// <param name="isPIDoutputDelayOneSample"></param>
        /// <returns>the simulated value, and the number of restarts</returns>
        public (double[],int) GetSimulatedU(PidParameters pidParams, UnitDataSet dataset,bool isPIDoutputDelayOneSample)
        {
            var pidModel = new PidModel(pidParams, "pid");
            (var isOk,var simulatedU, int numSimRestarts) =  PlantSimulatorHelper.SimulateSingle(dataset, pidModel);
            dataset.U_sim = Array2D<double>.CreateFromList(new List<double[]> { simulatedU });
            return (simulatedU,numSimRestarts);
        }
    }

}


