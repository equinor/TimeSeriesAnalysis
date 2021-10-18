using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test
{
    [TestFixture]
    class ProcessSimulatorSISOTests
    {
        int timeBase_s = 1;
        int N = 500;

        int Ysetpoint = 50;

        DefaultProcessModelParameters modelParameters1;
        DefaultProcessModelParameters modelParameters2;
        DefaultProcessModelParameters modelParameters3;
        DefaultProcessModel processModel1;
        DefaultProcessModel processModel2;
        DefaultProcessModel processModel3;
        PIDModelParameters pidParameters1;
        PIDModel pidModel1;

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();

            modelParameters1 = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 10,
                ProcessGains = new double[] { 1 },
                TimeDelay_s = 5,
                Bias = 5
            };
            modelParameters2 = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 20,
                ProcessGains = new double[] { 1.1 },
                TimeDelay_s = 10,
                Bias = 5
            };
            modelParameters3 = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 20,
                ProcessGains = new double[] { 1.1 },
                TimeDelay_s = 10,
                Bias = 5
            };

            processModel1 = new DefaultProcessModel(modelParameters1, timeBase_s, "SubProcess1");
            processModel2 = new DefaultProcessModel(modelParameters2, timeBase_s, "SubProcess2");
            processModel3 = new DefaultProcessModel(modelParameters3, timeBase_s, "SubProcess3");

            pidParameters1 = new PIDModelParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            pidModel1 = new PIDModel(pidParameters1, timeBase_s, "PID1");
        }

        // MISO= multiple-input/single-output
        // SISO= single-input/single-output

        [TestCase]
        public void SISO_Single_RunsAndConverges()
        {
            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel1});
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(N / 4, N, 50, 55));
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            CommonAsserts(simData);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim);
            Assert.IsTrue(Math.Abs(simY[0]- 55)<0.01);
            Assert.IsTrue(Math.Abs(simY.Last()- 60)<0.01);
            /*
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(processModel1.GetID(),SignalType.External_U)},
            new List<string> { "y1=y_sim1", "y3=u" },
            timeBase_s, "UnitTest_SingleSISO");*/
        }

        [TestCase]
        public void SISOs_2Serial_RunsAndConverges()
        {
            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel1, processModel2 });

            processSim.ConnectModels(processModel1, processModel2);
            processSim.AddSignal(processModel1,SignalType.External_U, TimeSeriesCreator.Step(N / 4, N, 50, 55));
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            CommonAsserts(simData);

            double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y_sim);
            Assert.IsTrue(Math.Abs(simY[0] - (55 * 1.1 + 5)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - (60 * 1.1 + 5)) < 0.01);

            /* Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel1.GetID(),SignalType.External_U)},
             new List<string> { "y1=y_sim1", "y1=y_sim2", "y3=u" },
             timeBase_s, "UnitTest_SerialProcess");*/
        }


        [TestCase]
        public void SISOs_3Serial_RunsAndConverges()
        {
            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel1, processModel2, processModel3 });

            processSim.ConnectModels(processModel1, processModel2);
            processSim.ConnectModels(processModel2, processModel3);

            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(N / 4, N, 50, 55));
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            CommonAsserts(simData);

            double[] simY = simData.GetValues(processModel3.GetID(), SignalType.Output_Y_sim);
            Assert.IsTrue(Math.Abs(simY[0] - ((55 * 1.1 + 5)*1.1+5)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - ((60 * 1.1 + 5)*1.1+5)) < 0.01);

            /*
            Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel3.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel1.GetID(),SignalType.External_U)},
            new List<string> { "y1=y_sim1", "y1=y_sim2", "y1=y_sim3", "y3=u" },
            timeBase_s, "UnitTest_SerialProcess");*/
        }









        public static void CommonAsserts(TimeSeriesDataSet simData)
        {
            var signalNames = simData.GetSignalNames();

            foreach (string signalName in signalNames)
            {
                var signal = simData.GetValues(signalName);
                // test that all systems start in steady-state
                double firstTwoValuesDiff = Math.Abs(signal.ElementAt(0) - signal.ElementAt(1));
                double lastTwoValuesDiff = Math.Abs(signal.ElementAt(signal.Length - 2) - signal.ElementAt(signal.Length - 1));

                Assert.IsTrue(firstTwoValuesDiff < 0.01, "system should start up in steady-state");
                Assert.IsTrue(lastTwoValuesDiff < 0.01, "system should end up in stedy-state");
            }
        }


        private void  BasicPIDCommonTests(TimeSeriesDataSet simData)
        {
            var U = simData.GetValues(pidModel1.GetID(), SignalType.PID_U);
            double UfirstTwoValuesDiff = Math.Abs(U.ElementAt(0) - U.ElementAt(1));
            double UlastTwoValuesDiff = Math.Abs(U.ElementAt(U.Length - 2) - U.ElementAt(U.Length - 1));

            Assert.IsTrue(UfirstTwoValuesDiff < 0.01, "PID output should start steady");
            Assert.IsTrue(UlastTwoValuesDiff  < 0.01, "PID output should end up steady");
        }

            [TestCase]
        public void BasicPID_DisturbanceStep_RunsAndConverges()
        {
            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            processSim.AddSignal(processModel1, SignalType.Distubance_D, TimeSeriesCreator.Step(N / 4, N, 0, 1));
            processSim.AddSignal(pidModel1, SignalType.Setpoint_Yset, TimeSeriesCreator.Constant(50, N));
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);

            double firstYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim).First() - Ysetpoint);
            double lastYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim).Last() - Ysetpoint);

            Assert.IsTrue(isOk);
            Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
            Assert.IsTrue(lastYsimE <0.01,"PID should bring system to setpoint after setpoint change");
            BasicPIDCommonTests(simData);
        }

        [TestCase]
        public void BasicPID_SetpointStep_RunsAndConverges()
        {
            double newSetpoint = 51;
            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            processSim.AddSignal(pidModel1, SignalType.Setpoint_Yset, 
                TimeSeriesCreator.Step(N / 4, N, Ysetpoint, newSetpoint));
            bool isOk = processSim.Simulate(out TimeSeriesDataSet simData);

            double firstYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim).First() - Ysetpoint);
            double lastYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim).Last() - newSetpoint);
            Assert.IsTrue(isOk);
            Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
            Assert.IsTrue(lastYsimE < 0.01, "PID should bring system to setpoint after disturbance");
            BasicPIDCommonTests(simData);
        }














    }
}
