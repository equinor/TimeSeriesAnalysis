using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Tests.Serialization
{
    internal class PlantSimulatorSerialization
    {
        int timeBase_s = 1;
        int N = 480;

        UnitParameters modelParameters1;
        UnitParameters modelParameters2;
        UnitParameters modelParameters3;
        UnitModel processModel1;
        UnitModel processModel2;
        UnitModel processModel3;
        PidParameters pidParameters1;
        PidModel pidModel1;
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

            processModel1 = new UnitModel(modelParameters1, "SubProcess1");
            processModel2 = new UnitModel(modelParameters2, "SubProcess2");
            processModel3 = new UnitModel(modelParameters3, "SubProcess3");

            pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            pidModel1 = new PidModel(pidParameters1, "PID1");

            minSelect1 = new Select(SelectType.MIN, "MINSELECT");
            maxSelect1 = new Select(SelectType.MAX, "MAXSELECT");

        }


        [Test]
        public void DeserializedPlantSimulatorAndTimeSeriesDataObjects_AreAbleToSimulate()
        {
            // 1. create a plantsimulator in 
            // based on  PIDandSerial2_RunsAndConverges ver 4
            List<ISimulatableModel> modelList = new List<ISimulatableModel>();
            modelList = new List<ISimulatableModel> { processModel1, pidModel1, processModel2, processModel3 };
            int pidIndex = 1;
            int externalUIndex = 0;
            var plantSim1 = new PlantSimulator(modelList, "DeserializedTest", "a test");

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

            var inputData2 = new TimeSeriesDataSet();
            inputData2.LoadFromCsv(inputDataJsonTxt);

            // 3. deserialize to a new object
            var plantSim2 = PlantSimulatorSerializer.LoadFromJsonTxt(plantsimJsonTxt);

            // 4. simulate the plant object created from Json
            var isOk = plantSim2.Simulate(inputData2, out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            Assert.IsTrue(plantSim2.plantName == "DeserializedTest");
            Assert.IsTrue(plantSim2.plantDescription == "a test");


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
        public void DeserializeFittedModel_ObjIncludesFittingObj()
        {
            var inputData = new TimeSeriesDataSet();
            TimeSeriesDataSet simData;
            {
                var modelList = new List<ISimulatableModel> { processModel1, pidModel1 };
                int pidIndex = 1;
                int externalUIndex = 0;
                var plantSim1 = new PlantSimulator(modelList, "DeserializedTest", "a test");

                plantSim1.ConnectModels(pidModel1, processModel1, pidIndex);
                plantSim1.ConnectModels(processModel1, pidModel1, (int)INDEX.FIRST);

                
                inputData.Add(plantSim1.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(150, N));
                inputData.Add(plantSim1.AddExternalSignal(processModel1, SignalType.External_U, externalUIndex), TimeSeriesCreator.Step(N / 2, N, 50, 55));
                inputData.CreateTimestamps(timeBase_s);

                var isOk = plantSim1.Simulate(inputData, out simData);
            }
            var fitData = new UnitDataSet();
            
            fitData.U = Array2D<double>.CreateFromList(new List<double[]> { 
                simData.GetValues(pidModel1.GetID(), SignalType.PID_U), 
                inputData.GetValues(processModel1.GetID(), SignalType.External_U)
            })  ;
            fitData.Y_meas = simData.GetValues(processModel1.GetID(), SignalType.Output_Y);
            fitData.Times= inputData.GetTimeStamps();

            var identModel = UnitIdentifier.Identify(ref fitData);

           Console.WriteLine(identModel.ToString());

            /*Plot.FromList(new List<double[]> {
                           fitData.Y_meas,
                           fitData.Y_sim,
                           fitData.U.GetColumn(0),
                           fitData.U.GetColumn(1),
                       },
                new List<string> { "y1=y_meas", "y1=y_mod", "y3=u1" },
                    timeBase_s, "UnitTest_DeserializeFittingObj"); */

            var modelList_ident = new List<ISimulatableModel>();
            modelList_ident = new List<ISimulatableModel> { identModel, pidModel1 };
            var plantSim_ident = new PlantSimulator(modelList_ident, "DeserializedTest_ident", "a test");

            var plantsimJsonTxt = plantSim_ident.SerializeTxt();
            var plantSim_ident_deserialized = PlantSimulatorSerializer.LoadFromJsonTxt(plantsimJsonTxt);

            var model= (UnitModel)plantSim_ident.modelDict.First().Value;
            var model_deseralized = (UnitModel)plantSim_ident_deserialized.modelDict.First().Value;

            Assert.IsTrue(model.modelParameters.Fitting != null);
            Assert.IsTrue(model_deseralized.modelParameters.Fitting!= null);


        }
    }
}
