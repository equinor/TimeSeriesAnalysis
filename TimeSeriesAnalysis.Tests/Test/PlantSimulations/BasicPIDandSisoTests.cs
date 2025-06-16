using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Accord.Statistics.Models.Regression.Fitting;
using NUnit.Framework;

using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.PlantSimulations
{
    [TestFixture]
    class BasicPIDandSISOTests
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

        [TestCase]
        public void SimulateSingle_InitsRunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U), TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y);
            Assert.IsTrue(Math.Abs(simY[0] - 55) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - 60) < 0.01);

            // now test that "simulateSingle" produces the same result!
            var isOk2 = PlantSimulatorHelper.SimulateSingle(inputData, processModel1, out TimeSeriesDataSet simData2);
           // var isOk2 = plantSim.SimulateSingle(inputData, processModel1.ID, out TimeSeriesDataSet simData2);

            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                simData2.GetValues(processModel1.GetID(),SignalType.Output_Y),
                inputData.GetValues(processModel1.GetID(),SignalType.External_U)},
                new List<string> { "y1=y_sim1", "y1=y_sim1(v2)", "y3=u" },
                timeBase_s, TestContext.CurrentContext.Test.Name);
                Shared.DisablePlots();
            }
            double[] simY2 = simData2.GetValues(processModel1.GetID(), SignalType.Output_Y);
            Assert.IsTrue(isOk2);
            Assert.IsTrue(Math.Abs(simY2[0] - 55) < 0.01);
            Assert.IsTrue(Math.Abs(simY2.Last() - 60) < 0.01);
        }




        // MISO= multiple-input/single-output
        // SISO= single-input/single-output


        [TestCase,Explicit]
        public void SimulateSingle_SecondOrderSystem()
        {
            string v1 = "7";
            int N = 5000;
            int Nstep = 50;

            var modelParams2ndOrder = new UnitParameters
            {
                TimeConstant_s = 50,
                DampingRatio = 0.1,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 0,
                Bias = 5
            };

            // using this "raw" value, we see that at damping= 1corresponds in a 1.order system to doublng the time constant 
            // and using the dampingzeta= 2 the time constant must be quadrupled
            //  double timeConstantCorrection = (1 + Math.Pow(DampingZeta, 2));
            //  double omega_n = 1 / (FilterTc_s * timeConstantCorrection);

            // if dampingratio = 0.25  then timeconst of 1.order system 50 (factor=1)
            // if dampingratio = 0.5  then timeconst of 1.order system  50(factor=1)
            // if dampingratio = 0.625  then timeconst of 1.order system  62.5(factor=1.25)
            // if dampingratio = 0.75  then timeconst of 1.order system  75(factor=1.5)
            // if dampingratio = 1 - then timeconstant of 1.order system must be 100(factor=2)
            // if dampingratio = 2  then timeconst of 1.order system must be 200(factor=4)
            // if dampingratio = 3  then timeconst of 1.order system must be 300(factor=6)
            // if dampingratio = 4  then timeconst of 1.order system must be 400(factor=8)
            // if dampingratio = 5  then timeconst of 1.order system must be 600(factor=10)
            // if dampingratio = 6  then timeconst of 1.order system must be 800(factor=12)

            //         var factor = Math.Max(1, 1 + Math.Pow(modelParams2ndOrder.DampingRatio - 0.5, 1 / 2)); // works between 0-1.

            var DampingZeta = modelParams2ndOrder.DampingRatio;

         /*   var factor = 1.0;
            if (DampingZeta <= 0)
            {
                factor = 0;
            }
            else if (DampingZeta < 1)
            {
                factor = Math.Max(1, 1 + Math.Pow(DampingZeta - 0.5, 1 / 2));
            }
            else
                factor = DampingZeta * 2;*/

            var modelParams1stOrder = new UnitParameters
            {
                TimeConstant_s = 50,
                DampingRatio = 0,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 0,
                Bias = 5
            };

            var procModel = new UnitModel(modelParams2ndOrder,"second order system");
            var procModel2 = new UnitModel(modelParams1stOrder, "first order system");

            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { procModel,procModel2 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(procModel, SignalType.External_U), TimeSeriesCreator.Step(Nstep, N, 50, 55));
            inputData.Add(plantSim.AddExternalSignal(procModel2, SignalType.External_U), TimeSeriesCreator.Step(Nstep, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
 
            if (true)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                simData.GetValues(procModel.GetID(),SignalType.Output_Y),
                simData.GetValues(procModel2.GetID(),SignalType.Output_Y),
                inputData.GetValues(procModel.GetID(),SignalType.External_U)},
                new List<string> { "y1=y_secondorder", "y1=y_firstorder",  "y3=u" },
                timeBase_s, TestContext.CurrentContext.Test.Name+ v1);
                Shared.DisablePlots();
            }
            PsTest.CommonAsserts(inputData, simData, plantSim);

        }

        // if linear gains are null, then the simulation should still run but with zero output.
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
            PsTest.CommonAsserts(inputData, simData, plantSim);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y);
            Assert.IsTrue(Math.Abs(simY[0]) == 0);

            // now test that "simulateSingle" produces the same result!
            var isOk2 = PlantSimulatorHelper.SimulateSingle(inputData, processModel1, out TimeSeriesDataSet simData2);
            //var isOk2 = plantSim.SimulateSingle(inputData, processModel1.ID, out TimeSeriesDataSet simData2);


            //plots 

            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                simData2.GetValues(processModel1.GetID(),SignalType.Output_Y),
                inputData.GetValues(processModel1.GetID(),SignalType.External_U)},
                new List<string> { "y1=y_sim1", "y1=y_sim1(v2)", "y3=u" },
                timeBase_s, "UnitTest_SingleSISO");
                Shared.DisablePlots();
            }


            // asserts

            Assert.IsTrue(isOk2);

            Assert.AreEqual(simData.GetValues(processModel1.GetID(), SignalType.Output_Y),
                simData2.GetValues(processModel1.GetID(), SignalType.Output_Y));

        }









        private void  BasicPIDCommonTests(TimeSeriesDataSet simData, PidModel pidModel)
        {
            var U = simData.GetValues(pidModel.GetID(), SignalType.PID_U);
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
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                 simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                 inputData.GetValues(processModel1.GetID(),SignalType.Disturbance_D) },
                 new List<string> { "y1=y_sim1", "y3=u", "y4=d" },
                 timeBase_s, "BasicPID_DisturbanceStep");
                Shared.DisablePlots();
            }

            Assert.IsTrue(isOk);
            Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
            Assert.IsTrue(lastYsimE < 0.01, "PID should bring system to setpoint after setpoint change");
            BasicPIDCommonTests(simData,pidModel1);

            SerializeHelper.Serialize("BasicPID_disturbanceStep",plantSim,inputData,simData);
            var combinedData = inputData.Combine(simData);
            // step 2: check that if given an inputDataset that includes simData-variables, the 
            // plant simulator is still able to run
        //    var isOk2 = plantSim.Simulate(combinedData, out TimeSeriesDataSet simData2);
        //    Assert.IsTrue(isOk2);

        }

        [TestCase(true)]
        [TestCase(false)]

        public void BasicPID_SetpointStep_RunsAndConverges(bool delayPidOutputOneSample)
        {
            var pidParameters = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20,
                DelayOutputOneSample = delayPidOutputOneSample
            };

            var modelParameters = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 0,
                Bias = 5
            };

            var pidModel = new PidModel(pidParameters,"PidModel");
            var processModel = new UnitModel(modelParameters,"ProcessModel");

            double newSetpoint = 51;
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel, processModel });
            plantSim.ConnectModels(processModel, pidModel);
            plantSim.ConnectModels(pidModel, processModel);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(pidModel, SignalType.Setpoint_Yset), 
                TimeSeriesCreator.Step(N / 4, N, Ysetpoint, newSetpoint));
            inputData.CreateTimestamps(timeBase_s);
            bool isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);

            
            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel.GetID(),SignalType.Output_Y),
                 simData.GetValues(pidModel.GetID(),SignalType.PID_U),
                 inputData.GetValues(pidModel.GetID(),SignalType.Setpoint_Yset) },
                 new List<string> { "y1=y_sim1", "y3=u", "y1=y_set" },
                 timeBase_s, TestContext.CurrentContext.Test.Name);
                Shared.DisablePlots();
            }

            SerializeHelper.Serialize("BasicPID_setpointStep", plantSim, inputData, simData);

            double firstYsimE = Math.Abs(simData.GetValues(processModel.GetID(), SignalType.Output_Y).First() - Ysetpoint);
            double lastYsimE = Math.Abs(simData.GetValues(processModel.GetID(), SignalType.Output_Y).Last() - newSetpoint);
            Assert.IsTrue(isOk);
            Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
            Assert.IsTrue(lastYsimE < 0.01, "PID should bring system to setpoint after disturbance");
            BasicPIDCommonTests(simData, pidModel);
        }


        [TestCase]
        public void Serial2_SISO_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2 }, "Serial2");

            plantSim.ConnectModels(processModel1, processModel2);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U), TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            plantSim.Serialize();

            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);

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
        public void Serial2_SISO_IgnoresBadDataPoints_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2 }, "Serial2");

            plantSim.ConnectModels(processModel1, processModel2);
            var inputData = new TimeSeriesDataSet();

            var uValues = TimeSeriesCreator.Step(N / 4, N, 50, 55);
            uValues[5] = inputData.BadDataID;

            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U), uValues);
            inputData.CreateTimestamps(timeBase_s);

            var isOk = plantSim.Simulate(inputData, true, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            Assert.IsTrue(simData.GetIndicesToIgnore().Count()>0,"no indices were tagged to be ignored!");

            PsTest.CommonAsserts(inputData, simData, plantSim);

            double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y);
            Assert.IsTrue(Math.Abs(simY[0] - (55 * 1.1 + 5)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - (60 * 1.1 + 5)) < 0.01);

            if (false)
            {
                Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y),
                 simData.GetValues(processModel1.GetID(),SignalType.External_U)},
                new List<string> { "y1=y_sim1", "y1=y_sim2", "y3=u" },
                timeBase_s, "UnitTest_SerialProcess");
            }
        }



        [TestCase]
        public void Serial3_SISO_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, processModel3 });

            plantSim.ConnectModels(processModel1, processModel2);
            plantSim.ConnectModels(processModel2, processModel3);

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U), TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            //var serialIsOk = plantSim.Serialize("SISO_Serial3",@"c:\appl");
            //Assert.IsTrue(serialIsOk);

            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);

            double[] simY = simData.GetValues(processModel3.GetID(), SignalType.Output_Y);
            Assert.IsTrue(Math.Abs(simY[0] - ((55 * 1.1 + 5) * 1.1 + 5)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - ((60 * 1.1 + 5) * 1.1 + 5)) < 0.01);

            if (false)
            {
                Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y),
                 simData.GetValues(processModel3.GetID(),SignalType.Output_Y),
                 inputData.GetValues(processModel1.GetID(),SignalType.External_U)},
                new List<string> { "y1=y_sim1", "y1=y_sim2", "y1=y_sim3", "y3=u" },
                timeBase_s, TestContext.CurrentContext.Test.Name);
            }
        }



        //
        // Incredibly important that this unit tests passes, as SimulateSingle is used to estimate the disturbance as part of initalization of 
        // Simulate(), so these two methods need to simulate in a consisten way for disturbance estimation to work, which again is vital for 
        // disturbance estimation and closed-loop unit identification to work.
        //

        [TestCase]
        public void BasicPIDSetpointStep_CompareSimulateAndSimulateSingle_MustGiveSameResultForDisturbanceEstToWork()
        {
            //var pidCopy = pidModel1.Clone();
            double newSetpoint = 51;
            int N = 100;
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            plantSim.ConnectModels(processModel1, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Step(1, N, Ysetpoint, newSetpoint));
            inputData.CreateTimestamps(timeBase_s);
            bool isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            var newSet = new TimeSeriesDataSet();
            newSet.AddSet(inputData);
            newSet.AddSet(simData);
            newSet.SetTimeStamps(inputData.GetTimeStamps().ToList());

            var isOk2 = PlantSimulatorHelper.SimulateSingle(newSet, pidModel1, out var simData2);

            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                      simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                      simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                      simData2.GetValues(pidModel1.GetID(),SignalType.PID_U),
                      inputData.GetValues(pidModel1.GetID(),SignalType.Setpoint_Yset)},
                   new List<string> { "y1=processOutSimulate", "y3=upidSim", "y3=upidSimSingle", "y2=setpoint" },
                   timeBase_s, TestContext.CurrentContext.Test.Name);
                Shared.DisablePlots();
            }
            double firstYsimE = Math.Abs(simData2.GetValues(pidModel1.GetID(), SignalType.PID_U).First() - simData.GetValues(pidModel1.GetID(), SignalType.PID_U).First());
            double lastYsimE = Math.Abs(simData2.GetValues(pidModel1.GetID(), SignalType.PID_U).Last() - simData.GetValues(pidModel1.GetID(), SignalType.PID_U).Last());

            Assert.IsTrue(isOk);
            Assert.IsTrue(firstYsimE < 0.01, "System should start in steady-state");
            BasicPIDCommonTests(simData,pidModel1);

            var vec = new Vec();
            var v1 = simData.GetValues(pidModel1.GetID(), SignalType.PID_U);
            var v2 = simData2.GetValues(pidModel1.GetID(), SignalType.PID_U);
            var errorPrc = vec.Mean(vec.Abs(vec.Subtract(v1, v2) )) / vec.Mean(vec.Abs(v2));
            Console.WriteLine("error prc:" + errorPrc.Value.ToString("F5"));
            Assert.IsTrue(errorPrc < 0.001 / 100, "true disturbance and actual disturbance too far apart");

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

            if (false)
            {

                   Plot.FromList(new List<double[]> {
                        simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                        simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                        inputData.GetValues(processModel1.GetID(),SignalType.Disturbance_D)},
                   new List<string> { "y1=processOut", "y3=pidOut", "y2=disturbance" },
                   timeBase_s, TestContext.CurrentContext.Test.Name);
            }
        }


        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        public void TimeDelay(int timeDelay_s)
        {
            var timeBase_s = 1;
            var parameters = new UnitParameters
            {
                LinearGains = new double[] { 1 },
                TimeConstant_s = 0,
                TimeDelay_s = timeDelay_s,
                Bias = 0
            };
            var model = new UnitModel(parameters);
            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 31),
                Vec<double>.Fill(1, 30));
            UnitDataSet dataSet = new UnitDataSet();
            dataSet.U = Array2D<double>.CreateFromList(new List<double[]> { u1 });
            dataSet.CreateTimeStamps(timeBase_s);

            (bool isOk, double[] y_sim) = PlantSimulatorHelper.SimulateSingle(dataSet, model);
            // plot
            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> { y_sim, u1 }, new List<string> { "y1=ymeas ", "y3=u1" }, timeBase_s);
                Shared.DisablePlots();
            }
            //assert
            //  Assert.IsNotNull(retSim);
            Assert.IsTrue(isOk);
            Assert.IsTrue(y_sim[30 + timeDelay_s] == 0, "step should not arrive at y_sim too early");
            Assert.IsTrue(y_sim[31 + timeDelay_s] == 1, "steps should be delayed exactly timeDelay_s later  ");
        }


    }
}
