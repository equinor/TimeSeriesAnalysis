using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using System.Net.Http.Headers;
using System.Data;
using System.Reflection;
using System.ComponentModel.Design;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Globalization;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Attempts to identify a gain-scheduled model, a model that uses multiple local linear models to approximate a nonlinearity.
    /// Should not be confused with gain-scheduling in terms of PID-control.
    /// </summary>
    static public class GainSchedIdentifier
    {
        /// <summary>
        /// Identify a gain scheduled model for the given dataset.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="gsFittingSpecs"></param>
        /// <returns></returns>
        static public GainSchedParameters Identify(UnitDataSet dataSet, GainSchedFittingSpecs gsFittingSpecs = null)
        {

            int gainSchedInputIndex = 0;
            if (gsFittingSpecs != null)
            {
                gainSchedInputIndex = gsFittingSpecs.uGainScheduledInputIndex;
            }

            var allYSimList = new List<double[]>( );
            var allGainSchedParams = new List<GainSchedParameters> { };
            // Reference case: no gain scheduling 
            {
                UnitDataSet dataSetNoGainSched = new UnitDataSet(dataSet);
                GainSchedParameters GSp_noGainSchedReference = new GainSchedParameters();
                GSp_noGainSchedReference.LinearGainThresholds = new double[] { }; ;
                UnitModel unitModel = UnitIdentifier.IdentifyLinear(ref dataSetNoGainSched, null,false);// Todo:consider modelling with nonlinear model?
                UnitParameters unitParams = unitModel.GetModelParameters();
                bool dodebug = false;
                if (dodebug)
                {
                    // Simulate
                    double[] y_ref = dataSet.Y_meas;
                    int timeBase_s = 1;
                    var plantSim = new PlantSimulator(new List<ISimulatableModel> { unitModel });
                    var inputData = new TimeSeriesDataSet();
                    for (int k = 0; k < dataSet.U.GetNColumns(); k++)
                    {
                        inputData.Add(plantSim.AddExternalSignal(unitModel, SignalType.External_U, (int)INDEX.FIRST), dataSet.U.GetColumn(k));
                    }
                    inputData.CreateTimestamps(timeBase_s);
                    var CorrectisSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
                    double[] unitModelSim = simData.GetValues(unitModel.GetID(), SignalType.Output_Y);
                    // Plot here!
                    Shared.EnablePlots();
                    Plot.FromList(new List<double[]> {
                        unitModelSim,
                        y_ref,
                        dataSet.U.GetColumn(0) },
                        new List<string> { "y1=simY1_UM1", "y1=y_ref", "y3=u1" },
                    timeBase_s,
                        "GainSched split ");
                    Shared.DisablePlots();
                }
                // Linear gains
                List<double[]> GS_LinearGains1 = new List<double[]>();
                GS_LinearGains1.Add(unitParams.LinearGains);
                GSp_noGainSchedReference.LinearGains = GS_LinearGains1;

                // Time constants
                double[] GS_TimeConstants_s1 = new double[] { unitParams.TimeConstant_s };
                GSp_noGainSchedReference.TimeConstant_s = GS_TimeConstants_s1;

                // time delays
                GSp_noGainSchedReference.TimeDelay_s = unitParams.TimeDelay_s;

                // Time constants thresholds
                double[] GS_TimeConstantThreshold1 = new double[] { };
                GSp_noGainSchedReference.TimeConstantThresholds = GS_TimeConstantThreshold1;
                GSp_noGainSchedReference.Fitting = unitParams.Fitting;
                // 
                GSp_noGainSchedReference.OperatingPoint_U = 0;
                GSp_noGainSchedReference.OperatingPoint_Y = unitParams.Bias;
                //////
                allYSimList.Add(dataSetNoGainSched.Y_sim);
                allGainSchedParams.Add(GSp_noGainSchedReference);
            }
            ////////////////////////////////////////////////////
            // note that this is a fairly computationally heavy call
            // higher values means higher accuracy but at the cost of more computations.. 
            const int globalSearchIterationsPass1 = 40;//should be big enough, but then more is not better. 
            const int globalSearchIterationsPass2 = 20;//should be big enough, but then more is not better. 

            (List<GainSchedParameters> potentialGainschedParametersList, List<double[]> potentialYsimList) =
                IdentifyGainScheduledGainsAndSingleThreshold(dataSet, gainSchedInputIndex,true, globalSearchIterationsPass1);
            // TODO: extend the method to consider multiple thresholds by calling the above method recursively?

            ////////////////////////////////////////////////////
            // Final step: choose the best GainSched from all the candidates
            allGainSchedParams.AddRange(potentialGainschedParametersList);
            allYSimList.AddRange(potentialYsimList);

            (var bestModel_pass1, var bestModelIdx_pass1) =  ChooseBestGainScheduledModel(allGainSchedParams, allYSimList, ref dataSet);

            // pass 2:
            const bool DO_PASS2 = true;
            const int pass2Width = 0;//0,1 or 2, design parameter about how wide to do pass 2 aroudn pass 1 result.(higher number is at the expense of accuracy)
            GainSchedParameters modelToReturn = new GainSchedParameters();
            if (bestModelIdx_pass1 > 1 + pass2Width && bestModelIdx_pass1 < allGainSchedParams.Count() - pass2Width && DO_PASS2)
            {
                double? gsSearchMin_pass2 = allGainSchedParams.ElementAt(Math.Max(bestModelIdx_pass1 - 1 - pass2Width, 0)).LinearGainThresholds.First();
                double? gsSearchMax_pass2 = allGainSchedParams.ElementAt(Math.Min(bestModelIdx_pass1 + 1 + pass2Width, allGainSchedParams.Count() - 1)).LinearGainThresholds.First();
                (List<GainSchedParameters> potentialGainschedParametersList_pass2, List<double[]> potentialYsimList_pass2) =
                    IdentifyGainScheduledGainsAndSingleThreshold(dataSet, gainSchedInputIndex, true, globalSearchIterationsPass2, gsSearchMin_pass2, gsSearchMax_pass2);
                (var bestModel_pass2, var bestModelIdx_pass2) = ChooseBestGainScheduledModel(potentialGainschedParametersList_pass2,
                    potentialYsimList_pass2, ref dataSet);
                modelToReturn = bestModel_pass2;
            }
            else
            {
                modelToReturn = bestModel_pass1;
              }
            EstimateTimeDelay(ref modelToReturn, ref dataSet);
            return modelToReturn;
        }

        /// <summary>
        /// Updates the given model to include a time-delay term if applicable.
        /// Method is based on trial-and-error reducing the estimated time-constants while increasint the time-delay and seeing 
        /// if this improves the fit of the model 
        /// </summary>
        /// <param name="gsParams">estiamted paramters from other estimation, time-constants should be estiamted, but time-delays are zero, both time-delays and time-constants are updated by this method</param>
        /// <param name="dataSet"></param>
        private static void EstimateTimeDelay(ref GainSchedParameters gsParams, ref UnitDataSet dataSet)
        {
            if (dataSet.Y_sim == null || dataSet.Y_meas == null)
                return;
            var minTc = (new Vec()).Min(gsParams.TimeConstant_s);
            var maxTc = (new Vec()).Max(gsParams.TimeConstant_s);
            var timeBase_s = dataSet.GetTimeBase();
            var vec = new Vec();

            var resultDict = new Dictionary<double, double>();
            double smallestObjFun = double.PositiveInfinity;
            {
                var objFun = vec.Sum(vec.Abs(vec.Subtract(dataSet.Y_sim, dataSet.Y_meas))).Value;
                resultDict.Add(0, objFun);
                smallestObjFun = objFun;
            }
            // simulate the model while increasing the time delay while decreasing the time-constants and see if this improves fit. 
            double timedelay_best_s = 0;
            double[] y_sim_best = new double[1];
            for (double timedelay_s = timeBase_s; timedelay_s < minTc; timedelay_s += timeBase_s)
            {
                var copiedGsParams = new GainSchedParameters(gsParams);
                copiedGsParams.TimeDelay_s = timedelay_s;
                copiedGsParams.TimeConstant_s = vec.Subtract(gsParams.TimeConstant_s, timedelay_s);

                var gsIdentModel = new GainSchedModel(copiedGsParams, "ident_model");
                var identModelSim = new PlantSimulator(new List<ISimulatableModel> { gsIdentModel });
                var inputDataIdent = new TimeSeriesDataSet();

                int index = 0;
                foreach (var id in gsIdentModel.GetModelInputIDs())
                {
                    inputDataIdent.Add(identModelSim.AddExternalSignal(gsIdentModel, SignalType.External_U, index), dataSet.U.GetColumn(index));
                    index++;
                }

                inputDataIdent.CreateTimestamps(dataSet.GetTimeBase());
                var isOk = identModelSim.Simulate(inputDataIdent, out TimeSeriesDataSet identModelSimData);
                if (isOk)
                {
                    var currentSimY = identModelSimData.GetValues(gsIdentModel.ID, SignalType.Output_Y);
                    var objFun = vec.Sum(vec.Abs(vec.Subtract(currentSimY, dataSet.Y_meas))).Value;
                    resultDict.Add(timedelay_s, objFun);
                    if (objFun < smallestObjFun)
                    {
                        smallestObjFun = objFun;
                        timedelay_best_s = timedelay_s;
                        y_sim_best = currentSimY;
                    }
                }
            }

            if (timedelay_best_s > 0)
            {
                dataSet.Y_sim = y_sim_best;
                gsParams.TimeDelay_s = timedelay_best_s;
                gsParams.TimeConstant_s = vec.Subtract(gsParams.TimeConstant_s, timedelay_best_s);
            }
        }


        /// <summary>
        /// Identify a model when a given set of thresholds is already given in the supplied gsFittingSpecs
        /// 
        /// Since specifying the thresholds simplifies estimation significantly, it is recommended to use this
        /// method over Identify() if possible.
        /// </summary>
        /// <param name="dataSet">tuning data set</param>
        /// <param name="gsFittingSpecs">the object in which the thresholds to be used are given</param>
        /// <param name="doTimeDelayEstimation">set to false to disable estimation of time delays, default is true</param>
        /// <returns></returns>
        public static GainSchedParameters IdentifyForGivenThresholds(UnitDataSet dataSet, GainSchedFittingSpecs gsFittingSpecs, bool doTimeDelayEstimation = true)
        {
            var vec = new Vec();
            GainSchedParameters retModel = new GainSchedParameters();
            retModel.GainSchedParameterIndex = gsFittingSpecs.uGainScheduledInputIndex;
            retModel.LinearGainThresholds = gsFittingSpecs.uGainThresholds;
            // for this to work roubustly, the training set for fitting each model may need to require adding in a "span" of neighboring models. 
            int numberOfInputs = dataSet.U.GetLength(1);
            double gsVarMinU, gsVarMaxU;
            var linearGains = new List<double[]>();
            var timeConstants = new List<double>();
            var allIdsOk = true;
            var warningNotEnoughExitationBetweenAllThresholds = false;
            var dataSetCopy = new UnitDataSet(dataSet);
            // estimate each of the gains one by one
            for (int curGainIdx = 0; curGainIdx < retModel.LinearGainThresholds.Count()+1; curGainIdx++)
            {
                double[] uMinFit = new double[numberOfInputs];
                double[] uMaxFit = new double[numberOfInputs];
                // double? u0 = 0;
                double? u0 = null;

                if (curGainIdx == 0)
                {
                    gsVarMinU = vec.Min(Array2D<double>.GetColumn(dataSetCopy.U, retModel.GainSchedParameterIndex));
                }
                else
                {
                    gsVarMinU = retModel.LinearGainThresholds[curGainIdx-1];
                }
                if (curGainIdx == retModel.LinearGainThresholds.Count())
                {
                    gsVarMaxU = vec.Max(Array2D<double>.GetColumn(dataSetCopy.U, retModel.GainSchedParameterIndex));
                }
                else
                {
                    gsVarMaxU = retModel.LinearGainThresholds[curGainIdx];
                }
                for (int idx = 0; idx < numberOfInputs; idx++)
                {
                    if (idx == retModel.GainSchedParameterIndex)
                    {
                        uMinFit[idx] = gsVarMinU;
                        uMaxFit[idx] = gsVarMaxU;
                    }
                    else
                    {
                        uMinFit[idx] = double.NaN;
                        uMaxFit[idx] = double.NaN;
                    }
                }
                var idResults = IdentifySingleLinearModelForGivenThresholds(ref dataSetCopy, uMinFit, uMaxFit, retModel.GainSchedParameterIndex,u0,false);
                if (idResults.NotEnoughExitationBetweenAllThresholds)
                    warningNotEnoughExitationBetweenAllThresholds = true;
                var uInsideUMinUMax = Vec<double>.GetValuesAtIndices(dataSetCopy.U.GetColumn(retModel.GainSchedParameterIndex), 
                    Index.InverseIndices(dataSetCopy.GetNumDataPoints(),dataSetCopy.IndicesToIgnore));
                var uMaxObserved = (new Vec()).Max(uInsideUMinUMax);
                var uMinObserved = (new Vec()).Min(uInsideUMinUMax);
                if (idResults.LinearGains == null)
                {
                    allIdsOk = false;
                }
                linearGains.Add(idResults.LinearGains);
                timeConstants.Add(idResults.TimeConstant_s);
                dataSetCopy.IndicesToIgnore = null;
            }

            // final gain:above the highest threshold
            retModel.LinearGains = linearGains;
            // time delay
            if (gsFittingSpecs.uTimeConstantThresholds == null)
            {
                retModel.TimeConstant_s = new double[] { vec.Mean(timeConstants.ToArray()).Value };
            }
            else if (Vec.Equal(gsFittingSpecs.uTimeConstantThresholds,gsFittingSpecs.uGainThresholds))
            {
                retModel.TimeConstant_s = timeConstants.ToArray();
                retModel.TimeConstantThresholds = gsFittingSpecs.uTimeConstantThresholds;
            }
            else
            {
                retModel.TimeConstant_s = null;//TODO: currently not supported to find timeconstants separately from gain thresholds. 
            }
            retModel.Fitting = new FittingInfo();
            retModel.Fitting.WasAbleToIdentify = allIdsOk;
            if (!allIdsOk)
                retModel.AddWarning(GainSchedIdentWarnings.UnableToIdentifySomeSubmodels);
            if(warningNotEnoughExitationBetweenAllThresholds)
                retModel.AddWarning(GainSchedIdentWarnings.InsufficientExcitationBetweenEachThresholdToBeCertainOfGains);

            // simulate the model and determine the optimal bias term:
            DetermineOperatingPointAndSimulate(ref retModel, ref dataSet);

            EstimateTimeDelay(ref retModel, ref dataSet);

            return retModel;
        }

        /// <summary>
        /// Determines the operating point(bias) of the gain-scheduled model that gives the smalles bias over the data
        /// The resulting simulated y is stored in dataset.Ysim
        /// </summary>
        /// <param name="gsParams">the gain-scheduled parameters that are to be updated with an operating point.</param>
        /// <param name="dataSet">tuning dataset to be updated with Y_sim </param>
        /// <returns></returns>
        private static bool DetermineOperatingPointAndSimulate(ref GainSchedParameters gsParams, ref UnitDataSet dataSet)
        {
            var gsIdentModel = new GainSchedModel(gsParams, "ident_model");
            var identModelSim = new PlantSimulator(new List<ISimulatableModel> { gsIdentModel });
            var inputDataIdent = new TimeSeriesDataSet();

            var vec = new Vec();

            for  (var index =0; index< dataSet.U.GetNColumns(); index++)
            {
                inputDataIdent.Add(identModelSim.AddExternalSignal(gsIdentModel, SignalType.External_U, index), dataSet.U.GetColumn(index));
            }

            inputDataIdent.CreateTimestamps(dataSet.GetTimeBase());
            var isOk = identModelSim.Simulate(inputDataIdent, out TimeSeriesDataSet identModelSimData);
            if (isOk)
            {
                var simY_nobias = identModelSimData.GetValues(gsIdentModel.ID, SignalType.Output_Y);
                var measY = dataSet.Y_meas;
                var estBias = vec.Mean(vec.Subtract(measY, simY_nobias));
                if (estBias.HasValue)
                {
                    gsParams.OperatingPoint_Y = estBias.Value;
                     gsParams.OperatingPoint_U = 0;
                   // gsParams.OperatingPoint_U = dataSet.U.GetColumn(gsParams.GainSchedParameterIndex).First();
                    dataSet.Y_sim = vec.Add(simY_nobias, gsParams.OperatingPoint_Y);
                    return true;
                }
                else
                {
                    dataSet.Y_sim = simY_nobias;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Perform a global search ("try-and-evaluate")
        /// for the threshold or thresholds that result in the gain-scheduling model with the best 
        /// fit
        /// 
        /// Because this call is based on global search it requires many rounds of linear identification
        /// and is thus fairly computationl expensive.
        /// 
        /// Note also that the operating point or bias is not set in the returned list of parameters.
        /// 
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="gainSchedInputIndex"></param>
        /// <param name="estimateOperatingPoint"> determine the operating point/bias (true by default)</param>
        /// <param name="globalSearchIterations"> number of global search iterations</param>
        /// <param name="gsSearchMin">minimum of the global search(used for pass 2+)</param>
        /// <param name="gsSearchMax">maximum of the global search(used for pass 2+)</param>
        /// <returns>a tuple of: a) A list of all the candiate gain-scheduling parameters that have been found by global search, 
        /// but note that these models do not include operating point, and
        /// b) the simulated output of each of the gain-scheduled paramters in the first list.</returns>
        private static (List<GainSchedParameters>, List<double[]>) IdentifyGainScheduledGainsAndSingleThreshold(UnitDataSet dataSet, 
            int gainSchedInputIndex, bool estimateOperatingPoint=true, 
            int globalSearchIterations = 40, double? gsSearchMin=null, double? gsSearchMax= null )
        {
            // how big a span of the range of u in the dataset to span with trehsolds (should be <= 1.00 and >0.)
            // usually it is pointless to search for thresholds at the edge of the dataset, so should be <1, but always much larger than 0.
            const double GS_RANGE_SEARCH_FRAC = 0.70;

            // when didving the dataset into "a" and "b" above and below the threshold, how many percentage overlap 
            // should these two datasets have 
            const double GAIN_THRESHOLD_A_B_OVERLAP_FACTOR = 0.02;

            UnitDataSet DSa = new UnitDataSet(dataSet);
            UnitDataSet DSb = new UnitDataSet(dataSet);
            int number_of_inputs = dataSet.U.GetNColumns();
            double gsVarMinU = dataSet.U.GetColumn(gainSchedInputIndex).Min();
            double gsVarMaxU = dataSet.U.GetColumn(gainSchedInputIndex).Max();

            // the two returned elements to be populated
            List<GainSchedParameters> potentialGainschedParameters = new List<GainSchedParameters>();
            List<double[]> potentialYSimList = new List<double[]>();

            // begin setting up global search
            double[] potential_gainthresholds = new double[globalSearchIterations];
            // default global search based on the range of values in the given dataset
            if (!gsSearchMin.HasValue || !gsSearchMax.HasValue)
            {
                double gsRange = gsVarMaxU - gsVarMinU;
                double gsSearchRange = gsRange * GS_RANGE_SEARCH_FRAC;
                for (int k = 0; k < globalSearchIterations; k++)
                {
                     potential_gainthresholds[k] = gsVarMinU + (1-GS_RANGE_SEARCH_FRAC) * gsRange / 2 + gsSearchRange * ((double)k / globalSearchIterations);
                }
            }
            else // search based on the given min and max (used for second or third passes to improve accuracy)
            {
                double gsRange = gsSearchMax.Value - gsSearchMin.Value;
                double gsSearchRange = gsRange ;
                for (int k = 0; k < globalSearchIterations; k++)
                {
                    potential_gainthresholds[k] = gsSearchMin.Value +  gsSearchRange * ((double)k / globalSearchIterations);
                }
            }

            // determine the time constants and gains for each for the a/b split of the dataset along the 
            // candidate threshold.
            for (int i = 0; i < potential_gainthresholds.Length; i++)
            {
                GainSchedParameters curGainSchedParams = new GainSchedParameters();
                // Linear gain thresholds
                double[] GS_LinearGainThreshold = new double[] { potential_gainthresholds[i] };
                curGainSchedParams.LinearGainThresholds = GS_LinearGainThreshold;

                List<double[]> curLinearGains = new List<double[]>();
                double[] curTimeConstants = new double[2];
                // a) identiy from the minimum value of gain-sched variable(u) to the candidate threshold
                {
                    double[] uMinFit = new double[number_of_inputs];
                    double[] uMaxFit = new double[number_of_inputs];
                    for (int idx = 0; idx < number_of_inputs; idx++)
                    {
                        if (idx == gainSchedInputIndex)
                        {
                            uMinFit[idx] = gsVarMinU;
                            uMaxFit[idx] = potential_gainthresholds[i] + (gsVarMaxU - gsVarMinU) * GAIN_THRESHOLD_A_B_OVERLAP_FACTOR;
                        }
                        else
                        {
                            uMinFit[idx] = double.NaN;
                            uMaxFit[idx] = double.NaN;
                        }
                    }
                    var ret = IdentifySingleLinearModelForGivenThresholds(ref DSa, uMinFit, uMaxFit, gainSchedInputIndex);
                    curLinearGains.Add(ret.LinearGains);
                    curTimeConstants[0] = ret.TimeConstant_s;
                }
                // b)identiy using data from the maximum value of gain-sched variable(u) to the candidate threshold
                {

                    double[] uMinFit = new double[number_of_inputs];
                    double[] uMaxFit = new double[number_of_inputs];
                    for (int idx = 0; idx < number_of_inputs; idx++)
                    {
                        if (idx == gainSchedInputIndex)
                        {
                            uMinFit[idx] = potential_gainthresholds[i] - (gsVarMaxU - gsVarMinU) * GAIN_THRESHOLD_A_B_OVERLAP_FACTOR;
                            uMaxFit[idx] = gsVarMaxU;
                        }
                        else
                        {
                            uMinFit[idx] = double.NaN;
                            uMaxFit[idx] = double.NaN;
                        }
                    }
                    var ret = IdentifySingleLinearModelForGivenThresholds(ref DSb,uMinFit, uMaxFit, gainSchedInputIndex);
                    curLinearGains.Add(ret.LinearGains);
                    curGainSchedParams.LinearGains = curLinearGains;
                    curTimeConstants[1] = ret.TimeConstant_s; 
                }
                curGainSchedParams.TimeConstant_s = curTimeConstants;
                curGainSchedParams.TimeConstantThresholds = new double[] { potential_gainthresholds[i] };
                curGainSchedParams.LinearGainThresholds[0] = potential_gainthresholds[i];

                DetermineOperatingPointAndSimulate(ref curGainSchedParams, ref DSb);
                potentialGainschedParameters.Add(curGainSchedParams);
                potentialYSimList.Add(DSb.Y_sim);
            }
            return (potentialGainschedParameters,potentialYSimList );
        }

        /// <summary>
        /// When a upper/lower threshold is given, estimate gain for a dataset.
        /// 
        /// Note that if there is no variation in dataSet between uMinFit and uMaxFit, then this method may actually expand these variables out internally.
        /// 
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="uLowerThreshold">the lower threshold of the gain-scheduled input</param>
        /// <param name="uHigherThreshold">the upper threshold of the gain-scheduled input</param>
        /// <param name="gainSchedVarIndex">index of the variable in the model that is gain-scheduled. by the fault 0</param>
        /// <param name="u0"> if set to null, the u between uLowerThreshold and uHigherThreshold is used </param>
        /// <param name="doTimeDelayEstimation"> if set to true, identification also considers time delay (increases computational load, use sparingly) </param>
        /// <returns>return a tuple, the first linear gains is null if unable to identify </returns>
        private static GainSchedSubModelResults IdentifySingleLinearModelForGivenThresholds(ref UnitDataSet dataSet,double[] uLowerThreshold, double[] uHigherThreshold, 
            int gainSchedVarIndex, double? u0= null, bool doTimeDelayEstimation = false )
        {
            var results = new GainSchedSubModelResults();

            const double MAX_tuningSetSpanOutsideThreshold_prc = 81;
            const double STEP_tuningSetSpanOutsideThreshold_prc = 20;

            double MIN_uRangeInChosenDataset = 0.5*(uHigherThreshold[gainSchedVarIndex] - uLowerThreshold[gainSchedVarIndex]);
            var fittingSpecs = new FittingSpecs();

            double tuningSetSpanOutsideThreshold_prc = 0;
            double uRangeInChosenDataset = -1;
            double fitRange = (uHigherThreshold[gainSchedVarIndex] - uLowerThreshold[gainSchedVarIndex]);

            results.NotEnoughExitationBetweenAllThresholds = false;

            int[] incomingIndToIgnore = null;
            if (dataSet.IndicesToIgnore != null)
            {
                incomingIndToIgnore = new int[dataSet.IndicesToIgnore.Count];
                dataSet.IndicesToIgnore.CopyTo( incomingIndToIgnore);
            }
            // if there is close to as many thresholds as there are steps in data, then there may not actually be enough data in the dataset to 
            // this while loop looks at the range betwen uMin and uMax for the data found between the upper and lower thresold. If the range 
            // is low, then the range of u to fit against is attempted increased to capture information as u steps between thresholds. 
            while (uRangeInChosenDataset < MIN_uRangeInChosenDataset && tuningSetSpanOutsideThreshold_prc < MAX_tuningSetSpanOutsideThreshold_prc)
            {
                if (incomingIndToIgnore == null)
                    dataSet.IndicesToIgnore = null;
                else
                    dataSet.IndicesToIgnore = new List<int>(incomingIndToIgnore);
                fittingSpecs.U_min_fit = uLowerThreshold;
                fittingSpecs.U_max_fit = uHigherThreshold;
                fittingSpecs.U_min_fit[gainSchedVarIndex] = uLowerThreshold[gainSchedVarIndex] - fitRange * tuningSetSpanOutsideThreshold_prc/100;
                fittingSpecs.U_max_fit[gainSchedVarIndex] = uHigherThreshold[gainSchedVarIndex] + fitRange * tuningSetSpanOutsideThreshold_prc/100;

                if (!u0.HasValue)
                    fittingSpecs.u0 = new double[] { uLowerThreshold[gainSchedVarIndex] + (uHigherThreshold[gainSchedVarIndex] - uLowerThreshold[gainSchedVarIndex]) / 2 };
                else
                    fittingSpecs.u0 = new double[] { u0.Value };

                dataSet.DetermineIndicesToIgnore(fittingSpecs);
                var indicesLocal = Index.InverseIndices(dataSet.GetNumDataPoints(), dataSet.IndicesToIgnore);
                var uChosen = Vec<double>.GetValuesAtIndices(dataSet.U.GetColumn(gainSchedVarIndex), indicesLocal);
                var uMaxInDataSet = (new Vec()).Max(uChosen);
                var uMinInDataSet = (new Vec()).Min(uChosen);
                uRangeInChosenDataset = uMaxInDataSet - uMinInDataSet;
                if (tuningSetSpanOutsideThreshold_prc > 0)
                {
                    results.NotEnoughExitationBetweenAllThresholds = true;
                }
                tuningSetSpanOutsideThreshold_prc += STEP_tuningSetSpanOutsideThreshold_prc;
            }

            // NB! the algorithm saves a lot of computational time by not doing time delay estimation here!
            var unitModel = UnitIdentifier.IdentifyLinear(ref dataSet, fittingSpecs, false);

            bool doDebug = false; // should be false unless debugging
            if (doDebug)
            {
                var indices = Index.InverseIndices(dataSet.GetNumDataPoints(), dataSet.IndicesToIgnore);
                    Shared.EnablePlots();
                    Plot.FromList(new List<double[]> {
                        Vec<double>.GetValuesAtIndices(dataSet.Y_sim,indices),
                        Vec<double>.GetValuesAtIndices(dataSet.Y_meas,indices),
                        Vec<double>.GetValuesAtIndices(dataSet.U.GetColumn(0),indices)
                        },
                            new List<string> { "y1=y_sim", "y1=y_ref", "y3=u1" },
                        Vec<DateTime>.GetValuesAtIndices(dataSet.Times,indices),
                            "IdentifySingleLinearGainForGivenThresholds_"+ uLowerThreshold[0].ToString("F1") + "_"+ uHigherThreshold[0].ToString("F1"));
                    Shared.DisablePlots();
            }

            var unitParams = unitModel.GetModelParameters();
            if (!unitModel.modelParameters.Fitting.WasAbleToIdentify)
            {
                results.LinearGains = null;
                results.TimeConstant_s = 0;
                results.WasAbleToIdentfiy = false;
            }
            else
            {
                results.LinearGains = unitParams.LinearGains;
                results.TimeConstant_s = unitParams.TimeConstant_s;
                results.WasAbleToIdentfiy = true;
            }
            return results;
        }

        /// <summary>
        /// Runs a simulation for each of the parameters sets given and returns the parameters that best matches the dataset
        /// </summary>
        /// <param name="allGainSchedParams"> all candidate gain-scheduled parameters</param>
        /// <param name="allYsimList"> list of all the simulated y corresponding to each parameter set in the first input</param>
        /// <param name="dataSet">the dataset to be returned, where y_sim is to be set based on the best model.</param>
        /// <returns>a tuple with the paramters of the "best" model and the index of this model among the list provided </returns>
        static private (GainSchedParameters, int) ChooseBestGainScheduledModel(List<GainSchedParameters> allGainSchedParams,
            List<double[]> allYsimList, ref UnitDataSet dataSet)
        {
            var vec = new Vec();
            GainSchedParameters BestGainSchedParams = null;
            double lowest_rms = double.MaxValue;
            int number_of_inputs = dataSet.U.GetNColumns();

            int bestModelIdx = 0;
            for (int curModelIdx = 0; curModelIdx < allGainSchedParams.Count; curModelIdx++)
            {
                var curGainSchedParams = allGainSchedParams[curModelIdx];
                {
                    var curSimY = allYsimList.ElementAt(curModelIdx);
                    var diff = vec.Subtract(curSimY, dataSet.Y_meas);
                    double rms = 0;
                    for (int j = 0; j < curSimY.Length; j++)
                    {
                        rms = rms + Math.Pow(diff.ElementAt(j), 2);
                    }

                    bool doDebugPlot = false;// should be false unless debugging.
                    if (doDebugPlot)
                    {
                        string str_linear_gains = "";
                        for (int j = 0; j < curGainSchedParams.LinearGains.Count; j++)
                        {
                            str_linear_gains = str_linear_gains + string.Concat(curGainSchedParams.LinearGains[j].Select(x => x.ToString("F2",CultureInfo.InvariantCulture))) + " ";
                        }
                        Shared.EnablePlots();
                        Plot.FromList(new List<double[]> {
                                    curSimY,
                                    dataSet.Y_meas,
                                    dataSet.U.GetColumn(0) },
                            new List<string> { "y1=simY1", "y1=y_ref", "y3=u1" },
                        dataSet.GetTimeBase(),
                            "GainSched split idx" + curModelIdx.ToString() + " gains" + str_linear_gains 
                            + "threshold "+string.Concat(curGainSchedParams.LinearGainThresholds.Select(x => x.ToString("F2", CultureInfo.InvariantCulture))));
                        Shared.DisablePlots();
                        Thread.Sleep(1000);
                    }
                    if (rms < lowest_rms)
                    {
                        BestGainSchedParams = curGainSchedParams;
                        lowest_rms = rms;
                        bestModelIdx = curModelIdx;
                    }
                }
            }
            dataSet.Y_sim = allYsimList.ElementAt(bestModelIdx);
            return (BestGainSchedParams, bestModelIdx);
        }




    } 
}
