using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.SysId;

namespace SysId.UnitTests
{
    /// <summary>
    /// DefaultModel unit tests
    /// In the naming convention I1 refers to one input, I2 two inputs etc.
    /// </summary>
    class DefaultModel
    {
        public DefaultProcessModel CreateDataAndIdentify(DefaultProcessModelParameters parameters, double[,] U ,int timeBase_s=1)
        {
            DefaultProcessModel model = new DefaultProcessModel(parameters, timeBase_s);

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
        }

        [Test]
        public void I1_Linear_Static()
        {
            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                Vec<double>.Fill(1, 50));
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1 });

            DefaultProcessModelParameters parameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 0,
                ProcessGains = new double[] { 1 },
                Bias = 0
            };
            var model = CreateDataAndIdentify(parameters, U);
            DefaultAsserts(model);
        }
        
        [Test]
        public void I2_Linear_Static()
        {
            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                Vec<double>.Fill(1, 50));
            double[] u2 = Vec<double>.Concat(Vec<double>.Fill(2, 31),
                    Vec<double>.Fill(1, 30));
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1, u2 });

            DefaultProcessModelParameters parameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 0,
                ProcessGains = new double[] { 1,2 },
                Bias = 0
            };
            var model = CreateDataAndIdentify(parameters,U);
            DefaultAsserts(model);
          //  Assert.IsTrue(model.GetModelParameters().ProcessGains);

           // Plot.FromList(new List<double[]> { dataSet.Y_meas, u1, u2 },
           //     new List<string> { "y1=y_meas", "y3=u1", "y3=u2" }, timeBase_s);
        }


    }
}
