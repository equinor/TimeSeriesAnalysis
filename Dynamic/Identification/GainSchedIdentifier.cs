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

namespace TimeSeriesAnalysis.Dynamic
{
    public class GainSchedIdentifier
    {
        public GainSchedParameters Identify(UnitDataSet dataSet, FittingSpecs fittingSpecs = null)
        {

            const int MAX_NUMBER_OF_THRESHOLDS = 40; // TODO: magic number
            const double  GAIN_THRESHOLD_MAGIC_FACTOR = 0.02;// TODO: Magic number
            const double GAIN_THRESHOLD_MAGIC_FACTOR_2 = 0.09;//TODO magic number

            UnitDataSet DS1 = new UnitDataSet(dataSet);
            UnitDataSet DS2 = new UnitDataSet(dataSet);
            UnitDataSet DS3 = new UnitDataSet(dataSet);

            int number_of_inputs = dataSet.U.GetNColumns();
            double[] min_time_constants = new double[] { 1 };

            GainSchedParameters GSp1 = new GainSchedParameters();

            double min_u = dataSet.U.GetColumn(0).Min();
            double max_u = dataSet.U.GetColumn(0).Max();

            // ## 1gain 1 time constant ##
            // Linear gain thresholds
            double[] GS_LinearGainThreshold1 = new double[] { };
            GSp1.LinearGainThresholds = GS_LinearGainThreshold1;

            // Unit identifiers
            {
                var ui_1_1 = new UnitIdentifier();
                FittingSpecs fittingSpecs_1g = new FittingSpecs();
                double[] u_min_fit_1g = new double[number_of_inputs];
                double[] u_max_fit_1g = new double[number_of_inputs];
                for (int idx = 0; idx < number_of_inputs; idx++)
                {
                    if (idx == 0)
                    {
                        u_min_fit_1g[idx] = min_u;
                        u_max_fit_1g[idx] = max_u;
                    }
                    else
                    {
                        u_min_fit_1g[idx] = double.NaN;
                        u_max_fit_1g[idx] = double.NaN;
                    }
                }
                fittingSpecs_1g.U_min_fit = u_min_fit_1g;
                fittingSpecs_1g.U_max_fit = u_max_fit_1g;
                UnitModel UM1 = ui_1_1.IdentifyLinear(ref DS1, fittingSpecs_1g, false);
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
                GSp1.LinearGains = GS_LinearGains1;

                // Time constants
                double[] GS_TimeConstants_s1 = new double[] {UMp1.TimeConstant_s };
                GSp1.TimeConstant_s = GS_TimeConstants_s1;

                // Time constants thresholds
                double[] GS_TimeConstantThreshold1 = new double[] { }; 
                GSp1.TimeConstantThresholds = GS_TimeConstantThreshold1;
                GSp1.Fitting = UMp1.Fitting;
                double GSp1_rsqdiff_points = UMp1.Fitting.RsqDiff;
            }
            ////////////////////////////////////////////////////
            // ## 2gain 1 time constant ##
            double[] potential_gainthresholds = new double[MAX_NUMBER_OF_THRESHOLDS]; 
            int m = (int)MAX_NUMBER_OF_THRESHOLDS/2;

            for (int k = -m; k < m; k++)
            {
                potential_gainthresholds[k+m] = (max_u-min_u)/2 + k* GAIN_THRESHOLD_MAGIC_FACTOR_2; 
            }
       
            List<GainSchedParameters> potential_2g_1t_gainsched_parameters = new List<GainSchedParameters>();
            for (int i = 0; i < potential_gainthresholds.Length; i++)
            {
                GainSchedParameters GSp2 = new GainSchedParameters();
                // Linear gain thresholds
                double[] GS_LinearGainThreshold2 = new double[] { potential_gainthresholds[i] };
                GSp2.LinearGainThresholds = GS_LinearGainThreshold2;         
               
                // a)
                var ui_2g_1t_a = new UnitIdentifier();
                FittingSpecs fittingSpecs_a = new FittingSpecs();
                double[] u_min_fit_a = new double[number_of_inputs];
                double[] u_max_fit_a = new double[number_of_inputs];
                for (int idx = 0;  idx < number_of_inputs; idx++)
                {
                    if (idx == 0)
                    {
                        u_min_fit_a[idx] = min_u;
                        u_max_fit_a[idx] = potential_gainthresholds[i] + (max_u - min_u)* GAIN_THRESHOLD_MAGIC_FACTOR; 
                    }
                    else
                    {
                        u_min_fit_a[idx] = double.NaN;
                        u_max_fit_a[idx] = double.NaN;
                    }
                }
                fittingSpecs_a.U_min_fit = u_min_fit_a;
                fittingSpecs_a.U_max_fit = u_max_fit_a;
                fittingSpecs_a.u0 = new double[] { u_min_fit_a[0] + (u_max_fit_a[0] - u_min_fit_a[0])/2 };

                List<double[]> GS_LinearGains2 = new List<double[]>();
                double[] GS_TimeConstants_s2 = new double[2];
                UnitModel UM2_a = ui_2g_1t_a.IdentifyLinear(ref DS2, fittingSpecs_a, false); ;
                UnitParameters UMp2_a = UM2_a.GetModelParameters();
                GS_LinearGains2.Add(UMp2_a.LinearGains);
                GS_TimeConstants_s2[0] = UMp2_a.TimeConstant_s;

                // b)
                var ui_2g_1t_b = new UnitIdentifier();
        
                double[] u_min_fit_b = new double[number_of_inputs];
                double[] u_max_fit_b = new double[number_of_inputs];
                for (int idx = 0; idx < number_of_inputs; idx++)
                {
                    if (idx == 0)
                    {
                        u_min_fit_b[idx] = potential_gainthresholds[i] - (max_u - min_u)*GAIN_THRESHOLD_MAGIC_FACTOR; 
                        u_max_fit_b[idx] = max_u;
                    }
                    else
                    {
                        u_min_fit_b[idx] = double.NaN;
                        u_max_fit_b[idx] = double.NaN;
                    }
                }
                FittingSpecs fittingSpecs_b = new FittingSpecs();
                fittingSpecs_b.U_min_fit = u_min_fit_b;
                fittingSpecs_b.U_max_fit = u_max_fit_b;
                fittingSpecs_b.u0 = new double[] { u_min_fit_b[0] + (u_max_fit_b[0] - u_min_fit_b[0])/2 };
 
                UnitModel UM2_b = ui_2g_1t_b.IdentifyLinear(ref DS3, fittingSpecs_b,false); ;
                UnitParameters UMp2_b = UM2_b.GetModelParameters();
               
                GS_LinearGains2.Add(UMp2_b.LinearGains);
                GSp2.LinearGains = GS_LinearGains2;
                GS_TimeConstants_s2[1] = UMp2_b.TimeConstant_s;
                GSp2.TimeConstant_s = GS_TimeConstants_s2;
                GSp2.TimeConstantThresholds = new double[] { potential_gainthresholds[i]};
                GSp2.LinearGainThresholds[0] = potential_gainthresholds[i];
                potential_2g_1t_gainsched_parameters.Add(GSp2);
            }


            // Evaluate GainScheds
            var allGainSchedParams = new List<GainSchedParameters> { GSp1 };
            allGainSchedParams.AddRange(potential_2g_1t_gainsched_parameters);
            return ChooseBestGainScheduledModel(allGainSchedParams, dataSet);
        }

        /// <summary>
        /// Runs a simulation for each of the parameters sets given and returns the paramtes that best matches the dataset
        /// </summary>
        /// <param name="allGainSchedParams"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        GainSchedParameters ChooseBestGainScheduledModel(List<GainSchedParameters> allGainSchedParams,UnitDataSet dataSet)
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
                var CorrectisSimulatable = plantSim.Simulate(inputData, out var simData);
                if (CorrectisSimulatable)
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
