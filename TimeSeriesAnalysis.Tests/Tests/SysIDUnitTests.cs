using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis.Test.SysID
{

    class UnitSimulation
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        public void UnitSimulate_TimeDelay(int timeDelay_s)
        {
            var timeBase_s = 1;
            var parameters = new UnitParameters
            {
                LinearGains = new double []{ 1},
                TimeConstant_s = 0,
                TimeDelay_s = timeDelay_s,
                Bias =0
            };
            var model   = new UnitModel(parameters);
            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 31),
                Vec<double>.Fill(1, 30));
            UnitDataSet dataSet = new UnitDataSet();
            dataSet.U = Array2D<double>.CreateFromList(new List<double[]> { u1 });
            dataSet.CreateTimeStamps(timeBase_s);
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
    class UnitIdentification
    {
        static bool doPlotting = false;
        static Plot4Test plot = new Plot4Test(doPlotting);
        double timeBase_s = 1;

        public UnitDataSet CreateDataSet(UnitParameters designParameters, double[,] U, double timeBase_s,
            double noiseAmplitude = 0, bool addInBadDataToYmeasAndU = false, double badValueId = Double.NaN,
            bool doNonWhiteNoise=false)
        {
            UnitModel model = new UnitModel(designParameters);
            this.timeBase_s = timeBase_s;

            UnitDataSet dataSet = new UnitDataSet();
            dataSet.U = U;
            dataSet.BadDataID = badValueId;
            dataSet.CreateTimeStamps(timeBase_s);
            var simulator = new UnitSimulator(model);
            if (doNonWhiteNoise)
            {
                simulator.SimulateYmeas(ref dataSet, 0);
                double rand = 0;
                var randObj = new Random(45466545);
                for (int i = 0; i < dataSet.Y_meas.Length; i++)
                {
                    rand += (randObj.NextDouble()-0.5)*2* noiseAmplitude;
                    dataSet.Y_meas[i] = dataSet.Y_meas[i] + rand;
                }
            }
            else
            {
                simulator.SimulateYmeas(ref dataSet, noiseAmplitude);
            }

            if (addInBadDataToYmeasAndU)
            {
                int addBadYDataEveryNthPoint = 20;
                int addBadUDataEveryNthPoint = 25;

                for (int curU = 0; curU < dataSet.U.GetNColumns(); curU++)
                {
                    int t_offset = (int)Math.Floor((double)(addBadUDataEveryNthPoint / (curU + 2)));
                    int curT = t_offset;
                    dataSet.U[curT, curU] = badValueId;
                }
                for (int i = 0; i < dataSet.Y_meas.Length; i += addBadYDataEveryNthPoint)
                {
                    dataSet.Y_meas[i] = badValueId;
                }
            }
            return dataSet;
        }

        public UnitModel Identify(UnitDataSet dataSet, UnitParameters designParameters)
        {
            var modelId = new UnitIdentifier();
            UnitModel identifiedModel = modelId.Identify(ref dataSet, designParameters.U0, designParameters.UNorm);
            return identifiedModel;
        }
        public UnitModel IdentifyStatic(UnitDataSet dataSet, UnitParameters designParameters)
        {
            var modelId = new UnitIdentifier();
            UnitModel identifiedModel = modelId.IdentifyStatic(ref dataSet, designParameters.U0, designParameters.UNorm);
            return identifiedModel;
        }
        public UnitModel CreateDataAndIdentify(
            UnitParameters designParameters, double[,] U, double timeBase_s,
            double noiseAmplitude = 0, bool addInBadDataToYmeasAndU = false, double badValueId = Double.NaN)
        {
            var dataSet = CreateDataSet(designParameters, U, timeBase_s, noiseAmplitude,
                addInBadDataToYmeasAndU, badValueId);
            return Identify(dataSet, designParameters);
        }

        public UnitModel CreateDataAndIdentifyStatic(
    UnitParameters designParameters, double[,] U, double timeBase_s,
    double noiseAmplitude = 0, bool addInBadDataToYmeasAndU = false, double badValueId = Double.NaN)
        {
            var dataSet = CreateDataSet(designParameters, U, timeBase_s, noiseAmplitude,
                addInBadDataToYmeasAndU, badValueId);
            return IdentifyStatic(dataSet, designParameters);
        }

        public UnitModel CreateDataWithNonWhiteNoiseAndIdentify(
             UnitParameters designParameters, double[,] U, double timeBase_s,
             double noiseAmplitude = 0, bool addInBadDataToYmeasAndU = false, double badValueId = Double.NaN)
        {
            var dataSet = CreateDataSet(designParameters, U, timeBase_s, noiseAmplitude,
                addInBadDataToYmeasAndU, badValueId,true);
            return Identify(dataSet, designParameters);
        }



        /// <summary>
        /// These test criteria shoudl normally pass, unless you are testing the negative
        /// </summary>
        public void DefaultAsserts(UnitModel model, UnitParameters designParameters,int numExpectedWarnings=0)
        {
            Console.WriteLine(model.ToString());

            Assert.IsNotNull(model, "returned model should never be null");
            Assert.IsTrue(model.GetModelParameters().Fitting.WasAbleToIdentify, "should be able to identify model");
            Assert.IsTrue(model.GetModelParameters().GetWarningList().Count == numExpectedWarnings, "gave wrong number of warnings");
            //  Assert.IsTrue(model.GetModelParameters().TimeDelayEstimationWarnings.Count == 0, "time delay estimation should give no warnings");

            double[] estGains = model.GetModelParameters().GetProcessGains();
/*            for (int k = 0; k < estGains.Count(); k++)
            {
                Assert.IsTrue(Math.Abs(designParameters.GetProcessGain(k) - estGains[k]) < 0.1,
                "est.gains should be close to actual gain. Est:" + estGains[k] + "real:" + designParameters.GetProcessGain(k));
            }*/
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
            Assert.IsTrue(Math.Abs(model.GetModelParameters().TimeDelay_s - designParameters.TimeDelay_s) < 0.1,
                "est.time delay should be close to actual");
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

            var id = new UnitIdentifier();
            var model = id.Identify(ref data);

        //    Plot.FromList(new List<double[]> { data.U.GetColumn(0),data.Y_sim, data.Y_meas},
         //       new List<string> { "y3=u", "y1=y_sim", "y1=y_meas" }, data.Times, "partlyoverlaptest");

            Assert.IsTrue(data.GetNumDataPoints() == 180);
            Assert.IsTrue(model.GetModelParameters().LinearGains.First()< 1.02);
            Assert.IsTrue(model.GetModelParameters().LinearGains.First() >0.98);
        }

        [TestCase(new int[] { 0, 10 })]
        [TestCase(new int[] { 0, 1,2,3,4,5,6,7,8,})]
        // [TestCase(new int[] { 0, 1,2,3,4,5,6,7,8,9,10,11 })]//fails
        // makes u2-non-observable -causing gain of u2 to be too big. (this needs to be treated more thourougly)
        //    [TestCase(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24 })]

        public void IndicesToIgnore(int[] badDataIndices)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(10, 30, 0, 1);
        //    double[] u2 = TimeSeriesCreator.Step(10, 100, 0, 1);
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1 });

            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = 10,
                TimeDelay_s = 0,
                LinearGains = new double[] { 1},
                Bias = 2
            };

            var dataSet = CreateDataSet(designParameters, U, timeBase_s, noiseAmplitude);
            foreach(var index in badDataIndices)
            dataSet.Y_meas[index] = +5;// data to be ignored

            dataSet.IndicesToIgnore = (badDataIndices).ToList(); 

            var modelId = new UnitIdentifier();
            var model = modelId.IdentifyLinear(ref dataSet,false);

            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim, model.GetFittedDataSet().Y_meas, u1 },
                new List<string> { "y1=ysim", "y1=ymeas", "y3=u1"}, (int)timeBase_s);

            DefaultAsserts(model, designParameters);
        }
        [TestCase(Double.NaN)]
        [TestCase(-9999)]
        [TestCase(-99.215)]
        public void AllBadValues(double badValueId, double bias = 2, double timeConstant_s = 10, int timeDelay_s = 5)
        {
            double[] u1 = TimeSeriesCreator.Step(150, 300, 0, 1);
            double[] u2 = Vec<double>.Fill(badValueId, 300);

            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2 });
            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1, 2 },
                U0 = Vec<double>.Fill(1, 2),
                Bias = bias
            };

            bool addInBadDataToYmeas = false;
            double noiseAmplitude = 0.01;
            var model = CreateDataAndIdentify(designParameters, U, timeBase_s, noiseAmplitude, addInBadDataToYmeas, badValueId);

            Assert.IsFalse(model.GetModelParameters().Fitting.WasAbleToIdentify);
            // also: the model shoudl not downright crash!
        }


        [TestCase(Double.NaN)]
        [TestCase(-9999)]
        [TestCase(-99.215)]
        public void BadValuesInUandY_DoesNotDestroyResult(double badValueId, double bias=2, double timeConstant_s=10, int timeDelay_s=5)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(150, 300, 0, 1);
            double[] u2 = TimeSeriesCreator.Step( 80, 300, 1, 3);

            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2 });

            bool addInBadDataToYmeas = true;

            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1, 2 },
                U0 = Vec<double>.Fill(1, 2),
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters, U, timeBase_s, noiseAmplitude,addInBadDataToYmeas, badValueId);

            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim, model.GetFittedDataSet().Y_meas, u1, u2 },
                new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2" }, (int)timeBase_s);

            DefaultAsserts(model, designParameters);
        }



        /// <summary>
        /// Identification of time-constants seems to get worse as the number of samples increases,
        /// The noise in the "diffs" drown out the information if there is so-called over-sampling.
        /// Thus if you are trying to identifiy a time-constant of 15 days, yo may get a better result if you 
        /// sample once a day, rather than once a minute. To get around this, there is a 
        /// downsampling feature in the dataset-class.
        /// </summary>
        /// <param name="N">number of samples in the dataset,</param>
        /// <param name="downsampleFactor">Only use every N-th sample for identification</param>
        [TestCase(100,0),Explicit]//requires no downsampling
        [TestCase(1000,10)]// downsample by factor 10
        public void DownsampleOversampledData(int N, int downsampleFactor)
        {
            double timeConstant_s = 20;
            int timeDelay_s = 0 ;

            double bias = 2;
            double noiseAmplitude = 0.01;

            double[] u1 = TimeSeriesCreator.Step((int)Math.Ceiling(N * 0.4), N, 0, 1) ;
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1 });

            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1 },
                U0 = Vec<double>.Fill(1, 1),
                Bias = bias
            };

            var dataSet = CreateDataSet(designParameters, U, timeBase_s, noiseAmplitude);
            UnitDataSet dataSetDownsampled;
            if (downsampleFactor > 1)
                dataSetDownsampled = new UnitDataSet(dataSet, downsampleFactor);
            else
                dataSetDownsampled = new UnitDataSet(dataSet);
            var modelId = new UnitIdentifier();
            var model  = modelId.Identify(ref dataSetDownsampled, designParameters.U0, designParameters.UNorm);

            string caseId = TestContext.CurrentContext.Test.Name;
            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim,
                model.GetFittedDataSet().Y_meas, u1 },
                 new List<string> { "y1=ysim", "y1=ymeas", "y3=u1" }, (int)timeBase_s, caseId, default,
                 caseId.Replace("(", "").Replace(")", "").Replace(",", "_"));

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
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1 });

            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1 },
                U0 = Vec<double>.Fill(1,1),
                Bias            = bias
            };
            var model = CreateDataAndIdentify(designParameters, U,timeBase_s,noiseAmplitude);
            string caseId = TestContext.CurrentContext.Test.Name; 
            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim, 
                model.GetFittedDataSet().Y_meas, u1 },
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
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1 });

            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1 },
                Curvatures = new double[] { curvature },
                UNorm = new double[] { 1.1 },
                U0 = new double[] { 1.5 },
                Bias = bias
            };

            UnitParameters paramtersNoCurvature = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1 },
                UNorm = new double[] { 1.1 },
                U0 = new double[] { 1 },
                Bias = bias
            };
            var refModel = new UnitModel(paramtersNoCurvature,"reference");

            var sim = new PlantSimulator(new List<ISimulatableModel> { refModel });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(sim.AddExternalSignal(refModel, SignalType.External_U), u1);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = sim.Simulate(inputData,out TimeSeriesDataSet refData);

            var model = CreateDataAndIdentify(designParameters, U, timeBase_s, noiseAmplitude);
            string caseId = TestContext.CurrentContext.Test.Name;
            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim,
                model.GetFittedDataSet().Y_meas,
                refData.GetValues(refModel.GetID(),SignalType.Output_Y_sim),
                u1 },
                 new List<string> { "y1=ysim", "y1=ymeas","y1=yref(linear)", "y3=u1" }, (int)timeBase_s, caseId, default,
                 caseId.Replace("(", "").Replace(")", "").Replace(",", "_"));

            DefaultAsserts(model, designParameters);
        }

        [TestCase(0.4, 0, 0, Category = "Nonlinear")]
        [TestCase(-0.2, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(-0.4, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(0.4, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        [TestCase(0.2, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        public void I2_OneNonlinearInput(double curvature, double timeConstant_s, int timeDelay_s)
        {
            var model = I2_Internal(curvature,timeConstant_s,timeDelay_s,false);

            Assert.IsTrue(model.GetModelParameters().Curvatures[1] == 0,"id should disable second curvature term");

        }

        [TestCase(0.4, 0, 0, Category = "Nonlinear")]
        [TestCase(-0.4, 0, 0, Category = "Nonlinear")]
        [TestCase(0.4, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(-0.4, 5, 0, Category = "Nonlinear,Dynamic")]
        [TestCase(0.4, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(-0.4, 0, 5, Category = "Nonlinear,Delayed")]
        [TestCase(0.4, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]
        [TestCase(-0.4, 5, 5, Category = "Nonlinear,Delayed,Dynamic")]

        public void I2_NonLinear(double curvature, double timeConstant_s, int timeDelay_s,bool curvatureOnBothInputs=true)
        {
            I2_Internal(curvature, timeConstant_s, timeDelay_s, curvatureOnBothInputs);
        }

        UnitModel I2_Internal(double curvature, double timeConstant_s, int timeDelay_s, bool curvatureOnBothInputs)
        {
            double bias = 1;
            double noiseAmplitude = 0.02;

            double[] u1 = TimeSeriesCreator.ThreeSteps(60, 120, 180, 240, 0, 1, 2, 3);
            double[] u2 = TimeSeriesCreator.ThreeSteps(90, 150, 210, 240, 2, 1, 3, 2);
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2 });

            double[] curvatures;
            if (curvatureOnBothInputs)
            {
                curvatures = new double[] { curvature, curvature };
            }
            else
            {
                curvatures = new double[] { curvature, 0 };
            }

            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1, 0.7 },
                Curvatures = curvatures,
                U0 = new double[] { 1.1, 1.1 },// set this to make results comparable
                UNorm = new double[] { 1, 1 },// set this to make results comparable
                Bias = bias
            };

            UnitParameters paramtersNoCurvature = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1, 0.7 },
                U0 = new double[] { 1.1, 1.1 },
                Bias = bias
            };
            var refModel = new UnitModel(paramtersNoCurvature, "reference");

            var sim = new PlantSimulator( new List<ISimulatableModel> { refModel });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(sim.AddExternalSignal(refModel, SignalType.External_U, (int)INDEX.FIRST), u1);
            inputData.Add(sim.AddExternalSignal(refModel, SignalType.External_U, (int)INDEX.SECOND),u2 );
            inputData.CreateTimestamps(timeBase_s);
            var isOk = sim.Simulate(inputData,out TimeSeriesDataSet refData);

            var model = CreateDataAndIdentify(designParameters, U, timeBase_s, noiseAmplitude);
            string caseId = TestContext.CurrentContext.Test.Name;
            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim,
                model.GetFittedDataSet().Y_meas,
                refData.GetValues(refModel.GetID(),SignalType.Output_Y_sim),
                u1, u2 },
                 new List<string> { "y1=ysim", "y1=ymeas", "y1=yref(linear)", "y3=u1", "y3=u2" }, (int)timeBase_s, caseId, default,
                 caseId.Replace("(", "").Replace(")", "").Replace(",", "_"));

            DefaultAsserts(model, designParameters);
            return model;
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
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2 });

            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1,2 },
                U0 = Vec<double>.Fill(1,2),
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters,U,timeBase_s, noiseAmplitude);

            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim, model.GetFittedDataSet().Y_meas, u1,u2 },
                new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2" }, (int)timeBase_s);
            DefaultAsserts(model, designParameters);
        }

        [TestCase(0, 0, 0, Category = "Static")]
        [TestCase(1, 0, 0, Category = "Static")]
        [TestCase(0, 15, 0, Category = "Dynamic")]
        [TestCase(1, 15, 0, Category = "Dynamic")]
        [TestCase(1, 0, 2, Category = "Delayed")]

        // test that specifying a non-unity Unorm is identified and simulated 
        // correctly
       /* public void I2_Linear_WithUNorm(double bias, double timeConstant_s, int timeDelay_s)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(50, 100, 0, 1);
            double[] u2 = TimeSeriesCreator.Step(40, 100, 0, 1);
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2 });

            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1, 2 },
                U0 = Vec<double>.Fill(1, 2),
                UNorm = Vec<double>.Fill(2, 2),// by default woudl be one
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters, U, timeBase_s, noiseAmplitude);

            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim, model.GetFittedDataSet().Y_meas, u1, u2 },
                new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2" }, (int)timeBase_s);
            DefaultAsserts(model, designParameters);
        }*/


        [TestCase(0, 0, 0, Category = "Static")]
        [TestCase(1, 0, 0, Category = "Static")]
       // [TestCase(0, 15, 0, Category = "Dynamic")]
       // [TestCase(1, 15, 0, Category = "Dynamic")]
       // [TestCase(1, 0, 2, Category = "Delayed")]

        public void I2_Linear_OnlyOneInputIsExcited_EstGainShouldBeZero(double bias, double timeConstant_s, int timeDelay_s)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(50, 100, 0, 1);
            double[] u2 = Vec<double>.Fill(0,100);// input is flat - the gain should in this case be zero!;//todo: try with nonzero const value
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2 });

            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1, 0 },
                U0 = Vec<double>.Fill(1, 2),
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters, U, timeBase_s, noiseAmplitude);

            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim, model.GetFittedDataSet().Y_meas, u1, u2 },
                new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2" }, (int)timeBase_s);
            DefaultAsserts(model, designParameters,1);
        }


        [TestCase(1, 0, 2, Category = "Delayed")]
        [TestCase(1, 15, 2, Category = "Dynamic")]
        public void ExcludeDisturbance_I2_Linear(double bias, double timeConstant_s, int timeDelay_s)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(60, 100, 0, 1);
            double[] u2 = TimeSeriesCreator.Step(40, 100, 1, 0);
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2 });

            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1, 2 },
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

            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim, model.GetFittedDataSet().Y_meas, u1, u2, disturbance },
                new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2","y4=dist" }, (int)timeBase_s,"excl_disturbance");

            DefaultAsserts(model, designParameters,1);
        }



        [TestCase(0, 0, Category = "Static")]
        [TestCase(1, 0, Category = "Static")]
        [TestCase(0, 20, Category = "Dynamic")]
        [TestCase(1, 20, Category = "Dynamic")]
        public void I3_Linear_singlesteps(double bias, double timeConstant_s)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(50, 100 ,0,1) ;
            double[] u2 = TimeSeriesCreator.Step(35, 100, 1, 0);
            double[] u3 = TimeSeriesCreator.Step(60, 100, 0, 1);
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2, u3 });
            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                LinearGains = new double[] { 1, 2, 1.5 },
                Bias = bias
            };
            var model = CreateDataAndIdentify(designParameters, U,timeBase_s, noiseAmplitude);
            DefaultAsserts(model, designParameters);
        }

        [TestCase(0, 0, Category = "Static")]
        [TestCase(1, 0, Category = "Static")]
        public void IdentifyStatic_I3_Linear_singlesteps(double bias, double timeConstant_s)
        {
            double noiseAmplitude = 0.01;
            double[] u1 = TimeSeriesCreator.Step(50, 100, 0, 1);
            double[] u2 = TimeSeriesCreator.Step(35, 100, 1, 0);
            double[] u3 = TimeSeriesCreator.Step(60, 100, 0, 1);
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2, u3 });
            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                LinearGains = new double[] { 1, 2, 1.5 },
                Bias = bias
            };
            var model = CreateDataAndIdentifyStatic(designParameters, U, timeBase_s, noiseAmplitude);
            DefaultAsserts(model, designParameters);
        }



        [TestCase(0, 0, Category = "Static")]
        [TestCase(1, 0, Category = "Static")]
        [TestCase(0, 20, Category = "Dynamic")]
        [TestCase(1, 20, Category = "Dynamic")]
        public void SignificantNonWhiteNoise_I3_Linear(double bias, double timeConstant_s)
        {
            Plot4Test plotLocal = new Plot4Test(false);
            int N = 1000;
            double noiseAmplitude = 0.07;
            double[] u1 = TimeSeriesCreator.Step(500, N, 0, 1);
            double[] u2 = TimeSeriesCreator.Step(350, N, 1, 0);
            double[] u3 = TimeSeriesCreator.Step(600, N, 0, 1);
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2, u3 });
            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                LinearGains = new double[] { 1, 2, 1.5 },
                Bias = bias
            };
            var model = CreateDataWithNonWhiteNoiseAndIdentify(designParameters, U, timeBase_s, noiseAmplitude);

            string caseId = TestContext.CurrentContext.Test.Name;
            plotLocal.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim,
                model.GetFittedDataSet().Y_meas,            
                u1, u2,u3 },
                 new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2","y3=u3" }, (int)timeBase_s, caseId, default,
                 caseId.Replace("(", "").Replace(")", "").Replace(",", "_"));

            // In this case, the model gains are not very accurate.
            // It is also the case that the time constant tends to be non-zero even if the underlying process is static
            Console.WriteLine(model.ToString());
            Assert.IsNotNull(model, "returned model should never be null");
            Assert.IsTrue(model.GetModelParameters().Fitting.WasAbleToIdentify, "should be able to identify model");
            Assert.IsTrue((new Vec()).Max(model.GetModelParameters().GetProcessGains())<3);
            Assert.IsTrue((new Vec()).Max(model.GetModelParameters().GetProcessGains()) > 0.5);
            // DefaultAsserts(model, designParameters);

            plot.FromList(new List<double[]> { model.GetFittedDataSet().Y_sim, model.GetFittedDataSet().Y_meas, u1, u2, u3 },
                  new List<string> { "y1=ysim", "y1=ymeas", "y3=u1", "y3=u2", "y3=u3" }, (int)timeBase_s, "NonwhiteNoise_I3");

        }





    }
}
