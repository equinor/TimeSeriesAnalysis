using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.PlantSimulations
{
    [TestFixture]
    class SISOTests
    {
        int timeBase_s = 1;
        int N = 500;

        int Ysetpoint = 50;

        UnitParameters modelParameters1;
        UnitParameters modelParameters2;
        UnitParameters modelParameters3;
        UnitModel processModel1;
        UnitModel processModel2;
        UnitModel processModel3;
        PidParameters pidParameters1;
        PidModel pidModel1;

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();

            modelParameters1 = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 5,
                Bias = 5
            };
            modelParameters2 = new UnitParameters
            {
                TimeConstant_s = 20,
                LinearGains = new double[] { 1.1 },
                TimeDelay_s = 10,
                Bias = 5
            };
            modelParameters3 = new UnitParameters
            {
                TimeConstant_s = 20,
                LinearGains = new double[] { 1.1 },
                TimeDelay_s = 10,
                Bias = 5
            };

            processModel1 = new UnitModel(modelParameters1, "SubProcess1");
            processModel2 = new UnitModel(modelParameters2, "SubProcess2");
            processModel3 = new UnitModel(modelParameters3, "SubProcess3");

            pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            pidModel1 = new PidModel(pidParameters1, "PID1");
        }

        // MISO= multiple-input/single-output
        // SISO= single-input/single-output

        [TestCase]
        public void Single_RunsAndConverges()
        {
            var processSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U), TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            CommonAsserts(inputData, simData);
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
        public void Serial2_RunsAndConverges()
        {
            var processSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2 },"Serial2");

            processSim.ConnectModels(processModel1, processModel2);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1,SignalType.External_U), TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);

            processSim.Serialize();

            Assert.IsTrue(isOk);
            CommonAsserts(inputData, simData);

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
        public void Serial3_RunsAndConverges()
        {
            var processSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, processModel3 });

            processSim.ConnectModels(processModel1, processModel2);
            processSim.ConnectModels(processModel2, processModel3);

            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U), TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);

            processSim.Serialize("SISO_Serial3");

            Assert.IsTrue(isOk);
            CommonAsserts(inputData, simData);

            double[] simY = simData.GetValues(processModel3.GetID(), SignalType.Output_Y_sim);
            Assert.IsTrue(Math.Abs(simY[0] - ((55 * 1.1 + 5)*1.1+5)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - ((60 * 1.1 + 5)*1.1+5)) < 0.01);

            /*
            Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel3.GetID(),SignalType.Output_Y_sim),
                 inputData.GetValues(processModel1.GetID(),SignalType.External_U)},
            new List<string> { "y1=y_sim1", "y1=y_sim2", "y1=y_sim3", "y3=u" },
            timeBase_s, "UnitTest_SerialProcess");*/
        }









        public static void CommonAsserts(TimeSeriesDataSet inputData,TimeSeriesDataSet simData)
        {
            var signalNames = simData.GetSignalNames();

            foreach (string signalName in signalNames)
            {
                var signal = simData.GetValues(signalName);
                // test that all systems start in steady-state
                double firstTwoValuesDiff = Math.Abs(signal.ElementAt(0) - signal.ElementAt(1));
                double lastTwoValuesDiff = Math.Abs(signal.ElementAt(signal.Length - 2) - signal.ElementAt(signal.Length - 1));

                Assert.AreEqual(signal.Count(), simData.GetLength(),"all signals should be same length as N");
                Assert.IsTrue(firstTwoValuesDiff < 0.01, "system should start up in steady-state");
                Assert.IsTrue(lastTwoValuesDiff < 0.01, "system should end up in stedy-state");
            }
            Assert.AreEqual(simData.GetLength(), simData.GetTimeStamps().Count(), "number of timestamps shoudl match number of data points in sim");
            Assert.AreEqual(simData.GetTimeStamps().Last(), inputData.GetTimeStamps().Last(),"datasets should end at same timestamp");
        }


        private void  BasicPIDCommonTests(TimeSeriesDataSet simData)
        {
            var U = simData.GetValues(pidModel1.GetID(), SignalType.PID_U);
            double UfirstTwoValuesDiff = Math.Abs(U.ElementAt(0) - U.ElementAt(1));
            double UlastTwoValuesDiff = Math.Abs(U.ElementAt(U.Length - 2) - U.ElementAt(U.Length - 1));

            Assert.IsTrue(UfirstTwoValuesDiff < 0.01, "PID output should start steady");
            Assert.IsTrue(UlastTwoValuesDiff  < 0.01, "PID output should end up steady");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void BasicPID_DisturbanceStep_RunsAndConverges(double disurbanceStartValue)
        {
            var processSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            var distID = processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D); 
            inputData.Add(distID, TimeSeriesCreator.Step(N / 4, N, disurbanceStartValue, disurbanceStartValue+1));
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);


            double firstYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim).First() - Ysetpoint);
            double lastYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim).Last() - Ysetpoint);

            if (false)
            {
                Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                 inputData.GetValues(processModel1.GetID(),SignalType.Disturbance_D) },
                 new List<string> { "y1=y_sim1", "y3=u", "y4=d" },
                 timeBase_s, "BasicPID_DisturbanceStep"); ;
            }

            processSim.Serialize("SISO_basicPID");
            inputData.Combine(simData).ToCsv("SISO_basicPID");



            Assert.IsTrue(isOk);
            Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
            Assert.IsTrue(lastYsimE <0.01,"PID should bring system to setpoint after setpoint change");
            BasicPIDCommonTests(simData);
        }

        [TestCase]
        public void BasicPID_SetpointStep_RunsAndConverges()
        {
            double newSetpoint = 51;
            var processSim = new Dynamic.PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), 
                TimeSeriesCreator.Step(N / 4, N, Ysetpoint, newSetpoint));
            inputData.CreateTimestamps(timeBase_s);
            bool isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);


            double firstYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim).First() - Ysetpoint);
            double lastYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim).Last() - newSetpoint);
            Assert.IsTrue(isOk);
            Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
            Assert.IsTrue(lastYsimE < 0.01, "PID should bring system to setpoint after disturbance");
            BasicPIDCommonTests(simData);
        }














    }
}
