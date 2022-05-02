using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

using NUnit.Framework;

using TimeSeriesAnalysis._Examples;


namespace TimeSeriesAnalysis.Test.PlantSimulations
{







    /// <summary>
    /// Tests that run the Processcontrol examples but with plotting disabled.
    /// <para>
    /// This way we both have examples that are free of an unit testing logic, but a
    /// also ensure that we have automatic verification that the example cases remain in working order,
    /// without having to duplicate the example code in separate unit tests.
    /// </para>
    /// </summary>
    [TestFixture]
    class Control
    {
        [Test]
        public void  CascadeControl()
        {
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.CascadeControl();

            Shared.EnablePlots();
        }

        [Test]
        public void FeedForwardControl_Part1()
        {
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.FeedForward_Part1();

            Shared.EnablePlots();
        }


        [Test]
        public void FeedForwardControl_Part2()
        {
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.FeedForward_Part2();

            Shared.EnablePlots();
        }

        [Test]
        public void GainScheduling()
        {
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.GainScheduling();

            Shared.EnablePlots();
        }
        [Test]
        public void MinSelect()
        {
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.MinSelect();
         //   dataSet.SetT0(new DateTime(2021,1,1));
        //    var isOk = dataSet.ToCSV(@"C:\Appl\source\TimeSeriesAnalysis\minSelect_large.csv");
      //      Assert.IsTrue(isOk);
        //    var dataSet2 = new TimeSeriesDataSet(@"C:\Appl\source\TimeSeriesAnalysis\minSelect.csv");
            Shared.EnablePlots();
        }
    }




    /// <summary>
    /// Test of process simulations where each of or some of the models have multiple inputs
    /// </summary>
    [TestFixture]
    class MISOTests
    {
        int timeBase_s = 1;
        int N = 480;

        UnitParameters modelParameters1;
        UnitParameters modelParameters2;
        UnitParameters modelParameters3 ;
        UnitModel processModel1;
        UnitModel processModel2;
        UnitModel processModel3;
        PidParameters pidParameters1;
        PidModel pidModel1;
        Select minSelect1;
        Select maxSelect1;

        bool writeTestDataToDisk = true;

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();

