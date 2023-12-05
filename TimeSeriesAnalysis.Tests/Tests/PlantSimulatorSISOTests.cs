using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

        // this test also tests that the model is re-set properly for a second run
        [TestCase]
        public void SimulateSingle_InitsRunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U), TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            CommonAsserts(inputData, simData, plantSim);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y);
            Assert.IsTrue(Math.Abs(simY[0]- 55)<0.01);
            Assert.IsTrue(Math.Abs(simY.Last()- 60)<0.01);

            // now test that "simulateSingle" produces the same result!
            var isOk2 = plantSim.SimulateSingle(inputData, processModel1.ID, out TimeSeriesDataSet simData2);
            /*
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                simData2.GetValues(processModel1.GetID(),SignalType.Output_Y),
                inputData.GetValues(processModel1.GetID(),SignalType.External_U)},
            new List<string> { "y1=y_sim1", "y1=y_sim1(v2)", "y3=u" },
            timeBase_s, "UnitTest_SingleSISO");
            */
            double[] simY2 = simData2.GetValues(processModel1.GetID(), SignalType.Output_Y);
            Assert.IsTrue(isOk2);
            Assert.IsTrue(Math.Abs(simY2[0] - 55) < 0.01);
            Assert.IsTrue(Math.Abs(simY2.Last() - 60) < 0.01);
        }

        // if linear gains are null, then the simualtion should still run but with zero output.
        [TestCase]
        public void SimulateSingle_NullGains_RunsWithZeroOutput()
        {
            modelParameters1 = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = null,
                TimeDelay_s = 0,
                Bias = 5
            };

            processModel1 = new UnitModel(modelParameters1, "SubProcess1");

            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U), TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            CommonAsserts(inputData, simData, plantSim);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y);
            Assert.IsTrue(Math.Abs(simY[0]) == 0);

            // now test that "simulateSingle" produces the same result!
            var isOk2 = plantSim.SimulateSingle(inputData, processModel1.ID, out TimeSeriesDataSet simData2);
            /*
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                simData2.GetValues(processModel1.GetID(),SignalType.Output_Y),
                inputData.GetValues(processModel1.GetID(),SignalType.External_U)},
            new List<string> { "y1=y_sim1", "y1=y_sim1(v2)", "y3=u" },
            timeBase_s, "UnitTest_SingleSISO");
            */
        }


        [TestCase]
        public void Serial2_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2 },"Serial2");

            plantSim.ConnectModels(processModel1, processModel2);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1,SignalType.External_U), TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);

            plantSim.Serialize();

            Assert.IsTrue(isOk);
            CommonAsserts(inputData, simData, plantSim);

            double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y);
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
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, processModel3 });

            plantSim.ConnectModels(processModel1, processModel2);
            plantSim.ConnectModels(processModel2, processModel3);

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U), TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);

            //var serialIsOk = plantSim.Serialize("SISO_Serial3",@"c:\appl");
            //Assert.IsTrue(serialIsOk);

            Assert.IsTrue(isOk);
            CommonAsserts(inputData, simData, plantSim);

            double[] simY = simData.GetValues(processModel3.GetID(), SignalType.Output_Y);
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









        public static void CommonAsserts(TimeSeriesDataSet inputData,TimeSeriesDataSet simData, PlantSimulator plant)
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



            Assert.AreEqual(simData.GetLength(), simData.GetTimeStamps().Count(), "number of timestamps should match number of data points in sim");
            Assert.AreEqual(simData.GetTimeStamps().Last(), inputData.GetTimeStamps().Last(),"datasets should end at same timestamp");

        /*    foreach (var modelKeyValuePair in plant.GetModels())
            {
                Assert.IsNotNull(simData.GetValues(modelKeyValuePair.Value.GetID(), SignalType.Output_Y),"model output was not simulated");
            }*/

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
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            plantSim.ConnectModels(processModel1, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            var distID = plantSim.AddExternalSignal(processModel1, SignalType.Disturbance_D); 
            inputData.Add(distID, TimeSeriesCreator.Step(N / 4, N, disurbanceStartValue, disurbanceStartValue+1));
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);

            double firstYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y).First() - Ysetpoint);
            double lastYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y).Last() - Ysetpoint);

            if (false)
            {
                Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                 simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                 inputData.GetValues(processModel1.GetID(),SignalType.Disturbance_D) },
                 new List<string> { "y1=y_sim1", "y3=u", "y4=d" },
                 timeBase_s, "BasicPID_DisturbanceStep"); ;
            }

            Assert.IsTrue(isOk);
            Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
            Assert.IsTrue(lastYsimE < 0.01, "PID should bring system to setpoint after setpoint change");
            BasicPIDCommonTests(simData);

            SerializeHelper.Serialize("BasicPID_disturbanceStep",plantSim,inputData,simData);
            var combinedData = inputData.Combine(simData);
            // step 2: check that if given an inputDataset that includes simData-variables, the 
            // plant simulator is still able to run
        //    var isOk2 = plantSim.Simulate(combinedData, out TimeSeriesDataSet simData2);
        //    Assert.IsTrue(isOk2);

        }

        [TestCase]
        public void BasicPID_SetpointStep_RunsAndConverges()
        {
            double newSetpoint = 51;
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            plantSim.ConnectModels(processModel1, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), 
                TimeSeriesCreator.Step(N / 4, N, Ysetpoint, newSetpoint));
            inputData.CreateTimestamps(timeBase_s);
            bool isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);

            SerializeHelper.Serialize("BasicPID_setpointStep", plantSim, inputData, simData);

            double firstYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y).First() - Ysetpoint);
            double lastYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y).Last() - newSetpoint);
            Assert.IsTrue(isOk);
            Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
            Assert.IsTrue(lastYsimE < 0.01, "PID should bring system to setpoint after disturbance");
            BasicPIDCommonTests(simData);
        }
        [TestCase]
        public void BasicPID_SimulateJustPID_sameresult()
        {
            double newSetpoint = 51;
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            plantSim.ConnectModels(processModel1, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Step(N / 4, N, Ysetpoint, newSetpoint));
            inputData.CreateTimestamps(timeBase_s);
            bool isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            var newSet = new TimeSeriesDataSet();
            newSet.AddSet(inputData);
            newSet.AddSet(simData);
            newSet.SetTimeStamps(inputData.GetTimeStamps().ToList());

            var isOK = plantSim.SimulateSingle(newSet, pidModel1.ID,out TimeSeriesDataSet simData2);

      //      Shared.EnablePlots();
          /*  Plot.FromList(new List<double[]> {
                    simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                    simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                    simData2.GetValues(pidModel1.GetID(),SignalType.PID_U),
                    inputData.GetValues(pidModel1.GetID(),SignalType.Setpoint_Yset)},
               new List<string> { "y1=processOut", "y3=pidOut", "y3=pidOut2","y2=disturbance" },
               timeBase_s, "UnitTest_SimulateJustPID");*/
        //    Shared.DisablePlots();

            double firstYsimE = Math.Abs(simData2.GetValues(pidModel1.GetID(), SignalType.PID_U).First() - simData.GetValues(pidModel1.GetID(), SignalType.PID_U).First());
            double lastYsimE = Math.Abs(simData2.GetValues(pidModel1.GetID(), SignalType.PID_U).Last() - simData.GetValues(pidModel1.GetID(), SignalType.PID_U).Last());
            Assert.IsTrue(isOk);
            Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
            Assert.IsTrue(lastYsimE < 0.01, "PID should bring system to setpoint after disturbance");
            BasicPIDCommonTests(simData);
        }

        [TestCase]
        public void BasicPID_SetpointStep_WithNoiseAndFiltering_FilteringWorks()
        {
            pidModel1.pidParameters.Filtering = new PidFilterParams(true,1,5);

            double newSetpoint = 51;
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            plantSim.ConnectModels(processModel1, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Step(N / 4, N, Ysetpoint, newSetpoint));
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.Disturbance_D),
                TimeSeriesCreator.Noise(N,1,1000) );
            inputData.CreateTimestamps(timeBase_s);
            bool isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            SerializeHelper.Serialize("BasicPID_setpointStepWithFiltering", plantSim, inputData, simData);

            double firstYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y).First() - Ysetpoint);
            double lastYsimE = Math.Abs(simData.GetValues(processModel1.GetID(), SignalType.Output_Y).Last() - newSetpoint);
            Assert.IsTrue(isOk);
          //  Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
         //   Assert.IsTrue(lastYsimE < 0.01, "PID should bring system to setpoint after disturbance");
            //BasicPIDCommonTests(simData);

         /*   Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                 simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                 inputData.GetValues(processModel1.GetID(),SignalType.Disturbance_D)},
            new List<string> { "y1=processOut", "y3=pidOut", "y2=disturbance" },
            timeBase_s, "UnitTest_PidWithNoise");*/
        }

    }
}
