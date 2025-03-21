﻿using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;


namespace TimeSeriesAnalysis.Test.PlantSimulations
{
    /// <summary>
    /// Test of process simulations where each of or some of the models have multiple inputs
    /// </summary>
    [TestFixture]
    class LargerSystemSimulations
    {
        int timeBase_s = 1;
        int N = 480;

        UnitParameters modelParameters1;
        UnitParameters modelParameters2;
        UnitParameters modelParameters3;
        UnitParameters modelParameters4;
        UnitModel processModel1;
        UnitModel processModel2;
        UnitModel processModel3;
        UnitModel processModel4;

        PidParameters pidParameters1, pidParametersDelay;
        PidModel pidModel1, pidModel_delayed;
        Select minSelect1;
        Select maxSelect1;

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();

            modelParameters1 = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1, 0.5 },
                TimeDelay_s = 5,
                Bias = 5
            };
            modelParameters2 = new UnitParameters
            {
                TimeConstant_s = 20,
                LinearGains = new double[] { 1.1, 0.6 },
                TimeDelay_s = 10,
                Bias = 5
            };
            modelParameters3 = new UnitParameters
            {
                TimeConstant_s = 20,
                LinearGains = new double[] { 0.8, 0.7 },
                TimeDelay_s = 10,
                Bias = 5
            };
            modelParameters4 = new UnitParameters
            {
                TimeConstant_s = 20,
                LinearGains = new double[] { 0.8, 0.7, 2 },
                TimeDelay_s = 10,
                Bias = 5
            };


            processModel1 = new UnitModel(modelParameters1, "SubProcess1");
            processModel2 = new UnitModel(modelParameters2, "SubProcess2");
            processModel3 = new UnitModel(modelParameters3, "SubProcess3");
            processModel4 = new UnitModel(modelParameters4, "SubProcess4");
            pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            pidModel1 = new PidModel(pidParameters1, "PID1");
            pidParametersDelay = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20,
                DelayOutputOneSample = true
            };
            pidModel_delayed = new PidModel(pidParametersDelay, "PidDelay");

            minSelect1 = new Select(SelectType.MIN, "MINSELECT");
            maxSelect1 = new Select(SelectType.MAX, "MAXSELECT");
        }

        [Test]
        public void MinSelectWithPID_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, minSelect1, pidModel1 });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Step(N/4, N, 5.5, 6));
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND),
                TimeSeriesCreator.Step(N*3/4, N, 0, 1));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST),
                TimeSeriesCreator.Step(N*2/5, N, 0, 1));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND),
                TimeSeriesCreator.Step(N*4/5, N, 0, 1));
            inputData.CreateTimestamps(timeBase_s);

            plantSim.ConnectModels(processModel1, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel1, (int)INDEX.FIRST);

            plantSim.ConnectModels(processModel1, minSelect1, (int)INDEX.FIRST);
            plantSim.ConnectModels(processModel2, minSelect1, (int)INDEX.SECOND);

            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]>
                {
                    simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                    simData.GetValues(processModel2.GetID(),SignalType.Output_Y),
                    inputData.GetValues(pidModel1.GetID(),SignalType.Setpoint_Yset),
                    inputData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                    inputData.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.FIRST),
                    inputData.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                    simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                    simData.GetValues(minSelect1.GetID(),SignalType.SelectorOut),
                },
                    new List<string> { "y1=y1", "y1=y2", "y1=y1_set","y3=y1_u1","y3=y2_u1","y3=y1_u1",
                "y3=u_pid", "y1=y_select" }, timeBase_s, "MinSelectWithPID");
                Shared.DisablePlots();
            }



            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);
            double[] simY = simData.GetValues(minSelect1.GetID(), SignalType.SelectorOut);


            //SerializeHelper.Serialize("MinSelectWithPID", plantSim, inputData, simData);

            //Assert.IsTrue(Math.Abs(simY[0] - (6.5)) < 0.01);
            //Assert.IsTrue(Math.Abs(simY.Last() - (6.5)) < 0.01);
        }





        [Test]
        public void MaxSelect_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, maxSelect1 });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Constant(1, N));
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Constant(1, N));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Constant(1, N));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Constant(1, N));
            inputData.CreateTimestamps(timeBase_s);

            plantSim.ConnectModels(processModel1, maxSelect1, (int)INDEX.FIRST);
            plantSim.ConnectModels(processModel2, maxSelect1, (int)INDEX.SECOND);

            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);
            double[] simY = simData.GetValues(maxSelect1.GetID(), SignalType.SelectorOut);

            Assert.IsTrue(Math.Abs(simY[0] - (6.7)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - (6.7)) < 0.01);
        }







        public void Single_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1 });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(180, N, 50, 45));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y);

            Assert.IsTrue(Math.Abs(simY[0] - (1 * 50 + 0.5 * 50 + 5)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - (1 * 55 + 0.5 * 45 + 5)) < 0.01);

              //Plot.FromList(new List<double[]> {
              //    simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
              //    simData.GetValues(processModel1.GetID(),SignalType.External_U,0),
              //    simData.GetValues(processModel1.GetID(),SignalType.External_U,1)
              //},
              //    new List<string> { "y1=y_sim1", "y3=u1", "y3=u2" },
              //    timeBase_s, "UnitTest_SingleMISO");
          }

        /* [TestCase(true)]
        [TestCase(false)]
        public void PIDAndSingle_RunsAndConverges(bool doReverseInputConnections)
        {
            int pidIndex = 0;
            int externalUIndex = 1;
            if (doReverseInputConnections)
            {
                pidIndex = 1;
                externalUIndex = 0;
            }

            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });

            plantSim.ConnectModels(processModel1, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel1, pidIndex);

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, externalUIndex), TimeSeriesCreator.Step(60, N, 50, 45) );
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(60,N)) ;
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);

            SerializeHelper.Serialize("PidAndSingle", plantSim, inputData, simData);

            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,externalUIndex)
            },
                new List<string> { "y1=y_sim1", "y3=u1", "y3=u2" },
                timeBase_s, "UnitTest_PIDandSingle");

          //double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y);
         // SISOTests.CommonAsserts(inputData, simData, plantSim);
         // Assert.IsTrue(Math.Abs(simY1[0] - (60)) < 0.01);
        //  Assert.IsTrue(Math.Abs(simY1.Last() - (60)) < 0.1);
      } */

        [TestCase]
        public void Serial2_MISO_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
            new List<ISimulatableModel> { processModel1, processModel2 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(180, N, 50, 45));
            inputData.CreateTimestamps(timeBase_s);
            plantSim.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(240, N, 50, 40));
               
            var isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData,simData, plantSim);

            //Shared.EnablePlots();
            //Plot.FromList(new List<double[]> {
            //     simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
            //     simData.GetValues(processModel2.GetID(),SignalType.Output_Y),
            //     inputData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.FIRST),
            //     inputData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.SECOND),
            //     inputData.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.SECOND),
            // },
            //new List<string> { "y1=y_sim1", "y1=y_sim2", "y3=u1", "y3=u2", "y3=u4" },
            //timeBase_s, "Serial2");
            //Shared.DisablePlots();  

            double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y);
            Assert.IsTrue(Math.Abs(simY[0] - ((1*50+0.5*50+5)*1.1+50*0.6+5)) < 0.01,"unexpected starting value");
            Assert.IsTrue(Math.Abs(simY.Last() - ((1*55+0.5*45+5)*1.1+40*0.6+5)) < 0.01, "unexpected ending value");
        }

        // try placing models in different order, this should not affect the result at all
        [TestCase(0, Description = "this is the easiest,as this is the order that requires no sorting ")]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4, Description = "an adidtional processmodel3 outside the pid loop is connected to processmodel1")]
        public void PIDandSerial2_RunsAndConverges(int ver)
        {
            List<ISimulatableModel> modelList = new List<ISimulatableModel>();
            if (ver == 0)
            {
                modelList = new List<ISimulatableModel> { pidModel1, processModel1, processModel2 };
            }
            else if (ver == 1)
            {
                modelList = new List<ISimulatableModel> { pidModel1, processModel2, processModel1 };
            }
            else if (ver == 2)
            {
                modelList = new List<ISimulatableModel> { processModel2, processModel1, pidModel1 };
            }
            else if (ver == 3)
            {
                modelList = new List<ISimulatableModel> { processModel1, pidModel1, processModel2 };
            }
            else if (ver == 4)
            {
                modelList = new List<ISimulatableModel> { processModel1, pidModel1, processModel2, processModel3 };
            }


            int pidIndex = 1;
            int externalUIndex = 0;
            var plantSim = new PlantSimulator(modelList);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(150, N));
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, externalUIndex), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(240, N, 50, 40));
            inputData.CreateTimestamps(timeBase_s);
            plantSim.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            plantSim.ConnectModels(processModel2, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel1, pidIndex);
            if (ver == 4)
            {
                inputData.Add(plantSim.AddExternalSignal(processModel3, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Constant(0, N));
                plantSim.ConnectModels(processModel2, processModel3, (int)INDEX.FIRST);
            }

            var isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);

            if (ver == 4)
            {
               SerializeHelper.Serialize("PidAndSerial2", plantSim, inputData, simData);
            }

            Assert.IsTrue(isOk,"simulation returned false, it failed");
            /*
                       Plot.FromList(new List<double[]> {
                           simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                           simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
                           simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                           simData.GetValues(processModel1.GetID(),SignalType.External_U,externalUIndex),
                           simData.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.SECOND)
                       },
                           new List<string> { "y1=y_sim1","y1=y_sim2", "y3=u1(pid)", "y3=u2", "y3=u3" },
                           timeBase_s, "UnitTest_PIDandSerial2");*/

            PsTest.CommonAsserts(inputData, simData, plantSim);
            double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y);
            Assert.IsTrue(Math.Abs(simY[0] - 150) < 0.01, "unexpected starting value");
            Assert.IsTrue(Math.Abs(simY.Last() - 150) < 0.1, "unexpected ending value");

            if (ver == 4)
            {
                double[] simY3 = simData.GetValues(processModel3.GetID(), SignalType.Output_Y);
                //  Plot.FromList(new List<double[]> { simData.GetValues(processModel3.GetID(), SignalType.Output_Y_sim) },
                //     new List<string> { "y1=y3" }   timeBase_s, "UnitTest_PIDandSerial2"););

                // Assert.IsTrue(Math.Abs(simY3[0] - 150) < 0.01, "unexpected starting value");
                // Assert.IsTrue(Math.Abs(simY3.Last() - 150) < 0.1, "unexpected ending value");
            } 

        }

        // in this case there is a "computational loop":
        //  - output of first model is input to second model 
        //  - output of second model is input to first model
        [Test]
        public void ComputationalLoop_TwoModels_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(240, N, 50, 40));
            plantSim.ConnectModels(processModel1, processModel2, (int)INDEX.SECOND);
            plantSim.ConnectModels(processModel2, processModel1, (int)INDEX.SECOND);
               inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);

         /*   Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y),
                 inputData.GetValues(processModel1.GetID(),SignalType.External_U),
                 inputData.GetValues(processModel2.GetID(), SignalType.External_U)},
            new List<string> { "y1=y_sim1", "y1=y_sim2", "y3=u1", "y3=u2" },
            timeBase_s, "UnitTest_CompLoop_TwoModels");
            Shared.DisablePlots();*/
        }



        [Test]

        public void ComputationalLoop_TwoModelsLoop_One_Upstream_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, processModel3 });

            var inputData = new TimeSeriesDataSet();

            //inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(240, N, 50, 40));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(120, N, 50, 65));
            inputData.Add(plantSim.AddExternalSignal(processModel3, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(160, N, 54, 35));
            inputData.Add(plantSim.AddExternalSignal(processModel3, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(30, N, 23, 57));

            plantSim.ConnectModels(processModel1, processModel2, (int)INDEX.SECOND);
            plantSim.ConnectModels(processModel2, processModel1, (int)INDEX.SECOND);
            plantSim.ConnectModels(processModel3, processModel1, (int)INDEX.FIRST);

            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);
            /*
            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                simData.GetValues(processModel2.GetID(),SignalType.Output_Y),
                inputData.GetValues(processModel1.GetID(),SignalType.External_U),
                inputData.GetValues(processModel2.GetID(), SignalType.External_U)},
            new List<string> { "y1=y_sim1", "y1=y_sim2", "y3=u1", "y3=u2" },
            timeBase_s, "UnitTest_CompLoop_TwoModels");
            Shared.DisablePlots();
            */
        }

        // one model is downstream of the loop and one upstream. 
        [Test]

        public void ComputationalLoop_TwoModelsLoop_OneUpstream_OneDownstream_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, processModel3,processModel4 });

            var inputData = new TimeSeriesDataSet();

            //inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(240, N, 50, 40));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(120, N, 50, 65));
            inputData.Add(plantSim.AddExternalSignal(processModel3, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(160, N, 54, 35));
            inputData.Add(plantSim.AddExternalSignal(processModel3, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(30, N, 23, 57));
            inputData.Add(plantSim.AddExternalSignal(processModel4, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(300, N, 66, 57));
            inputData.Add(plantSim.AddExternalSignal(processModel4, SignalType.External_U, (int)INDEX.THIRD), TimeSeriesCreator.Step(350, N, 55, 50));

            plantSim.ConnectModels(processModel1, processModel2, (int)INDEX.SECOND);
            plantSim.ConnectModels(processModel2, processModel1, (int)INDEX.SECOND);
            plantSim.ConnectModels(processModel3, processModel1, (int)INDEX.FIRST);// process model 3 is upstream
            plantSim.ConnectModels(processModel1, processModel4, (int)INDEX.FIRST);// process model 4 is upstream

            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);

            /*
            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel4.GetID(),SignalType.Output_Y)
             },
            new List<string> { "y1=y_sim4" },
            timeBase_s, "UnitTest_CompLoop_FourModels");
            Shared.DisablePlots();
            */
        }






        // this is a loop that consist of more than two subprocesses.
        // subprocess1 and subpprocess 2 both connect to subprocess3, and subprocess3 connects back to both.

        [Test]
        public void ComputationalLoop_ThreeModelsLoop_RunsAndConverges()
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, processModel4 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(240, N, 50, 40));
            inputData.Add(plantSim.AddExternalSignal(processModel4, SignalType.External_U, (int)INDEX.THIRD), TimeSeriesCreator.Step(125, N, 45, 35));

            plantSim.ConnectModels(processModel1, processModel4, (int)INDEX.FIRST);
            plantSim.ConnectModels(processModel2, processModel4, (int)INDEX.SECOND);
            plantSim.ConnectModels(processModel4, processModel1, (int)INDEX.SECOND);
            plantSim.ConnectModels(processModel4, processModel2, (int)INDEX.SECOND);

            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);
                        
     /*       Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                simData.GetValues(processModel2.GetID(),SignalType.Output_Y),
                simData.GetValues(processModel4.GetID(),SignalType.Output_Y),
                inputData.GetValues(processModel1.GetID(),SignalType.External_U),
                inputData.GetValues(processModel2.GetID(), SignalType.External_U), 
                inputData.GetValues(processModel4.GetID(), SignalType.External_U)},
            new List<string> { "y1=y_sim1", "y1=y_sim2", "y1=y_sim4", "y3=u1", "y3=u2", "y3=u4" },
            timeBase_s, "UnitTest_CompLoop_ThreeModels");
            Shared.DisablePlots();*/
        }






        [TestCase(0,Description ="this is the easiest, as it requires no res-")]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(2)]


        public void Serial3_MISO_RunsAndConverges(int ver)
        {
            List<ISimulatableModel> modelList = new List<ISimulatableModel>();
            if (ver == 0)
            {
                modelList = new List<ISimulatableModel> { processModel1, processModel2, processModel3 };
            }
            else if (ver == 1)
            {
                modelList = new List<ISimulatableModel> { processModel1, processModel3, processModel2 };
            }
            else if (ver == 2)
            {
                modelList = new List<ISimulatableModel> { processModel3, processModel2, processModel1 };
            }
            else if (ver == 3)
            {
                modelList = new List<ISimulatableModel> { processModel2, processModel3, processModel1 };
            }


            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, processModel3 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(180, N, 50, 45));

            plantSim.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(240, N, 50, 40));

            plantSim.ConnectModels(processModel2, processModel3, (int)INDEX.FIRST);
            inputData.Add(plantSim.AddExternalSignal(processModel3, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(300, N, 30, 40));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData,out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);

            double[] simY = simData.GetValues(processModel3.GetID(), SignalType.Output_Y);
            double expStartVal  = ((1 * 50 + 0.5 * 50 + 5) * 1.1 + 50 * 0.6 + 5)*0.8 + 0.7*30 + 5;
            double expEndVal    = ((1 * 55 + 0.5 * 45 + 5) * 1.1 + 40 * 0.6 + 5)*0.8 + 0.7*40 + 5;

            Assert.IsTrue(Math.Abs(simY[0] - expStartVal) < 0.01, "unexpected starting value");
            Assert.IsTrue(Math.Abs(simY.Last() - expEndVal) < 0.01, "unexpected ending value");
            /*
            
            Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel3.GetID(),SignalType.Output_Y_sim),
                 inputData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.FIRST),
                 inputData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                 inputData.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                 inputData.GetValues(processModel3.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                },
                new List<string> { "y1=y_sim1", "y1=y_sim2","y1=y_sim3", "u1", "u2", "u4","u6" },
                timeBase_s, "UnitTest_MISO3Serial");*/

        }



        [Test]
        public void Divide_RunsAndConverges()
        {
            DivideParameters divParams = new DivideParameters();

            Divide divideModel = new Divide(divParams, "divider");

            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, divideModel });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(180, N, 50, 45));

            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(90, N, 30, 45));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(12, N, 60, 45));

            plantSim.ConnectModels(processModel1, divideModel, (int)INDEX.FIRST);
            plantSim.ConnectModels(processModel2, divideModel, (int)INDEX.SECOND);

            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);

            SerializeHelper.Serialize("Divide", plantSim, inputData, simData);

        }


            [Test]
        public void TwoProcessesToOne_RunsAndConverges()
        {

            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, processModel3 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(plantSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(180, N, 50, 45));

            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(90, N, 30, 45));
            inputData.Add(plantSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(12, N, 60, 45));

            plantSim.ConnectModels(processModel1, processModel3, (int)INDEX.FIRST);
            plantSim.ConnectModels(processModel2, processModel3, (int)INDEX.SECOND);
  
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            PsTest.CommonAsserts(inputData, simData, plantSim);

            SerializeHelper.Serialize("TwoProcessesToOne",plantSim,inputData,simData);


        //    Assert.IsTrue(Math.Abs(simY[0] - expStartVal) < 0.01, "unexpected starting value");
        //   Assert.IsTrue(Math.Abs(simY.Last() - expEndVal) < 0.01, "unexpected ending value");
            /*
            
            Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel3.GetID(),SignalType.Output_Y_sim),
                 inputData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.FIRST),
                 inputData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                 inputData.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                 inputData.GetValues(processModel3.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                },
                new List<string> { "y1=y_sim1", "y1=y_sim2","y1=y_sim3", "u1", "u2", "u4","u6" },
                timeBase_s, "UnitTest_MISO3Serial");*/

        }












    }
}
