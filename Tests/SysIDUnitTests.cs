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
    /// <summary>
    /// DefaultModel unit tests
    /// In the naming convention I1 refers to one input, I2 two inputs etc.
    /// </summary>
    class DefaultModel
    {
        public DefaultProcessModel CreateDataAndIdentify(DefaultProcessModelParameters designParameters, double[,] U ,int timeBase_s=1)
        {
            designParameters.WasAbleToIdentify = true;//only if this flag is set will the process simulator simulate

            DefaultProcessModel model = new DefaultProcessModel(designParameters, timeBase_s);

            ProcessDataSet dataSet = new ProcessDataSet(null, U, timeBase_s);
            ProcessSimulator<DefaultProcessModel,DefaultProcessModelParameters>.EmulateYmeas(model, ref dataSet);

            DefaultProcessModelIdentifier modelId = new DefaultProcessModelIdentifier();
            DefaultProcessModel identifiedModel = modelId.Identify(ref dataSet);
            ProcessSimulator<DefaultProcessModel, DefaultProcessModelParameters>.Simulate(identifiedModel, ref dataSet);

            return identifiedModel;
        }

        /// <summary>
        /// These test criteria shoudl normally pass, unless you are testing the negative
        /// </summary>
        public void DefaultAsserts(DefaultProcessModel model)
        {
            Assert.IsTrue(model.GetModelParameters().AbleToIdentify(),"should be able to identify model");
            Assert.IsTrue(model.GetModelParameters().GetWarningList().Count == 0,"should give no warnings");

            Console.WriteLine(model.ToString());
        }

        // TODO: adding noise to datasets
        // TODO: testing the uncertainty estimtates(after adding them back)
        // TODO: testing the ability to automatically filter out bad input data
        // TODO: test ability to identify time delay
        // TODO: test ability to identify time constant
        // TODO: test ability to identify process gain curvatures

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        public void I1_Linear_Static(double bias)
        {
            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                Vec<double>.Fill(1, 50));
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1 });

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = 0,
                ProcessGains = new double[] { 1 },
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters, U);
            DefaultAsserts(model);
            double estGain = model.GetModelParameters().ProcessGains.ElementAt(0);
            Assert.IsTrue(0.98< estGain  && estGain < 1.02,"estimated gains shoudl be close to actual gain");
            Assert.IsTrue(model.GetModelParameters().TimeConstant_s < 0.1,"static data should give static model");
            Assert.IsTrue(model.GetModelParameters().TimeDelay_s < 0.1, "static data should give static model");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        public void I2_Linear_Static(double bias)
        {
            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                Vec<double>.Fill(1, 50));
            double[] u2 = Vec<double>.Concat(Vec<double>.Fill(2, 31),
                    Vec<double>.Fill(1, 30));
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1, u2 });

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = 0,
                ProcessGains = new double[] { 1,2 },
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters,U);
            DefaultAsserts(model);
            double[] estGains = model.GetModelParameters().ProcessGains;
            Assert.IsTrue(0.98 < estGains[0] && estGains[0] < 1.02, "estimated gains should be close to actual gain");
            Assert.IsTrue(1.98 < estGains[1] && estGains[1] < 2.02, "estimated gains should be close to actual gain");
            Assert.IsTrue(model.GetModelParameters().TimeConstant_s < 0.1, "static data should give static model");
            Assert.IsTrue(model.GetModelParameters().TimeDelay_s < 0.1, "static data should give static model");

            // Plot.FromList(new List<double[]> { dataSet.Y_meas, u1, u2 },
            //     new List<string> { "y1=y_meas", "y3=u1", "y3=u2" }, timeBase_s);
        }


    }
}
