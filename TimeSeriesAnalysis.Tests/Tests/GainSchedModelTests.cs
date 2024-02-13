using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;


namespace TimeSeriesAnalysis.Test.PlantSimulations
{
    /// <summary>
    /// Test of process simulations where each of or some of the models have multiple inputs
    /// </summary>
    [TestFixture]
    class GainsSchedModelTests
    {
        int timeBase_s = 1;
        int N = 480;

        GainSchedModel gainSched1_singleThreshold_singleInput;
        GainSchedModel gainSched2_singleThreshold_singleInput;
        GainSchedModel gainSched3_singleThreshold_singleInput;

        GainSchedModel gainSched4_tenThresholds_singleInput;

        GainSchedModel gainSched8_singleThreshold_threeInputs;
        GainSchedModel gainSched9_singleThreshold_threeInputs_bias_and_timedelay;
        GainSchedParameters gainSchedP1_singleThreshold_singleInput_static;
        GainSchedParameters gainSchedP2_singleThreshold_singleInput;
        GainSchedParameters gainSchedP3_singleThreshold_singleInput_bias_and_timedelay;

        GainSchedParameters gainSchedP4_nineThresholds_singleInput;

        GainSchedParameters gainSchedP8_singleThreshold_threeInputs;
        GainSchedParameters gainSchedP9_singleThreshold_threeInputs_nonzeroGainSchedIdx_bias_and_time_delay;

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();

            gainSchedP1_singleThreshold_singleInput_static = new GainSchedParameters
            {
                TimeConstant_s = null,
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { 5 }, new double[] { 10 } },
                LinearGainThresholds = new double[] { 2.5 },
                TimeDelay_s = 0,
                Bias = 0,
                GainSchedParameterIndex = 0
            };

