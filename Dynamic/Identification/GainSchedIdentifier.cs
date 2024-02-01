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
    static public class GainSchedIdentifier
    {

        const int THRESHOLD_GLOBALSEARCH_MAX_IT = 40; // TODO: magic number
        const double GAIN_THRESHOLD_MAGIC_FACTOR = 0.02;// TODO: Magic number
        const double GAIN_THRESHOLD_MAGIC_FACTOR_2 = 0.09;//TODO magic number


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
            ///
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
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="gsFittingSpecs"></param>
        /// <returns></returns>
        public static GainSchedParameters IdentifyGainsForGivenThresholds(UnitDataSet dataSet, GainSchedFittingSpecs gsFittingSpecs)
        {
            var vec = new Vec();
            GainSchedParameters ret = new GainSchedParameters();
            ret.GainSchedParameterIndex = gsFittingSpecs.uGainScheduledInputIndex;
            ret.LinearGainThresholds = gsFittingSpecs.uGainThresholds;

            const int WIDTH = 1; // if 0 each identification only consider data inside two threshold pair, if 1, then also the two neighboring sections are added. 

            int number_of_inputs = dataSet.U.GetLength(1);
            double gsVarMinU = vec.Min(Array2D<double>.GetColumn(dataSet.U, ret.GainSchedParameterIndex));
            double gsVarMaxU = 0;
            var linearGains = new List<double[]>();
            for (int curGainIdx = 0; curGainIdx < ret.LinearGainThresholds.Count()+1; curGainIdx++)
            {
                double[] uMinFit = new double[number_of_inputs];
                double[] uMaxFit = new double[number_of_inputs];
                double u0 = 0;

                if (curGainIdx < ret.LinearGainThresholds.Count()- WIDTH)
                    gsVarMaxU = ret.LinearGainThresholds[curGainIdx+ WIDTH];
                else
                    gsVarMaxU = vec.Max(Array2D<double>.GetColumn(dataSet.U, ret.GainSchedParameterIndex));
                for (int idx = 0; idx < number_of_inputs; idx++)
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
            //    Console.WriteLine("min"+gsVarMinU+ " max:"+ gsVarMaxU);

                var idResults = IdentifySingleGainForGivenThresholds(ref dataSet , uMinFit, uMaxFit,null);//
                dataSet.IndicesToIgnore = null;
                if (curGainIdx>-1+ WIDTH)
                    gsVarMinU = ret.LinearGainThresholds[curGainIdx- WIDTH];
               //  if (idResults.Item1 != null)
                linearGains.Add(idResults.Item1);
            }
            // final gain:above the highest threshold
            ret.LinearGains = linearGains;
            return ret;

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
                    var ret = IdentifySingleGainForGivenThresholds(ref DS2, uMinFit, uMaxFit);
                    GS_LinearGains2.Add(ret.Item1);
                    GS_TimeConstants_s2[0] = ret.Item2;
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
                    var ret = IdentifySingleGainForGivenThresholds(ref DS3, uMinFit, uMaxFit);
                    GS_LinearGains2.Add(ret.Item1);
                    GSp2.LinearGains = GS_LinearGains2;
                    GS_TimeConstants_s2[1] = ret.Item2; 
                }
                GSp2.TimeConstant_s = GS_TimeConstants_s2;
                GSp2.TimeConstantThresholds = new double[] { potential_gainthresholds[i] };
                GSp2.LinearGainThresholds[0] = potential_gainthresholds[i];
                potentialGainschedParameters.Add(GSp2);
            }
            return potentialGainschedParameters;
        }

        private static (double[], double) IdentifySingleGainForGivenThresholds(ref UnitDataSet dataSet,double[] u_min_fit, double[] u_max_fit, double? u0= null)
        {
            var fittingSpecs = new FittingSpecs();
            fittingSpecs.U_min_fit = u_min_fit;
            fittingSpecs.U_max_fit = u_max_fit;
             if (!u0.HasValue )
                 fittingSpecs.u0 = new double[] { u_min_fit[0] + (u_max_fit[0] - u_min_fit[0]) / 2 };
             else
                 fittingSpecs.u0 = new double[] { u0.Value };
            
            //fittingSpecs.u0 = new double[] { 0};

            var unitModel = UnitIdentifier.IdentifyLinear(ref dataSet, fittingSpecs, false); ;
          //  var unitModel = UnitIdentifier.IdentifyLinearAndStatic(ref dataSet, fittingSpecs, false); ;
            var unitParams = unitModel.GetModelParameters();

            return (unitParams.LinearGains, unitParams.TimeConstant_s);
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
