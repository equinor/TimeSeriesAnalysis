using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceID
{
    [TestFixture]
    class CloosedLoopAndDisturbanceIdentTests
    {
        const bool doPlot = true;

        UnitParameters staticModelParameters = new UnitParameters
        {
            TimeConstant_s = 0,
            LinearGains = new double[] { 1.5 },
            TimeDelay_s = 0,
            Bias = 5
        };

        UnitParameters dynamicModelParameters = new UnitParameters
        {
            TimeConstant_s = 10,
            LinearGains = new double[] { 1.5 },
            TimeDelay_s = 5,
            Bias = 5
        };

        PidParameters pidParameters1 = new PidParameters()
        {
            Kp = 0.2,
            Ti_s = 20
        };


        int timeBase_s = 1;
        int N = 300;// TODO:influences the results!
        DateTime t0 = new DateTime(2010,1,1);


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }


        public void CommonPlotAndAsserts(UnitDataSet pidDataSet, double[] estDisturbance, double[] trueDisturbance,
            UnitModel identifiedModel, UnitModel trueModel, double maxAllowedGainOffsetPrc)
        {
            Vec vec = new Vec();

            double distTrueAmplitude = vec.Max(vec.Abs(trueDisturbance));

            Assert.IsTrue(estDisturbance != null);
            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                Replace(")", "_").Replace(",", "_") + "y";
         //   if (doPlot)
            {
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),
                estDisturbance, trueDisturbance },
                    new List<string> { "y1=y meas", "y1=y set", "y2=u(right)", "y3=est disturbance", "y3=true disturbance" },
                    pidDataSet.GetTimeBase(), caseId);
            }

            Assert.IsTrue(vec.Mean(vec.Abs(vec.Subtract(trueDisturbance, estDisturbance))) < distTrueAmplitude / 10,"true disturbance and actual disturbance too far apart");

            double estGain = identifiedModel.modelParameters.GetTotalCombinedProcessGain(0);
            double trueGain = trueModel.modelParameters.GetTotalCombinedProcessGain(0);
            double gainOffsetPrc = Math.Abs(estGain - trueGain) / Math.Abs(trueGain) * 100;
            Assert.IsTrue(gainOffsetPrc < maxAllowedGainOffsetPrc,"est.gain:"+ estGain+ "|true gain:"+trueGain);
        }

        [TestCase(-5)]
        [TestCase(5)]
        public void Static_StepDisturbance_EstimatesOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }


        [TestCase(1,1.5,10)]
        //[TestCase(1,-1.5,30)]
        public void Static_RandomWalk_EstimatesOk(double noiseAmplitude, double systemGain, double precisionPrc)
        {
            UnitParameters staticModelParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { systemGain },
                TimeDelay_s = 0,
                Bias = 5
            };

            Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.RandomWalk( N, noiseAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance,true, precisionPrc);
            Shared.DisablePlots();
        }



        // this works as long as only static identifiation is used in the closed-looop identifier,
        // otherwise the model 
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
         
        public void Static_LongStepDisturbance_EstimatesOk(double stepAmplitude)
        {
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }

        // this works as long as only static identifiation is used in the closed-looop identifier,

        [Test]
        public void FlatData_DoesNotCrash()
        {
            double stepAmplitude = 0;
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), 
                trueDisturbance,false);
        }

        [TestCase(-5,5)]
        [TestCase(5, 5)]
        [TestCase(10, 5)]
        public void Dynamic_Step_EstimatesOk(double stepAmplitude, double processGainAllowedOffsetPrc)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(dynamicModelParameters, "DynamicProcess"), trueDisturbance,true, 
                processGainAllowedOffsetPrc);
        }
        /*
        [TestCase(-5,20),Explicit]// process gains are 4.4,far too big, but disturbance amplitude is +/- 8  
        public void NOTWORKING_Static_SinusDisturbance_EstimatesDistOk(double sinusAmplitude=5,       double sinusPeriod=20)
        { 
            Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.Sinus(sinusAmplitude, sinusPeriod, timeBase_s, N);
            GenericDisturbanceTest( new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
            Shared.DisablePlots();
        }

        [Test,Explicit] // disturbance is exactly double the actual value, process gains are 4.67, should be 1!
       // [TestCase(-5, 20)]
        public void NOTWORKING_Dynamic_SinusDisturbance_EstimatesDistOk(double sinusAmplitude=-5, double sinusPeriod=20)
        {
            Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.Sinus(sinusAmplitude, sinusPeriod, timeBase_s, N);
            GenericDisturbanceTest(new UnitModel(dynamicModelParameters, "DynamicProcess"), trueDisturbance);
            Shared.DisablePlots();
        }
        */
        public void GenericDisturbanceTest  (UnitModel processModel, double[] trueDisturbance, 
            bool doAssertResult=true, double processGainAllowedOffsetPrc=10)
        {
            // create synthetic dataset
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel });
            processSim.ConnectModels(processModel, pidModel1);
            processSim.ConnectModels(pidModel1, processModel);
            var inputData = new TimeSeriesDataSet();
           
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel, SignalType.Disturbance_D), trueDisturbance);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var modelId = new ClosedLoopUnitIdentifier();
            (var identifiedModel, var estDisturbance) = modelId.Identify(pidDataSet, pidModel1.GetModelParameters());

            Console.WriteLine(identifiedModel.ToString());
            Console.WriteLine();

            DisturbancesToString(estDisturbance, trueDisturbance);
            if (doAssertResult)
            {
                CommonPlotAndAsserts(pidDataSet, estDisturbance, trueDisturbance, processModel,identifiedModel, processGainAllowedOffsetPrc);
            }
        }

        void DisturbancesToString(double[] estDisturbance, double[] trueDisturbance)
        { 
            StringBuilder sb = new StringBuilder();
            Vec vec = new Vec();

            //var estAvg = vec.Mean(estDisturbance).Value.ToString("F1");
            var estMin = vec.Min(estDisturbance).ToString("F1") ;
            var estMax = vec.Max(estDisturbance).ToString("F1");
            var trueMin = vec.Min(trueDisturbance).ToString("F1");
            var trueMax = vec.Max(trueDisturbance).ToString("F1");
            //var trueAvg = vec.Mean(estDisturbance).Value.ToString("F1");

            sb.AppendLine("disturbance min:"+estMin+" max:"+estMax+"(actual min:"+trueMin+"max:"+trueMax+")");
            Console.WriteLine(sb.ToString());
        }



    }
}
