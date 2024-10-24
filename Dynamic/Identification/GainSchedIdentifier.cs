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

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Attempts to identify a gain-scheduled model, a model that uses multiple local linear models to approximate a nonlinearity.
    /// Should not be confused with gain-scheduling in terms of PID-control.
    /// </summary>
    static public class GainSchedIdentifier
    {
        const int THRESHOLD_GLOBALSEARCH_MAX_IT = 40; // TODO: magic number
        const double GAIN_THRESHOLD_MAGIC_FACTOR = 0.02;// TODO: Magic number
        const double GAIN_THRESHOLD_MAGIC_FACTOR_2 = 0.09;//TODO magic number

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

            UnitDataSet DS1 = new UnitDataSet(dataSet);
            int number_of_inputs = dataSet.U.GetNColumns();
            GainSchedParameters GSp_noGainSchedReference = new GainSchedParameters();

            // ## 1gain 1 time constant ##
            // Linear gain thresholds
            double[] GS_LinearGainThreshold1 = new double[] { };
            GSp_noGainSchedReference.LinearGainThresholds = GS_LinearGainThreshold1;

            // Reference case: no gain scheduling 
            {
                UnitModel UM1 = UnitIdentifier.IdentifyLinear(ref DS1, null, false);// Todo:consider modelling with nonlinear model?
                UnitParameters UMp1 = UM1.GetModelParameters();

                // Simulate
                /* double[] y_ref = dataSet.Y_meas;
                  int timeBase_s = 1;
                  var plantSim = new PlantSimulator(new List<ISimulatableModel> { UM1 });
                  var inputData = new TimeSeriesDataSet();
                  for (int k = 0; k < number_of_inputs; k++)
                  {
                      inputData.Add(plantSim.AddExternalSignal(UM1, SignalType.External_U, (int)INDEX.FIRST), dataSet.U.GetColumn(k));
                  }
                  inputData.CreateTimestamps(timeBase_s);
                  var CorrectisSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
                  double[] simY1 = simData.GetValues(UM1.GetID(), SignalType.Output_Y);*/
                // Plot here!

                //Shared.EnablePlots();
                //Plot.FromList(new List<double[]> {
                //    simY1,
                //    y_ref,
                //    dataSet.U.GetColumn(0) },
                //    new List<string> { "y1=simY1_UM1", "y1=y_ref", "y3=u1" },
                //timeBase_s,
                //    "GainSched split ");
                //Shared.DisablePlots();

                // Linear gains
                List<double[]> GS_LinearGains1 = new List<double[]>();
                GS_LinearGains1.Add(UMp1.LinearGains);
                GSp_noGainSchedReference.LinearGains = GS_LinearGains1;

                // Time constants
                double[] GS_TimeConstants_s1 = new double[] { UMp1.TimeConstant_s };
                GSp_noGainSchedReference.TimeConstant_s = GS_TimeConstants_s1;

                // Time constants thresholds
                double[] GS_TimeConstantThreshold1 = new double[] { };
                GSp_noGainSchedReference.TimeConstantThresholds = GS_TimeConstantThreshold1;
                GSp_noGainSchedReference.Fitting = UMp1.Fitting;
                // double GSp1_rsqdiff_points = UMp1.Fitting.RsqDiff;
            }
            ////////////////////////////////////////////////////
            //
            var allGainSchedParams = new List<GainSchedParameters> { GSp_noGainSchedReference };

            ////////////////////////////////////////////////////
            int nThresholds = 1;
            List<GainSchedParameters> potentialGainschedParameters = 
                IdentifyGainScheduledGainsAndThresholds(dataSet, nThresholds, gainSchedInputIndex);

            ////////////////////////////////////////////////////
            // Final step: choose the best GainSched from all the candidates
            allGainSchedParams.AddRange(potentialGainschedParameters);
            return ChooseBestGainScheduledModel(allGainSchedParams, dataSet);
        }


        /// <summary>
        /// Identify a model when a given set of thresholds is already given in the supplied gsFittingSpecs
        /// 
        /// Since specifying the thresholds simplifies estimation significantly, it is reccommended to use this
        /// method over Identify() if possible.
        /// </summary>
        /// <param name="dataSet">tuning data set</param>
        /// <param name="gsFittingSpecs">the object in which the thresholds to be used are given</param>
        /// <returns></returns>
        public static GainSchedParameters IdentifyForGivenThresholds(UnitDataSet dataSet, GainSchedFittingSpecs gsFittingSpecs)
        {
            var vec = new Vec();
            GainSchedParameters ret = new GainSchedParameters();
            ret.GainSchedParameterIndex = gsFittingSpecs.uGainScheduledInputIndex;
            ret.LinearGainThresholds = gsFittingSpecs.uGainThresholds;
            // for this to work roubustly, the training set for fitting each model may need to require adding in a "span" of neighboring models. 
            int numberOfInputs = dataSet.U.GetLength(1);
            double gsVarMinU, gsVarMaxU;
            var linearGains = new List<double[]>();
            var timeConstants = new List<double>();
            var allIdsOk = true;
            var warningNotEnoughExitationBetweenAllThresholds = false;
            var dataSetCopy = new UnitDataSet(dataSet);
            // estimate each of the gains one by one
            for (int curGainIdx = 0; curGainIdx < ret.LinearGainThresholds.Count()+1; curGainIdx++)
            {
                double[] uMinFit = new double[numberOfInputs];
                double[] uMaxFit = new double[numberOfInputs];
                // double? u0 = 0;
                double? u0 = null;

                if (curGainIdx == 0)
                {
                    gsVarMinU = vec.Min(Array2D<double>.GetColumn(dataSetCopy.U, ret.GainSchedParameterIndex));
                }
                else
                {
                    gsVarMinU = ret.LinearGainThresholds[curGainIdx-1];
                }
                if (curGainIdx == ret.LinearGainThresholds.Count())
                {
                    gsVarMaxU = vec.Max(Array2D<double>.GetColumn(dataSetCopy.U, ret.GainSchedParameterIndex));
                }
                else
                {
                    gsVarMaxU = ret.LinearGainThresholds[curGainIdx];
                }
                for (int idx = 0; idx < numberOfInputs; idx++)
                {
                    if (idx == ret.GainSchedParameterIndex)
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
                var idResults = IdentifySingleLinearModelForGivenThresholds(ref dataSetCopy, uMinFit, uMaxFit, ret.GainSchedParameterIndex,u0);
                if (idResults.NotEnoughExitationBetweenAllThresholds)
                    warningNotEnoughExitationBetweenAllThresholds = true;
                var uInsideUMinUMax = Vec<double>.GetValuesAtIndices(dataSetCopy.U.GetColumn(ret.GainSchedParameterIndex), 
                    Index.InverseIndices(dataSetCopy.GetNumDataPoints(),dataSetCopy.IndicesToIgnore));
                var uMaxObserved = (new Vec()).Max(uInsideUMinUMax);
                var uMinObserved = (new Vec()).Min(uInsideUMinUMax);
                if (idResults.LinearGains == null)
                {
                    allIdsOk = false;
                 //   Console.WriteLine("min" + gsVarMinU + " max:" + gsVarMaxU + " gain: FAILED!!");//todo:comment out when working
                }
                else
                {
                   // Console.WriteLine("min" + gsVarMinU + " max:" + gsVarMaxU + " gain:" + idResults.Item1[0] + " umin:"+ uMinObserved + " umax:"+ uMaxObserved);//todo:comment out when working
                }
                linearGains.Add(idResults.LinearGains);
                timeConstants.Add(idResults.TimeConstant_s);
                dataSetCopy.IndicesToIgnore = null;
            }

            

            // final gain:above the highest threshold
            ret.LinearGains = linearGains;
            if (gsFittingSpecs.uTimeConstantThresholds == null)
            {
                ret.TimeConstant_s = new double[] { vec.Mean(timeConstants.ToArray()).Value };
            }
            else if (Vec.Equal(gsFittingSpecs.uTimeConstantThresholds,gsFittingSpecs.uGainThresholds))
            {
                ret.TimeConstant_s = timeConstants.ToArray();
                ret.TimeConstantThresholds = gsFittingSpecs.uTimeConstantThresholds;
            }
            else
            {
                ret.TimeConstant_s = null;//TODO: currently not supported to find timeconstants separately from gain thresholds. 
            }
            ret.Fitting = new FittingInfo();
            ret.Fitting.WasAbleToIdentify = allIdsOk;
            if (!allIdsOk)
                ret.AddWarning(GainSchedIdentWarnings.UnableToIdentifySomeSubmodels);
            if(warningNotEnoughExitationBetweenAllThresholds)
                ret.AddWarning(GainSchedIdentWarnings.InsufficientExcitationBetweenEachThresholdToBeCertainOfGains);

            // simulate the model and determine the optimal bias term:
            {
                var gsIdentModel = new GainSchedModel(ret, "ident_model");
                var identModelSim = new PlantSimulator(new List<ISimulatableModel> { gsIdentModel });
                var inputDataIdent = new TimeSeriesDataSet();


                //inputDataIdent.Add(identModelSim.AddExternalSignal(gsIdentModel, SignalType.External_U, (int)INDEX.FIRST), dataSet.U.GetColumn(0));// todo row 0 is not general

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
                    var simY_nobias = identModelSimData.GetValues(gsIdentModel.ID, SignalType.Output_Y);
                    //var measY = dataSet.Y_meas;
                    //var estBias = vec.Mean(vec.Subtract(measY, simY_nobias));
                    //if (estBias.HasValue)
                   // {
                   //     ret.Bias = estBias.Value;
                   //     dataSet.Y_sim = vec.Add(simY_nobias, ret.Bias);
                   // }
                   // else
                   // {
                        dataSet.Y_sim = simY_nobias;
                   // }
                }
            }
            return ret;
        }

        /// <summary>
        /// Updates the gain-scheduled model so that for inptu value u0, it output passes through y0. 
        /// </summary>
        /// <param name="gsModel"></param>
        /// <param name="u0"></param>
        /// <param name="y0"></param>
        public static void SetModelOperatingPoint(ref GainSchedParameters gsModel, double u0, double y0)
        { 
            
        
        
        }




        private static List<GainSchedParameters> IdentifyGainScheduledGainsAndThresholds(UnitDataSet dataSet, 
            int nThresholds, int gainSchedInputIndex )
        {
            UnitDataSet DS2 = new UnitDataSet(dataSet);
            UnitDataSet DS3 = new UnitDataSet(dataSet);
            int number_of_inputs = dataSet.U.GetNColumns();
            double gsVarMinU = dataSet.U.GetColumn(gainSchedInputIndex).Min();
            double gsVarMaxU = dataSet.U.GetColumn(gainSchedInputIndex).Max();

            List<GainSchedParameters> potentialGainschedParameters = new List<GainSchedParameters>();

            double[] potential_gainthresholds = new double[THRESHOLD_GLOBALSEARCH_MAX_IT];

            int m = (int)THRESHOLD_GLOBALSEARCH_MAX_IT / 2;
            for (int k = -m; k < m; k++)// TODO: rename m and k
            {
                potential_gainthresholds[k + m] = (gsVarMaxU - gsVarMinU) / 2 + k * GAIN_THRESHOLD_MAGIC_FACTOR_2;
            }

            for (int i = 0; i < potential_gainthresholds.Length; i++)
            {
                GainSchedParameters GSp2 = new GainSchedParameters();
                // Linear gain thresholds
                double[] GS_LinearGainThreshold2 = new double[] { potential_gainthresholds[i] };
                GSp2.LinearGainThresholds = GS_LinearGainThreshold2;

                List<double[]> GS_LinearGains2 = new List<double[]>();
                double[] GS_TimeConstants_s2 = new double[2];
                // a)
                {
                    double[] uMinFit = new double[number_of_inputs];
                    double[] uMaxFit = new double[number_of_inputs];
                    for (int idx = 0; idx < number_of_inputs; idx++)
                    {
                        if (idx == gainSchedInputIndex)
                        {
                            uMinFit[idx] = gsVarMinU;
                            uMaxFit[idx] = potential_gainthresholds[i] + (gsVarMaxU - gsVarMinU) * GAIN_THRESHOLD_MAGIC_FACTOR;
                        }
                        else
                        {
                            uMinFit[idx] = double.NaN;
                            uMaxFit[idx] = double.NaN;
                        }
                    }
                    var ret = IdentifySingleLinearModelForGivenThresholds(ref DS2, uMinFit, uMaxFit, gainSchedInputIndex);
                    GS_LinearGains2.Add(ret.LinearGains);
                    GS_TimeConstants_s2[0] = ret.TimeConstant_s;
                }
                // b)
                {
                  
                    double[] uMinFit = new double[number_of_inputs];
                    double[] uMaxFit = new double[number_of_inputs];
                    for (int idx = 0; idx < number_of_inputs; idx++)
                    {
                        if (idx == gainSchedInputIndex)
                        {
                            uMinFit[idx] = potential_gainthresholds[i] - (gsVarMaxU - gsVarMinU) * GAIN_THRESHOLD_MAGIC_FACTOR;
                            uMaxFit[idx] = gsVarMaxU;
                        }
                        else
                        {
                            uMinFit[idx] = double.NaN;
                            uMaxFit[idx] = double.NaN;
                        }
                    }
                    var ret = IdentifySingleLinearModelForGivenThresholds(ref DS3, uMinFit, uMaxFit, gainSchedInputIndex);
                    GS_LinearGains2.Add(ret.LinearGains);
                    GSp2.LinearGains = GS_LinearGains2;
                    GS_TimeConstants_s2[1] = ret.TimeConstant_s; 
                }
                GSp2.TimeConstant_s = GS_TimeConstants_s2;
                GSp2.TimeConstantThresholds = new double[] { potential_gainthresholds[i] };
                GSp2.LinearGainThresholds[0] = potential_gainthresholds[i];
                potentialGainschedParameters.Add(GSp2);
            }
            return potentialGainschedParameters;
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
        /// <returns>return a tuple, the first linear gains is null if unable to identify </returns>
        private static GainSchedSubModelResults IdentifySingleLinearModelForGivenThresholds(ref UnitDataSet dataSet,double[] uLowerThreshold, double[] uHigherThreshold, 
            int gainSchedVarIndex, double? u0= null )
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

            // if there is close to as many thresholds as there are steps in data, then there may not actually be enough data in the dataset to 
            // this while loop looks at the range betwen uMin and uMax for the data found between the upper and lower thresold. If the range 
            // is low, then the range of u to fit against is attempted increased to capture information as u steps between thresholds. 
            while (uRangeInChosenDataset < MIN_uRangeInChosenDataset && tuningSetSpanOutsideThreshold_prc < MAX_tuningSetSpanOutsideThreshold_prc)
            {
                dataSet.IndicesToIgnore = null;// TODO: store any incoming?
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
                    //      Console.WriteLine("uRangeInChosenDataset:" + uRangeInChosenDataset + " tuningSetSpanOutsideThreshold_prc:" + tuningSetSpanOutsideThreshold_prc);
                }
                tuningSetSpanOutsideThreshold_prc += STEP_tuningSetSpanOutsideThreshold_prc;
            }

            // NB! save a lot of comutational time by not doing time delay estimation here!
           // var unitModel = UnitIdentifier.Identify(ref dataSet, fittingSpecs, false);
            var unitModel = UnitIdentifier.IdentifyLinear(ref dataSet, fittingSpecs, false); 
            // static gains are more accurate if time constant is estimated alongside gains.
            // var unitModel = UnitIdentifier.IdentifyLinearAndStatic(ref dataSet, fittingSpecs, false); ;

            // debugging plot, comment out when not debugging.
       /*   var indices = Index.InverseIndices(dataSet.GetNumDataPoints(), dataSet.IndicesToIgnore);
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
        */    

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
        /// Runs a simulation for each of the parameters sets given and returns the paramtes that best matches the dataset
        /// </summary>
        /// <param name="allGainSchedParams"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        static private GainSchedParameters ChooseBestGainScheduledModel(List<GainSchedParameters> allGainSchedParams,UnitDataSet dataSet)
        {
            var vec = new Vec();
            GainSchedParameters BestGainSchedParams = null;
            double lowest_rms = 100000000000;
            int number_of_inputs = dataSet.U.GetNColumns();

            for (int i = 0; i < allGainSchedParams.Count; i++)
            {
                var GSp = allGainSchedParams[i];
                GainSchedModel GSM = new GainSchedModel(GSp, i.ToString());

                var plantSim = new PlantSimulator(new List<ISimulatableModel> { GSM });
                var inputData = new TimeSeriesDataSet();

                for (int k = 0; k < number_of_inputs; k++)
                {
                    inputData.Add(plantSim.AddExternalSignal(GSM, SignalType.External_U, (int)INDEX.FIRST), dataSet.U.GetColumn(k));
                }
                inputData.CreateTimestamps(dataSet.GetTimeBase());
                var isSimOk = plantSim.Simulate(inputData, out var simData);
                if (isSimOk)
                {
                    var simY1 = simData.GetValues(GSM.GetID(), SignalType.Output_Y);
                    var diff = vec.Subtract(simY1, dataSet.Y_meas);
                    double rms = 0;
                    for (int j = 0; j < simY1.Length; j++)
                    {
                        rms = rms + Math.Pow(diff.ElementAt(j), 2);
                    }
                    /*                string str_linear_gains = "";
                                    for (int j = 0; j < GSp.LinearGains.Count; j++)
                                    {
                                        str_linear_gains = str_linear_gains + string.Concat(GSp.LinearGains[j].Select(x => x.ToString())) + " - ";
                                    }

                                    Shared.EnablePlots();
                                    Plot.FromList(new List<double[]> {
                                    simY1,
                                    y_ref,
                                    dataSet.U.GetColumn(0) },
                                        new List<string> { "y1=simY1", "y1=y_ref", "y3=u1" },
                                    timeBase_s,
                                        "GainSched split " + i.ToString() + " " +  str_linear_gains + string.Concat(GSp.LinearGainThresholds.Select(x => x.ToString())));
                                    Shared.DisablePlots();
                    */
                    if (rms < lowest_rms)
                    {
                        BestGainSchedParams = GSp;
                        lowest_rms = rms;
                    }
                }
            }
            return BestGainSchedParams;

        }




    } 
}
