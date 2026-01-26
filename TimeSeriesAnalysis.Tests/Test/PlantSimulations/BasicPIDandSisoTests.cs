using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Statistics.Models.Regression.Fitting;
using NUnit.Framework;
using System.Globalization;

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
            var isOk2 = PlantSimulatorHelper.SimulateSingle(inputData, processModel1, false,out TimeSeriesDataSet simData2);
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
            var  zetasToTry = new List<double> { 0.05, 0.10, 0.15, 0.20, 0.25, 0.5, 1, 2 };

            var ySimList = new List<double[]>();

         
            int N = 5000;
            int Nstep = 50;

            var simDescList = new List<string>();
            var inputData = new TimeSeriesDataSet();

            var simData = new TimeSeriesDataSet();

            var modelParams1stOrder = new UnitParameters
            {
                TimeConstant_s = 50,
                DampingRatio = 0,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 0,
                Bias = 5
            };
            string v1 = "TimeConstant "+ modelParams1stOrder.TimeConstant_s+"s";

            var procModel2 = new UnitModel(modelParams1stOrder, "first order system");

            foreach (var zeta in zetasToTry)
            {
                simDescList.Add("y1=y_sim zeta " + zeta.ToString("F3", CultureInfo.InvariantCulture));
                var modelParams2ndOrder = new UnitParameters
                {
                    TimeConstant_s = 150,
                    DampingRatio = zeta,
                    LinearGains = new double[] { 1 },
                    TimeDelay_s = 0,
                    Bias = 5
                };

                var DampingZeta = modelParams2ndOrder.DampingRatio;
                var procModel = new UnitModel(modelParams2ndOrder, "second order system");
                var plantSim = new PlantSimulator(
                    new List<ISimulatableModel> { procModel, procModel2 });
    
                inputData.Add(plantSim.AddExternalSignal(procModel, SignalType.External_U), TimeSeriesCreator.Step(Nstep, N, 50, 55));
                inputData.Add(plantSim.AddExternalSignal(procModel2, SignalType.External_U), TimeSeriesCreator.Step(Nstep, N, 50, 55));
                inputData.CreateTimestamps(timeBase_s);
                var isOk = plantSim.Simulate(inputData, out simData);

                ySimList.Add(simData.GetValues(procModel.GetID(), SignalType.Output_Y));
                Assert.IsTrue(isOk);
                PsTest.CommonAsserts(inputData, simData, plantSim);
            }

            if (true)
            {
                simDescList.Add("y3=u");
                ySimList.Add(inputData.GetValues(procModel2.GetID(), SignalType.External_U));

                simDescList.Add("y1= first order");
                ySimList.Add(simData.GetValues(procModel2.GetID(), SignalType.Output_Y));

                Shared.EnablePlots();
                Plot.FromList(ySimList,simDescList,
                timeBase_s, TestContext.CurrentContext.Test.Name+ v1);
                Shared.DisablePlots();
            }



            /*   if (true)
               {
                   Shared.EnablePlots();
                   Plot.FromList(new List<double[]> {
                   simData.GetValues(procModel.GetID(),SignalType.Output_Y),
                   simData.GetValues(procModel2.GetID(),SignalType.Output_Y),
                   inputData.GetValues(procModel.GetID(),SignalType.External_U)},
                   new List<string> { "y1=y_secondorder", "y1=y_firstorder",  "y3=u" },
                   timeBase_s, TestContext.CurrentContext.Test.Name+ v1);
                   Shared.DisablePlots();
               }*/
         //   PsTest.CommonAsserts(inputData, simData, plantSim);

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
            var isOk2 = PlantSimulatorHelper.SimulateSingle(inputData, processModel1, false,out TimeSeriesDataSet simData2);
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

        //    SerializeHelper.Serialize("BasicPID_disturbanceStep",plantSim,inputData,simData);
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

        //[TestCase(1, 1, true)]
        //[TestCase(3, 1, true)]
        [TestCase(1, 0,false)]
        [TestCase(3, 1, false)]
        public void Serial2_SISO_IgnoresBadDataPoints_RunsRestartsSimulatorAndConverges(int nBadIndices, int expectSimRestarts,
            bool letSimulatorDetermineIndToIgnore)
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2 }, "Serial2");

            plantSim.ConnectModels(processModel1, processModel2);
            var inputData = new TimeSeriesDataSet();

            var uValues = TimeSeriesCreator.Step(N / 4, N, 50, 55);
            var badIdxList = new List<int>();
            for (int i = 0; i < nBadIndices; i++)
            {
                int badIdx = 5 + i;
                badIdxList.Add(badIdx);
                uValues[badIdx] = inputData.BadDataID;
            }

            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U), uValues);
            inputData.CreateTimestamps(timeBase_s);

            var simData = new TimeSeriesDataSet(); ;
            var isOk = false;
            if (letSimulatorDetermineIndToIgnore)
            {
                isOk = plantSim.Simulate(inputData, doDetermineIndicesToIgnore: letSimulatorDetermineIndToIgnore,
                   out simData);
            }
            else
            {
                //NB! need to append trailing indices if given an external list of indices to ignore.
                inputData.SetIndicesToIgnore(badIdxList);
                isOk = plantSim.Simulate(inputData, doDetermineIndicesToIgnore: letSimulatorDetermineIndToIgnore,
                    out simData);
            }

            Console.WriteLine(simData);

            if (false) 
            { 
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                    simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                    simData.GetValues(processModel2.GetID(),SignalType.Output_Y),
                    inputData.GetValues(processModel1.GetID(),SignalType.External_U)},
                new List<string> { "y1=y_sim1", "y1=y_sim2", "y3=u" },
                timeBase_s, TestContext.CurrentContext.Test.Name);
                Shared.DisablePlots();
            }

            Assert.IsTrue(isOk,"simulation did not run");
            Assert.IsTrue(simData.GetIndicesToIgnore().Count() > 0, "no indices were tagged to be ignored!");
            Assert.IsTrue(simData.GetIndicesToIgnore().Count()>0,"no indices were tagged to be ignored!");

            PsTest.CommonAsserts(inputData, simData, plantSim);

            double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y);
            Assert.IsTrue(Math.Abs(simY[0] - (55 * 1.1 + 5)) < 0.01,"should start up in steady state");
            Assert.IsTrue(Math.Abs(simY.Last() - (60 * 1.1 + 5)) < 0.01,"should end in steady-state, ended at:"+ simY.Last());

       //     Assert.AreEqual(expectSimRestarts,simData.GetNumSimulatorRestarts(), "sim restarted wrong number of times");


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
        // Simulate(), so these two methods need to simulate in a consistent way for disturbance estimation to work, which again is vital for 
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

            var isOk2 = PlantSimulatorHelper.SimulateSingle(newSet, pidModel1, false,out var simData2);

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



        [TestCase(100,1,1,0.05 )]
        public void BasicPID_wFlatlinesCoSimulate_SimRestartIsBumpless(int N, double timeBase, int flatlinePeriods, double flatlineProportion)
        {
            int firstFlatLineStartIndex = -1;
            int firstFlatLineEndIndex = -1;

            // Define parameters
            var trueParameters = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 50
            };

            // Create plant model
            var pidModel1 = new PidModel(trueParameters, "PID1");
            var processSim = new PlantSimulator( new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);

            // Create synthetic data
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Sinus(10, timeBase * 20, timeBase, N));
            inputData.CreateTimestamps(timeBase);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk, "simulate did not run");
            var combinedData = inputData.Combine(simData);
            var pidDataSet = processSim.GetUnitDataSetForPID(combinedData, pidModel1);

            var combinedDataFlatLines = new TimeSeriesDataSet(combinedData);
            // Identify on both original and flatlined datasets

            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").Replace(")", "_").Replace(",", "_") + "y";

            if (false)// plot the raw data before flatline is created
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> { pidDataSet.Y_meas, pidDataSet.Y_setpoint, pidDataSet.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u" },
                    pidDataSet.GetTimeBase(), caseId + "_beforeSim");
                Shared.DisablePlots();
            }
            // Create synthetic data with flatlines (Create them anew to avoid shallow copies / references)
            int flatlinePeriodLength = (int)(flatlineProportion * N / flatlinePeriods);
            var pidDataSetWithFlatlines = processSim.GetUnitDataSetForPID(combinedDataFlatLines, pidModel1);
            /// create the flat data sets.
            for (int i = 0; i < flatlinePeriods; i++)
            {
                int flatlineStartIndex = (int)(N * ((double)i + 0.5) / flatlinePeriods - flatlinePeriodLength / 2);
                if (i == 0)
                    firstFlatLineStartIndex = flatlineStartIndex;
                for (int j = 1; j < flatlinePeriodLength; j++)
                {
                    pidDataSetWithFlatlines.U[flatlineStartIndex + j, 0] = pidDataSetWithFlatlines.U[flatlineStartIndex, 0];
                    pidDataSetWithFlatlines.Y_meas[flatlineStartIndex + j] = pidDataSetWithFlatlines.Y_meas[flatlineStartIndex];
                    pidDataSetWithFlatlines.Y_setpoint[flatlineStartIndex + j] = pidDataSetWithFlatlines.Y_setpoint[flatlineStartIndex];
                    if (i == 0) 
                    {
                        firstFlatLineEndIndex = flatlineStartIndex + j;
                    }
                }
            }

            // try to simualte dataset, and see that a simulator reset is performed and that the pid-controller is "warm-started" correctly in a bumpless
            var inputData_withFlatLines = new TimeSeriesDataSet();
            inputData_withFlatLines.Add("PID1-Setpoint_Yset", pidDataSetWithFlatlines.Y_setpoint);
            inputData_withFlatLines.SetTimeStamps(inputData.GetTimeStamps().ToList());

            inputData_withFlatLines.Add("SubProcess1-Output_Y", pidDataSetWithFlatlines.Y_meas);
            inputData_withFlatLines.Add("PID1-PID_U", pidDataSetWithFlatlines.U.GetColumn(0));

            bool determineIndicesToIgnore = true;
            var isOk2 = processSim.Simulate(inputData_withFlatLines, determineIndicesToIgnore, out var simData_withFlatLines);

            var simU = simData_withFlatLines.GetValues(pidModel1.GetID(), SignalType.PID_U);

            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]>{ pidDataSetWithFlatlines.Y_meas, pidDataSetWithFlatlines.Y_setpoint,
                    pidDataSetWithFlatlines.U.GetColumn(0), simU},
                    new List<string> { "y1=y_meas_with_flatlines", "y1=y_setpoint_with_flatlines", "y3=u_with_flatlines", "y3=u_sim_with_flatlines" },
                    pidDataSetWithFlatlines.GetTimeBase(), caseId + "_with_flatlines");
                Shared.DisablePlots();
            }
            var fitScore = FitScoreCalculator.GetPlantWideSimulated(processSim, inputData_withFlatLines, simData_withFlatLines);
            
            Assert.IsTrue(isOk2);
       //     Assert.AreEqual(1, simData_withFlatLines.GetNumSimulatorRestarts(),"simulator should restart once");
            Assert.IsTrue(fitScore > 60, "simulation should restart and this should ensure that there is no large devaiation after the flatline period and a high fitscore.");
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

            (bool isOk, double[] y_sim, _) = PlantSimulatorHelper.SimulateSingle(dataSet, model);
            // plot
            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> { y_sim, u1 }, new List<string> { "y1=ymeas ", "y3=u1" }, timeBase_s);
                Shared.DisablePlots();
            }
            Assert.IsTrue(isOk);
            Assert.IsTrue(y_sim[30 + timeDelay_s] == 0, "step should not arrive at y_sim too early");
            Assert.IsTrue(y_sim[31 + timeDelay_s] == 1, "steps should be delayed exactly timeDelay_s later  ");
        }

        [Test]
 /*       public void VariableTimeStepSimulations_PIDPerfectDataShouldMatchFixedStepSize()
        {
            double newSetpoint = 51;
            int N = 100;

            var plantSimFixed = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            plantSimFixed.ConnectModels(processModel1, pidModel1);
            plantSimFixed.ConnectModels(pidModel1, processModel1);

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSimFixed.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Step(1, N, Ysetpoint, newSetpoint));
            inputData.CreateTimestamps(timeBase_s);

            var plantSimVar = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            plantSimVar.ConnectModels(processModel1, pidModel1);
            plantSimVar.ConnectModels(pidModel1, processModel1);

            bool isOk2 = plantSimVar.Simulate(inputData, doDetermineIndicesToIgnore: false, out TimeSeriesDataSet simDataVar,
                enableSimulatorRestarting: false, doVariableTimeBase: true);

            bool isOk = plantSimFixed.Simulate(inputData, doDetermineIndicesToIgnore:false, out TimeSeriesDataSet simDataFixed,
                enableSimulatorRestarting:false,doVariableTimeBase: false);

            Assert.AreEqual(simDataFixed.GetValues(processModel1.GetID(), SignalType.Output_Y), simDataVar.GetValues(processModel1.GetID(), SignalType.Output_Y));
            Assert.AreEqual(simDataFixed.GetValues(pidModel1.GetID(), SignalType.PID_U), simDataVar.GetValues(pidModel1.GetID(), SignalType.PID_U));

            if (true)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<(double[], DateTime[])> {
                      (simDataFixed.GetValues(processModel1.GetID(),SignalType.Output_Y),simDataFixed.GetTimeStamps()),
                      (simDataVar.GetValues(processModel1.GetID(),SignalType.Output_Y),simDataVar.GetTimeStamps()),
                      (simDataFixed.GetValues(pidModel1.GetID(),SignalType.PID_U),simDataFixed.GetTimeStamps()),
                      (simDataVar.GetValues(pidModel1.GetID(),SignalType.PID_U),simDataVar.GetTimeStamps()),
                      (inputData.GetValues(pidModel1.GetID(),SignalType.Setpoint_Yset),inputData.GetTimeStamps())},
                   new List<string> { "y1=y_measFixed", "y1=y_measVar", "y3=u_pidFixed", "y3=u_pidVar", "y1=setpoint" },
                   TestContext.CurrentContext.Test.Name);
                Shared.DisablePlots();
            }
        }*/

        //TODO: test also with timedelay
        [TestCase(new int[] { 5, 15, 25 })]
        public void VariableTimeStepSimulations_UnitMOdelSkipsOverBadIndices(int[] indToIgnore)
        {
            int N = 30;

            var plantSimFixed = new PlantSimulator(
                new List<ISimulatableModel> { processModel1 });
            var inputData = new TimeSeriesDataSet();
 
            inputData.Add(plantSimFixed.AddExternalSignal(processModel1, SignalType.External_U),
                TimeSeriesCreator.Step(1, N, 50, 51));
            inputData.CreateTimestamps(timeBase_s);
            bool isOk = plantSimFixed.Simulate(inputData, doDetermineIndicesToIgnore: false, out TimeSeriesDataSet simDataFixed,
                enableSimulatorRestarting: false, doVariableTimeBase: false);

            var plantSimVar = new PlantSimulator(
                new List<ISimulatableModel> { processModel1 });

            inputData.SetIndicesToIgnore(new List<int>(indToIgnore));

            var extU = inputData.GetValues(processModel1.GetID(), SignalType.External_U);
            foreach (var index in indToIgnore)
                extU[index] = double.NaN;

            inputData.ReplaceValues(processModel1.GetID(), SignalType.External_U, extU);


            bool isOk2 = plantSimVar.Simulate(inputData, doDetermineIndicesToIgnore: false, out TimeSeriesDataSet simDataVar,
                enableSimulatorRestarting: false, doVariableTimeBase: false);

            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<(double[], DateTime[])> {
                      (simDataFixed.GetValues(processModel1.GetID(),SignalType.Output_Y),simDataFixed.GetTimeStamps()),
                      (simDataVar.GetValues(processModel1.GetID(),SignalType.Output_Y),simDataVar.GetTimeStamps())
                      },
                   new List<string> { "y1=y_measFixed", "y1=y_measVar" },
                   TestContext.CurrentContext.Test.Name);
                Shared.DisablePlots();
            }

            //     Assert.AreEqual(simDataFixed.GetValues(processModel1.GetID(), SignalType.Output_Y), simDataVar.GetValues(processModel1.GetID(), SignalType.Output_Y));
            //     Assert.AreEqual(simDataFixed.GetValues(pidModel1.GetID(), SignalType.PID_U), simDataVar.GetValues(pidModel1.GetID(), SignalType.PID_U));
        }




        //TODO: test also with timedelay
        [TestCase(new int[] { 4})]
      //  [TestCase(new int[] { 10, 11 } )]
        public void VariableTimeStepSimulations_PIDSkipsOverBadIndices(int[] indToIgnore)
        {
            double newSetpoint = 51;
            int N = 30;

            var plantSimFixed = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            plantSimFixed.ConnectModels(processModel1, pidModel1);
            plantSimFixed.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSimFixed.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Step(1, N, Ysetpoint, newSetpoint));
            inputData.CreateTimestamps(timeBase_s);
            bool isOk = plantSimFixed.Simulate(inputData, doDetermineIndicesToIgnore: false, out TimeSeriesDataSet simDataFixed,
                enableSimulatorRestarting: false, doVariableTimeBase: false);

            var plantSimVar = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });
            plantSimVar.ConnectModels(processModel1, pidModel1);
            plantSimVar.ConnectModels(pidModel1, processModel1);

            var pid_u = inputData.GetValues(pidModel1.GetID(), SignalType.Setpoint_Yset);
            foreach (var index in indToIgnore)
                pid_u[index] = double.NaN;

            inputData.ReplaceValues(pidModel1.GetID(), SignalType.Setpoint_Yset, pid_u);


            inputData.SetIndicesToIgnore(new List<int>(indToIgnore));

            bool isOk2 = plantSimVar.Simulate(inputData, doDetermineIndicesToIgnore: false, out TimeSeriesDataSet simDataVar,
                enableSimulatorRestarting: false, doVariableTimeBase: false);

            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<(double[], DateTime[])> {
                      (simDataFixed.GetValues(processModel1.GetID(),SignalType.Output_Y),simDataFixed.GetTimeStamps()),
                      (simDataVar.GetValues(processModel1.GetID(),SignalType.Output_Y),simDataVar.GetTimeStamps()),
                      (simDataFixed.GetValues(pidModel1.GetID(),SignalType.PID_U),simDataFixed.GetTimeStamps()),
                      (simDataVar.GetValues(pidModel1.GetID(),SignalType.PID_U),simDataVar.GetTimeStamps()),
                      (inputData.GetValues(pidModel1.GetID(),SignalType.Setpoint_Yset),inputData.GetTimeStamps())},
                   new List<string> { "y1=y_measFixed", "y1=y_measVar", "y3=u_pidFixed", "y3=u_pidVar", "y1=setpoint" },
                   TestContext.CurrentContext.Test.Name);
                Shared.DisablePlots();
            }

            //     Assert.AreEqual(simDataFixed.GetValues(processModel1.GetID(), SignalType.Output_Y), simDataVar.GetValues(processModel1.GetID(), SignalType.Output_Y));
            //     Assert.AreEqual(simDataFixed.GetValues(pidModel1.GetID(), SignalType.PID_U), simDataVar.GetValues(pidModel1.GetID(), SignalType.PID_U));
        }








    }
}