            modelParameters1 = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1,0.5 },
                TimeDelay_s = 5,
                Bias = 5
            };
            modelParameters2 = new UnitParameters
            {
                TimeConstant_s = 20,
                LinearGains = new double[] { 1.1,0.6 },
                TimeDelay_s = 10,
                Bias = 5
            };
            modelParameters3 = new UnitParameters
            {
                TimeConstant_s = 20,
                LinearGains = new double[] { 0.8,0.7 },
                TimeDelay_s = 10,
                Bias = 5
            };

            processModel1 = new UnitModel(modelParameters1,  "SubProcess1");
            processModel2 = new UnitModel(modelParameters2,  "SubProcess2");
            processModel3 = new UnitModel(modelParameters3,  "SubProcess3");

            pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            pidModel1 = new PidModel(pidParameters1,  "PID1");

            minSelect1 = new Select(SelectType.MIN,"MINSELECT");
            maxSelect1 = new Select(SelectType.MAX, "MAXSELECT");

        }
        
        [Test,Explicit(reason:"fails_wip")]
        public void DeserializedPlantSimulatorAndTimeSeriesDataObjects_AreAbleToSimulate()
        {
            // 1. create a plantsimulator in 
            // based on  PIDandSerial2_RunsAndConverges ver 4
            List<ISimulatableModel> modelList = new List<ISimulatableModel>();
            modelList = new List<ISimulatableModel> { processModel1, pidModel1, processModel2, processModel3 };

            int pidIndex = 1;
            int externalUIndex = 0;
            var plantSim1 = new PlantSimulator(modelList);

            plantSim1.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            plantSim1.ConnectModels(processModel2, pidModel1);
            plantSim1.ConnectModels(pidModel1, processModel1, pidIndex);
            plantSim1.ConnectModels(processModel1, processModel3, (int)INDEX.FIRST);

            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim1.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(150, N));
            inputData.Add(plantSim1.AddExternalSignal(processModel1, SignalType.External_U, externalUIndex), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(plantSim1.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(240, N, 50, 40));
            inputData.Add(plantSim1.AddExternalSignal(processModel3, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Constant(0, N));
            inputData.CreateTimestamps(timeBase_s);
            // 2. serialize to text
            var plantsimJsonTxt = plantSim1.SerializeTxt();
            var inputDataJsonTxt = new CsvContent(inputData.ToCsvText());

            var inputData2 = new TimeSeriesDataSet(inputDataJsonTxt);

            // 3. deserialize to a new object
            var plantSim2 = PlantSimulatorHelper.LoadFromJson(plantsimJsonTxt);

            // 4. simulate the plant object created from Json
            var isOk = plantSim2.Simulate(inputData2, out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);

/*           
           Plot.FromList(new List<double[]> {
               simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
               simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
               simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
               inputData2.GetValues(processModel1.GetID(),SignalType.External_U,externalUIndex),
               inputData2.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.SECOND)
           },
               new List<string> { "y1=y_sim1","y1=y_sim2", "y3=u1(pid)", "y3=u2", "y3=u3" },
               timeBase_s, "UnitTest_PIDandSerial2");*/
            
        }





        [TestCase]
        public void MISO_Single_RunsAndConverges()
        {
            var processSim = new PlantSimulator(new List<ISimulatableModel> { processModel1 });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(180, N, 50, 45));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(inputData,simData);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim);

            Assert.IsTrue(Math.Abs(simY[0] -(1*50 + 0.5*50 +5) ) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() -(1*55 + 0.5*45 +5) ) < 0.01);

            /*Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,0),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,1)
            },
                new List<string> { "y1=y_sim1", "y3=u1","y3=u2" },
                timeBase_s, "UnitTest_SingleMISO");*/
        }

        [Test]
        public void MinSelect_RunsAndConverges()
        {
            var processSim = new Dynamic.PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, minSelect1 });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST),TimeSeriesCreator.Constant(1, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Constant(1, N));
            inputData.Add(processSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Constant(1, N));
            inputData.Add(processSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Constant(1, N));
            inputData.CreateTimestamps(timeBase_s);
            processSim.ConnectModels(processModel1, minSelect1, (int)INDEX.FIRST);
            processSim.ConnectModels(processModel2, minSelect1, (int)INDEX.SECOND);

            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(inputData,simData);
            double[] simY = simData.GetValues(minSelect1.GetID(), SignalType.SelectorOut);
            /*
            if (writeTestDataToDisk)
            {
                processSim.Serialize("MISO_MinSelect");
                var combinedData = inputData.Combine(simData);
                combinedData.ToCsv("MISO_MinSelect");
            }*/
            Assert.IsTrue(Math.Abs(simY[0] - (6.5)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - (6.5)) < 0.01);
        }

        [Test]
        public void MinSelectWithPID_RunsAndConverges()
        {
            var processSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, minSelect1 });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(N/4, N,0,1));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(N*3/4, N,0,1));
            inputData.Add(processSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(N*2/5, N,0,1));
            inputData.Add(processSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(N*4/5, N,0,1));
            inputData.CreateTimestamps(timeBase_s);
            processSim.ConnectModels(processModel1, minSelect1, (int)INDEX.FIRST);
            processSim.ConnectModels(processModel2, minSelect1, (int)INDEX.SECOND);

            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(inputData, simData);
            double[] simY = simData.GetValues(minSelect1.GetID(), SignalType.SelectorOut);
            
            if (writeTestDataToDisk)
            {
                processSim.Serialize("MISO_MinSelectWithPID");
                var combinedData = inputData.Combine(simData);
                combinedData.ToCsv("MISO_MinSelectWithPID");
            }
            //Assert.IsTrue(Math.Abs(simY[0] - (6.5)) < 0.01);
            //Assert.IsTrue(Math.Abs(simY.Last() - (6.5)) < 0.01);
        }





        [Test]
        public void MaxSelect_RunsAndConverges()
        {
            var processSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1,processModel2, maxSelect1 });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST),TimeSeriesCreator.Constant(1, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Constant(1, N));
            inputData.Add(processSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Constant(1, N));
            inputData.Add(processSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Constant(1, N));
            inputData.CreateTimestamps(timeBase_s);

            processSim.ConnectModels(processModel1, maxSelect1, (int)INDEX.FIRST);
            processSim.ConnectModels(processModel2, maxSelect1, (int)INDEX.SECOND);

            if (writeTestDataToDisk)
            {
                processSim.Serialize("MISO_MaxSelect");
                inputData.ToCsv("MISO_MaxSelect");
            }

            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(inputData, simData);
            double[] simY = simData.GetValues(maxSelect1.GetID(), SignalType.SelectorOut);

            Assert.IsTrue(Math.Abs(simY[0] - (6.7)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - (6.7)) < 0.01);
        }




        public void Single_RunsAndConverges()
        {
            var processSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1 });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(180, N, 50, 45));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(inputData, simData);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim);

            Assert.IsTrue(Math.Abs(simY[0] - (1 * 50 + 0.5 * 50 + 5)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - (1 * 55 + 0.5 * 45 + 5)) < 0.01);

          /*  Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,0),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,1)
            },
                new List<string> { "y1=y_sim1", "y3=u1", "y3=u2" },
                timeBase_s, "UnitTest_SingleMISO");*/
        }

        [TestCase(true)]
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

            var processSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel1 });

            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1, pidIndex);

            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, externalUIndex), TimeSeriesCreator.Step(60, N, 50, 45) );
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(60,N)) ;
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);

            processSim.Serialize("PIDandSingle");
         //   inputData.ToCsv("PIDandSingle.csv");

           /* Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,externalUIndex)
            },
                new List<string> { "y1=y_sim1", "y3=u1", "y3=u2" },
                timeBase_s, "UnitTest_PIDandSingle");*/

            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim);
            SISOTests.CommonAsserts(inputData, simData);
            Assert.IsTrue(Math.Abs(simY[0] - (60)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - (60)) < 0.1);
        }

        public void Serial2_RunsAndConverges()
        {
            var processSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(180, N, 50, 45));
            inputData.CreateTimestamps(timeBase_s);
            processSim.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            inputData.Add(processSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(240, N, 50, 40));
               
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(inputData,simData);

            /*
            Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.FIRST),
                 simData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                 simData.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.SECOND),
             },
            new List<string> { "y1=y_sim1", "y1=y_sim2", "u1", "u2", "u4" },
            timeBase_s, "UnitTest_MISO2Serial");
            */

            double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y_sim);
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
            var processSim = new PlantSimulator(modelList);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(150, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, externalUIndex), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(processSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(240, N, 50, 40));
            inputData.CreateTimestamps(timeBase_s);
            processSim.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            processSim.ConnectModels(processModel2, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1, pidIndex);
            if (ver == 4)
            {
                inputData.Add(processSim.AddExternalSignal(processModel3, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Constant(0, N));
                processSim.ConnectModels(processModel1, processModel3, (int)INDEX.FIRST);
            }

            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);

            if (ver == 4)
            {
                if (writeTestDataToDisk)
                {
                    processSim.Serialize("MISO_PIDandSerial2");
                    inputData.ToCsv("MISO_PIDandSerial2");
                }
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
            
            SISOTests.CommonAsserts(inputData, simData);
            double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y_sim);
            Assert.IsTrue(Math.Abs(simY[0] - 150) < 0.01, "unexpected starting value");
            Assert.IsTrue(Math.Abs(simY.Last() - 150) < 0.1, "unexpected ending value");

            if (ver == 4)
            {
                double[] simY3 = simData.GetValues(processModel3.GetID(), SignalType.Output_Y_sim);
                //  Plot.FromList(new List<double[]> { simData.GetValues(processModel3.GetID(), SignalType.Output_Y_sim) },
                //     new List<string> { "y1=y3" }   timeBase_s, "UnitTest_PIDandSerial2"););

                // Assert.IsTrue(Math.Abs(simY3[0] - 150) < 0.01, "unexpected starting value");
                // Assert.IsTrue(Math.Abs(simY3.Last() - 150) < 0.1, "unexpected ending value");
            } 

        }




        [TestCase(0,Description ="this is the easiest, as it requires no res-")]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(2)]


        public void Serial3_RunsAndConverges(int ver)
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


            var processSim = new PlantSimulator(
                new List<ISimulatableModel> { processModel1, processModel2, processModel3 });

            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.FIRST), TimeSeriesCreator.Step(60, N, 50, 55));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(180, N, 50, 45));

            processSim.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            inputData.Add(processSim.AddExternalSignal(processModel2, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(240, N, 50, 40));

            processSim.ConnectModels(processModel2, processModel3, (int)INDEX.FIRST);
            inputData.Add(processSim.AddExternalSignal(processModel3, SignalType.External_U, (int)INDEX.SECOND), TimeSeriesCreator.Step(300, N, 30, 40));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);

            if (ver == 2)
            {
                if (writeTestDataToDisk)
                {
                    processSim.Serialize("MISO_Serial3");
                    inputData.ToCsv("MISO_Serial3");
                }
            }
            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(inputData, simData);

            double[] simY = simData.GetValues(processModel3.GetID(), SignalType.Output_Y_sim);
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








    }
}
