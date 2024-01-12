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
        public GainSchedParameters GainSchedIdentify(UnitDataSet dataSet, FittingSpecs fittingSpecs = null)
        {
            UnitDataSet DS1 = new UnitDataSet(dataSet); // TODO: Why is it necessary with copies? - because the UnitModel.Identify affect the data.
            UnitDataSet DS2 = new UnitDataSet(dataSet);
            UnitDataSet DS3 = new UnitDataSet(dataSet);

            int number_of_inputs = dataSet.U.GetNColumns();
            double[] min_time_constants = new double[] { 1 };

            GainSchedParameters GSp1 = new GainSchedParameters();
            //List<GainSchedParameters> GainSchedParameterList3 = null;
            //List<GainSchedParameters> GainSchedParameterList4 = null;
            //List<GainSchedParameters> GainSchedParameterList5 = null;
            //List<GainSchedParameters> GainSchedParameterList6 = null;
            //List<GainSchedParameters> GainSchedParameterList7 = null;

            double min_u = dataSet.U.GetColumn(0).Min();
            double max_u = dataSet.U.GetColumn(0).Max();

            // ## 1gain 1 time constant ##
            // Linear gain thresholds
            double[] GS_LinearGainThreshold1 = new double[] { };
            GSp1.LinearGainThresholds = GS_LinearGainThreshold1;

            // Unit identifiers
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
            UnitModel UM1 = ui_1_1.IdentifyLinear(ref DS1, fittingSpecs_1g);
            UnitParameters UMp1 = UM1.GetModelParameters();

            // Simulate
            double[] y_ref = dataSet.Y_meas;
            int timeBase_s = 1;
            var plantSim = new PlantSimulator(new List<ISimulatableModel> { UM1 });
            var inputData = new TimeSeriesDataSet();
            for (int k = 0; k < number_of_inputs; k++)
            {
                inputData.Add(plantSim.AddExternalSignal(UM1, SignalType.External_U, (int)INDEX.FIRST), dataSet.U.GetColumn(k));
            }
            inputData.CreateTimestamps(timeBase_s);
            var CorrectisSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            double[] simY1 = simData.GetValues(UM1.GetID(), SignalType.Output_Y);

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
            // TODO: Should consider to remove this threshold as this should be the same thresholds as gainSchedThresholds
            double[] GS_TimeConstantThreshold1 = new double[] { }; 

            GSp1.TimeConstantThresholds = GS_TimeConstantThreshold1;

            GSp1.Fitting = UMp1.Fitting;

            double GSp1_rsqdiff_points = UMp1.Fitting.RsqDiff;

            //GainSchedModel GSM1 = new GainSchedModel(GSp1, "my simple model");

            //var plantSim1 = new PlantSimulator(new List<ISimulatableModel> { GSM1 });
            //var inputData1 = new TimeSeriesDataSet();
            //for (int k = 0; k < number_of_inputs; k++)
            //{
            //    inputData1.Add(plantSim.AddExternalSignal(GSM1, SignalType.External_U, (int)INDEX.FIRST), dataSet.U.GetColumn(k));
            //}
            //inputData1.CreateTimestamps(timeBase_s);
            //var CorrectisSimulatable1 = plantSim1.Simulate(inputData1, out TimeSeriesDataSet simData1);
            //double[] simY11 = simData1.GetValues(GSM1.GetID(), SignalType.Output_Y);

            //// Plot here!

            //Shared.EnablePlots();
            //Plot.FromList(new List<double[]> {
            //    simY11,
            //    y_ref,
            //    dataSet.U.GetColumn(0) },
            //    new List<string> { "y1=simY1_GSM1", "y1=y_ref", "y3=u1" },
            //timeBase_s,
            //    "GainSched split ");
            //Shared.DisablePlots();


            // ## 2gain 1 time constant ##
            double[] potential_gainthresholds = new double[10]; // TODO: magic number

            for (int k = -5; k < 5; k++)
            {
                potential_gainthresholds[k+5] = (max_u-min_u)/2 + k*0.2; // TODO: magic number
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
                        u_max_fit_a[idx] = potential_gainthresholds[i] + (max_u - min_u)*0.05; // TODO: Magic number
                    }
                    else
                    {
                        u_min_fit_a[idx] = double.NaN;
                        u_max_fit_a[idx] = double.NaN;
                    }
                }
                fittingSpecs_a.U_min_fit = u_min_fit_a;
                fittingSpecs_a.U_max_fit = u_max_fit_a;

                List<double[]> GS_LinearGains2 = new List<double[]>();
                double[] GS_TimeConstants_s2 = new double[2];
                double[] GS_TimeConstantThreshold2 = new double[1]; // TODO: Should be considered to remove

                UnitModel UM2_a = ui_2g_1t_a.IdentifyLinear(ref DS2, fittingSpecs_a); ;
                UnitParameters UMp2_a = UM2_a.GetModelParameters(); ;
      
                // Linear gains
                GS_LinearGains2.Add(UMp2_a.LinearGains);

                // Time constants
                GS_TimeConstants_s2[0] = UMp2_a.TimeConstant_s;

                // b)
                var ui_2g_1t_b = new UnitIdentifier();
                FittingSpecs fittingSpecs_b = new FittingSpecs();
                double[] u_min_fit_b = new double[number_of_inputs];
                double[] u_max_fit_b = new double[number_of_inputs];
                for (int idx = 0; idx < number_of_inputs; idx++)
                {
                    if (idx == 0)
                    {
                        u_min_fit_b[idx] = potential_gainthresholds[i] - (max_u - min_u)*0.05; // TODO: Magic number
                        u_max_fit_b[idx] = max_u;
                    }
                    else
                    {
                        u_min_fit_b[idx] = double.NaN;
                        u_max_fit_b[idx] = double.NaN;
                    }
                }
                fittingSpecs_b.U_min_fit = u_min_fit_b;
                fittingSpecs_b.U_max_fit = u_max_fit_b;

                UnitModel UM2_b = ui_2g_1t_b.IdentifyLinear(ref DS3, fittingSpecs_b); ;
                UnitParameters UMp2_b = UM2_b.GetModelParameters(); ;
               
                // Linear gains
                GS_LinearGains2.Add(UMp2_b.LinearGains);
                GSp2.LinearGains = GS_LinearGains2;

                // Time constants
                GS_TimeConstants_s2[1] = UMp2_b.TimeConstant_s;
                GSp2.TimeConstant_s = GS_TimeConstants_s2;

                // Time constant thresholds
                // TODO: Should consider to remove this threshold as this should be the same thresholds as gainSchedThresholds
                GSp2.TimeConstantThresholds = new double[] { potential_gainthresholds[i]};
                GSp2.LinearGainThresholds[0] = potential_gainthresholds[i];
                

                potential_2g_1t_gainsched_parameters.Add(GSp2);
                

                // ## End of 2 gains, 1 time constant

                //// 2gains 2 time constants
                //for time_series_slice in sliced_time_series:
                //{
                //    GainSchedParameters GSPs = Identify(time_series_slice, TimeConstantThresh);
                //    GainSchedParameterList3.append(GSPs);
                //}


                //// 3gains 1 time constants
                //for time_series_slice in sliced_time_series:
                //{
                //    GainSchedParameters GSPs = Identify(time_series_slice)
                //    GainSchedParameterList4.append(GainSchedParameters);
                //}


                //// 3gains 2 time constants
                //for time_series_slice in sliced_time_series:
                //{
                //    GainSchedParameters GSPs = Identify(time_series_slice, TimeConstantThresh);
                //    GainSchedParameterList5.append(GainSchedParameters);
                //}


                //// 4gains 1 time constant
                //for time_series_slice in sliced_time_series:
                //{
                //    GainSchedParameters = Identify(time_series_slice);
                //    GainSchedParameterList6.append(GainSchedParameters);
                //}


                //// 4gains 2 time constants
                //for time_series_slice in sliced_time_series:
                //{
                //    GainSchedParameters = Identify(time_series_slice, TimeConstantThresh);
                //    GainSchedParameterList7.append(GainSchedParameters);
                //}
            }


            // Evaluate GainScheds
            List<GainSchedParameters> allGainSchedParams = new List<GainSchedParameters> { GSp1 };
            allGainSchedParams.AddRange(potential_2g_1t_gainsched_parameters);
            

            GainSchedParameters BestGainSchedParams = null;
            double lowest_rms = 100000000000;
            //int timeBase_s = 1;
            for (int i = 0; i < allGainSchedParams.Count; i++)
            {
                var GSp = allGainSchedParams[i];
                GainSchedModel GSM = new GainSchedModel(GSp, i.ToString());
                //double[] y_ref = dataSet.Y_meas;

                plantSim = new PlantSimulator(new List<ISimulatableModel> { GSM });
                inputData = new TimeSeriesDataSet();
                for (int k = 0; k < number_of_inputs; k++)
                {
                    inputData.Add(plantSim.AddExternalSignal(GSM, SignalType.External_U, (int)INDEX.FIRST), dataSet.U.GetColumn(k));
                }
                inputData.CreateTimestamps(timeBase_s);
                CorrectisSimulatable = plantSim.Simulate(inputData, out simData);
                simY1 = simData.GetValues(GSM.GetID(), SignalType.Output_Y);

                // Evaluate
                var diff = y_ref.Zip(simY1, (ai, bi) => ai - bi);
                double rms = 0;
                for (int j = 0; j < simY1.Length; j++)
                {
                    rms = rms + Math.Pow(diff.ElementAt(j),2);
                }

                string str_linear_gains = "";
                for (int j = 0; j < GSp.LinearGains.Count; j++)
                {
                    str_linear_gains = str_linear_gains + string.Concat(GSp.LinearGains[j].Select(x => x.ToString())) + " - ";
                }

                //Shared.EnablePlots();
                //Plot.FromList(new List<double[]> {
                //simY1,
                //y_ref,
                //dataSet.U.GetColumn(0) },
                //    new List<string> { "y1=simY1", "y1=y_ref", "y3=u1" },
                //timeBase_s,
                //    "GainSched split " + i.ToString() + " " +  str_linear_gains + string.Concat(GSp.LinearGainThresholds.Select(x => x.ToString())));
                //Shared.DisablePlots();

                if (rms < lowest_rms)
                {
                    BestGainSchedParams = GSp;
                    lowest_rms = rms;
                }
            }
            return BestGainSchedParams;
        }
        
    } 
}
