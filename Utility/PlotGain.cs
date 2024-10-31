using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis.Utility
{
    /// <summary>
    /// Make debug plots of the gain of one or more model, especially useful for gain-scheduled or other
    /// nonlinear models.
    /// </summary>
    public class PlotGain
    {
        /// <summary>
        /// Plots an "x-y" plot of the steady-state of one or two models.
        /// 
        /// Currently supported are either UnitModels or GainSchedModels
        /// 
        /// </summary>
        /// <param name="model1">model of gains to be plotted</param>
        /// <param name="model2">optional seond model to be compared in the plots</param>
        /// <param name="comment">comment to be added to figure</param>
        /// <param name="uMin">optional umin array over which to plot gain plots</param>
        /// <param name="uMax">optional umax array over which to plot gain plots</param>
        /// <param name="numberOfPlotPoints">the number of points along the axis that are to be calculated to produce lines</param>

        public static void PlotSteadyState(ISimulatableModel model1, ISimulatableModel model2 = null, string comment = null,
            double[] uMin = null, double[] uMax = null,int numberOfPlotPoints = 100)
        {
            string outputId = model1.GetOutputID();
            if (outputId == null)
            {
                outputId = "output";
            }
            var model1Name = "model1";
            var model2Name = "model2";
            if (model1.GetID() != null && model1.GetID() != "not_named")
            {
                model1Name = model1.GetID();
            }
            for (var inputIdx = 0; inputIdx < model1.GetModelInputIDs().Count(); inputIdx++)
            {
                var inputSignalID = model1.GetModelInputIDs()[inputIdx];
                if (inputSignalID == null)
                {
                    inputSignalID = "input" + inputIdx;
                }
                List<XYTable> tables_m1 = new List<XYTable>();
  
                if (model1.GetType() == typeof(GainSchedModel))
                {
                    tables_m1 = CreateSteadyStateXYTablesFromGainSchedModel((GainSchedModel)model1, model1Name, inputIdx, numberOfPlotPoints, uMin, uMax);
                }
                else if (model1.GetType() == typeof(UnitModel))
                {
                    if (( (UnitModel)model1).modelParameters.Fitting != null && uMin == null && uMax == null)
                    {
                        uMin = ((UnitModel)model1).modelParameters.Fitting.Umin;
                        uMax = ((UnitModel)model1).modelParameters.Fitting.Umax;
                    }
                    tables_m1 = CreateSteadyStateXYTablesFromUnitModel((UnitModel)model1, model1Name, inputIdx, numberOfPlotPoints, uMin, uMax);
                }

                if (model2 == null)
                {
                    PlotXY.FromTables(tables_m1, "ss" + outputId + "_" + inputSignalID + "_gains", comment);
                    return;
                }
                //////////////////////////////////
                // if model2 is not null, then plot it as well 
                if (model2.GetID() != null && model2.GetID() != "not_named")
                {
                    model2Name = model2.GetID();
                }
                List<XYTable> tables_m2 = new List<XYTable>();
                if (model2.GetType() == typeof(GainSchedModel))
                {
                    tables_m2 = CreateSteadyStateXYTablesFromGainSchedModel((GainSchedModel)model2, model2Name, inputIdx, numberOfPlotPoints, uMin, uMax);
                }
                else if (model2.GetType() == typeof(UnitModel))
                {
                    if (((UnitModel)model2).modelParameters.Fitting != null && uMin == null && uMax == null)
                    {
                        uMin = ((UnitModel)model2).modelParameters.Fitting.Umin;
                        uMax = ((UnitModel)model2).modelParameters.Fitting.Umax;
                    }
                    tables_m2 = CreateSteadyStateXYTablesFromUnitModel((UnitModel)model2, model2Name, inputIdx, numberOfPlotPoints, uMin, uMax);
                }

                var plotTables = new List<XYTable>();
                plotTables.AddRange(tables_m1);
                plotTables.AddRange(tables_m2);
                PlotXY.FromTables(plotTables, outputId + "_" + inputSignalID + "_gains", comment);
            }
        }



        /// <summary>
        /// Plots an "x-y" plot of the gain-scheduled "linear gains" of one or two gain-scheduled models.
        /// 
        /// Note that it makes no sense to give the "min or "max" of these plots
        /// 
        /// </summary>
        /// <param name="model1">model of gains to be plotted</param>
        /// <param name="model2">optional seond model to be compared in the plots</param>
        /// <param name="comment">comment to be added to figure</param>


        public static void PlotGainSched(GainSchedModel model1, GainSchedModel model2 = null, string comment= null)
        {
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

               /* if (model1.modelParameters.Fitting != null && uMin == null && uMax == null)
                {
                    uMin = model1.modelParameters.Fitting.Umin;
                    uMax = model1.modelParameters.Fitting.Umax;
                }*/
                tables_m1 = CreateGainXYTablesFromGainSchedModel(model1, model1Name, inputIdx);

                if (model2 == null)
                {
                    PlotXY.FromTables(tables_m1, "gs_" + outputId + "_" + inputSignalID + "_gains", comment);
                    return;
                }
                //////////////////////////////////
                // if model2 is not null, then plot it as well 
                if (model2.ID != null && model2.ID != "not_named")
                {
                    model2Name = model2.ID;
                }
                List<XYTable>  tables_m2 = CreateGainXYTablesFromGainSchedModel(model2, model2Name, inputIdx);

                var plotTables = new List<XYTable>();
                plotTables.AddRange(tables_m1);
                plotTables.AddRange(tables_m2);
                PlotXY.FromTables(plotTables, outputId + "_" + inputSignalID + "_gains", comment);
            }
        }

        /// <summary>
        /// This method creates a table of the steady-state calculated model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="modelName"></param>
        /// <param name="inputIdx"></param>
        /// <param name="uMinExt"></param>
        /// <param name="uMaxExt"></param>
        /// <param name="numberOfPlotPoints"></param>
        /// <returns></returns>
        static private List<XYTable> CreateSteadyStateXYTablesFromGainSchedModel(GainSchedModel model, 
            string modelName, int inputIdx, int numberOfPlotPoints,
            double[] uMinExt = null, double[] uMaxExt = null)
        {

            if (inputIdx > 0)
            {
                Console.WriteLine("currently only supports gain-sched variables with one input");
                return null;
            }
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

            XYTable xyTableGain = new XYTable("ssgain_" + modelName + "_fit", new List<string> { inputSignalID, outputId }, XYlineType.line);
            XYTable xyTableU0 = new XYTable("op_" + modelName, new List<string> { inputSignalID, outputId }, XYlineType.withMarkers);
            xyTableU0.AddRow(new double[] { model.GetModelParameters().OperatingPoint_U, model.GetModelParameters().OperatingPoint_Y });


            double uMin = 0, uMax = 100;
            if (uMinExt != null)
                uMin = uMinExt[inputIdx];
            if (uMaxExt != null)
                uMax = uMaxExt[inputIdx];

            // TODO: this is not completely general, if gain-sched model has more than one input
            var u0 = model.modelParameters.OperatingPoint_U;
          //  double[] u_vec = new double[u0.Length];
            double[] u_vec = new double[1];
//            Array.Copy(u0, u_vec, u0.Length);
            for (double uCurInputCurVal = uMin; uCurInputCurVal < uMax; uCurInputCurVal += (uMax - uMin) / numberOfPlotPoints)
            {
                u_vec[inputIdx] = uCurInputCurVal;
                var y = model.GetSteadyStateOutput(u_vec);
                if (y.HasValue)
                    xyTableGain.AddRow(new double[] { uCurInputCurVal, y.Value });
            }
            return new List<XYTable> { xyTableGain,xyTableU0};
        }

      

        static private List<XYTable> CreateSteadyStateXYTablesFromUnitModel(UnitModel model, string modelName, int inputIdx,
            int numberOfPlotPoints,double[] uMinExt=null, double[] uMaxExt=null )
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

            XYTable xyTableGain = new XYTable("ssgain_" + modelName + "_fit", new List<string> { inputSignalID, outputId }, XYlineType.line);
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

        ///////////////////////////////////////
        ///
        //// "gain" table

        /// <summary>
        /// This method creates a table from the LinearGains contained in model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="modelName"></param>
        /// <param name="inputIdx"></param>
        /// <returns></returns>
        static private List<XYTable> CreateGainXYTablesFromGainSchedModel(GainSchedModel model, string modelName, int inputIdx)
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

            XYTable xyTableGain = new XYTable("gsgain_" + modelName + "_fit", new List<string> { inputSignalID, outputId }, XYlineType.line);

            var thresholdIdx = -1;

            for (int i = 0; i < model.modelParameters.LinearGains.Count(); i++)
            {
                var gain = model.modelParameters.LinearGains.ElementAt(i);
                // note that usually there is one less threshold than there are gains, 

                double x = 0;
                if (thresholdIdx == -1)
                {
                    // if (uMinExt == null)
                    {
                        x = 0;// todo: this is not completely general
                    }
                    /*    else
                        {
                            x = uMinExt.ElementAt(inputIdx);
                        }*/
                }
                else
                    x = model.modelParameters.LinearGainThresholds.ElementAt(thresholdIdx);
                xyTableGain.AddRow(new double[] { x, gain.ElementAt(inputIdx) });
                thresholdIdx++;
            }

            return new List<XYTable> { xyTableGain };
        }




    }
}
