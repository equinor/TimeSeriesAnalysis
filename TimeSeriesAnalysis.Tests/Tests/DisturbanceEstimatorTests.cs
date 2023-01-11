using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceID
{
    /// <summary>
    /// In these tests, the UnitModel is given, and the aim is to verify that for a known model the distubance estimator is able
    /// to arrive at the correct disturbance time-series.
    /// </summary>
    [TestFixture]
    class DisturbanceEstimatorTests
    {
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

        public void CommonPlotAndAsserts(UnitDataSet pidDataSet, DisturbanceIdResult estDisturbance, double[] trueDisturbance)
        {
            Vec vec = new Vec();
            double distTrueAmplitude = vec.Max(vec.Abs(trueDisturbance));
            var d_est = estDisturbance.d_est;

            Assert.IsTrue(d_est != null);
            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                Replace(")", "_").Replace(",", "_") + "y";

            Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
            pidDataSet.U.GetColumn(0),
            d_est, trueDisturbance },
                new List<string> { "y1=y meas", "y1=y set", "y2=u(right)", "y3=est disturbance", "y3=true disturbance" },
                pidDataSet.GetTimeBase(), caseId);

            Assert.IsTrue(vec.Mean(vec.Abs(vec.Subtract(trueDisturbance, d_est))) < distTrueAmplitude / 10,"true disturbance and actual disturbance too far apart");
        }
 
        [TestCase(-5)]
        [TestCase(5)]
        public void Static_StepDisturbance_EstimatesOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }

        [TestCase(-5)]
        [TestCase(5)]
        public void Static_SinusDisturbance_EstimatesOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Sinus(stepAmplitude, timeBase_s*15, timeBase_s,N );
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }


        [TestCase(-5)]
        [TestCase(5)]
        public void Dynamic_StepDisturbance_EstimatesOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(dynamicModelParameters, "DynamicProcess"), trueDisturbance);
        }

        public void GenericDisturbanceTest  (UnitModel processModel, double[] trueDisturbance, 
            bool doAssertResult=true, double processGainAllowedOffsetPrc=10)
        {
            // create synthetic dataset
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var plantSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel });
            plantSim.ConnectModels(processModel, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel);
            var inputData = new TimeSeriesDataSet();
           
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(plantSim.AddExternalSignal(processModel, SignalType.Disturbance_D), trueDisturbance);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            var pidDataSet = plantSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var result = DisturbanceIdentifier.EstimateDisturbance(pidDataSet, processModel);
            if (doAssertResult)
            {
                CommonPlotAndAsserts(pidDataSet, result,trueDisturbance);
            }
        }

    }
}