            gainSchedP2_singleThreshold_singleInput = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 10, 20 },
                TimeConstantThresholds = new double[] { 2 },
                LinearGains = new List<double[]> { new double[] { 20 }, new double[] { 5 } },
                LinearGainThresholds = new double[] { 0.5 },
                TimeDelay_s = 0,
                Bias = 0,
                GainSchedParameterIndex = 0
            };

            gainSchedP3_singleThreshold_singleInput_bias_and_timedelay = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 10, 20 },
                TimeConstantThresholds = new double[] { 2 },
                LinearGains = new List<double[]> { new double[] { -20 }, new double[] { -15 } },
                LinearGainThresholds = new double[] { 1.5 },
                TimeDelay_s = timeBase_s * 2,
                Bias = 5,
                GainSchedParameterIndex = 0
            };

            gainSchedP4_nineThresholds_singleInput = new GainSchedParameters
            {
                TimeConstant_s = null,
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { 0 }, new double[] { 1 }, new double[] { 2 }, new double[] { 3 }, new double[] { 4 }, new double[] { 5 },
                    new double[] { 6 }, new double[] { 7 }, new double[] { 8 }, new double[] { 9 }, new double[] { 10 } },
                LinearGainThresholds = new double[] { 1.5,2.5,3.5,4.5,5.5,6.5,7.5,8.5,9.5,10.5},
                TimeDelay_s = 0,
                Bias = 5,
                GainSchedParameterIndex = 0
            };

            gainSchedP8_singleThreshold_threeInputs = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 10, 20 },
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 } },
                LinearGainThresholds = new double[] { 2.5 },
                TimeDelay_s = 0,
                Bias = 0,
                GainSchedParameterIndex = 0
            };
            
            gainSchedP9_singleThreshold_threeInputs_nonzeroGainSchedIdx_bias_and_time_delay = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 10, 20 },
                TimeConstantThresholds = new double[] { 2 },
                LinearGains = new List<double[]> { new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 } },
                LinearGainThresholds = new double[] { 2.5 },
                TimeDelay_s = timeBase_s*2,
                Bias = 5,
                GainSchedParameterIndex = 1
            };


            gainSched1_singleThreshold_singleInput = new GainSchedModel(gainSchedP1_singleThreshold_singleInput_static, "GainSched1");
            gainSched2_singleThreshold_singleInput = new GainSchedModel(gainSchedP2_singleThreshold_singleInput, "GainSched2");
            gainSched3_singleThreshold_singleInput = new GainSchedModel(gainSchedP3_singleThreshold_singleInput_bias_and_timedelay, "GainSched3");

            gainSched4_tenThresholds_singleInput = new GainSchedModel(gainSchedP4_nineThresholds_singleInput, "GainSched4");

            gainSched8_singleThreshold_threeInputs = new GainSchedModel(gainSchedP8_singleThreshold_threeInputs, "GainSched8");
            gainSched9_singleThreshold_threeInputs_bias_and_timedelay =
                new GainSchedModel(gainSchedP9_singleThreshold_threeInputs_nonzeroGainSchedIdx_bias_and_time_delay, "GainSched9");

        }

        [Test]//,Ignore("work in progress")

        public void TenThresholds_DifferentGainsAboveEachThreshold()
        {
        //    var tolerance = 0.2;
            // Arrange
            GainSchedModel refModel = gainSched4_tenThresholds_singleInput;
            int N = 40;
            var plantSim = new PlantSimulator(new List<ISimulatableModel> { refModel });
            var inputData = new TimeSeriesDataSet();
            var input = TimeSeriesCreator.ThreeSteps(N / 4, N *2/4, N * 3/4, N, 0, 1, 2, 3).
                Concat( TimeSeriesCreator.ThreeSteps(N / 4, N *2/4, N * 3/4, N, 4, 5, 6, 7)).
                Concat( TimeSeriesCreator.ThreeSteps(N / 4, N *2/4, N * 3/4, N, 8, 9, 10, 11)).ToArray();
            inputData.Add(plantSim.AddExternalSignal(refModel, SignalType.External_U, (int)INDEX.FIRST),input );
            inputData.CreateTimestamps(timeBase_s);

            // Act
            var isSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            SISOTests.CommonAsserts(inputData, simData, plantSim);
            double[] simY1 = simData.GetValues(refModel.GetID(), SignalType.Output_Y); 

            for (int stepIdx = 0; stepIdx < gainSchedP4_nineThresholds_singleInput.LinearGains.Count; stepIdx++)
            {
                //assume that steps happen every N/4, data points     
                int idxAfter = (stepIdx+1) * N / 4 + 3;
                var uVal = inputData.GetValue(
                    SignalNamer.GetSignalName(refModel.GetID(), SignalType.External_U), idxAfter);
                double observedGain = (simY1[idxAfter]- refModel.modelParameters.Bias)/uVal.Value; // all steps are exactly 1.
                Assert.AreEqual( gainSchedP4_nineThresholds_singleInput.LinearGains[stepIdx].First(), observedGain , "step idx:"+stepIdx);
            }

           /* Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simY1,
                inputData.GetValues(refModel.GetID(),SignalType.External_U,0),
                },
                new List<string> { "y1=y_sim", "y3=u1" },
                timeBase_s, TestContext.CurrentContext.Test.Name);
            Shared.DisablePlots();*/
        }


        [TestCase(1, 5, 30)]
        [TestCase(2, 5, 15)]
        [TestCase(3, -15, -40)]

        public void SingleThreshold_DifferentGainsAboveAndBelowThreshold(int ver, double step1Out, double step3Out)
        {
            var tolerance = 0.2;
            // Arrange
            GainSchedModel gainSched = null;

            if (ver == 1)
            {
                gainSched = gainSched1_singleThreshold_singleInput;
            }
            else if (ver == 2)
            {
                gainSched = gainSched2_singleThreshold_singleInput;
            }
            else if (ver == 3)
            {
                gainSched = gainSched3_singleThreshold_singleInput;
            }

            var plantSim = new PlantSimulator(new List<ISimulatableModel> { gainSched });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.FIRST),
                TimeSeriesCreator.ThreeSteps(N/5, N/3, N/2, N, 0, 1, 2, 3));
            inputData.CreateTimestamps(timeBase_s);

            // Act
            var isSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            SISOTests.CommonAsserts(inputData, simData, plantSim);
            double[] simY1 = simData.GetValues(gainSched.GetID(), SignalType.Output_Y); // TODO: Change .GetID() with input ID from parameterlist?
            
            // Assert
            Assert.IsTrue(isSimulatable);
            Assert.IsTrue(Math.Abs(simY1[N/3-2] - step1Out) < tolerance, "first step should have a gain of " + step1Out.ToString());
            Assert.IsTrue(Math.Abs(simY1.Last() - step3Out) < tolerance, "third step should have a gain of " + step3Out.ToString());
            //  Assert.IsTrue(Math.Abs(simY.Last() - (1 * 55 + 0.5 * 45 + 5)) < 0.01);


            //Shared.EnablePlots();
            //Plot.FromList(new List<double[]> {
            //    simY1,
            //    inputData.GetValues(gainSched.GetID(),SignalType.External_U,0),
            //    },
            //    new List<string> { "y1=y_sim" + ver.ToString(), "y3=u1" },
            //    timeBase_s, "GainSched_Single");
            //Shared.DisablePlots();

        }

        [TestCase(8, 48,18)]
        [TestCase(9, 23,47)]
        public void SingleThreshold_ThreeInputs_DifferentGainsAboveAndBelowThreshold(int ver, double step1Out,double step3Out)
        {
            // Arrange
            GainSchedModel gainSched = null;
            if (ver == 8)
            {
                gainSched = gainSched8_singleThreshold_threeInputs;
            }
            else if (ver == 9)
            {
                gainSched = gainSched9_singleThreshold_threeInputs_bias_and_timedelay;
            }

            var plantSim = new PlantSimulator(new List<ISimulatableModel> { gainSched });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.FIRST),
                TimeSeriesCreator.TwoSteps(N/3, N*2/3, N, 5, 2, 1));
            inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.SECOND),
                TimeSeriesCreator.TwoSteps(N/3, N*2/3, N, 2 ,3, 4));
            inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.THIRD),
                TimeSeriesCreator.TwoSteps(N/3, N/2  , N, 3 ,2, 3));
            inputData.CreateTimestamps(timeBase_s);

            // Act
            var isSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            double[] simY1 = simData.GetValues(gainSched.GetID(), SignalType.Output_Y); 

            // Assert
            Assert.IsTrue(isSimulatable);
            Assert.IsTrue(Math.Abs(simY1[N/3-2] - step1Out) < 0.2, "first step should have a gain of " + step1Out.ToString()+" was:"+ simY1[N / 3 - 2].ToString());
            Assert.IsTrue(Math.Abs(simY1.Last() - step3Out) < 0.01, "value after third step should be " + step3Out.ToString() + " was:" + simY1.Last().ToString());

            //Shared.EnablePlots();
            //Plot.FromList(new List<double[]> {
            //    simY1,
            //    inputData.GetValues(gainSched.GetID(),SignalType.External_U,0),
            //    inputData.GetValues(gainSched.GetID(),SignalType.External_U,1),
            //    inputData.GetValues(gainSched.GetID(),SignalType.External_U,2)},
            //    new List<string> { "y1=y_sim" + ver.ToString(), "y3=u1", "y3=u2", "y3=u3"},
            //    timeBase_s, "GainSched_Multiple");
            //Shared.DisablePlots();

        }


    }
}
