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
        const bool doPlot = true;

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
        int N = 800;
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
            string caseId = TestContext.CurrentContext.Test.Name+"y";
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

        [TestCase(-5)]
        [TestCase(5)]
        public void Static_StepChangeDisturbance_ProcessAndDisturbanceEstimatedOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }

        [TestCase(-5)]
        [TestCase(5)]
        public void Dynamic_StepChangeDisturbance_ProcessAndDisturbanceEstimatedOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(dynamicModelParameters, "DynamicProcess"), trueDisturbance);
        }

        [TestCase(5, 20)]
        [TestCase(-5, 20)]
        public void Static_SinusDisturbance_ProcessAndDisturbanceEstimatedOk(double sinusAmplitude, double sinusPeriod)
        {
            var trueDisturbance = TimeSeriesCreator.Sinus(sinusAmplitude, sinusPeriod, timeBase_s, N);
            GenericDisturbanceTest( new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }

        [TestCase(5, 20)]
        [TestCase(-5, 20)]
        public void Dynamic_SinusDisturbance_ProcessAndDisturbanceEstimatedOk(double sinusAmplitude, double sinusPeriod)
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
            (var identifiedModel, var estDisturbance) = modelId.Identify(pidDataSet);

            Console.WriteLine(identifiedModel.ToString());
            CommonPlotAndAsserts(pidDataSet, estDisturbance, trueDisturbance);
        }
    }
}
