using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Dynamic;

namespace DefaultModel.UnitTests
{

    class DefaultModel_Simulation
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        public void Simulate_TimeDelay(int timeDelay_s)
        {
            var timeBase_s = 1;
            var parameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                ProcessGains = new double []{ 1},
                TimeConstant_s = 0,
                TimeDelay_s = timeDelay_s,
                Bias =0
            };
            var model   = new DefaultProcessModel(parameters, timeBase_s);
            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 31),
                Vec<double>.Fill(1, 30));
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1});
            ProcessDataSet dataSet = new ProcessDataSet(timeBase_s, U);
            bool ret  = ProcessSimulator<DefaultProcessModel, DefaultProcessModelParameters>.
                Simulate(model, ref dataSet);

         ///   Plot.FromList(new List<double[]>{ dataSet.Y_sim,u1},new List<string>{"y1=ymeas ","y3=u1"}, timeBase_s);
            Assert.IsTrue(ret);
            Assert.IsTrue(dataSet.Y_sim[30+ timeDelay_s] == 0,"step should not arrive at y_sim too early");
            Assert.IsTrue(dataSet.Y_sim[31+ timeDelay_s] == 1, "steps should be delayed exactly timeDelay_s later  ");
        }

    }



    /// <summary>
    /// DefaultModel unit tests
    /// In the naming convention I1 refers to one input, I2 two inputs etc.
    /// </summary>
    class DefaultModel_Identification
    {
        static bool doPlotting = false;
        static Plot4Test plot = new Plot4Test(doPlotting);
        double timeBase_s;
        public DefaultProcessModel CreateDataAndIdentify(
            DefaultProcessModelParameters designParameters, double[,] U ,
            int timeBase_s=1)
        {
            designParameters.WasAbleToIdentify = true;//only if this flag is set will the process simulator simulate

            DefaultProcessModel model = new DefaultProcessModel(designParameters, timeBase_s);
            this.timeBase_s = timeBase_s;
            ProcessDataSet dataSet = new ProcessDataSet(timeBase_s, U);
            ProcessSimulator<DefaultProcessModel,DefaultProcessModelParameters>.
                EmulateYmeas(model, ref dataSet);

            DefaultProcessModelIdentifier modelId = new DefaultProcessModelIdentifier();
            DefaultProcessModel identifiedModel = modelId.Identify(ref dataSet, designParameters.U0);
            //ProcessSimulator<DefaultProcessModel, DefaultProcessModelParameters>.
            //    Simulate(identifiedModel, ref dataSet);

            return identifiedModel;
        }

        /// <summary>
        /// These test criteria shoudl normally pass, unless you are testing the negative
        /// </summary>
        public void DefaultAsserts(DefaultProcessModel model, DefaultProcessModelParameters designParameters)
        {
            Assert.IsNotNull(model,"returned model should never be null");
            Assert.IsTrue(model.GetModelParameters().AbleToIdentify(),"should be able to identify model");
            Assert.IsTrue(model.GetModelParameters().GetWarningList().Count == 0,"should give no warnings");

            Console.WriteLine(model.ToString());

            double[] estGains = model.GetModelParameters().ProcessGains;
            for (int k=0;k<estGains.Count(); k++)
            {
                Assert.IsTrue(Math.Abs(designParameters.ProcessGains[k]- estGains[k] )< 0.02,
                    "est.gains should be close to actual gain");
            }
         
            Assert.IsTrue(Math.Abs(model.GetModelParameters().TimeConstant_s - designParameters.TimeConstant_s) < 0.1,
                "est.timeconstant should be close to actual tc");
            Assert.IsTrue(Math.Abs(model.GetModelParameters().TimeDelay_s - designParameters.TimeDelay_s) < 0.1,
                "est.time delay should be close to actual");
            Assert.IsTrue(Math.Abs(model.GetModelParameters().Bias - designParameters.Bias) < 0.1,
                "est. Bias should be close to actual");

            //Plot.FromList(new List<double[]> {model.FittedDataSet.Y_meas, model.FittedDataSet.Y_meas, u1,u2 }
            //    ,new List<string> {"y1=y_meas","y1=y_sim","y3=u1","y3=u2"}, (int)timeBase_s);

        }


        // TODO: adding noise to datasets
        // TODO: testing the uncertainty estimtates(after adding them back)
        // TODO: testing the ability to automatically filter out bad input data
        // TODO: test ability to identify process gain curvatures
        [TestCase(0, 0, 0,0, Category = "Static")]
        [TestCase(21, 0, 0, 0, Category = "Static")]
        [TestCase(0, 10, 0, 0, Category = "Dynamic")]
        [TestCase(2, 2, 0, 0, Category = "Dynamic")]//Description("NOT WORKING AS OF 01.10.21")]//NOTWORKING
        [TestCase(21,10, 0, 0, Category = "Dynamic")]//Description("NOT WORKING AS OF 01.10.21")]//NOTWORKING
        [TestCase(0, 0, 10, 0, Category = "TimeDelayed")]
        [TestCase(21, 0, 5, 0, Category = "TimeDelayed")]
        //[TestCase(0, 0, 8,0, Category = "TimeDelayed")]
    



        public void I1_Linear(double bias, double timeConstant_s, int timeDelay_s,double u0)
        {
            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                Vec<double>.Fill(1, 50));
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1 });

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                ProcessGains = new double[] { 1 },
                U0 = Vec<double>.Fill(1,1),// new double[] { u0 },
                Bias            = bias
            };
            var model = CreateDataAndIdentify(designParameters, U);
            string caseId = NUnit.Framework.TestContext.CurrentContext.Test.Name; 
            plot.FromList(new List<double[]> { model.FittedDataSet.Y_sim, 
                model.FittedDataSet.Y_meas, u1 },
                 new List<string> { "y1=ysim", "y1=ymeas", "y3=u1" }, (int)timeBase_s, caseId, default,
                 caseId.Replace("(","").Replace(")","").Replace(",","_"));

            DefaultAsserts(model, designParameters);
        }

        [TestCase(0,0,0,Category="Static")]
        [TestCase(1,0,0, Category ="Static")]
        [TestCase(0,15,0, Category ="Dynamic")]
        [TestCase(1,15,0, Category ="Dynamic")]
        [TestCase(1, 0, 2, Category = "TimeDelayed")]
     //    [TestCase(1, 0, 5, Category = "TimeDelayed")]//TODO:not working

        public void I2_Linear(double bias, double timeConstant_s, int timeDelay_s)
        {
            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                Vec<double>.Fill(1, 50));
            double[] u2 = Vec<double>.Concat(Vec<double>.Fill(2, 31),
                    Vec<double>.Fill(1, 30));
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1, u2 });

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                ProcessGains = new double[] { 1,2 },
                U0 = Vec<double>.Fill(1,2),
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters,U);

            plot.FromList(new List<double[]> { model.FittedDataSet.Y_sim, model.FittedDataSet.Y_meas, u1,u2 },
                new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2" }, (int)timeBase_s);

            DefaultAsserts(model, designParameters);

        }

        [TestCase(0, 0, Category = "Static")]
        [TestCase(1, 0, Category = "Static")]
        [TestCase(0, 20, Category = "Dynamic")]
        [TestCase(1, 20, Category = "Dynamic")]
        public void I3_Linear(double bias, double timeConstant_s)
        {
            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                Vec<double>.Fill(1, 50));
            double[] u2 = Vec<double>.Concat(Vec<double>.Fill(2, 31),
                    Vec<double>.Fill(1, 30));
            double[] u3 = Vec<double>.Concat(Vec<double>.Fill(2, 21),
                    Vec<double>.Fill(1, 40));

            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1, u2, u3 });

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = timeConstant_s,
                ProcessGains = new double[] { 1, 2, 1.5 },
                U0 = Vec<double>.Fill(1,3),
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters, U);
            DefaultAsserts(model, designParameters);
        }



    }
}
