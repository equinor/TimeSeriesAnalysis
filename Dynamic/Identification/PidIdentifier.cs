using Accord.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Dynamic;
using System.ComponentModel.Design;
using Accord.Collections;

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
        /// <returns>the identified parameters of the PID-controller</returns>
        public PidParameters Identify(ref UnitDataSet dataSet)
        {
            //double[] usimFrozen, usimUnfrozen;

            // try both with and without frozen data detection, and pick the best result
            List<int> indToIgnoreOrig = null;
            if (dataSet.IndicesToIgnore != null)
                 indToIgnoreOrig = new List<int>(dataSet.IndicesToIgnore);
            var paramWithoutFrozen = Identify_Level1(ref dataSet, doDetectFrozenData: false);
          //  usimUnfrozen = dataSet.U_sim.GetColumn(0); ;
            dataSet.IndicesToIgnore = indToIgnoreOrig;
            var paramWithFrozen =  Identify_Level1(ref dataSet,doDetectFrozenData: true);

          //  return paramWithFrozen;//debug, TODO:Remove

            /* usimFrozen = dataSet.U_sim.GetColumn(0);
             Shared.EnablePlots();
             Plot.FromList(new List<double[]> { usimUnfrozen, usimFrozen, dataSet.U.GetColumn(0) },
                 new List<string> { "y1= u_simUnfrozen", "y1= u_simfronzen", "y1=umeas" },dataSet.Times);
             Shared.DisablePlots();
            */
            var fitScoreWithFrozen = paramWithFrozen.Fitting.FitScorePrc;
            var fitScoreWithoutFrozen = paramWithoutFrozen.Fitting.FitScorePrc;

            if (!paramWithFrozen.Fitting.WasAbleToIdentify && paramWithoutFrozen.Fitting.WasAbleToIdentify)
                return paramWithoutFrozen;
            if (paramWithFrozen.Fitting.WasAbleToIdentify && !paramWithoutFrozen.Fitting.WasAbleToIdentify)
                return paramWithFrozen;

            if (fitScoreWithoutFrozen > fitScoreWithFrozen)
            {
                return paramWithoutFrozen;
            }
            else
            {
                return paramWithFrozen;
            }
           
        }

        /// <summary>
        /// Identifies a PID-controller from a UnitDataSet
        /// </summary>
        /// <param name="dataSet">a UnitDataSet, where .Y_meas, .Y_setpoint and .U are analyzed</param>
        /// <param name="doDetectFrozenData">if true, the model attempts to find and ignore portions of frozen data(will also detect oversampled data)</param>
        /// <returns>the identified parameters of the PID-controller</returns>
        public PidParameters Identify_Level1(ref UnitDataSet dataSet, bool doDetectFrozenData)
        {
            const bool doFiltering = true; // default is true (this improves performance significantly)
            const bool returnFilterParameters = false; // even if filtering helps improve estimates, the filter should normally not be returned
            const bool doFlipKp = true; // test if flipping the sign of Kp produces a better fit score.

            // 1. try identification with delay of one sample but without filtering
            (var idParamsWithDelay, var U_withDelay, var indicesToIgnore_withDelay)
                = Identify_Level2(dataSet, true,null, false, doDetectFrozenData);

            // 2. try identification without delay of one sample (yields better results often if dataset is downsampled
            //    relative to the clock that the pid algorithm ran on originally)
            dataSet.IndicesToIgnore = null;// nb! avoid re-using indices to ignore from step above..
            (var idParamsWithoutDelay, var U_withoutDelay, var indicesToIgnore_withoutDelay) = 
                Identify_Level2(dataSet, false, null, false, doDetectFrozenData);

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
                    dataSet.IndicesToIgnore = null;// nb! avoid re-using indices to ignore from step above..
                    (var idParamsWithFilter, var U_withFilter, var indicesToIgnoreWithFilter) = 
                        Identify_Level2(dataSet, doDelay, pidFilterParams, false, doDetectFrozenData);

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
                if (true)
                {
                    bool filterUmeas = true;
                    for (double filterTime_s = timeBase_s; filterTime_s < maxFilterTime_s; filterTime_s += timeBase_s)
                    {
                        var pidFilterParams = new PidFilterParams(true, 1, filterTime_s);
                        dataSet.IndicesToIgnore = null;// nb! avoid re-using indices to ignore from step above..
                        (var idParamsWithFilter, var UwithFilter, var indicesToIgnore_withFilter) =
                            Identify_Level2(dataSet, doDelay, pidFilterParams, filterUmeas, doDetectFrozenData);

                        if (IsFirstModelBetterThanSecondModel(idParamsWithFilter, bestPidParameters))
                        {
                            bestU = UwithFilter;
                            bestPidParameters = idParamsWithFilter;
                            bestIndicesToIgnore = indicesToIgnore_withFilter;
                        }
                    }
                }

                // 5. check if flipping the Kp produces a better Kp. 
                if (doFlipKp)
                {
                    var pidParamFlipped = new PidParameters(bestPidParameters);
                    pidParamFlipped.Kp = -bestPidParameters.Kp;

                    var uSimFlipped = Array2D<double>.Create(GetSimulatedU(pidParamFlipped, dataSet, bestPidParameters.DelayOutputOneSample, bestIndicesToIgnore).Item1);
                    double flippedFitScore = FitScoreCalculator.Calc(dataSet.U.GetColumn(0), uSimFlipped.GetColumn(0), dataSet.BadDataID, bestIndicesToIgnore);

                    if (flippedFitScore > 0 && flippedFitScore > bestPidParameters.Fitting.FitScorePrc)
                    {
                        if (pidParamFlipped.Fitting == null)
                            pidParamFlipped.Fitting = new FittingInfo();

                        pidParamFlipped.Fitting.WasAbleToIdentify = true;

                        if (pidParamFlipped.Fitting.SolverID == null)
                            pidParamFlipped.Fitting.SolverID = "PidIdentifier (Kp flipped)";
                        else
                            pidParamFlipped.Fitting.SolverID += "(Kp flipped)";

                        bestU = uSimFlipped;
                        bestPidParameters = pidParamFlipped;
                        bestPidParameters.Fitting.FitScorePrc = flippedFitScore;
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

                int kernelLength = (int)Math.Floor(pidFilter.GetParams().TimeConstant_s / timeBase_s);
                y_filt = (new Vec()).NonCausalSmooth(dataSet.Y_meas, kernelLength);
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
        /// <param name="doFilterUmeas"> if set to true, the measurement of the manipulated variable will be filtered
        /// This helps identification if there are periods with flatlined data that are oversampled,
        /// but will give incorrect Ti if the entire dataset is oversampled.</param>
        /// <param name="doDetectFrozenData">If true, the identification will try to identify periods where all data is frozen and ignore those portions. </param>
        /// <returns></returns>
        private (PidParameters, double[,], List<int>) Identify_Level2(UnitDataSet dataSet, bool isPIDoutputDelayOneSample,
            PidFilterParams pidFilterParams = null, bool doFilterUmeas=false, bool doDetectFrozenData = true)
        {
            const bool useConstantTimeBase = false;// default is true, false is experimental. 

            this.timeBase_s = dataSet.GetTimeBase();
            var pidParam = new PidParameters();
            string solverID = "PidIdentifier";
            if (useConstantTimeBase)
            {
                solverID += "(constant timebase)";
            }
            else
            {
                solverID += "(variable timebase)";
            }

            if (pidFilterParams != null)
            {
                pidFilter = new PidFilter(pidFilterParams, timeBase_s);
                pidParam.Filtering = pidFilterParams;
                solverID += "(ymeas filtered)";
            }
           
            pidParam.Fitting = new FittingInfo();
            if (pidScaling!=null)
                pidParam.Scaling = pidScaling;
            else
                pidParam.Scaling = new PidScaling();//default scaling
            if (dataSet.Y_setpoint == null)
            {
                pidParam.Fitting.WasAbleToIdentify = false;
                return (pidParam, null, null);
            }
            else if (vec.IsAllNaN(dataSet.Y_setpoint))
            {
                pidParam.Fitting.WasAbleToIdentify = false;
                return (pidParam, null, null);
            }

            double[] e_unscaled = GetErrorTerm(dataSet, pidFilter);
            int bufferLength = dataSet.GetNumDataPoints() - 1;
            double[] uMinusFF = GetUMinusFF(dataSet);
            if (doFilterUmeas && pidFilter != null)
            {
                int kernelLength = (int)Math.Floor(pidFilter.GetParams().TimeConstant_s / timeBase_s);
                uMinusFF = (new Vec()).NonCausalSmooth(uMinusFF, kernelLength);
                solverID += "(Umeas was filtered)";
            }
            double uRange = vec.Max(uMinusFF) - vec.Min(uMinusFF);
            double Kpest, Tiest, Rsq; 

            double[] yMod ;
            RegressionResults regressResults = null;
            var  indToIgnore = new List<int>();

            //
            // Note that these are the indices to be fed to the regression, and does not 1-to-1 conicide with indices numbering of the
            // given dataset without conversion!
            // 
            var  indicesToIgnoreReg = new List<int>();
            int nSamplesToLookBack = 0;
            if (isPIDoutputDelayOneSample)
            {
                nSamplesToLookBack = 1;
            }


            var goodIndices = new List<int>();

            //since eprev and eprevprev are needed, need to look two itertaions back, which means first to indices need to be ignored
            // in regression
            {
                int nIterationsToLookBack = 2;
                int idxStart = nIterationsToLookBack;
                int idxEnd = dataSet.GetNumDataPoints() - 1;

                // regression
                double[] ucurReg, uprevReg, ecurReg, eprevReg;

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
                ucurReg = Vec<double>.SubArray(uMinusFF, idxStart, idxEnd);
                uprevReg = Vec<double>.SubArray(uMinusFF, idxStart - 1, idxEnd - 1);

                ecurReg = Vec<double>.SubArray(e_scaled, idxStart - nSamplesToLookBack, idxEnd - nSamplesToLookBack);
                eprevReg = Vec<double>.SubArray(e_scaled, idxStart - 1 - nSamplesToLookBack, idxEnd - 1 - nSamplesToLookBack);

                // replace -9999 or NaNs in dataset
                var indBadUcurReg = BadDataFinder.GetAllBadIndicesPlussNext(ucurReg, dataSet.BadDataID);
                var indBadEcurReg = Index.Shift(BadDataFinder.GetAllBadIndicesPlussNext(ecurReg, dataSet.BadDataID).ToArray(), -nSamplesToLookBack);

                var umaxIndReg = Index.AppendTrailingIndices(vec.FindValues(ucurReg, pidParam.Scaling.GetUmax(), VectorFindValueType.BiggerOrEqual));
                var uminIndReg = Index.AppendTrailingIndices(vec.FindValues(ucurReg, pidParam.Scaling.GetUmin(), VectorFindValueType.SmallerOrEqual));
                var ymaxIndReg = Index.AppendTrailingIndices(vec.FindValues(ucurReg, pidParam.Scaling.GetYmax(), VectorFindValueType.BiggerOrEqual));
                var yminIndReg = Index.AppendTrailingIndices(vec.FindValues(ucurReg, pidParam.Scaling.GetYmin(), VectorFindValueType.SmallerOrEqual));
                var indToIgnoreFromChooserReg = Index.Shift(CommonDataPreprocessor.ChooseIndicesToIgnore(dataSet, detectBadData: false, 
                    detectFrozenData: doDetectFrozenData).ToArray(),-nIterationsToLookBack);
                if ( doDetectFrozenData && indToIgnoreFromChooserReg.Count()>0 )
                {
                    solverID += "(oversampled data removed)";
                }


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
                indicesToIgnoreReg = indicesToIgnoreReg.Union(indBadUcurReg).ToList();
                indicesToIgnoreReg = indicesToIgnoreReg.Union(indBadEcurReg).ToList();
                indicesToIgnoreReg = indicesToIgnoreReg.Union(umaxIndReg).ToList();
                indicesToIgnoreReg = indicesToIgnoreReg.Union(uminIndReg).ToList();
                indicesToIgnoreReg = indicesToIgnoreReg.Union(ymaxIndReg).ToList();
                indicesToIgnoreReg = indicesToIgnoreReg.Union(yminIndReg).ToList();
                indicesToIgnoreReg = indicesToIgnoreReg.Union(indToIgnoreFromChooserReg).ToList();
                indicesToIgnoreReg.Sort();

                double[] X1reg = new double[bufferLength];
                double[] X2reg = new double[bufferLength];
                double[] Yreg = new double[bufferLength];

                if (useConstantTimeBase)
                {
                    X1reg = new double[bufferLength];
                    X2reg = new double[bufferLength];
                    Yreg = new double[bufferLength];
                    Yreg = vec.Subtract(ucurReg, uprevReg);
                    X1reg = vec.Subtract(ecurReg, eprevReg);
                    X2reg = vec.Multiply(ecurReg, timeBase_s);
                }
                else
                {
                    goodIndices = Index.InverseIndices(ucurReg.Count(), indicesToIgnoreReg);
                    X1reg = new double[goodIndices.Count()];
                    X2reg = new double[goodIndices.Count()];
                    Yreg = new double[goodIndices.Count()];
                    var timeSinceLastGoodInd_s = timeBase_s;
                    for (int goodInd = 0; goodInd < goodIndices.Count; goodInd++)
                    {
                        if (goodInd > 0)// "variable timebase"
                            timeSinceLastGoodInd_s = (goodIndices[goodInd] - goodIndices[goodInd - 1]) * timeBase_s;
                        var ind = goodIndices[goodInd];
                        Yreg[goodInd] = ucurReg[ind] - uprevReg[ind];
                        X1reg[goodInd] = ecurReg[ind] - eprevReg[ind];
                        X2reg[goodInd] = ecurReg[ind] * timeSinceLastGoodInd_s;
                    }
                }


                double[][] inputs = { X1reg, X2reg };
                // important: use the un-regularized solver here!!
                if (useConstantTimeBase)
                    regressResults = vec.RegressUnRegularized(Yreg, inputs, indicesToIgnoreReg.ToArray());
                else
                    regressResults = vec.RegressUnRegularized(Yreg, inputs);

                var b = regressResults.Param;

                Rsq = regressResults.Rsq;
                yMod = regressResults.Y_modelled;

                if (b == null)
                {
                    Tiest = badValueIndicatingValue;
                    Kpest = badValueIndicatingValue;
                    pidParam.Fitting.WasAbleToIdentify = false;
                    solverID += "(Regression failed)";
                    return (pidParam, null, null);
                }
                else
                {
                    Kpest = -b[0];
                }
            
                double eMax = vec.Max(ecurReg);
                double eMin = vec.Min(ecurReg);
                double uMax = vec.Max(ucurReg);
                double uMin = vec.Min(ucurReg);
                
                double expectedRoughtEstimateForKp = Math.Abs((uMax - uMin) / (eMax - eMin));
                if (Math.Abs(Kpest) < expectedRoughtEstimateForKp * CUTOFF_FOR_GUESSING_PID_IN_MANUAL_FRAC)
                {
                    solverID += "(PID not in auto?)";
                    pidParam.Fitting.WasAbleToIdentify = false;
                    return (pidParam, null, null);
                }

                if (b[1] == 0)
                {
                    // this seems to happen if for instance the entire Y is constant, for instance in case 
                    // of input saturation, or if the controller is in manual the entire time or it is the wrong signal
                    solverID += "(Y appears uncorrelated with U)";
                    Tiest = 0;
                }
                else
                    Tiest = Math.Abs(b[0] / b[1]);

                indToIgnore = Index.Shift(indicesToIgnoreReg.ToArray(), nIterationsToLookBack).ToList();
            }

            pidParam.Fitting.SolverID = solverID;
            if (Double.IsNaN(Kpest) || Double.IsNaN(Tiest))
            {
                pidParam.Fitting.WasAbleToIdentify = false;
                solverID += "(regression failed)";
                return (pidParam, null, null);
            }
            
            const int nDigits = 7;
            const int nDigitsParams = 4;

            pidParam.Kp = Kpest;
            pidParam.Ti_s = Tiest;

            if (dataSet.Times.Count() > 0)
            {
                pidParam.Fitting.StartTime = dataSet.Times.First();
                pidParam.Fitting.EndTime = dataSet.Times.Last();
            }

            (var u_sim, int numSimRestarts) = GetSimulatedU(pidParam, dataSet, isPIDoutputDelayOneSample, indToIgnore);
            double[,] uSim = Array2D<double>.Create(u_sim);

            pidParam.Fitting.WasAbleToIdentify = true;
            dataSet.U_sim = uSim;


            // check if flipping Kp results in a singificantly higher Fitscore, if so, go for it. 
            double fitScore = FitScoreCalculator.Calc(dataSet.U.GetColumn(0), uSim.GetColumn(0), dataSet.BadDataID, indToIgnore);
            pidParam.Fitting.FitScorePrc = fitScore;

            pidParam.Kp = SignificantDigits.Format(pidParam.Kp, nDigitsParams);
            pidParam.Ti_s = SignificantDigits.Format(pidParam.Ti_s, nDigitsParams);
            pidParam.Td_s = SignificantDigits.Format(pidParam.Td_s, nDigitsParams);

            pidParam.Fitting.TimeBase_s = dataSet.GetTimeBase();
            pidParam.Fitting.StartTime = dataSet.Times.First();
            pidParam.Fitting.EndTime = dataSet.Times.Last();
            pidParam.Fitting.Umin = new double[] { SignificantDigits.Format(vec.Min(dataSet.U.GetColumn(0)), nDigitsParams) };
            pidParam.Fitting.Umax = new double[] { SignificantDigits.Format(vec.Max(dataSet.U.GetColumn(0)), nDigitsParams) };

            if (useConstantTimeBase)
            {
                pidParam.Fitting.NFittingTotalDataPoints = regressResults.NfittingTotalDataPoints;
                pidParam.Fitting.NFittingBadDataPoints = regressResults.NfittingBadDataPoints;
            }
            else
            {
                pidParam.Fitting.NFittingTotalDataPoints = dataSet.GetNumDataPoints(); 
                pidParam.Fitting.NFittingBadDataPoints = dataSet.GetNumDataPoints() - goodIndices.Count();
            }

            pidParam.Fitting.RsqDiff = regressResults.Rsq;

            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> { dataSet.U.GetColumn(0), dataSet.U_sim.GetColumn(0) },
                    new List<string> { "y1=u_meas", "y1=u_sim"},
                    dataSet.GetTimeBase(), "PidIdentifier_Debug");
                Shared.DisablePlots();
            }
            pidParam.Fitting.RsqDiff = SignificantDigits.Format(pidParam.Fitting.RsqDiff, nDigits);
            pidParam.Fitting.NumSimulatorRestarts = numSimRestarts;
            pidParam.DelayOutputOneSample = isPIDoutputDelayOneSample;
            return (pidParam, dataSet.U_sim, indToIgnore);
        }


        /// <summary>
        /// Returns the simulated time series of the manipulated variable u as given by the PID-controller.
        /// </summary>
        /// <param name="pidParams"></param>
        /// <param name="dataset"> dataset, including including the "indicesToIgnore"</param>
        /// <param name="isPIDoutputDelayOneSample"></param>
        /// <param name="indToIgnore"></param>
        /// <returns>the simulated value, and the number of restarts</returns>
        public (double[],int) GetSimulatedU(PidParameters pidParams, UnitDataSet dataset,bool isPIDoutputDelayOneSample, 
            List<int> indToIgnore)
        {
            bool enableSimulatorRestarting = false;
            bool doVariableTimeBase = false;//todo: currently this does not work, is not implemented properly
            var pidModel = new PidModel(pidParams, "pid");
            (var isOk,var simulatedU, int numSimRestarts) =  PlantSimulatorHelper.SimulateSingle(dataset, pidModel, indToIgnore,
                enableSimulatorRestarting, doVariableTimeBase);
            dataset.U_sim = Array2D<double>.CreateFromList(new List<double[]> { simulatedU });
            return (simulatedU,numSimRestarts);
        }
    }

}


