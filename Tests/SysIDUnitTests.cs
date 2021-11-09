using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis.Dynamic.DefaultModelTests
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
            UnitDataSet dataSet = new UnitDataSet(timeBase_s, U);
            var simulator = new UnitSimulator(model);
            var ret  = simulator.Simulate(ref dataSet);

         //   Plot.FromList(new List<double[]>{ dataSet.Y_sim,u1},new List<string>{"y1=ymeas ","y3=u1"}, timeBase_s);
            Assert.IsNotNull(ret);
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
        double timeBase_s = 1;

        public UnitDataSet CreateDataSet(DefaultProcessModelParameters designParameters, double[,] U, double timeBase_s,
            double noiseAmplitude = 0, bool addInBadDataToYmeasAndU = false, double badValueId = Double.NaN)
        {
            designParameters.WasAbleToIdentify = true;//only if this flag is set will the process simulator simulate

            DefaultProcessModel model = new DefaultProcessModel(designParameters, timeBase_s);
            this.timeBase_s = timeBase_s;

            UnitDataSet dataSet = new UnitDataSet(timeBase_s, U);
            dataSet.BadDataID = badValueId;
            var simulator = new UnitSimulator(model);
            simulator.SimulateYmeas(ref dataSet, noiseAmplitude);

            if (addInBadDataToYmeasAndU)
            {
                int addBadYDataEveryNthPoint = 20;
                int addBadUDataEveryNthPoint = 25;

                for (int curU = 0; curU < dataSet.U.GetNColumns(); curU++)
                {
                    int t_offset = (int)Math.Floor((double)(addBadUDataEveryNthPoint / (curU + 2)));
                    int curT = t_offset;
                    //  for (int curT = t_offset; curT < dataSet.U.GetNRows(); curT+= addBadUDataEveryNthPoint)
                    {
                        dataSet.U[curT, curU] = badValueId;
                    }
                }
                for (int i = 0; i < dataSet.Y_meas.Length; i += addBadYDataEveryNthPoint)
                {
                    dataSet.Y_meas[i] = badValueId;
                }
            }
            return dataSet;
        }

        public DefaultProcessModel Identify(UnitDataSet dataSet, DefaultProcessModelParameters designParameters)
        {
            var modelId = new DefaultProcessModelIdentifier();
            DefaultProcessModel identifiedModel = modelId.Identify(ref dataSet, designParameters.U0, designParameters.UNorm);
            return identifiedModel;
        }

        public DefaultProcessModel CreateDataAndIdentify(
            DefaultProcessModelParameters designParameters, double[,] U, double timeBase_s,
            double noiseAmplitude = 0, bool addInBadDataToYmeasAndU = false, double badValueId = Double.NaN)
        {
            var dataSet = CreateDataSet(designParameters, U, timeBase_s, noiseAmplitude,
                addInBadDataToYmeasAndU, badValueId);
            return Identify(dataSet, designParameters);
        }

        /// <summary>
        /// These test criteria shoudl normally pass, unless you are testing the negative
        /// </summary>
        public void DefaultAsserts(DefaultProcessModel model, DefaultProcessModelParameters designParameters)
        {
            Console.WriteLine(model.ToString());

            Assert.IsNotNull(model, "returned model should never be null");
            Assert.IsTrue(model.GetModelParameters().AbleToIdentify(), "should be able to identify model");
            Assert.IsTrue(model.GetModelParameters().GetWarningList().Count == 0, "should give no warnings");
            //  Assert.IsTrue(model.GetModelParameters().TimeDelayEstimationWarnings.Count == 0, "time delay estimation should give no warnings");

            double[] estGains = model.GetModelParameters().ProcessGains;
            for (int k = 0; k < estGains.Count(); k++)
            {
                Assert.IsTrue(Math.Abs(designParameters.ProcessGains[k] - estGains[k]) < 0.1,
                    "est.gains should be close to actual gain. Est:" + estGains[k] + "real:" + designParameters.ProcessGains[k]);
            }
            if (designParameters.TimeConstant_s < 0.5)
            {
                Assert.IsTrue(Math.Abs(model.GetModelParameters().TimeConstant_s - designParameters.TimeConstant_s) < 0.1,
                    "est.timeconstant should be close to actual tc.Est:" + model.GetModelParameters().TimeConstant_s +
                    "real:" + designParameters.TimeConstant_s);
            }
            else
            {
                Assert.IsTrue(Math.Abs(designParameters.TimeConstant_s / model.GetModelParameters().TimeConstant_s - 1) < 0.10,
                        "est.timeconstant should be close to actual tc");
            }
            //       double[] curvatures = model.GetModelParameters().Curvatures;
            //      if 



            Assert.IsTrue(Math.Abs(model.GetModelParameters().TimeDelay_s - designParameters.TimeDelay_s) < 0.1,
                "est.time delay should be close to actual");
            Assert.IsTrue(Math.Abs(model.GetModelParameters().Bias - designParameters.Bias) < 0.1,
                "est. Bias should be close to actual");

        }


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }


        [TestCase()]
        
        public void PartlyOverlappingDatasets()
        {
            var u = TimeSeriesCreator.Step(120,240,0,1);
            var y = TimeSeriesCreator.Step(60,240,2,3);// step occurs at same time
            
            // u-dataset starts one minute before y and finishes one minute before
            var dateu = TimeSeriesCreator.CreateDateStampArray(new DateTime(2000, 1, 1, 0, 0, 0), 1, 240);
            var datey = TimeSeriesCreator.CreateDateStampArray(new DateTime(2000, 1, 1, 0, 1, 0), 1, 240);

            var data = new UnitDataSet((u, dateu), (y, datey));

            var id = new DefaultProcessModelIdentifier();
            var model = id.Identify(ref data);

        //    Plot.FromList(new List<double[]> { data.U.GetColumn(0),data.Y_sim, data.Y_meas},
         //       new List<string> { "y3=u", "y1=y_sim", "y1=y_meas" }, data.Times, "partlyoverlaptest");

            Assert.IsTrue(data.NumDataPoints == 180);
            Assert.IsTrue(model.GetModelParameters().ProcessGains.First()< 1.02);
            Assert.IsTrue(model.GetModelParameters().ProcessGains.First() >0.98);


        }



        [TestCase(Double.NaN)]
        [TestCase(-9999)]
        [TestCase(-99.215)]
        public void BadValuesInUandY_DoesNotDestroyResult(double badValueId, double bias=2, double timeConstant_s=10, int timeDelay_s=5)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(150, 300, 0, 1);
            double[] u2 = TimeSeriesCreator.Step( 80, 300, 1, 3);

            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1, u2 });

            bool addInBadDataToYmeas = true;

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                ProcessGains = new double[] { 1, 2 },
                U0 = Vec<double>.Fill(1, 2),
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters, U, timeBase_s, noiseAmplitude,addInBadDataToYmeas, badValueId);

            plot.FromList(new List<double[]> { model.FittedDataSet.Y_sim, model.FittedDataSet.Y_meas, u1, u2 },
                new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2" }, (int)timeBase_s);

            DefaultAsserts(model, designParameters);
        }

        // TODO: testing the uncertainty estimates(after adding them back)
        [TestCase( 0, 0, 0,  Category = "Static")]
        [TestCase(21, 0, 0,  Category = "Static")]
        [TestCase( 0,10, 0,  Category = "Dynamic")]
        [TestCase( 2, 2, 0,  Category = "Dynamic")]
        [TestCase(21,10, 0,  Category = "Dynamic")]
        [TestCase( 0, 0,10,  Category = "Delayed")]
        [TestCase(21, 0, 5,  Category = "Delayed")]

        public void I1_Linear(double bias, double timeConstant_s, int timeDelay_s)
        {
            double noiseAmplitude = 0.01;

            double[] u1 = TimeSeriesCreator.Step(40, 100, 0, 1);
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1 });

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                ProcessGains = new double[] { 1 },
                U0 = Vec<double>.Fill(1,1),
                Bias            = bias
            };
            var model = CreateDataAndIdentify(designParameters, U,timeBase_s,noiseAmplitude);
            string caseId = NUnit.Framework.TestContext.CurrentContext.Test.Name; 
            plot.FromList(new List<double[]> { model.FittedDataSet.Y_sim, 
                model.FittedDataSet.Y_meas, u1 },
                 new List<string> { "y1=ysim", "y1=ymeas", "y3=u1" }, (int)timeBase_s, caseId, default,
                 caseId.Replace("(","").Replace(")","").Replace(",","_"));

            DefaultAsserts(model, designParameters);
        }

        [TestCase(0.4, 0, 0, Category = "Nonlinear")]
        [TestCase(0.2, 0, 0, Category = "Nonlinear")]
        [TestCase(-0.1, 0, 0, Category = "Nonlinear")]
        [TestCase(-0.2, 0, 0, Category = "Nonlinear")]
        [TestCase(-0.4, 0, 0, Category = "Nonlinear")]
        [TestCase(0.4, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(0.2, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(-0.1, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(-0.2, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(-0.4, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(0.4, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(0.2, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(-0.1, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(-0.2, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(-0.4, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(0.4, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        [TestCase(0.2, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        [TestCase(-0.1, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        [TestCase(-0.2, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        [TestCase(-0.4, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]

        public void I1_NonLinear(double curvature, double timeConstant_s, int timeDelay_s)
        {
            double bias = 1;
            double noiseAmplitude = 0.01;

            double[] u1 = TimeSeriesCreator.ThreeSteps(60, 120, 180, 240, 0, 1, 2, 3);
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1 });

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                ProcessGains = new double[] { 1 },
                Curvatures = new double[] { curvature },
                UNorm = new double[] { 1.1 },
                U0 = new double[] { 1 },
                Bias = bias
            };

            DefaultProcessModelParameters paramtersNoCurvature = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                ProcessGains = new double[] { 1 },
                UNorm = new double[] { 1.1 },
                U0 = new double[] { 1 },
                Bias = bias
            };
            var refModel = new DefaultProcessModel(paramtersNoCurvature,timeBase_s,"reference");

            var sim = new PlantSimulator((int)timeBase_s,new List<ISimulatableModel> { refModel });
            sim.AddSignal(refModel, SignalType.External_U, u1);
            var isOk = sim.Simulate(out TimeSeriesDataSet refData);

            var model = CreateDataAndIdentify(designParameters, U, timeBase_s, noiseAmplitude);
            string caseId = TestContext.CurrentContext.Test.Name;
            plot.FromList(new List<double[]> { model.FittedDataSet.Y_sim,
                model.FittedDataSet.Y_meas,
                refData.GetValues(refModel.GetID(),SignalType.Output_Y_sim),
                u1 },
                 new List<string> { "y1=ysim", "y1=ymeas","y1=yref(linear)", "y3=u1" }, (int)timeBase_s, caseId, default,
                 caseId.Replace("(", "").Replace(")", "").Replace(",", "_"));

            DefaultAsserts(model, designParameters);
        }


        [TestCase(0.4, 0, 0, Category = "Nonlinear")]
        [TestCase(0.2, 0, 0, Category = "Nonlinear")]
        [TestCase(-0.1, 0, 0, Category = "Nonlinear")]
        [TestCase(-0.2, 0, 0, Category = "Nonlinear")]
        [TestCase(-0.4, 0, 0, Category = "Nonlinear")]
        [TestCase(0.4, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(0.2, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(-0.1, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(-0.2, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(-0.4, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(0.4, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(0.2, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(-0.1, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(-0.2, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(-0.4, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(0.4, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        [TestCase(0.2, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        [TestCase(-0.1, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        [TestCase(-0.2, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        [TestCase(-0.4, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]

        public void I2_NonLinear(double curvature, double timeConstant_s, int timeDelay_s)
        {
            double bias = 1;
            double noiseAmplitude = 0.02;

            double[] u1 = TimeSeriesCreator.ThreeSteps(60, 120, 180, 240, 0, 1, 2, 3);
            double[] u2 = TimeSeriesCreator.ThreeSteps(90, 150, 210, 240, 2, 1, 3, 2);
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1,u2 });

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                ProcessGains = new double[] { 1, 0.7 },
                Curvatures = new double[] { curvature,curvature },// same curvature
                U0 = new double[] { 1,1 },
                Bias = bias
            };

            DefaultProcessModelParameters paramtersNoCurvature = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                ProcessGains = new double[] { 1, 0.7 },
                U0 = new double[] { 1,1 },
                Bias = bias
            };
            var refModel = new DefaultProcessModel(paramtersNoCurvature, timeBase_s, "reference");

            var sim = new PlantSimulator((int)timeBase_s, new List<ISimulatableModel> { refModel });
            sim.AddSignal(refModel, SignalType.External_U, u1, (int)INDEX.FIRST);
            sim.AddSignal(refModel, SignalType.External_U, u2,(int)INDEX.SECOND);

            var isOk = sim.Simulate(out TimeSeriesDataSet refData);

            var model = CreateDataAndIdentify(designParameters, U, timeBase_s, noiseAmplitude);
            string caseId = TestContext.CurrentContext.Test.Name;
            plot.FromList(new List<double[]> { model.FittedDataSet.Y_sim,
                model.FittedDataSet.Y_meas,
                refData.GetValues(refModel.GetID(),SignalType.Output_Y_sim),
                u1, u2 },
                 new List<string> { "y1=ysim", "y1=ymeas", "y1=yref(linear)", "y3=u1","y3=u2" }, (int)timeBase_s, caseId, default,
                 caseId.Replace("(", "").Replace(")", "").Replace(",", "_"));

            DefaultAsserts(model, designParameters);
        }



        [TestCase(0,0,0,Category="Static")]
        [TestCase(1,0,0, Category ="Static")]
        [TestCase(0,15,0, Category ="Dynamic")]
        [TestCase(1,15,0, Category ="Dynamic")]
        [TestCase(1, 0, 2, Category = "Delayed")]

        public void I2_Linear(double bias, double timeConstant_s, int timeDelay_s)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(50, 100, 0, 1);
            double[] u2 = TimeSeriesCreator.Step(40, 100, 0, 1);
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1, u2 });

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                ProcessGains = new double[] { 1,2 },
                U0 = Vec<double>.Fill(1,2),
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters,U,timeBase_s, noiseAmplitude);

            plot.FromList(new List<double[]> { model.FittedDataSet.Y_sim, model.FittedDataSet.Y_meas, u1,u2 },
                new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2" }, (int)timeBase_s);

            DefaultAsserts(model, designParameters);
        }

        [TestCase(1, 0, 2, Category = "Delayed")]
        [TestCase(1, 15, 2, Category = "Dynamic")]
        public void ExcludeDisturbance_I2_Linear(double bias, double timeConstant_s, int timeDelay_s)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(60, 100, 0, 1);
            double[] u2 = TimeSeriesCreator.Step(40, 100, 1, 0);
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1, u2 });

            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                ProcessGains = new double[] { 1, 2 },
                U0 = Vec<double>.Fill(1, 2),
                Bias = bias
            };
            var dataSet = CreateDataSet(designParameters, U, timeBase_s, noiseAmplitude);
            // introduce disturbance signal in y
            var sin_amplitude = 0.1;
            var sin_period_s = 30;
            var disturbance = TimeSeriesCreator.Sinus(sin_amplitude, sin_period_s,(int)timeBase_s,U.GetNRows());
            dataSet.Y_meas = (new Vec()).Add(dataSet.Y_meas,disturbance);
            dataSet.D = disturbance;
            var model = Identify(dataSet, designParameters);

            plot.FromList(new List<double[]> { model.FittedDataSet.Y_sim, model.FittedDataSet.Y_meas, u1, u2, disturbance },
                new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2","y4=dist" }, (int)timeBase_s,"excl_disturbance");

            DefaultAsserts(model, designParameters);
        }



        [TestCase(0, 0, Category = "Static")]
        [TestCase(1, 0, Category = "Static")]
        [TestCase(0, 20, Category = "Dynamic")]
        [TestCase(1, 20, Category = "Dynamic")]
        public void I3_Linear(double bias, double timeConstant_s)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(50, 100 ,0,1) ;
            double[] u2 = TimeSeriesCreator.Step(35, 100, 1, 0);
            double[] u3 = TimeSeriesCreator.Step(60, 100, 0, 1);
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]> { u1, u2, u3 });
            DefaultProcessModelParameters designParameters = new DefaultProcessModelParameters
            {
                TimeConstant_s = timeConstant_s,
                ProcessGains = new double[] { 1, 2, 1.5 },
                U0 = Vec<double>.Fill(1,3),
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters, U,timeBase_s, noiseAmplitude);
            DefaultAsserts(model, designParameters);
        }



    }
}
