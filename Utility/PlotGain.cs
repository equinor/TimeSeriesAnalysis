using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis.Utility
{
    public class PlotGain
    {
        const int numberOfPlotPoints = 30;

        /// <summary>
        /// Plots an "x-y" plot of the steady-state gains of one or two models.
        /// </summary>
        /// <param name="model1">model of gains to be plotted</param>
        /// <param name="model2">optional seond model to be compared in the plots</param>
        /// <param name="uMin">optional umin array over which to plot gain plots</param>
        /// <param name="uMax">optional umax aray  over which to plot gain plots</param>
        public static void Plot(UnitModel model1, UnitModel model2 = null, double[] uMin=null, double[] uMax=null)
        {
            // TODO: could also plot the uminfit/umaxfit if applicable
            //double[] uMinFit = model.modelParameters.FittingSpecs.U_min_fit;
            //double[] uMaxFit = model.modelParameters.FittingSpecs.U_max_fit;

            string outputId = model1.outputID;
            if (outputId == null)
            {
                outputId = "output";
            }

            var model1Name = "model1";
            var model2Name = "model2";

            if (model1.ID != null && model1.ID != "not_named")
            {
                model1Name = model1.ID;
            }

            for (var inputIdx = 0; inputIdx < model1.ModelInputIDs.Count(); inputIdx++)
            {
                var inputSignalID = model1.ModelInputIDs[inputIdx];
                if (inputSignalID == null)
                {
                    inputSignalID = "input" + inputIdx;
                }
                List<XYTable> tables_m1 = new List<XYTable>();
                if (uMin != null && uMax != null)
                {
                    tables_m1 = CreateXYTablesFromUnitModel(model1, model1Name, inputIdx, uMin, uMax);
                }
                else if (model1.modelParameters.Fitting == null)
                {
                    if (model2 != null)
                    {
                        if (model2.modelParameters.Fitting != null)
                            tables_m1 = CreateXYTablesFromUnitModel(model1, model1Name, inputIdx, 
                                model2.modelParameters.Fitting.Umin, model2.modelParameters.Fitting.Umax);
                        else
                            throw new Exception("Unable to plot, no FittingInfo in model(s),specify Umin/Umax and rerun.");
                    }
                }
                else
                {
                    tables_m1 = CreateXYTablesFromUnitModel(model1, model1Name, inputIdx);
                }

                if (model2 == null)
                {
                    PlotXY.FromTables(tables_m1, outputId + "_" + inputSignalID + "_gains");
                    return;
                }
                //////////////////////////////////
                // if model2 is not null, then plot it as well 
                if (model2.ID != null && model2.ID != "not_named")
                {
                    model2Name = model2.ID;
                }
                List<XYTable> tables_m2 = new List<XYTable>();
                if (uMin != null && uMax != null)
                {
                    tables_m2 = CreateXYTablesFromUnitModel(model2, model2Name, inputIdx, uMin, uMax);
                }
                else if (model2.modelParameters.Fitting == null)
                {
                    if (model1.modelParameters.Fitting != null)
                        tables_m2 = CreateXYTablesFromUnitModel(model2, model1Name, inputIdx, 
                            model1.modelParameters.Fitting.Umin, model1.modelParameters.Fitting.Umax);
                    else
                        throw new Exception("Unable to plot, no FittingInfo in model(s),specify Umin/Umax and rerun.");

                }
                else
                    tables_m2 = CreateXYTablesFromUnitModel(model2, model2Name, inputIdx);

                var plotTables = new List<XYTable>();
                plotTables.AddRange(tables_m1);
                plotTables.AddRange(tables_m2);
                PlotXY.FromTables(plotTables, outputId + "_" + inputSignalID + "_gains");
            }
        }

        /// <summary>
        /// Plots an "x-y" plot of the steady-state gains of one or two models.
        /// </summary>
        /// <param name="model1">model of gains to be plotted</param>
        /// <param name="model2">optional seond model to be compared in the plots</param>
        /// <param name="uMin">optional umin array over which to plot gain plots</param>
        /// <param name="uMax">optional umax aray  over which to plot gain plots</param>
        public static void Plot(GainSchedModel model1, GainSchedModel model2 = null, string comment= null)
        {
            // TODO: could also plot the uminfit/umaxfit if applicable
            //double[] uMinFit = model.modelParameters.FittingSpecs.U_min_fit;
            //double[] uMaxFit = model.modelParameters.FittingSpecs.U_max_fit;

            string outputId = model1.outputID;
            if (outputId == null)
            {
                outputId = "output";
            }

            var model1Name = "model1";
            var model2Name = "model2";

            if (model1.ID != null && model1.ID != "not_named")
            {
                model1Name = model1.ID;
            }

            for (var inputIdx = 0; inputIdx < model1.ModelInputIDs.Count(); inputIdx++)
            {
                var inputSignalID = model1.ModelInputIDs[inputIdx];
                if (inputSignalID == null)
                {
                    inputSignalID = "input" + inputIdx;
                }
                List<XYTable> tables_m1 = new List<XYTable>();

                double[] uMin = null;
                double[] uMax = null;
                if (model1.modelParameters.Fitting != null)
                {
                    uMin = model1.modelParameters.Fitting.Umin;
                    uMax = model1.modelParameters.Fitting.Umax;
                }
                tables_m1 = CreateXYTablesFromGainSchedModel(model1, model1Name, inputIdx, uMin, uMax);

                /*if (uMin != null && uMax != null)
                {
                    tables_m1 = CreateXYTablesFromModel(model1, model1Name, inputIdx, uMin, uMax);
                }
                else if (model1.modelParameters.Fitting == null)
                {
                    if (model2 != null)
                    {
                        if (model2.modelParameters.Fitting != null)
                            tables_m1 = CreateXYTablesFromModel(model1, model1Name, inputIdx,
                                model2.modelParameters.Fitting.Umin, model2.modelParameters.Fitting.Umax);
                        else
                            throw new Exception("Unable to plot, no FittingInfo in model(s),specify Umin/Umax and rerun.");
                    }
                }
                else
                {
                    tables_m1 = CreateXYTablesFromModel(model1, model1Name, inputIdx);
                }*/

                if (model2 == null)
                {
                    PlotXY.FromTables(tables_m1, outputId + "_" + inputSignalID + "_gains", comment);
                    return;
                }
                //////////////////////////////////
                // if model2 is not null, then plot it as well 
                if (model2.ID != null && model2.ID != "not_named")
                {
                    model2Name = model2.ID;
                }
                /*List<XYTable> tables_m2 = new List<XYTable>();
                if (uMin != null && uMax != null)
                {
                    tables_m2 = CreateXYTablesFromGainSchedModel(model2, model2Name, inputIdx, uMin, uMax);
                }
                else if (model2.modelParameters.Fitting == null)
                {
                    if (model1.modelParameters.Fitting != null)
                        tables_m2 = CreateXYTablesFromGainSchedModel(model2, model1Name, inputIdx,
                            model1.modelParameters.Fitting.Umin, model1.modelParameters.Fitting.Umax);
                    else
                        throw new Exception("Unable to plot, no FittingInfo in model(s),specify Umin/Umax and rerun.");

                }
                else
                    tables_m2 = CreateXYTablesFromGainSchedModel(model2, model2Name, inputIdx);*/
                List<XYTable>  tables_m2 = CreateXYTablesFromGainSchedModel(model2, model2Name, inputIdx, uMin, uMax);

                var plotTables = new List<XYTable>();
                plotTables.AddRange(tables_m1);
                plotTables.AddRange(tables_m2);
                PlotXY.FromTables(plotTables, outputId + "_" + inputSignalID + "_gains", comment);
            }
        }



        static private List<XYTable> CreateXYTablesFromGainSchedModel(GainSchedModel model, string modelName, int inputIdx,
            double[] uMinExt = null, double[] uMaxExt = null)
        {
            string outputId = model.outputID;
            if (outputId == null)
            {
                outputId = "output";
            }

            if (model.ID != null && model.ID != "not_named")
            {
                modelName = model.ID;
            }

            var inputSignalID = model.ModelInputIDs[inputIdx];
            if (inputSignalID == null)
            {
                inputSignalID = "input" + inputIdx;
            }

            XYTable xyTableGain = new XYTable("gain_" + modelName + "_fit", new List<string> { inputSignalID, outputId }, XYlineType.line);
            //XYTable xyTableU0 = new XYTable("u0_" + modelName, new List<string> { inputSignalID, outputId }, XYlineType.withMarkers);

            var thresholdIdx = -1;

            for (int i = 0; i < model.modelParameters.LinearGains.Count(); i++ )
            {
                var gain = model.modelParameters.LinearGains.ElementAt(i);
                // note that usually there is one less threshold than there are gains, 
    
                double x = 0;
                if (thresholdIdx == -1)
                {
                    if (uMinExt == null)
                    {
                        x = 0;// todo: this is not completely general
                    }
                    else
                    {
                        x = uMinExt.ElementAt(inputIdx);
                    }
                }
                else
                    x = model.modelParameters.LinearGainThresholds.ElementAt(thresholdIdx);
                 xyTableGain.AddRow(new double[] { x,gain.ElementAt(inputIdx) } );
                //xyTableGain.AddRow(new double[] { gain.ElementAt(inputIdx),x });
                thresholdIdx++;
            }
            
            /* {
                var fittingInfo = model.modelParameters.Fitting;
                double uMax = 100, uMin = 0;
                if (fittingInfo == null)
                {
                    if (uMinExt != null && uMaxExt != null)
                    {
                        uMin = uMinExt.ElementAt(inputIdx);
                        uMax = uMaxExt.ElementAt(inputIdx);
                    }
                    else
                    {
                        throw new Exception("no Umin/Umax given externall or found in fittingInfo.");
                    }
                }

                else
                {
                    uMax = fittingInfo.Umax.ElementAt(inputIdx);
                    uMin = fittingInfo.Umin.ElementAt(inputIdx);
                }
                var u0 = model.modelParameters.U0;
                var y0 = model.GetSteadyStateOutput(u0);
                if (y0.HasValue)
                    xyTableU0.AddRow(new double[] { u0.ElementAt(inputIdx), y0.Value });

                double[] u_vec = new double[u0.Length];
                Array.Copy(u0, u_vec, u0.Length);
                for (double uCurInputCurVal = uMin; uCurInputCurVal < uMax; uCurInputCurVal += (uMax - uMin) / numberOfPlotPoints)
                {
                    u_vec[inputIdx] = uCurInputCurVal;
                    var y = model.GetSteadyStateOutput(u_vec);
                    if (y.HasValue)
                        xyTableGain.AddRow(new double[] { uCurInputCurVal, y.Value });
                }
            }*/
            return new List<XYTable> { xyTableGain };
        }




        static private List<XYTable> CreateXYTablesFromUnitModel(UnitModel model, string modelName, int inputIdx, 
            double[] uMinExt=null, double[] uMaxExt=null)
        {
            string outputId = model.outputID;
            if (outputId == null)
            {
                outputId = "output";
            }

            if (model.ID != null && model.ID != "not_named")
            {
                modelName = model.ID;
            }

            var inputSignalID = model.ModelInputIDs[inputIdx];
            if (inputSignalID == null)
            {
                inputSignalID = "input" + inputIdx;
            }

            XYTable xyTableGain = new XYTable("gain_" + modelName + "_fit", new List<string> { inputSignalID, outputId }, XYlineType.line);
            XYTable xyTableU0= new XYTable("u0_" + modelName, new List<string> { inputSignalID, outputId }, XYlineType.withMarkers);
            {
                var fittingInfo = model.modelParameters.Fitting;
                double uMax = 100, uMin = 0;
                if (fittingInfo == null)
                {
                    if (uMinExt != null && uMaxExt != null)
                    {
                        uMin = uMinExt.ElementAt(inputIdx);
                        uMax = uMaxExt.ElementAt(inputIdx);
                    }
                    else
                    {
                        throw new Exception("no Umin/Umax given externall or found in fittingInfo.");
                    }
                }

                else
                {
                    uMax = fittingInfo.Umax.ElementAt(inputIdx);
                    uMin = fittingInfo.Umin.ElementAt(inputIdx);
                }
                var u0 = model.modelParameters.U0;
                var y0 = model.GetSteadyStateOutput(u0);
                if (y0.HasValue)
                    xyTableU0.AddRow(new double[] { u0.ElementAt(inputIdx), y0.Value });

                double[] u_vec = new double[u0.Length];
                Array.Copy(u0, u_vec, u0.Length);
                for (double uCurInputCurVal = uMin; uCurInputCurVal < uMax; uCurInputCurVal += (uMax - uMin) / numberOfPlotPoints)
                {
                    u_vec[inputIdx] = uCurInputCurVal;
                    var y = model.GetSteadyStateOutput(u_vec);
                    if (y.HasValue)
                        xyTableGain.AddRow(new double[] { uCurInputCurVal, y.Value });
                }
            }
            return new List<XYTable> { xyTableGain, xyTableU0 };
        }






    }
}
