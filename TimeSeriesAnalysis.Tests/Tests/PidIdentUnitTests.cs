using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.PidID
{
    [TestFixture]
    class PidIdentUnitTests
    {
        UnitParameters modelParameters1 = new UnitParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 10,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 5,
                Bias = 5
            };

        int timeBase_s = 1;
        int N = 200;
        UnitModel processModel1;


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
            processModel1 = new UnitModel(modelParameters1, timeBase_s, "SubProcess1");
        }
        
        [Test]
        public void YsetpointStepChange_KpAndTiEstimatedOk()
        {
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            var pidModel1 = new PidModel(pidParameters1, timeBase_s, "PID1");
            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet(timeBase_s);
            inputData.Add(processSim.AddSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Step(N/2, N,50,55));
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);

            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var idResult = new PidIdentifier().Identify(pidDataSet);

            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp)< pidParameters1.Kp/10);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s / 10);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
        }

        [TestCase(5)]
        public void DisturbanceStepChange_KpAndTiEstimatedOk(double stepAmplitude)
        {
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            var pidModel1 = new PidModel(pidParameters1, timeBase_s, "PID1");
            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet(timeBase_s);
            inputData.Add(processSim.AddSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50,N));
            inputData.Add(processSim.AddSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Step(N/2,N,0,stepAmplitude));
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);

            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var idResult = new PidIdentifier().Identify(pidDataSet);

            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp) < pidParameters1.Kp / 10);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s / 10);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
        }

        // TODO: test that feedforward is handled correclty



    }
}
