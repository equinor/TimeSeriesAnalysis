using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TimeSeriesAnalysis;
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
        private const int MAX_ESTIMATIONS_PER_DATASET = 1;
        private const double MIN_DATASUBSET_URANGE_PRC = 0;
        private double badValueIndicatingValue;

        private double maxExpectedTc_s;
        private bool enableKPchangeDetection = true;
        private bool enableTichangeDetection = true;
        //private bool doDebugging = true;
        private PidScaling pidScaling;
        private PidControllerType type;
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
        /*
        public void TurnOffDebuggingPlots()
        {
            doDebugging = false;
        }*/


        public void SetTiChangeDetect(bool doChangeDetect)
        {
            enableTichangeDetection = doChangeDetect;
        }
        public void SetKPChangeDetect(bool doChangeDetect)
        {
            enableKPchangeDetection = doChangeDetect;
        }
        public bool GetTiChangeDetect()
        {
            return enableTichangeDetection;
        }

        public bool GetKPChangeDetect()
        {
            return enableKPchangeDetection;
        }

        private void DetermineUminUmax(UnitDataSet dataSet, out double uMin, out double uMax)
        {
            double[] e = GetErrorTerm(dataSet);

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
                //Test2
                /*  if (uMaxInd.Count() / N > NthresholdValuesFrac)
                       uMax = uMaxObserved;*/
            }

        }

        /// <summary>
        /// Identifies a PID-controller from a UnitDataSet
        /// </summary>
        /// <param name="dataSet">a UnitDataSet, where .Y_meas, .Y_setpoint and .U are analyzed</param>
        /// <returns>the identified parameters of the PID-controller</returns>
        public PidParameters Identify(ref UnitDataSet dataSet)
        {

            PidParameters results_withDelay = IdentifyInternal(dataSet, true);

            /*const bool checkIfNoDelayPIDfitsDataBetter = true;//should be true unless debugging.
            if (checkIfNoDelayPIDfitsDataBetter)
            {
                PidParameters results_noDelay = IdentifyInternal(dataSet, false);

                if (results_noDelay.GoodnessOfFit_prc > results_withDelay.GoodnessOfFit_prc)
                    return results_noDelay;
                else
                    return results_withDelay;
            }
            else
                return results_withDelay;*/

            dataSet.U_sim = Array2D<double>.Create(GetSimulatedU(results_withDelay,dataSet,true));

            return results_withDelay;


        }

        const double rSquaredCutoffForInTrackingWarning = 0.02;//must be between 0 and 1


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

        private double[] GetErrorTerm(UnitDataSet dataSet)
        {
            return vec.Subtract(dataSet.Y_meas, dataSet.Y_setpoint);
        }


        private PidParameters IdentifyInternal(UnitDataSet dataSet, bool isPIDoutputDelayOneSample)
        {
            this.timeBase_s = dataSet.GetTimeBase();
            PidParameters pidParam = new PidParameters();
            if (pidScaling!=null)
                pidParam.Scaling = pidScaling;
            else
                pidParam.Scaling = new PidScaling();//default scaling
            if (dataSet.Y_setpoint == null)
            {
                pidParam.AddWarning(PidIdentWarning.NotPossibleToIdentifyPIDcontroller_YsetIsBad);
            }
            else if (vec.IsAllNaN(dataSet.Y_setpoint))
            {
                pidParam.AddWarning(PidIdentWarning.NotPossibleToIdentifyPIDcontroller_YsetIsBad);
            }

            double[] e_unscaled = GetErrorTerm(dataSet);

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

            int numEstimations = 1;
            int bufferLength = dataSet.GetNumDataPoints() - 1;

            if (maxExpectedTc_s > 0 && (enableKPchangeDetection || enableTichangeDetection))
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

            for (int curEstidx = 0; curEstidx < numEstimations; curEstidx++)
            {
                int nIterationsToLookBack = 2;//since eprev and eprevprev are needed, look two iteration back.

                int idxStart = nIndexesBetweenWindows * curEstidx + nIterationsToLookBack;
                int idxEnd = nIndexesBetweenWindows * (curEstidx + 1) - 1;//+ nIterationsToLookBack
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
                List<int> indBadU = SysIdBadDataFinder.GetAllBadIndicesPlussNext(ucur);
                //                List<int> indUMinus9999     = Vec.FindValues(ucur, -9999, FindValues.Equal);
                //        List<int> indUprevMinus9999 = Vec.Sub(indUMinus9999.ToArray(), -1).ToList();
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
                
                /*
                if (doDebugging)
                {
                    //   Plot.Three(Y_ols, X1_ols, X2_ols, (int)TimeBase_s, "Y", "X1", "X2");
                }*/

                var regressResults = vec.Regress(Y_ols, inputs, indicesToIgnore.ToArray());
                //  out double[] notUsed, out double[] Y_mod, out double Rsq_cur;
                var b = regressResults.Param;

                Rsq[curEstidx] = regressResults.Rsq;
                yModList.Add(regressResults.Y_modelled);

                if (b == null)
                {
                    Tiest[curEstidx] = badValueIndicatingValue;
                    Kpest[curEstidx] = badValueIndicatingValue;
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
                    //       result.AddWarning(PIDidentWarnings.PIDControllerDoesNotAppearToBeInAuto);
                    Kpest[curEstidx] = badValueIndicatingValue;
                    Tiest[curEstidx] = badValueIndicatingValue;
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
            /*
            // fill result in full-size vector
            result.Kpest_raw = new double[dataSet.NumDataPoints];
            result.Tiest_raw = new double[dataSet.NumDataPoints];
            int curEstInd = 0;
            if (Kpest.Length > 0 && Tiest.Length > 0)
            {
                for (int i = 0; i < dataSet.NumDataPoints; i++)
                {
                    if (ind_result[curEstInd] < i && curEstInd < Kpest.Length - 2)
                        curEstInd++;
                    result.Kpest_raw[i] = Kpest[curEstInd];
                    result.Tiest_raw[i] = Tiest[curEstInd];
                }
            }
            result.test_raw = t_result;
            result.Kpest = Kpest;
            result.Tiest = Tiest;

            PIDidResultsPP.evaluateTimeSeriesForStepChange(ind_result, Kpest, enableKPchangeDetection,
                out double Kpmean1, out double Kpmean2, out int? KpchangeT);

            result.Kpest_val1 = Kpmean1;
            result.Kpest_val2 = Kpmean2;
            result.Kpest_changeInd = KpchangeT;
            if (dataSet.t != null && result.Kpest_changeInd.HasValue)
                result.Kpest_changeTime = dataSet.t[result.Kpest_changeInd.Value];

            result.Kpest = PIDidResultsPP.createReturnableTimeSeriesFromMeans(Kpmean1, Kpmean2, KpchangeT, dataSet.GetUMinusFF());

            PIDidResultsPP.evaluateTimeSeriesForStepChange(ind_result, Tiest, enableTichangeDetection,
                out double Timean1, out double Timean2, out int? TichangeT);

            if (Timean1 > 50)
                Timean1 = Math.Round(Timean1);
            if (Timean2 > 50)
                Timean2 = Math.Round(Timean2);
            result.Tiest_val1 = Timean1;
            result.Tiest_val2 = Timean2;
            result.Tiest_changeInd = TichangeT;
            if (dataSet.t != null && result.Tiest_changeInd.HasValue)
                result.Kpest_changeTime = dataSet.t[result.Tiest_changeInd.Value];

            result.Tiest = PIDidResultsPP.createReturnableTimeSeriesFromMeans(Timean1, Timean2, TichangeT, dataSet.GetUMinusFF());
            */
            // this forward-integration of modelledU will be subject to a slight drift-off
            // over time and so will start off similar to output of the simulatiing with 
            // PIDcontroller.cs but will end up being off by a growing margin.


            /*
            pidParam.modelledU_minusFF = GetSimulatedU(Kpmean1, Timean1, dataSet.Y_meas, dataSet.Y_setpoint, uMinusFF, pidParam.isPIDoutputDelayOneSample);


            List<int> indToIgnoreEntireVec = BadDataFinder.GetAllBadIndices(dataSet.GetUinclFF());
            pidParam.GoodnessOfFit_prc = Vec.RSquared(pidParam.modelledU_minusFF, dataSet.GetUMinusFF(), indToIgnoreEntireVec) * 100;

            if (0 <= pidParam.GoodnessOfFit_prc && pidParam.GoodnessOfFit_prc < rSquaredCutoffForInTrackingWarning * 100)
            {
                pidParam.AddWarning(PidIdentWarning.PIDControllerPossiblyInTracking);
            }


            double uObjFun = Vec.SumOfSquareErr(pidParam.modelledU_minusFF, uMinusFF);
            double uSelfObjFun = vec.SelfSumOfSquareErr(uMinusFF);
            */


            // see if using "next value= last value" gives better objective function than the model found"
            // if so it is an indication that something is wrong
            // if data is "only noise" then the two can be very close even if model is good, for that reason add
            // a little margin 
            /*
            const double poorModelFitMaringFrac = 1.1;// should be equal or above 1
            if (uObjFun > uSelfObjFun * poorModelFitMaringFrac)
            {
                pidParam.AddWarning(PidIdentWarning.PoorModelFit);
            }
            // if there is too little variation in U, then Kp tends to close to zero.
            if (Math.Abs(Kpmean1) < 0.0001)//in some cases the value can be less than 0.01 on real plants
            {
                pidParam.AddWarning(PidIdentWarning.PIDControllerDoesNotAppearToBeInAuto);
            }*/
            pidParam.Kp = Kpest[0];
            pidParam.Ti_s = Tiest[0];
            pidParam.Fitting = new FittingInfo();
            pidParam.Fitting.WasAbleToIdentify = true;


            return pidParam;
        }

        // todo: this code should be replaced after porting code into TimeSeriesAnalysis. Re-use common simulation rather than re-doing it.

        //public Double[] GetSimulatedU(double Kp, double Ti, Double[] ymeas, Double[] yset, Double[] u, bool outputDelayedOneSample = false)
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
            for (int i = firstGoodDataPointToStartSimIdx; i < simulatedU.Length - samplesToDelayOutput; i++)
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


