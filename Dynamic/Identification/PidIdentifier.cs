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
    /// Class that attempts to identify the parameters(such as Kp and Ti) of a PID-controller from a 
    /// given set of time-series of the input and output of said controller.
    /// </summary>
    public class PidIdentifier
    {
        private const double CUTOFF_FOR_GUESSING_PID_IN_MANUAL_FRAC = 0.005;
        const double rSquaredCutoffForInTrackingWarning = 0.02;//must be between 0 and 1

        private const int MAX_ESTIMATIONS_PER_DATASET = 1;
        private const double MIN_DATASUBSET_URANGE_PRC = 0;
        private double badValueIndicatingValue;

        private double maxExpectedTc_s;
        private PidScaling pidScaling;
        private PidControllerType type;
        private PidFilter pidFilter;
        private Vec vec;
        private double timeBase_s;

        public PidIdentifier(PidScaling pidScaling=null, double maxExpectedTc_s=0, double badValueIndicatingValue=-9999,
            PidControllerType type = PidControllerType.Unset)
        {
            this.maxExpectedTc_s = maxExpectedTc_s;
            this.badValueIndicatingValue = badValueIndicatingValue;
            this.pidScaling = pidScaling;
            this.type = type;
            this.vec = new Vec();
        }


        private void DetermineUminUmax(UnitDataSet dataSet, out double uMin, out double uMax)
        {
            double[] e = GetErrorTerm(dataSet, pidFilter);

            // output constraints will cause multiple equal values u of next to each other.
            uMin = 0; // default value
            uMax = 100; //default value
            // because real u close to a constraint often "jitters" becasuse of P-action on controller or noise
            // on the ouput measurement, we might need to accept values very close to umin and umax as also part of 

            // uTol: changes in u below this value are considered "negligable changes"
            double uTol = 0.1;// values inside range but very close to min/max might also be considere

            double[] uMinusFF = GetUMinusFF(dataSet);

            double uMinObserved = vec.Min(uMinusFF);
            double uMaxObserved = vec.Max(uMinusFF);

            // logic to count 
            int N = dataSet.GetNumDataPoints(); 
            const int minNumberOfIndicesToConsider = 5;
            const double minFracAtConstraintConsidered = 0.10;//0.1 = 10%

            const double E_TRESHOLDFACTOR = 1.5;//"tuning factor">>1

            if (uMaxObserved - uMinObserved < uTol)
            {
                return;
            }

            // UMIN
            if (uMinObserved > uMin + uTol)
            {
                List<int> uMinInd = vec.FindValues(uMinusFF, uMinObserved + uTol, VectorFindValueType.SmallerOrEqual);

                if (uMinInd.Count > minNumberOfIndicesToConsider && (double)uMinInd.Count / N > minFracAtConstraintConsidered)
                {

                    List<int> notUMinInd = Index.InverseIndices(dataSet.GetNumDataPoints(), uMinInd);
                    double[] eAtUmin = Vec<double>.GetValuesAtIndices(e, uMinInd);
                    double[] eAtNotUmin = Vec<double>.GetValuesAtIndices(e, notUMinInd);
                    double eMeanAtUmin = vec.Mean(eAtUmin).Value;
                    double eMeanAtNotUmin = vec.Mean(eAtNotUmin).Value;
                    double eDifferenceFactor = Math.Abs(eMeanAtUmin - eMeanAtNotUmin) / Math.Abs(eMeanAtNotUmin);
                    if (eDifferenceFactor > E_TRESHOLDFACTOR)
                    {
                        uMin = uMinObserved;
                    }
                }
            }
            // UMAX
            if (uMaxObserved < uMax - uTol)
            {
                List<int> uMaxInd = vec.FindValues(uMinusFF, uMaxObserved - uTol, VectorFindValueType.BiggerOrEqual);
                if (uMaxInd.Count > minNumberOfIndicesToConsider && (double)uMaxInd.Count / N > minFracAtConstraintConsidered)
                {
                    List<int> notUMaxInd = Index.InverseIndices(dataSet.GetNumDataPoints(), uMaxInd);
                    double[] eAtUmax = Vec<double>.GetValuesAtIndices(e, uMaxInd);
                    double[] eAtNotUax = Vec<double>.GetValuesAtIndices(e, notUMaxInd);

                    double eMeanAtUmax = vec.Mean(eAtUmax).Value;
                    double eMeanAtNotUmax = vec.Mean(eAtNotUax).Value;

                    double eDifferenceFactor = Math.Abs(eMeanAtUmax - eMeanAtNotUmax) / Math.Abs(eMeanAtNotUmax);

                    if (eDifferenceFactor > E_TRESHOLDFACTOR)
                    {
                        uMax = uMaxObserved;
                    }
                }
            }

        }

        private bool IsFirstModelBetterThanSecondModel(PidParameters firstModel, PidParameters secondModel)
        {
            if (firstModel.Fitting.RsqDiff > secondModel.Fitting.RsqDiff)
                return true;
            else
                return false;

        }


        /// <summary>
        /// Identifies a PID-controller from a UnitDataSet
        /// </summary>
        /// <param name="dataSet">a UnitDataSet, where .Y_meas, .Y_setpoint and .U are analyzed</param>
        /// <param name="isPIDoutputDelayOneSample">specify if the pid-controller is acting on the e[k-1] if true, or e[k] if false</param>
        /// <returns>the identified parameters of the PID-controller</returns>
        public PidParameters Identify(ref UnitDataSet dataSet)
        {
            const bool doOnlyWithDelay = false;//should be false unless debugging something


            // 1. try identification with delay of one sample but without filtering
            (PidParameters results_withDelay, double[,] U_withDelay) = IdentifyInternal(dataSet, true);
            if (doOnlyWithDelay)
            {
                dataSet.U_sim = U_withDelay;
                return results_withDelay;
            }

            // 2. try identification wihtout delay of one sample (yields better results often if dataset is downsampled)
            //    relative to the clock that the pid algorithm ran on originally
            (PidParameters results_withoutDelay, double[,] U_withoutDelay) = IdentifyInternal(dataSet, false);

            // save which is the "best" estimate for comparison 
            bool doDelay = true;
            PidParameters bestPidParameters= results_withoutDelay;
            double[,] bestU = U_withoutDelay;
            //  if (results_withDelay.Fitting.ObjFunValAbs < results_withoutDelay.Fitting.ObjFunValAbs)
            if (IsFirstModelBetterThanSecondModel(results_withDelay, results_withoutDelay))
            {
                doDelay = true;
                bestPidParameters = results_withDelay;
                bestU = U_withDelay;

            }
            else
            {
                doDelay = false;
                bestPidParameters = results_withoutDelay;
                bestU = U_withoutDelay;
            }

            // 3. try filtering y_meas and see if this improves fit 
            // if there is noise on y_meas that is not filtered, this may cause too small Kp/Ti
            double maxFilterTime_s = 6 * timeBase_s;
            for (double filterTime_s = timeBase_s; filterTime_s < maxFilterTime_s; filterTime_s += timeBase_s)
            { 
                var pidFilterParams = new PidFilterParams(true, 1, filterTime_s);
                (PidParameters results_withFilter, double[,] U_withFilter) = IdentifyInternal(dataSet, doDelay, pidFilterParams);

                if (IsFirstModelBetterThanSecondModel(results_withFilter,bestPidParameters))
                {
                    bestU = U_withFilter;
                    bestPidParameters = results_withFilter;
                }
            }

            // 4. try filtering the input u_meas
            // this is experimental, but if downsampled, Kp/Ti seems often too low, and hypothesis is that this is because
            // small variations in u_meas/y_meas are no longer tightly correlated, so identification should perhaps focus on fitting
            // to only "larger" changes. 
            bool filterUmeas = true;
            for (double filterTime_s = timeBase_s; filterTime_s < maxFilterTime_s; filterTime_s += timeBase_s)
            {
                var pidFilterParams = new PidFilterParams(true, 1, filterTime_s);
                (PidParameters results_withFilter, double[,] U_withFilter) = 
                    IdentifyInternal(dataSet, doDelay, pidFilterParams,filterUmeas);

                if (IsFirstModelBetterThanSecondModel(results_withFilter, bestPidParameters))
                {
                    bestU = U_withFilter;
                    bestPidParameters = results_withFilter;
                }
            }
            // 5. finally return the "best" result
            dataSet.U_sim = bestU;
            return bestPidParameters;
        }

        /// <summary>
        /// a steady state-offset between U_sim and U is an indication of noise in Y_meas getting into controller and 
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
            // see if average max deviation is signficant in amplitude
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
            // see if there is big deviation between amplitude of overshoot and undershoot.
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
        /// Note that if there is noise on y, then this identification algorithm is observed to under-estimate Kp and Ti.
        /// This is the motivation for including the ability to include a filter on y
        /// This method also takes pidScaling into account!
        /// </summary>
        /// <param name="dataSet">dataset to filter over</param>
        /// <param name="isPIDoutputDelayOneSample"></param>
        /// <param name="pidFilter">optional filter to apply to y</param>
        /// <returns></returns>
        private (PidParameters, double[,]) IdentifyInternal(UnitDataSet dataSet, bool isPIDoutputDelayOneSample,
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
                return (pidParam, null);
            }
            else if (vec.IsAllNaN(dataSet.Y_setpoint))
            {
                pidParam.Fitting.WasAbleToIdentify = false;
                pidParam.AddWarning(PidIdentWarning.NotPossibleToIdentifyPIDcontroller_YsetIsBad);
                return (pidParam, null);
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
            int numEstimations = 1;
            int bufferLength = dataSet.GetNumDataPoints() - 1;

            if (maxExpectedTc_s > 0)
            {
                int bufferLengthShortest = (int)Math.Ceiling(maxExpectedTc_s * 4 / timeBase_s);// shortest possible to have at least one settling time in it.
                int bufferLengthLongest = dataSet.GetNumDataPoints() / MAX_ESTIMATIONS_PER_DATASET;
                bufferLength = Math.Max(bufferLengthShortest, bufferLengthLongest);
                numEstimations = dataSet.GetNumDataPoints() / bufferLength;
                if (numEstimations < 3)
                {
                    pidParam.AddWarning(PidIdentWarning.DataSetVeryShortComparedtoTMax);
                }
            }
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

            int nIndexesBetweenWindows = (int)Math.Floor((double)dataSet.GetNumDataPoints() / numEstimations);
            double[] Kpest = new double[numEstimations];
            double[] Tiest = new double[numEstimations];
            DateTime[] t_result = new DateTime[numEstimations];
            int[] ind_result = new int[numEstimations];
            double[] objFunEst = new double[numEstimations];
            List<double[]> yModList = new List<double[]>();

            double[] Rsq = new double[numEstimations];

            RegressionResults regressResults = null;

            for (int curEstidx = 0; curEstidx < numEstimations; curEstidx++)
            {
                int nIterationsToLookBack = 2;//since eprev and eprevprev are needed, look two iteration back.

                int idxStart = nIndexesBetweenWindows * curEstidx + nIterationsToLookBack;
                int idxEnd = nIndexesBetweenWindows * (curEstidx + 1) - 1;
                ind_result[curEstidx] = idxEnd;

                double[] ucur, uprev, ecur, eprev;

                List<int> indicesToIgnore = new List<int>();

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
                int nSamplesToLookBack = 0;
                if (isPIDoutputDelayOneSample)
                {
                    nSamplesToLookBack = 1;
                }
                ecur = Vec<double>.SubArray(e_scaled, idxStart - nSamplesToLookBack, idxEnd - nSamplesToLookBack);
                eprev = Vec<double>.SubArray(e_scaled, idxStart - 1 - nSamplesToLookBack, idxEnd - 1 - nSamplesToLookBack);

                // replace -9999 in dataset
                List<int> indBadU = SysIdBadDataFinder.GetAllBadIndicesPlussNext(ucur,dataSet.BadDataID);
                List<int> indBadEcur = vec.FindValues(ecur, -9999, VectorFindValueType.Equal);
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
                indicesToIgnore = indicesToIgnore.Union(indBadU).ToList();
                indicesToIgnore = indicesToIgnore.Union(indBadEcur).ToList();
                indicesToIgnore = indicesToIgnore.Union(indBadEprev).ToList();
                //TODO: need to remove indice if _previous_ value was umax or umin
                indicesToIgnore = indicesToIgnore.Union(umaxInd).ToList();
                indicesToIgnore = indicesToIgnore.Union(uminInd).ToList();
                indicesToIgnore.Sort();

                if (indicesToIgnore.Count() > ucur.Count() * 0.5)
                {
                    pidParam.Fitting.WasAbleToIdentify = false;
                    pidParam.AddWarning(PidIdentWarning.NotPossibleToIdentifyPIDcontroller_BadInputData);
                    continue;
                }
                double uCurRange = vec.Max(ucur) - vec.Min(ucur);
                if (uCurRange < uRange * MIN_DATASUBSET_URANGE_PRC / 100)
                {
                    continue;
                }
                Y_ols = vec.Subtract(ucur, uprev);

                X1_ols = vec.Subtract(ecur, eprev);
                X2_ols = vec.Multiply(ecur, timeBase_s);

                double[][] inputs = { X1_ols, X2_ols };
                // important: use the un-regularized solver here!!
                regressResults = vec.RegressUnRegularized(Y_ols, inputs, indicesToIgnore.ToArray());
                //  out double[] notUsed, out double[] Y_mod, out double Rsq_cur;
                var b = regressResults.Param;

                Rsq[curEstidx] = regressResults.Rsq;
                yModList.Add(regressResults.Y_modelled);

                if (b == null)
                {
                    Tiest[curEstidx] = badValueIndicatingValue;
                    Kpest[curEstidx] = badValueIndicatingValue;
                    pidParam.Fitting.WasAbleToIdentify = false;
                    if (numEstimations == 1)
                        pidParam.AddWarning(PidIdentWarning.RegressionProblemFailedToYieldSolution);
                    continue;
                }
                else
                {
                    Kpest[curEstidx] = -b[0];
                }
                double eMax = vec.Max(ecur);
                double eMin = vec.Min(ecur);
                double uMax = vec.Max(ucur);
                double uMin = vec.Min(ucur);
                
                double expectedRoughtEstimateForKp = Math.Abs((uMax - uMin) / (eMax - eMin));
                if (Math.Abs(Kpest[curEstidx]) < expectedRoughtEstimateForKp * CUTOFF_FOR_GUESSING_PID_IN_MANUAL_FRAC)
                {
                    pidParam.AddWarning(PidIdentWarning.PIDControllerDoesNotAppearToBeInAuto);
                    pidParam.Fitting.WasAbleToIdentify = false;
             //       Kpest[curEstidx] = badValueIndicatingValue;
              //      Tiest[curEstidx] = badValueIndicatingValue;
                    continue;
                }

                if (b[1] == 0)
                {
                    // this seems to happen in for instance entire Y is constant, for instance in case 
                    /// of input saturation, or if the controller is in manual the entire time or is the wrong signal
                    pidParam.AddWarning(PidIdentWarning.NotPossibleToIdentifyPIDcontroller_UAppearsUncorrelatedWithY);
                    Tiest[curEstidx] = badValueIndicatingValue;
                    continue;
                }
                else
                    Tiest[curEstidx] = Math.Abs(b[0] / b[1]);

                if (dataSet.Times != null)
                {
                    t_result[curEstidx] = dataSet.Times[idxEnd];
                }
            }


            // see if using "next value= last value" gives better objective function than the model found"
            // if so it is an indication that something is wrong
            // if data is "only noise" then the two can be very close even if model is good, for that reason add
            // a little margin 
            // if there is too little variation in U, then Kp tends to close to zero.

            const int nDigits = 7;
            const int nDigitsParams = 4;

            pidParam.Kp = Kpest[0];
            pidParam.Ti_s = Tiest[0];
            pidParam.Fitting.SolverID = "PidIdentifier v1.0";

            if (dataSet.Times.Count() > 0)
            {
                pidParam.Fitting.StartTime = dataSet.Times.First();
                pidParam.Fitting.EndTime = dataSet.Times.Last();
            }
            if (regressResults == null)
            {
                pidParam.Fitting.WasAbleToIdentify = false;
                return (pidParam,null);
            }

            double[,] U_sim = Array2D<double>.Create(GetSimulatedU(pidParam, dataSet, isPIDoutputDelayOneSample));

            pidParam.Fitting.WasAbleToIdentify = true;
            pidParam.Kp = SignificantDigits.Format(pidParam.Kp, nDigitsParams);
            pidParam.Ti_s = SignificantDigits.Format(pidParam.Ti_s, nDigitsParams);
            pidParam.Td_s = SignificantDigits.Format(pidParam.Td_s, nDigitsParams);

            pidParam.Fitting.NFittingTotalDataPoints = regressResults.NfittingTotalDataPoints;
            pidParam.Fitting.NFittingBadDataPoints = regressResults.NfittingBadDataPoints;
            pidParam.Fitting.RsqDiff = regressResults.Rsq;
            pidParam.Fitting.ObjFunValDiff = regressResults.ObjectiveFunctionValue;
            pidParam.Fitting.FitScorePrc = SignificantDigits.Format(FitScore.Calc(dataSet.U.GetColumn(0), U_sim.GetColumn(0)), nDigits);
            
            pidParam.Fitting.ObjFunValAbs  = vec.SumOfSquareErr(dataSet.U.GetColumn(0), U_sim.GetColumn(0), 0);
            pidParam.Fitting.RsqAbs = vec.RSquared(dataSet.U.GetColumn(0), U_sim.GetColumn(0), null, 0) * 100;

            pidParam.Fitting.RsqAbs = SignificantDigits.Format(pidParam.Fitting.RsqAbs, nDigits);
            pidParam.Fitting.RsqDiff = SignificantDigits.Format(pidParam.Fitting.RsqDiff, nDigits);
            pidParam.Fitting.ObjFunValDiff = SignificantDigits.Format(pidParam.Fitting.ObjFunValDiff, nDigits);
            pidParam.Fitting.ObjFunValAbs = SignificantDigits.Format(pidParam.Fitting.ObjFunValAbs, nDigits);
            // fitting abs?
            return (pidParam,U_sim);
        }

        // todo: this code should be replaced after porting code into TimeSeriesAnalysis. Re-use common simulation rather than re-doing it.

        public double[] GetSimulatedU(PidParameters pidParams, UnitDataSet dataset,bool isPIDoutputDelayOneSample)
        {
            int firstGoodDataPointToStartSimIdx = 0;
            while ((dataset.Y_setpoint[firstGoodDataPointToStartSimIdx] == badValueIndicatingValue ||
                dataset.Y_meas[firstGoodDataPointToStartSimIdx] == badValueIndicatingValue ||
                dataset.U[firstGoodDataPointToStartSimIdx,0] == badValueIndicatingValue) &&
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
    }

}


