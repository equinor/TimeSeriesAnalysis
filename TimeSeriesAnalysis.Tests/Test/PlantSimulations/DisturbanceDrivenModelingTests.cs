using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.PlantSimulations
{
    internal class DisturbanceDrivenModelingTests
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
        UnitModel staticModel;
        PidParameters pidParameters1;
        PidModel pidModel1;
        PidParameters pidParameters2;
        PidModel pidModel2;

        UnitParameters staticModelParameters;

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();

            modelParameters1 = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 0,
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

            staticModelParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 0,
                Bias = 0
            };


            processModel1 = new UnitModel(modelParameters1, "SubProcess1");
            processModel2 = new UnitModel(modelParameters2, "SubProcess2");
            processModel3 = new UnitModel(modelParameters3, "SubProcess3");
            staticModel = new UnitModel(staticModelParameters, "Static");


            pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            pidModel1 = new PidModel(pidParameters1, "PID1");

            pidParameters2 = new PidParameters()
            {
                Kp = 0.3,
                Ti_s = 24
            };
            pidModel2 = new PidModel(pidParameters2, "PID2");

        }


        // one pid loop, driven by a specified external disturbance D, and then the output "y" of the loop is the input to "processModel2"
        [Test]
        public void BasicPIDwithProcessConnectedToOutputY_SpecifiedDisturbanceSignal_RunsAndConverges()
        {
            double disurbanceStartValue = 0;
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1, processModel2 });
            plantSim.ConnectModels(processModel1, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel1);
            plantSim.ConnectModels(processModel1, processModel2);

            var inputData = new TimeSeriesDataSet();
            var distID = plantSim.AddExternalSignal(processModel1, SignalType.Disturbance_D);
            inputData.Add(distID, TimeSeriesCreator.Step(N / 4, N, disurbanceStartValue, disurbanceStartValue + 1));
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            //    Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                inputData.GetValues(processModel1.GetID(),SignalType.Disturbance_D) },
                new List<string> { "y1=y_sim1", "y3=u", "y4=d" },
                timeBase_s, "BasicPIDwithProcessConnectedToOutput_RunsAndConverges");
            //       Shared.DisablePlots();

            Assert.IsTrue(isOk);
            AssertHasValuesAndIsNotNullOrNanOrFlat(simData.GetValues(processModel2.GetID(), SignalType.Output_Y),N);
        }



        // one pid loop, and then "processModel2" is connected to the output "u" of the pid

        [Test]
        public void BasicPIDwithProcessConnectedToPID_U_SpecifiedDisturbanceSignal_RunsAndConverges()
        {
            double disurbanceStartValue = 0;
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1, processModel2 });
            plantSim.ConnectModels(processModel1, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel1);
            plantSim.ConnectModels(pidModel1, processModel2);

            var inputData = new TimeSeriesDataSet();
            var distID = plantSim.AddExternalSignal(processModel1, SignalType.Disturbance_D);
            inputData.Add(distID, TimeSeriesCreator.Step(N / 4, N, disurbanceStartValue, disurbanceStartValue + 1));
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            //    Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                inputData.GetValues(processModel1.GetID(),SignalType.Disturbance_D) },
                new List<string> { "y1=y_sim1", "y3=u", "y4=d" },
                timeBase_s, "BasicPIDwithProcessConnectedToOutput_RunsAndConverges");
            //       Shared.DisablePlots();

            Assert.IsTrue(isOk);
            AssertHasValuesAndIsNotNullOrNanOrFlat(simData.GetValues(processModel2.GetID(), SignalType.Output_Y), N);
        }



         [Test]
        public void TwoLoops_U_of_loop1_drives_D_of_loop2_RunsAndConverges()
        {
            //////////////////////////////////////////////////
            ///  first we need to simulate with known disturbance, to get the y and u signals 
            var inputData = new TimeSeriesDataSet();
            {
                var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
                plantSim.ConnectModels(processModel1, pidModel1);
                plantSim.ConnectModels(pidModel1, processModel1);

                var inputDataLoc = new TimeSeriesDataSet();
                var distID = plantSim.AddExternalSignal(processModel1, SignalType.Disturbance_D);
                inputDataLoc.Add(distID, TimeSeriesCreator.Step(N / 4, N, 0, 0 + 1));
                inputDataLoc.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
                inputDataLoc.CreateTimestamps(timeBase_s);
                var isOk = plantSim.Simulate(inputDataLoc, out TimeSeriesDataSet simData);

                var combinedData = inputDataLoc.Combine(simData);
                var signals = new List<string>();
                // add just y, u, and setpoint, but importantly don't add the disturbance!
                signals.Add(SignalNamer.GetSignalName(processModel1.GetID(), SignalType.Output_Y));
                signals.Add(SignalNamer.GetSignalName(pidModel1.GetID(), SignalType.Setpoint_Yset));
                signals.Add(SignalNamer.GetSignalName(pidModel1.GetID(), SignalType.PID_U));
                foreach (var signal in signals)
                    inputData.Add(signal, combinedData.GetValues(signal));
                inputData.SetTimeStamps(combinedData.GetTimeStamps().ToList());

            }

            ///////////////////////////////////////////////////////////////////
            /// step2 is to make a plant with two full loops, 
            /// the PlantSimulator will have to create the disturbance signal and then use that to simulate the downstream output of processModel2
            /// 
            {
                var plantSim = new PlantSimulator(
                   new List<ISimulatableModel> { pidModel1, processModel1, staticModel, processModel3, pidModel2 });//note: third process model
                plantSim.ConnectModels(processModel1, pidModel1);
                plantSim.ConnectModels(pidModel1, processModel1);

                plantSim.ConnectModels(processModel1, staticModel);
                plantSim.ConnectModelToOutput(staticModel, processModel3);

                plantSim.ConnectModels(pidModel2, processModel3);
                plantSim.ConnectModels(processModel3, pidModel2);

                var setpointId = SignalNamer.GetSignalName(pidModel2.GetID(), SignalType.Setpoint_Yset);
                inputData.Add(setpointId, TimeSeriesCreator.Constant(50, N));
                plantSim.AddExternalSignal(pidModel2, SignalType.Setpoint_Yset);
                var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
                Assert.IsTrue(isOk);

                AssertHasValuesAndIsNotNullOrNanOrFlat(simData.GetValues(processModel1.GetID(), SignalType.Disturbance_D), N);
                AssertHasValuesAndIsNotNullOrNanOrFlat(simData.GetValues(staticModel.GetID(), SignalType.Output_Y), N);
                AssertHasValuesAndIsNotNullOrNanOrFlat(simData.GetValues(processModel3.GetID(), SignalType.Output_Y), N);
            }
        }

        void AssertHasValuesAndIsNotNullOrNanOrFlat(double[] values, int Nexpected)
        {
            Assert.IsNotNull(values);
            Assert.IsTrue(values.Count() == Nexpected);
            foreach (var value in values)
            {
                Assert.That(!Double.IsNaN(value), "value should not be nan");
                Assert.That(!Double.IsInfinity(value), "value should not be inf");
            }
            Assert.That(!Vec<double>.IsConstant(values), "value should not be flat/constant");


        }


    }
}
