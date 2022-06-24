using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceID
{
    [TestFixture]
    class DisturbanceIDUnitTests
    {
        const bool doPlot = false;

        UnitParameters staticModelParameters = new UnitParameters
        {
            TimeConstant_s = 0,
            LinearGains = new double[] { 1 },
            TimeDelay_s = 0,
            Bias = 5
        };

        UnitParameters dynamicModelParameters = new UnitParameters
        {
            TimeConstant_s = 10,
            LinearGains = new double[] { 1 },
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


        public void  CommonPlotAndAsserts(UnitDataSet pidDataSet, double[] estDisturbance, double[] trueDisturbance)
        {
            Vec vec = new Vec();

            double distTrueAmplitude = vec.Max(vec.Abs(trueDisturbance));

            Assert.IsTrue(estDisturbance != null);
            string caseId = TestContext.CurrentContext.Test.Name.Replace("(","_").
                Replace(")","_").Replace(",","_")+"y";
            if (doPlot)
            {
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),
                estDisturbance, trueDisturbance },
                    new List<string> { "y1=y meas", "y1=y set", "y2=u(right)", "y3=est disturbance", "y3=true disturbance" },
                    pidDataSet.GetTimeBase(), caseId);
            }
 
            Assert.IsTrue(vec.Mean(vec.Abs(vec.Subtract(trueDisturbance, estDisturbance))) < distTrueAmplitude / 10);

            //         Assert.IsTrue(Math.Abs(modelParameters1.GetTotalCombinedProcessGain()- identifiedModel.modelParameters.GetTotalCombinedProcessGain()));
        }

       // [TestCase(-5)]
        [TestCase(5, Explicit = true)]
        public void Static_Step_EstimatesOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }


        // this works as long as only static identifiation is used in the closed-looop identifier,
        // otherwise the model 
        [Test,Explicit]
        public void Static_LongStep_EstimatesOk()
        {
            double stepAmplitude = 5;
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }

        // [TestCase(-5)]
        [Test,Explicit]// gains are slightly too high, around 1.14
        public void NOTWORKING_Dynamic_Step_EstimatesOk(double stepAmplitude=5)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(dynamicModelParameters, "DynamicProcess"), trueDisturbance);
        }

        [Test,Explicit]// process gains are 4.4,far too big, but disturbance amplitude is +/- 8  
     //   [TestCase(-5, 20)]
        public void NOTWORKING_Static_Sinus_EstimatesOk(double sinusAmplitude=5, 
            double sinusPeriod=20)
        {
            var trueDisturbance = TimeSeriesCreator.Sinus(sinusAmplitude, sinusPeriod, timeBase_s, N);
            GenericDisturbanceTest( new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }

        [Test,Explicit] // disturbance is exactly double the actual value, process gains are 4.67, should be 1!
       // [TestCase(-5, 20)]
        public void NOTWORKING_Dynamic_Sinus_EstimatesOk(double sinusAmplitude=-5, double sinusPeriod=20)
        {
            var trueDisturbance = TimeSeriesCreator.Sinus(sinusAmplitude, sinusPeriod, timeBase_s, N);
            GenericDisturbanceTest(new UnitModel(dynamicModelParameters, "DynamicProcess"), trueDisturbance);
        }

        public void GenericDisturbanceTest  (UnitModel processModel, double[] trueDisturbance)
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
            CommonPlotAndAsserts(pidDataSet, estDisturbance, trueDisturbance);
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
