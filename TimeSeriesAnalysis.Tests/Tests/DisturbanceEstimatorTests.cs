using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceID
{
    /// <summary>
    /// In these tests, the UnitModel is given, and the aim is to verify that for a known model the distubance estimator is able
    /// to arrive at the correct disturbance time-series.
    /// </summary>
    [TestFixture]
    class DisturbanceEstimatorTests
    {
        UnitParameters staticModelParameters = new UnitParameters
        {
            TimeConstant_s = 0,
            LinearGains = new double[] { 1.5 },
            TimeDelay_s = 0,
            Bias = 5
        };

        UnitParameters dynamicModelParameters = new UnitParameters
        {
            TimeConstant_s = 10,
            LinearGains = new double[] { 1.5 },
            TimeDelay_s = 5,
            Bias = 5
        };

        PidParameters pidParameters1 = new PidParameters()
        {
            Kp = 0.2,
            Ti_s = 20
        };

        int timeBase_s = 1;
        int N = 300;// TODO:influences the results!
        DateTime t0 = new DateTime(2010,1,1);


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }

        public void CommonPlotAndAsserts(UnitDataSet pidDataSet, double[] d_est, double[] trueDisturbance)
        {
            Vec vec = new Vec();
            double distTrueAmplitude = vec.Max(vec.Abs(trueDisturbance));

            Assert.IsTrue(d_est != null);
            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                Replace(")", "_").Replace(",", "_") + "y";

            Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
            pidDataSet.U.GetColumn(0),
            d_est, trueDisturbance },
                new List<string> { "y1=y meas", "y1=y set", "y2=u(right)", "y3=est disturbance", "y3=true disturbance" },
                pidDataSet.GetTimeBase(), caseId);

            Assert.IsTrue(vec.Mean(vec.Abs(vec.Subtract(trueDisturbance, d_est))) < distTrueAmplitude / 10,"true disturbance and actual disturbance too far apart");
        }
 
        [TestCase(-5)]
        [TestCase(5)]
        public void Static_StepDisturbance_EstimatesOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }

        [TestCase(-5)]
        [TestCase(5)]
        public void Static_SinusDisturbance_EstimatesOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Sinus(stepAmplitude, timeBase_s*15, timeBase_s,N );
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance);
        }


        [TestCase(-5)]
        [TestCase(5)]
        public void Dynamic_StepDisturbance_EstimatesOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(dynamicModelParameters, "DynamicProcess"), trueDisturbance);
        }
        [TestCase(-4)]
        [TestCase(4)]
        public void PlantSimulatorSingle_StepDisturbance_EstimatesOk(double stepAmplitude)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            DisturbanceTestUsingPlantSimulateSingle(new UnitModel(dynamicModelParameters, "PlantSim_d"), trueDisturbance);
        }

        // an extension of the above test to use the more general PlantSimulator.Simulate, rather than the PlantSimulator.SimulateSingle
        [TestCase(4)]
        public void PlantSimulator_StepDisturbance_EstimatesOk(double stepAmplitude)
        {
       //     Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            DisturbanceTestUsingPlantSimulator(new UnitModel(dynamicModelParameters, "PlantSim_d"), trueDisturbance);
         //   Shared.DisablePlots();
        }

        public void GenericDisturbanceTest  (UnitModel processModel, double[] trueDisturbance, 
            bool doAssertResult=true)
        {
            // create synthetic dataset
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var plantSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel });
            plantSim.ConnectModels(processModel, pidModel1);
            plantSim.ConnectModels(pidModel1, processModel);
            var inputData = new TimeSeriesDataSet();
           
            inputData.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(plantSim.AddExternalSignal(processModel, SignalType.Disturbance_D), trueDisturbance);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            var pidDataSet = plantSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var result = DisturbanceIdentifier.EstDisturbanceBasedOnProcessModel(pidDataSet, processModel);
            if (doAssertResult)
            {
                CommonPlotAndAsserts(pidDataSet, result.d_est, trueDisturbance);
            }
        }


        public void DisturbanceTestUsingPlantSimulator(UnitModel processModel, double[] trueDisturbance,
            bool doAssertResult = true)
        {
            TimeSeriesDataSet referenceSimDataSet;
            TimeSeriesDataSet referenceInputDataSet;
            // 1 .create synthetic dataset - where a "true" known disturbance is specified
            {
                var pidModel1 = new PidModel(pidParameters1, "PID1");
                var processModel2 = new UnitModel(dynamicModelParameters, "Proc1");
                var plantSim = new PlantSimulator(
                    new List<ISimulatableModel> { pidModel1, processModel2 });
                plantSim.ConnectModels(processModel2, pidModel1);
                plantSim.ConnectModels(pidModel1, processModel2);
                var refYsetSignal = "yset";
                plantSim.AddAndConnectExternalSignal(pidModel1, refYsetSignal, SignalType.Setpoint_Yset);
                var refYmeasSignal = "ymeas";
                plantSim.AddAndConnectExternalSignal(pidModel1, refYmeasSignal, SignalType.Output_Y);
                var refDistSignal = "dist";
                plantSim.AddAndConnectExternalSignal(processModel2, refDistSignal, SignalType.Disturbance_D);

                referenceInputDataSet = new TimeSeriesDataSet();
                referenceInputDataSet.Add(refYsetSignal, TimeSeriesCreator.Constant(50, N));
                referenceInputDataSet.Add(refDistSignal, trueDisturbance);
                referenceInputDataSet.CreateTimestamps(timeBase_s);
  
                var simOk = plantSim.Simulate(referenceInputDataSet, out referenceSimDataSet);
                Assert.IsTrue(simOk,"simulating reference case failed!");
            }
            // 2.create plant model without disturbance, and try to to find the disturbance signal
            {
                var pidModel1 = new PidModel(pidParameters1, "PID1");
              
                // important to add a disturbance signal with naming convention to the output of the process, to signal that the disturbance is to be estimated!
                   var processModel3 = new UnitModel(dynamicModelParameters, "Proc1");
                var distSignal = SignalNamer.EstDisturbance(processModel3);
                processModel3.AddSignalToOutput(distSignal);
                var plantSim = new PlantSimulator(
                    new List<ISimulatableModel> { pidModel1, processModel3 });
                plantSim.ConnectModels(processModel3, pidModel1);
                plantSim.ConnectModels(pidModel1, processModel3);

                // signals can really be named anything, but important for this to work that the names are the same
                // in the model objects and in the inputData object
                var ysetSignal = SignalNamer.GetSignalName(pidModel1.GetID(), SignalType.Setpoint_Yset); 
                plantSim.AddAndConnectExternalSignal(pidModel1, ysetSignal, SignalType.Setpoint_Yset);
                var ymeasSignal = processModel3.GetOutputID() ;
                plantSim.AddAndConnectExternalSignal(processModel3, ymeasSignal, SignalType.Output_Y);
                var uMeasSignal = SignalNamer.GetSignalName(pidModel1.GetID(), SignalType.PID_U); ;
                plantSim.AddAndConnectExternalSignal(pidModel1, uMeasSignal, SignalType.PID_U);
                // nb! do not specify the disturbance in this case, instead add the "output_Y" from the abo

                /////////////////
                ///
                ///adding u and y to inputdata, should enable the plant simualtor to back-calculate the disturbance.
                /// 
                var inputData = new TimeSeriesDataSet();
                inputData.Add(ysetSignal, referenceInputDataSet.GetValues("yset"));
                inputData.Add(uMeasSignal, referenceSimDataSet.GetValues("PID1", SignalType.PID_U));
                inputData.Add(ymeasSignal, referenceSimDataSet.GetValues("Proc1", SignalType.Output_Y));
                /////////////////
                Assert.IsTrue(inputData.GetSignalNames().Count() == 3 );//sanity check for configuration errors 
                inputData.CreateTimestamps(timeBase_s);

                //////////////////////////////////
                // TODO: fails below
                // the simulate function needs to mimick the PlantSimulator.SimulateSingle function and 
                // itself estimate the disturbance signal based on the external signals "umeas" and "ymeas"

                var isOK = plantSim.Simulate(inputData, out TimeSeriesDataSet simDataSetWithDisturbance);
                //////////////////////////////////


                Assert.IsTrue(isOK);
                Assert.IsTrue(simDataSetWithDisturbance.ContainsSignal(distSignal));
                if (doAssertResult)
                {
                    var pidDataSet = plantSim.GetUnitDataSetForPID(inputData.Combine(simDataSetWithDisturbance), pidModel1);
                    CommonPlotAndAsserts(pidDataSet, simDataSetWithDisturbance.GetValues(distSignal),
                        trueDisturbance);
                }
            }
        }

        public void DisturbanceTestUsingPlantSimulateSingle(UnitModel processModel, double[] trueDisturbance,
            bool doAssertResult = true)
        {
            TimeSeriesDataSet referenceSimDataSet;
            TimeSeriesDataSet referenceInputDataSet;
            // 1 .create synthetic dataset - where disturbance is specified
            {  
                var pidModel1 = new PidModel(pidParameters1, "PID1");
                var plantSim = new PlantSimulator(
                    new List<ISimulatableModel> { pidModel1, processModel });
                plantSim.ConnectModels(processModel, pidModel1);
                plantSim.ConnectModels(pidModel1, processModel);
                referenceInputDataSet = new TimeSeriesDataSet();
                referenceInputDataSet.Add(plantSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
                referenceInputDataSet.Add(plantSim.AddExternalSignal(processModel, SignalType.Disturbance_D), trueDisturbance);
                referenceInputDataSet.CreateTimestamps(timeBase_s);
                plantSim.Simulate(referenceInputDataSet, out referenceSimDataSet);
            }
            // 2.create plant model without disturbance, and try to to find the disturbance signal
            {
                var pidModel1 = new PidModel(pidParameters1, "PID1");
                var plantSim = new PlantSimulator(
                    new List<ISimulatableModel> { pidModel1, processModel });
                plantSim.ConnectModels(processModel, pidModel1);
                plantSim.ConnectModels(pidModel1, processModel);
                var inputData = new TimeSeriesDataSet();
                inputData.Add(SignalNamer.GetSignalName(pidModel1.GetID(), SignalType.Setpoint_Yset),
                    referenceInputDataSet.GetValues(pidModel1.ID, SignalType.Setpoint_Yset));
                /////////////////
                ///
                ///adding u and y to inputdata, should enable the plant simulator to back-calculate the disturbance.
                /// 
                // u 
                inputData.Add(SignalNamer.GetSignalName(pidModel1.GetID(), SignalType.PID_U),
               referenceSimDataSet.GetValues(pidModel1.ID, SignalType.PID_U));// use the input u from the other dataset, simulating a "field data" set
                //y_meas - should trigger determining the disturbance
                inputData.Add(SignalNamer.GetSignalName(processModel.GetID(), SignalType.Output_Y),
                    referenceSimDataSet.GetValues(processModel.ID, SignalType.Output_Y));
                /////////////////
                inputData.CreateTimestamps(timeBase_s);
                var isOK = plantSim.SimulateSingle(inputData, processModel.ID, 
                    out TimeSeriesDataSet simDataSetWithDisturbance);
                Assert.IsTrue(isOK);
                Assert.IsTrue(simDataSetWithDisturbance.ContainsSignal(SignalNamer.EstDisturbance(processModel)));
                if (doAssertResult)
                {
                    var pidDataSet = plantSim.GetUnitDataSetForPID(inputData.Combine(simDataSetWithDisturbance), pidModel1);
                    CommonPlotAndAsserts(pidDataSet, simDataSetWithDisturbance.GetValues(SignalNamer.EstDisturbance(processModel)),
                        trueDisturbance);
                }
            }
        }



    }
}
