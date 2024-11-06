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
using System.Text.RegularExpressions;

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
        /// 
        /// This method will also identify thresholds, but with limits on how many thresholds the method can find.
        /// 
        /// See also <c>IdentifyForGivenThresholds</c> for when the thresholds are given, 
        /// this makes identification a much simpler and faster process. 
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="gsFittingSpecs"></param>
        /// <returns></returns>
        static public GainSchedModel Identify(UnitDataSet dataSet, GainSchedFittingSpecs gsFittingSpecs = null)
        {
            const bool doIdentifyV2 = true; 
            const bool chooseModelV2 = false;// this for some reason causes worse performance, better if "false"

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
                    var trueIsSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
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
                GSp_noGainSchedReference.SetOperatingPoint(0, unitParams.Bias);
                //////
                allYSimList.Add(dataSetNoGainSched.Y_sim);
                allGainSchedParams.Add(GSp_noGainSchedReference);
            }
            ////////////////////////////////////////////////////
            // note that this is a fairly computationally heavy call
            // higher values means higher accuracy but at the cost of more computations..
            // 
            int globalSearchIterationsPass1 = 40;//should be big enough, but then more is not better. 
            int globalSearchIterationsPass2 = 20;//should be big enough, but then more is not better. 

            var potentialGainschedParametersList = new List<GainSchedParameters>();
            var potentialYsimList = new List<double[]>();
            if (doIdentifyV2)
                (potentialGainschedParametersList, potentialYsimList) =
                    IdentifyGainScheduledGainsAndSingleThresholdV2(dataSet, gainSchedInputIndex, true, globalSearchIterationsPass1);
            else
                (potentialGainschedParametersList,  potentialYsimList) =
                    IdentifyGainScheduledGainsAndSingleThresholdV1(dataSet, gainSchedInputIndex,true, globalSearchIterationsPass1);


            ////////////////////////////////////////////////////
            // Final step: choose the best GainSched from all the candidates
            allGainSchedParams.AddRange(potentialGainschedParametersList);
            allYSimList.AddRange(potentialYsimList);

            GainSchedParameters bestModel_pass1 ;
            int bestModelIdx_pass1;
            if (chooseModelV2)
                (bestModel_pass1, bestModelIdx_pass1) = ChooseBestModelFromFittingInfo(allGainSchedParams, ref dataSet);
            else
                (bestModel_pass1, bestModelIdx_pass1) =  ChooseBestModelFromSimulationList(allGainSchedParams, allYSimList, ref dataSet);
           //  (var bestModel_pass1, var bestModelIdx_pass1) = ChooseBestModelFromSimulationList(allGainSchedParams, ref dataSet);
            // pass 2:
            const bool DO_PASS2 = true;
            int pass2Width = 0;//0,1 or 2, design parameter about how wide to do pass 2 aroudn pass 1 result.(higher number is at the expense of accuracy)
            if (doIdentifyV2)
                pass2Width = 0;

            GainSchedParameters paramsToReturn = new GainSchedParameters();
            if (bestModelIdx_pass1 > 1 + pass2Width && bestModelIdx_pass1 < allGainSchedParams.Count() - pass2Width && DO_PASS2)
            {
                double? gsSearchMin_pass2 = allGainSchedParams.ElementAt(Math.Max(bestModelIdx_pass1 - 1 - pass2Width, 0)).LinearGainThresholds.First();
                double? gsSearchMax_pass2 = allGainSchedParams.ElementAt(Math.Min(bestModelIdx_pass1 + 1 + pass2Width, allGainSchedParams.Count() - 1)).LinearGainThresholds.First();
                var potentialGainschedParametersList_pass2 = new List<GainSchedParameters>();
                var potentialYsimList_pass2 = new List<double[]>();
                if (doIdentifyV2)
                    (potentialGainschedParametersList_pass2, potentialYsimList_pass2) =
                         IdentifyGainScheduledGainsAndSingleThresholdV2(dataSet, gainSchedInputIndex, true, globalSearchIterationsPass2, gsSearchMin_pass2, gsSearchMax_pass2);
                else
                    (potentialGainschedParametersList_pass2,potentialYsimList_pass2) =
                         IdentifyGainScheduledGainsAndSingleThresholdV1(dataSet, gainSchedInputIndex, true, globalSearchIterationsPass2, gsSearchMin_pass2, gsSearchMax_pass2);
                GainSchedParameters bestModel_pass2;
                int bestModelIdx_pass2;

                if (chooseModelV2)
                    (bestModel_pass2, bestModelIdx_pass2) = ChooseBestModelFromFittingInfo(potentialGainschedParametersList_pass2,ref dataSet);
                else

                    (bestModel_pass2,  bestModelIdx_pass2) = ChooseBestModelFromSimulationList(potentialGainschedParametersList_pass2,
                         potentialYsimList_pass2, ref dataSet);
                paramsToReturn = bestModel_pass2;
            }
            else
            {
                paramsToReturn = bestModel_pass1;
            }

            if (paramsToReturn.Fitting.WasAbleToIdentify)
            {
                //////////////////////////////
                EstimateTimeDelay(ref paramsToReturn, ref dataSet);

                //////////////////////////////
                //// EXPERIMENTAL: determining two time-constants. 
                // const int numTcIterations = 50;
                //  var bestParams = EvaluateMultipleTimeConstantsForGivenGainThreshold(ref paramsToReturn, dataSet,numTcIterations);
            }
            ////////////////////////////////////
            if (paramsToReturn.Fitting == null)
            {
                paramsToReturn.Fitting = new FittingInfo();
            }
            paramsToReturn.Fitting.SolverID = "Identify(thresholds estimated)";
            return new GainSchedModel(paramsToReturn,"identified");
        }

        /// <summary>
        /// 
        /// Identify a model when a given set of thresholds is already given in the supplied gsFittingSpecs
        /// 
        /// </summary>
        /// <param name="dataSet">tuning data set</param>
        /// <param name="gsFittingSpecs">the object in which the thresholds to be used are given</param>
        /// <param name="doTimeDelayEstimation">set to false to disable estimation of time delays, default is true</param>
        /// <returns></returns>
        public static GainSchedModel IdentifyForGivenThresholds(UnitDataSet dataSet, GainSchedFittingSpecs gsFittingSpecs, bool doTimeDelayEstimation = true)
        {
            var vec = new Vec(dataSet.BadDataID);
            GainSchedParameters idParams = new GainSchedParameters();
            idParams.GainSchedParameterIndex = gsFittingSpecs.uGainScheduledInputIndex;
            idParams.LinearGainThresholds = gsFittingSpecs.uGainThresholds;
            // for this to work roubustly, the training set for fitting each model may need to require adding in a "span" of neighboring models. 
            int numberOfInputs = dataSet.U.GetLength(1);
            double gsVarMinU, gsVarMaxU;
            var linearGains = new List<double[]>();
            var timeConstants = new List<double>();
            var allIdsOk = true;
            var warningNotEnoughExitationBetweenAllThresholds = false;
            var dataSetCopy = new UnitDataSet(dataSet);
            // estimate each of the gains one by one
            for (int curGainIdx = 0; curGainIdx < idParams.LinearGainThresholds.Count() + 1; curGainIdx++)
            {
                double[] uMinFit = new double[numberOfInputs];
                double[] uMaxFit = new double[numberOfInputs];
                // double? u0 = 0;
                double? u0 = null;

                if (curGainIdx == 0)
                {
                    gsVarMinU = vec.Min(Array2D<double>.GetColumn(dataSetCopy.U, idParams.GainSchedParameterIndex));
                }
                else
                {
                    gsVarMinU = idParams.LinearGainThresholds[curGainIdx - 1];
                }
                if (curGainIdx == idParams.LinearGainThresholds.Count())
                {
                    gsVarMaxU = vec.Max(Array2D<double>.GetColumn(dataSetCopy.U, idParams.GainSchedParameterIndex));
                }
                else
                {
                    gsVarMaxU = idParams.LinearGainThresholds[curGainIdx];
                }
                for (int idx = 0; idx < numberOfInputs; idx++)
                {
                    if (idx == idParams.GainSchedParameterIndex)
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
                var idResults = IdentifySingleLinearModelForGivenThresholds(ref dataSetCopy, uMinFit, uMaxFit, idParams.GainSchedParameterIndex, u0, false);
                if (idResults.NotEnoughExitationBetweenAllThresholds)
                    warningNotEnoughExitationBetweenAllThresholds = true;
                var uInsideUMinUMax = Vec<double>.GetValuesAtIndices(dataSetCopy.U.GetColumn(idParams.GainSchedParameterIndex),
                    Index.InverseIndices(dataSetCopy.GetNumDataPoints(), dataSetCopy.IndicesToIgnore));
                var uMaxObserved = (new Vec(dataSet.BadDataID)).Max(uInsideUMinUMax);
                var uMinObserved = (new Vec(dataSet.BadDataID)).Min(uInsideUMinUMax);
                if (idResults.LinearGains == null)
                {
                    allIdsOk = false;
                }
      
                linearGains.Add(idResults.LinearGains);
                timeConstants.Add(idResults.TimeConstant_s);
                dataSetCopy.IndicesToIgnore = null;
            }

            // final gain:above the highest threshold
            idParams.LinearGains = linearGains;
            // time constant

            if (gsFittingSpecs.uTimeConstantThresholds == null)
            {
                var averageTimeConstants = vec.Mean(timeConstants.ToArray()).Value;
                idParams.TimeConstant_s = new double[] { averageTimeConstants };
            }
            else if (Vec.Equal(gsFittingSpecs.uTimeConstantThresholds, gsFittingSpecs.uGainThresholds))
            {
                idParams.TimeConstant_s = timeConstants.ToArray();
                idParams.TimeConstantThresholds = gsFittingSpecs.uTimeConstantThresholds;
            }
            else
            {
                idParams.TimeConstant_s = null;//TODO: currently not supported to find timeconstants separately from gain thresholds. 
            }
            if (idParams.Fitting == null)
            {
                idParams.Fitting = new FittingInfo();
            }
            idParams.Fitting.SolverID = "IdentifyForGivenThresholds";
            idParams.Fitting.WasAbleToIdentify = allIdsOk;
            if (!allIdsOk)
                idParams.AddWarning(GainSchedIdentWarnings.UnableToIdentifySomeSubmodels);
            if (warningNotEnoughExitationBetweenAllThresholds)
                idParams.AddWarning(GainSchedIdentWarnings.InsufficientExcitationBetweenEachThresholdToBeCertainOfGains);

            // post-processing : improving the dynamic model terms by analysis after setting the static model
            if (idParams.Fitting.WasAbleToIdentify)
            {
                // simulate the model and determine the optimal bias term:
                DetermineOperatingPointAndSimulate(ref idParams, ref dataSet);

                if (doTimeDelayEstimation)
                    EstimateTimeDelay(ref idParams, ref dataSet);
            }
            return new GainSchedModel(idParams, "identified");
        }

        // -----------------------------------------------------------------------
        //
        // PRIVATE METHODS BELOW
        // 
        //

        /// <summary>
        /// Chooses the best parameter in list,based on the .Fitting information in each model.
        /// </summary>
        /// <param name="candidateGainSchedParams"> all candidate gain-scheduled parameters</param>
        /// <param name="dataSet">the dataset to be returned, where y_sim is to be set based on the best model.</param>
        /// <param name="simulateAndAddToYsimInDataSet"></param>
        /// <param name="doDebugOutput"></param>
        /// <returns>a tuple with the paramters of the "best" model and array of objectives </returns>
        static private (GainSchedParameters, int) ChooseBestModelFromFittingInfo(
            List<GainSchedParameters> candidateGainSchedParams, ref UnitDataSet dataSet, bool simulateAndAddToYsimInDataSet=true, bool doDebugOutput=false)
        {
            var vec = new Vec(dataSet.BadDataID);
            GainSchedParameters BestGainSchedParams = null;
            double lowestObj = double.MaxValue;
            int numInputs = dataSet.U.GetNColumns();

            int bestModelIdx = 0;
            for (int curModelIdx = 0; curModelIdx < candidateGainSchedParams.Count; curModelIdx++)
            {
                var curGainSchedParams = candidateGainSchedParams[curModelIdx];
                {
                    var objFun = curGainSchedParams.Fitting.FitScorePrc;
                    if (doDebugOutput)
                    {
                        string str = "Tc1:";
                        str += curGainSchedParams.TimeConstant_s.First() + " ";
                        if (curGainSchedParams.TimeConstant_s.Length > 1)
                        {
                            str += "Tc2:" +curGainSchedParams.TimeConstant_s.ElementAt(1) + " ";
                            str += "thr:"+ curGainSchedParams.TimeConstantThresholds.ElementAt(0) + " ";
                        }
                        Console.WriteLine("model idx:" + curModelIdx + " FitScore:" + objFun + " " + str);
                    }
                        // 
                    if (objFun < lowestObj)
                    {
                        BestGainSchedParams = curGainSchedParams;
                        lowestObj = objFun;
                        bestModelIdx = curModelIdx;
                    }
                }
            }
            if (simulateAndAddToYsimInDataSet)
            {
                var bestModel = new GainSchedModel(BestGainSchedParams);
                PlantSimulator.SimulateSingle(dataSet, bestModel, true);
            }
            return (BestGainSchedParams, bestModelIdx);
        }




        /// <summary>
        /// Chooses the best parameter in list, given a list of the simulation results of all of the candidates
        /// </summary>
        /// <param name="candidateGainSchedParams"> all candidate gain-scheduled parameters</param>
        /// <param name="candidateYsim"> list of all the simulated y corresponding to each parameter set in the first input</param>
        /// <param name="dataSet">the dataset to be returned, where y_sim is to be set based on the best model.</param>
        /// <param name="doDebugOutput">give debug output to console</param>
        /// <returns>a tuple with the paramters of the "best" model and array of objectives </returns>
        static private (GainSchedParameters, int) ChooseBestModelFromSimulationList(List<GainSchedParameters> candidateGainSchedParams,
            List<double[]> candidateYsim, ref UnitDataSet dataSet, bool doDebugOutput = false)
        {
            var vec = new Vec(dataSet.BadDataID);
            GainSchedParameters BestGainSchedParams = null;
            double lowestObj = double.MaxValue;
            int numInputs = dataSet.U.GetNColumns();

            int bestModelIdx = 0;
            for (int curModelIdx = 0; curModelIdx < candidateGainSchedParams.Count; curModelIdx++)
            {
                var curGainSchedParams = candidateGainSchedParams[curModelIdx];
                {
                    var curSimY = candidateYsim.ElementAt(curModelIdx);
                    var residuals = vec.Abs(vec.Subtract(curSimY, dataSet.Y_meas));

                    //V1: rms-based selection.                    
                    /* double rms = 0;
                     for (int j = 0; j < curSimY.Length; j++)
                     {
                         rms = rms + Math.Pow(residuals.ElementAt(j), 2);
                     }
                     if (rms < lowestRms)
                     {
                         BestGainSchedParams = curGainSchedParams;
                         lowestRms = rms;
                         bestModelIdx = curModelIdx;
                     }*/

                    //V2: abs value of residuals : slightly better in gain-scheduled ramp tests.
                    {
                        var objFun = vec.Sum(residuals).Value;
                        // 
                        if (objFun < lowestObj)
                        {
                            BestGainSchedParams = curGainSchedParams;
                            lowestObj = objFun;
                            bestModelIdx = curModelIdx;
                        }
                        /*      if (doDebugOutput)
                              {
                                  string str = "";
                                  if (curGainSchedParams.TimeConstant_s.Count() == 2)
                                      str += "Tc1:" + curGainSchedParams.TimeConstant_s[0] + " Tc1:" + curGainSchedParams.TimeConstant_s[1];
                                  else if (curGainSchedParams.TimeConstant_s.Count() == 1)
                                      str += "Tc1:" + curGainSchedParams.TimeConstant_s[0];
                                  else if (curGainSchedParams.TimeConstant_s.Count() == 1)
                                      str += "no Tc";
                                  if (curGainSchedParams.TimeConstantThresholds!=null)
                                      str += " threshold:" + curGainSchedParams.TimeConstantThresholds[0];
                                  Console.WriteLine("idx:" + curModelIdx + " objFun:" + objFun.ToString("F5") + "("+ str + ")");
                              }*/
                    }

                    // debug plot
                    bool doDebugPlot = false;// should be false unless debugging.
                    if (doDebugPlot)
                    {
                        string str_linear_gains = "";
                        for (int j = 0; j < curGainSchedParams.LinearGains.Count; j++)
                        {
                            str_linear_gains = str_linear_gains + string.Concat(curGainSchedParams.LinearGains[j].Select(x => x.ToString("F2", CultureInfo.InvariantCulture))) + " ";
                        }
                        Shared.EnablePlots();
                        Plot.FromList(new List<double[]> {
                                    curSimY,
                                    dataSet.Y_meas,
                                    dataSet.U.GetColumn(0) },
                            new List<string> { "y1=simY1", "y1=y_ref", "y3=u1" },
                        dataSet.GetTimeBase(),
                            "GainSched split idx" + curModelIdx.ToString() + " gains" + str_linear_gains
                            + "threshold " + string.Concat(curGainSchedParams.LinearGainThresholds.Select(x => x.ToString("F2", CultureInfo.InvariantCulture))));
                        Shared.DisablePlots();
                        Thread.Sleep(1000);
                    }

                }
            }
            dataSet.Y_sim = candidateYsim.ElementAt(bestModelIdx);
            return (BestGainSchedParams, bestModelIdx);
        }

        /// <summary>
        /// Determines the operating point, by selecting U, calculating a Y that causes the smalles offset of the dataset
        /// 
        /// The resulting simulated y is stored in dataset.Ysim
        /// </summary>
        /// <param name="gsParams">the gain-scheduled parameters that are to be updated with an operating point.</param>
        /// <param name="dataSet">tuning dataset to be updated with Y_sim </param>
        /// <returns>true if able to estiamte bias, otherwise false</returns>
        private static bool DetermineOperatingPointAndSimulate(ref GainSchedParameters gsParams, ref UnitDataSet dataSet)
        {
            // if set to true, then the operating point is set to the average U in the dataset, otherwise it is set to equal the start
            const bool doMeanU = false; // can be true or false, makes little difference?

            var gsIdentModel = new GainSchedModel(gsParams, "ident_model");
            var vec = new Vec(dataSet.BadDataID);

            //V1: set the operating point to equal the first data point in the tuning set
            var desiredOpU = dataSet.U.GetColumn(gsParams.GainSchedParameterIndex).First();
            //V2: set the operating point to equal the first data point in the tuning set
            var val = vec.Mean(vec.GetValues(dataSet.U.GetColumn(gsParams.GainSchedParameterIndex), dataSet.IndicesToIgnore));
            if (val.HasValue && doMeanU)
            {
                desiredOpU = dataSet.U.GetColumn(gsParams.GainSchedParameterIndex).First();
            }
            gsParams.MoveOperatingPointUWithoutChangingModel(desiredOpU);
            
            (var isOk, var y_sim) = PlantSimulator.SimulateSingle(dataSet, gsIdentModel,false);

            if (isOk)
            {
                var simY_nobias = y_sim;
                var estBias = vec.Mean(vec.Subtract(vec.GetValues(dataSet.Y_meas, dataSet.IndicesToIgnore),
                    vec.GetValues(simY_nobias, dataSet.IndicesToIgnore)));

                if (estBias.HasValue)
                {
                    gsParams.IncreaseOperatingPointY(estBias.Value);
                    (var isOk2, var y_sim2) = PlantSimulator.SimulateSingle(dataSet, gsIdentModel, false);
                    dataSet.Y_sim = y_sim2;
                    if (gsParams.Fitting == null)
                        gsParams.Fitting = new FittingInfo();
                    gsParams.Fitting.FitScorePrc = FitScoreCalculator.Calc(dataSet.Y_meas,dataSet.Y_sim);
                    gsParams.Fitting.WasAbleToIdentify = true;
                    gsParams.Fitting.NFittingTotalDataPoints = dataSet.GetNumDataPoints();
                    if  (dataSet.IndicesToIgnore != null)
                        gsParams.Fitting.NFittingBadDataPoints = dataSet.IndicesToIgnore.Count();
                    return true;
                }
                else
                {
                    if (gsParams.Fitting == null)
                        gsParams.Fitting = new FittingInfo();
                    gsParams.Fitting.WasAbleToIdentify = false;
                    dataSet.Y_sim = simY_nobias;
                    return false;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the given model to include a time-delay term if applicable.
        /// Method is based on trial-and-error reducing the estimated time-constants while increasing the time-delay and seeing 
        /// if this improves the fit of the model 
        /// </summary>
        /// <param name="gsParams">estiamted paramters from other estimation, time-constants should be estiamted, but time-delays are zero, both time-delays and time-constants are updated by this method</param>
        /// <param name="dataSet"></param>
        private static void EstimateTimeDelay(ref GainSchedParameters gsParams, ref UnitDataSet dataSet)
        {
            bool doDebugConsoleOut = false;

            if (dataSet.Y_sim == null || dataSet.Y_meas == null)
                return;
            var minTc = (new Vec(dataSet.BadDataID)).Min(gsParams.TimeConstant_s);
            var maxTc = (new Vec(dataSet.BadDataID)).Max(gsParams.TimeConstant_s);
            var timeBase_s = dataSet.GetTimeBase();
            var vec = new Vec(dataSet.BadDataID);

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
                (var isOk, var y_sim) = PlantSimulator.SimulateSingle(dataSet, gsIdentModel, false);

                if (isOk)
                {
                    var objFun = vec.Sum(vec.Abs(vec.Subtract(y_sim, dataSet.Y_meas))).Value;
                    resultDict.Add(timedelay_s, objFun);

                    if (doDebugConsoleOut)
                        Console.WriteLine("objFun:" + objFun + " Td:" + timedelay_s);

                    if (objFun < smallestObjFun)
                    {
                        smallestObjFun = objFun;
                        timedelay_best_s = timedelay_s;
                        y_sim_best = y_sim;
                    }
                }
            }

            if (timedelay_best_s > 0)
            {
                double CORRECTIONFACTOR = 2.0;// not sure why this works best.

                dataSet.Y_sim = y_sim_best;
                gsParams.TimeDelay_s = timedelay_best_s;
                gsParams.TimeConstant_s = vec.Subtract(gsParams.TimeConstant_s, timedelay_best_s * CORRECTIONFACTOR);
            }
        }

        /// <summary>
        /// Takes as an input a model that already has a threshold for the linear gains that has been determined 
        /// by other means, and then evalutes if model match improves if using two -2-  different time-constants. 
        /// 
        /// This method will only have a chance of finding time-constants if the given gsParams has a reaonable LinearGainThreshold and LinearGains
        /// 
        /// </summary>
        /// <param name="gsParams"></param>
        /// <param name="dataSet"></param>
        /// <param name="globalSearchIterations"></param>
        /// <param name="gsSearchMin"></param>
        /// <param name="gsSearchMax"></param>
        private static GainSchedParameters EvaluateMultipleTimeConstantsForGivenGainThreshold(ref GainSchedParameters gsParams, UnitDataSet dataSet,
            int globalSearchIterations = 40, double? gsSearchMin = null, double? gsSearchMax = null)
        {
            bool doConsoleDebug = true;
            double smallestMeaningfullTc_s = dataSet.GetTimeBase()/4;

            // this method should not return multiple time-constant unless this is superior to a single time-constants
            // this methods should not have a time-constant threshold different from the gain-threshold unless this is clearly superior

            // how big a span of the range of u in the dataset to span with trehsolds (should be <= 1.00 and >0.)
            // usually it is pointless to search for thresholds at the edge of the dataset, so should be <1, but always much larger than 0.
            const double GS_RANGE_SEARCH_FRAC = 0.70;

            // when didving the dataset into "a" and "b" above and below the threshold, how many percentage overlap 
            // should these two datasets have 
            const double GAIN_THRESHOLD_A_B_OVERLAP_FACTOR = 0.02;

            UnitDataSet DSa = new UnitDataSet(dataSet);
            UnitDataSet DSb = new UnitDataSet(dataSet);
            UnitDataSet DS_commonTc1 = new UnitDataSet(dataSet);
            UnitDataSet DS_commonTc2 = new UnitDataSet(dataSet);
            UnitDataSet DS_separateTc = new UnitDataSet(dataSet);


            int number_of_inputs = dataSet.U.GetNColumns();
            double gsVarMinU = dataSet.U.GetColumn(gsParams.GainSchedParameterIndex).Min();
            double gsVarMaxU = dataSet.U.GetColumn(gsParams.GainSchedParameterIndex).Max();

            // the two returned elements to be populated
            List<GainSchedParameters> candGainschedParameters = new List<GainSchedParameters>();
            List<double[]> candYSimList = new List<double[]>();
            candGainschedParameters.Add(gsParams);
            candYSimList.Add(dataSet.Y_sim);

            if (doConsoleDebug)
            {
                Console.WriteLine("given model:");
                Console.WriteLine(new GainSchedModel(gsParams));
            }

            // begin setting up global search
            double[] candidateTcThresholds = new double[globalSearchIterations];
            // default global search based on the range of values in the given dataset
            if (!gsSearchMin.HasValue || !gsSearchMax.HasValue)
            {
                double gsRange = gsVarMaxU - gsVarMinU;
                double gsSearchRange = gsRange * GS_RANGE_SEARCH_FRAC;
                for (int k = 0; k < globalSearchIterations; k++)
                {
                    candidateTcThresholds[k] = gsVarMinU + (1 - GS_RANGE_SEARCH_FRAC) * gsRange / 2 + gsSearchRange * ((double)k / globalSearchIterations);
                }
            }
            else if (gsSearchMin.Value == gsSearchMax.Value)
            {
                candidateTcThresholds = new double[] { gsSearchMin.Value };
            }
            else // search based on the given min and max (used for second or third passes to improve accuracy)
            {
                double gsRange = gsSearchMax.Value - gsSearchMin.Value;
                double gsSearchRange = gsRange;
                for (int k = 0; k < globalSearchIterations; k++)
                {
                    candidateTcThresholds[k] = gsSearchMin.Value + gsSearchRange * ((double)k / globalSearchIterations);
                }
            }

            // determine the time constants and gains for each for the a/b split of the dataset along the 
            // candidate threshold.
            for (int i = 0; i < candidateTcThresholds.Length; i++)
            {
                GainSchedParameters curGainSchedParams_separateTc = new GainSchedParameters(gsParams);
                // Linear gain thresholds
                double[] GS_candTcThreshold = new double[] { candidateTcThresholds[i] };
                curGainSchedParams_separateTc.TimeConstantThresholds = GS_candTcThreshold;

                List<double[]> curLinearGains = new List<double[]>();
                double[] curTimeConstants = new double[2];
                // a) identiy from the minimum value of gain-sched variable(u) to the candidate threshold
                {
                    double[] uMinFit = new double[number_of_inputs];
                    double[] uMaxFit = new double[number_of_inputs];
                    for (int idx = 0; idx < number_of_inputs; idx++)
                    {
                        if (idx == gsParams.GainSchedParameterIndex)
                        {
                            uMinFit[idx] = gsVarMinU;
                            uMaxFit[idx] = candidateTcThresholds[i] + (gsVarMaxU - gsVarMinU) * GAIN_THRESHOLD_A_B_OVERLAP_FACTOR;
                        }
                        else
                        {
                            uMinFit[idx] = double.NaN;
                            uMaxFit[idx] = double.NaN;
                        }
                    }
                    var ret = IdentifySingleLinearModelForGivenThresholds(ref DSa, uMinFit, uMaxFit, gsParams.GainSchedParameterIndex);
                    curLinearGains.Add(ret.LinearGains);
                    curTimeConstants[0] = ret.TimeConstant_s;
                }
                // b)identify using data from the maximum value of gain-sched variable(u) to the candidate threshold
                {
                    double[] uMinFit = new double[number_of_inputs];
                    double[] uMaxFit = new double[number_of_inputs];
                    for (int idx = 0; idx < number_of_inputs; idx++)
                    {
                        if (idx == gsParams.GainSchedParameterIndex)
                        {
                            uMinFit[idx] = candidateTcThresholds[i] - (gsVarMaxU - gsVarMinU) * GAIN_THRESHOLD_A_B_OVERLAP_FACTOR;
                            uMaxFit[idx] = gsVarMaxU;
                        }
                        else
                        {
                            uMinFit[idx] = double.NaN;
                            uMaxFit[idx] = double.NaN;
                        }
                    }
                    var ret = IdentifySingleLinearModelForGivenThresholds(ref DSb, uMinFit, uMaxFit, gsParams.GainSchedParameterIndex);
                    curLinearGains.Add(ret.LinearGains);
                    curGainSchedParams_separateTc.LinearGains = curLinearGains;
                    curTimeConstants[1] = ret.TimeConstant_s;
                }
                if(doConsoleDebug)
                    Console.WriteLine("Tc1:" + curTimeConstants[0]+ " Tc2:" + curTimeConstants[1] + " threshold:" + candidateTcThresholds[i]);

                // ---------------------------------
                // step2 :
                // compare using 
                // separate time-constants
                // using the time-constant found in the first half
                // using the time-constant found in the second half.
                // (in principle you could also try to identify the time-constant that work best across the dataset given frozen steady states)

                var candModelsStep2 = new List<GainSchedParameters>();
                var candYsimStep2 = new List<double[]>();
                // add the given model to the canidate set

                if (Math.Abs(curTimeConstants[0] - curTimeConstants[1]) > smallestMeaningfullTc_s)
                {
                    curGainSchedParams_separateTc.TimeConstant_s = curTimeConstants;
                    curGainSchedParams_separateTc.TimeConstantThresholds = new double[] { candidateTcThresholds[i] };
                    DetermineOperatingPointAndSimulate(ref curGainSchedParams_separateTc, ref DS_separateTc);
                    candModelsStep2.Add(new GainSchedParameters(curGainSchedParams_separateTc));
                    //   candYsimStep2.Add((double[])DS_separateTc.Y_sim.Clone());
                }
                else
                {
                    curGainSchedParams_separateTc.TimeConstant_s = new double[] { curTimeConstants.First() };
                    curGainSchedParams_separateTc.TimeConstantThresholds = null;
                    DetermineOperatingPointAndSimulate(ref curGainSchedParams_separateTc, ref DS_separateTc);
                    candModelsStep2.Add(new GainSchedParameters(curGainSchedParams_separateTc));
                }
/*
                {
                    var curGainSchedParams_commonTc1 = new GainSchedParameters(curGainSchedParams_separateTc);
                    curGainSchedParams_commonTc1.TimeConstant_s = new double[] { curTimeConstants[0] };
                    curGainSchedParams_commonTc1.TimeConstantThresholds = null;
                    DetermineOperatingPointAndSimulate(ref curGainSchedParams_commonTc1, ref DS_commonTc1);
                    candModelsStep2.Add(new GainSchedParameters(curGainSchedParams_commonTc1));
                    // candYsimStep2.Add((double[])DS_commonTc1.Y_sim.Clone());
                }
                {
                    var curGainSchedParams_commonTc2 = new GainSchedParameters(curGainSchedParams_separateTc);
                    curGainSchedParams_commonTc2.TimeConstant_s = new double[] { curTimeConstants[1] };
                    curGainSchedParams_commonTc2.TimeConstantThresholds = null;
                    DetermineOperatingPointAndSimulate(ref curGainSchedParams_commonTc2, ref DS_commonTc2);
                    candModelsStep2.Add(new GainSchedParameters(curGainSchedParams_commonTc2));
                    // candYsimStep2.Add((double[])DS_commonTc2.Y_sim.Clone());
                }
*/

                bool doDebugPlot = false;
                if (doDebugPlot)
                {
                    Shared.EnablePlots();
                    candYsimStep2.Add(DS_commonTc1.Y_meas);
                    Plot.FromList(candYsimStep2,
                        new List<string> { "y1=y_separateTc", "y1=y_commonTc1", "y1=y_commonTc2 ", "y1=y_meas", }, DS_commonTc1.Times,
                        "DEBUGthreshold" + candidateTcThresholds[i].ToString("F1"));
                    Shared.DisablePlots();
                    Task.Delay(100);
                }
                (var bestModelStep2, var indexOfBestModel) = ChooseBestModelFromFittingInfo(candModelsStep2, ref DSa, false);
                candGainschedParameters.Add(bestModelStep2);
                //  candYSimList.Add((double[])DSa.Y_sim.Clone());
            }


            (var bestGSParams, int bestIdx) = ChooseBestModelFromFittingInfo(candGainschedParameters, ref dataSet,true, doConsoleDebug);
            gsParams = bestGSParams;
            return bestGSParams;
        }




        /// <summary>
        /// Perform a global search ("try-and-evaluate")
        /// for the threshold or thresholds that result in the gain-scheduling model with the best 
        /// fit
        /// 
        /// Unlike V1, this method calls the IdentifyForGivenThresholds(), based on the observation that this method has quite
        /// good performance. 
        /// 
        /// 
        /// Note also that the operating point or bias is not set in the returned list of parameters.
        /// and that time-delays are not considered yet (this is done as a final step.)
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
        private static (List<GainSchedParameters>, List<double[]>) IdentifyGainScheduledGainsAndSingleThresholdV2(UnitDataSet dataSet,
            int gainSchedInputIndex, bool estimateOperatingPoint = true,
            int globalSearchIterations = 40, double? gsSearchMin = null, double? gsSearchMax = null)
        {
            // how big a span of the range of u in the dataset to span with trehsolds (should be <= 1.00 and >0.)
            // usually it is pointless to search for thresholds at the edge of the dataset, so should be <1, but always much larger than 0.
            const double GS_RANGE_SEARCH_FRAC = 0.70;

            UnitDataSet internalDS = new UnitDataSet(dataSet);

            var vec = new Vec(dataSet.BadDataID);

            int number_of_inputs = dataSet.U.GetNColumns();
            double gsVarMinU = vec.GetValues(dataSet.U.GetColumn(gainSchedInputIndex), dataSet.IndicesToIgnore).Min();
            double gsVarMaxU = vec.GetValues(dataSet.U.GetColumn(gainSchedInputIndex), dataSet.IndicesToIgnore).Max();

            // the two returned elements to be populated
            List<GainSchedParameters> candGainschedParameters = new List<GainSchedParameters>();
            List<double[]> candYSimList = new List<double[]>();

            // begin setting up global search
            double[] candidateGainThresholds = new double[globalSearchIterations];
            // default global search based on the range of values in the given dataset
            if (!gsSearchMin.HasValue || !gsSearchMax.HasValue)
            {
                double gsRange = gsVarMaxU - gsVarMinU;
                double gsSearchRange = gsRange * GS_RANGE_SEARCH_FRAC;
                for (int k = 0; k < globalSearchIterations; k++)
                {
                    candidateGainThresholds[k] = gsVarMinU + (1 - GS_RANGE_SEARCH_FRAC) * gsRange / 2 + gsSearchRange * ((double)k / globalSearchIterations);
                }
            }
            else // search based on the given min and max (used for second or third passes to improve accuracy)
            {
                double gsRange = gsSearchMax.Value - gsSearchMin.Value;
                double gsSearchRange = gsRange;
                for (int k = 0; k < globalSearchIterations; k++)
                {
                    candidateGainThresholds[k] = gsSearchMin.Value + gsSearchRange * ((double)k / globalSearchIterations);
                }
            }
            var gsFittingSpecs = new GainSchedFittingSpecs();
            // determine the time constants and gains for each for the a/b split of the dataset along the 
            // candidate threshold.
            for (int i = 0; i < candidateGainThresholds.Length; i++)
            {
                GainSchedParameters curGainSchedParams_separateTc = new GainSchedParameters();
                // Linear gain thresholds
                double[] GS_LinearGainThreshold = new double[] { candidateGainThresholds[i] };
                curGainSchedParams_separateTc.LinearGainThresholds = GS_LinearGainThreshold;

                gsFittingSpecs.uGainThresholds = GS_LinearGainThreshold;
                var idModel = IdentifyForGivenThresholds(internalDS, gsFittingSpecs,false);
                candGainschedParameters.Add(idModel.modelParameters);
                candYSimList.Add(internalDS.Y_sim); 


                /*
                bool doDebugPlot = false;
                if (doDebugPlot)
                {
                    Shared.EnablePlots();
                    candYsimStep2.Add(DS_commonTc1.Y_meas);
                    Plot.FromList(candYsimStep2,
                        new List<string> { "y1=y_separateTc", "y1=y_commonTc1", "y1=y_commonTc2 ", "y1=y_meas", }, DS_commonTc1.Times,
                        "DEBUGthreshold" + candidateGainThresholds[i].ToString("F1"));
                    Shared.DisablePlots();
                    Task.Delay(100);
                }*/
                
            }

            return (candGainschedParameters, candYSimList);
        }









        /// <summary>
        /// (Deprecated??????)
        /// Perform a global search ("try-and-evaluate")
        /// for the threshold or thresholds that result in the gain-scheduling model with the best 
        /// fit
        /// 
        /// Because this call is based on global search it requires many rounds of linear identification
        /// and is thus fairly computationl expensive.
        /// 
        /// Note also that the operating point or bias is not set in the returned list of parameters.
        /// and that time-delays are not considered yet (this is done as a final step.)
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
        private static (List<GainSchedParameters>, List<double[]>) IdentifyGainScheduledGainsAndSingleThresholdV1(UnitDataSet dataSet, 
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
            UnitDataSet DS_commonTc1 = new UnitDataSet(dataSet);
            UnitDataSet DS_commonTc2 = new UnitDataSet(dataSet);
            UnitDataSet DS_separateTc = new UnitDataSet(dataSet);

            var vec = new Vec(dataSet.BadDataID);

            int number_of_inputs = dataSet.U.GetNColumns();
            double gsVarMinU = vec.GetValues(dataSet.U.GetColumn(gainSchedInputIndex),dataSet.IndicesToIgnore).Min();
            double gsVarMaxU = vec.GetValues(dataSet.U.GetColumn(gainSchedInputIndex),dataSet.IndicesToIgnore).Max();

            // the two returned elements to be populated
            List<GainSchedParameters> candGainschedParameters = new List<GainSchedParameters>();
            List<double[]> candYSimList = new List<double[]>();

            // begin setting up global search
            double[] candidateGainThresholds = new double[globalSearchIterations];
            // default global search based on the range of values in the given dataset
            if (!gsSearchMin.HasValue || !gsSearchMax.HasValue)
            {
                double gsRange = gsVarMaxU - gsVarMinU;
                double gsSearchRange = gsRange * GS_RANGE_SEARCH_FRAC;
                for (int k = 0; k < globalSearchIterations; k++)
                {
                     candidateGainThresholds[k] = gsVarMinU + (1-GS_RANGE_SEARCH_FRAC) * gsRange / 2 + gsSearchRange * ((double)k / globalSearchIterations);
                }
            }
            else // search based on the given min and max (used for second or third passes to improve accuracy)
            {
                double gsRange = gsSearchMax.Value - gsSearchMin.Value;
                double gsSearchRange = gsRange ;
                for (int k = 0; k < globalSearchIterations; k++)
                {
                    candidateGainThresholds[k] = gsSearchMin.Value +  gsSearchRange * ((double)k / globalSearchIterations);
                }
            }

            // determine the time constants and gains for each for the a/b split of the dataset along the 
            // candidate threshold.
            for (int i = 0; i < candidateGainThresholds.Length; i++)
            {
                GainSchedParameters curGainSchedParams_separateTc = new GainSchedParameters();
                // Linear gain thresholds
                double[] GS_LinearGainThreshold = new double[] { candidateGainThresholds[i] };
                curGainSchedParams_separateTc.LinearGainThresholds = GS_LinearGainThreshold;

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
                            uMaxFit[idx] = candidateGainThresholds[i] + (gsVarMaxU - gsVarMinU) * GAIN_THRESHOLD_A_B_OVERLAP_FACTOR;
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
                // b)identify using data from the maximum value of gain-sched variable(u) to the candidate threshold
                {

                    double[] uMinFit = new double[number_of_inputs];
                    double[] uMaxFit = new double[number_of_inputs];
                    for (int idx = 0; idx < number_of_inputs; idx++)
                    {
                        if (idx == gainSchedInputIndex)
                        {
                            uMinFit[idx] = candidateGainThresholds[i] - (gsVarMaxU - gsVarMinU) * GAIN_THRESHOLD_A_B_OVERLAP_FACTOR;
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
                    curGainSchedParams_separateTc.LinearGains = curLinearGains;
                    curTimeConstants[1] = ret.TimeConstant_s; 
                }
                // ---------------------------------
                // step2 :
                //compare using 
                // separate time-constants
                // using the time-constant found in the first half
                // using the time-constant found in the second half.
                // (in principle you could also try to identify the time-constant that work best across the dataset given frozen steady states)

                var candModelsStep2 = new List<GainSchedParameters>();
                var candYsimStep2 = new List<double[]>();
                {
                    curGainSchedParams_separateTc.TimeConstant_s = curTimeConstants;
                    curGainSchedParams_separateTc.TimeConstantThresholds = new double[] { candidateGainThresholds[i] };
                    curGainSchedParams_separateTc.LinearGainThresholds[0] = candidateGainThresholds[i];
                    DetermineOperatingPointAndSimulate(ref curGainSchedParams_separateTc, ref DS_separateTc);
                    candModelsStep2.Add(curGainSchedParams_separateTc);
                    candYsimStep2.Add(DS_separateTc.Y_sim);

                }
                {
                    var curGainSchedParams_commonTc1 = new GainSchedParameters(curGainSchedParams_separateTc);
                    curGainSchedParams_commonTc1.TimeConstant_s = new double[] { curTimeConstants[0] };
                    curGainSchedParams_commonTc1.TimeConstantThresholds = null;
                    DetermineOperatingPointAndSimulate(ref curGainSchedParams_commonTc1, ref DS_commonTc1);
                    candModelsStep2.Add(curGainSchedParams_commonTc1);
                    candYsimStep2.Add(DS_commonTc1.Y_sim);
                }
                {
                    var curGainSchedParams_commonTc2 = new GainSchedParameters(curGainSchedParams_separateTc);
                    curGainSchedParams_commonTc2.TimeConstant_s = new double[] { curTimeConstants[1] };
                    curGainSchedParams_commonTc2.TimeConstantThresholds = null;
                    DetermineOperatingPointAndSimulate(ref curGainSchedParams_commonTc2, ref DS_commonTc2);
                    candModelsStep2.Add(curGainSchedParams_commonTc2);
                    candYsimStep2.Add(DS_commonTc2.Y_sim);
                }
                bool doDebugPlot = false;
                if (doDebugPlot)
                {
                    Shared.EnablePlots();
                    candYsimStep2.Add(DS_commonTc1.Y_meas);
                    Plot.FromList(candYsimStep2,
                        new List<string> { "y1=y_separateTc", "y1=y_commonTc1", "y1=y_commonTc2 ", "y1=y_meas", }, DS_commonTc1.Times,
                        "DEBUGthreshold" + candidateGainThresholds[i].ToString("F1"));
                    Shared.DisablePlots();
                    Task.Delay(100);
                }
                (var bestModelStep2,var indexOfBestModel) = ChooseBestModelFromSimulationList(candModelsStep2, candYsimStep2, ref DSa);
                candGainschedParameters.Add(bestModelStep2);
                candYSimList.Add(DSa.Y_sim);
            }
            return (candGainschedParameters,candYSimList );
        }

        /// <summary>
        /// When a upper/lower threshold is given, estimate local linear model for a dataset.
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
                var uMaxInDataSet = (new Vec(dataSet.BadDataID)).Max(uChosen);
                var uMinInDataSet = (new Vec(dataSet.BadDataID)).Min(uChosen);
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
                if (unitParams.TimeConstant_s>dataSet.GetTimeBase())
                    results.TimeConstant_s = unitParams.TimeConstant_s;
                else
                    results.TimeConstant_s = 0;
                results.WasAbleToIdentfiy = true;
            }
            return results;
        }




    } 
}
