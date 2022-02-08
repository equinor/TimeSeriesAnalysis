using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Tests.PidID
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
            var processSim = new PlantSimulator(timeBase_s,
             new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
   //         processSim.AddSignal(processModel1, SignalType.Disturbance_D, //TimeSeriesCreator.Step(N / 4, N, 0, 1));
            processSim.AddSignal(pidModel1, SignalType.Setpoint_Yset, TimeSeriesCreator.Constant(50, N));
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);

            //TODO: veriy 

        }



    }
}
