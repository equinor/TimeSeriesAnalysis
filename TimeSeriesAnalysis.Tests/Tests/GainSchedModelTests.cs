using Accord;
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

        GainSchedParameters gainSchedP1_singleThreshold_singleInput_static = new GainSchedParameters
          {
                TimeConstant_s = null,
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { 5 }, new double[] { 10 } },
                LinearGainThresholds = new double[] { 2.5 },
                TimeDelay_s = 0,
                OperatingPoint_U = 0,
                OperatingPoint_Y = 0,
                GainSchedParameterIndex = 0
            };
        GainSchedParameters gainSchedP2_singleThreshold_singleInput = new GainSchedParameters
        {
            TimeConstant_s = new double[] { 10, 20 },
            TimeConstantThresholds = new double[] { 2 },
            LinearGains = new List<double[]> { new double[] { 20 }, new double[] { 5 } },
            LinearGainThresholds = new double[] { 0.5 },
            TimeDelay_s = 0,
            OperatingPoint_U = 0,
            OperatingPoint_Y = 0,
            GainSchedParameterIndex = 0
        };
        GainSchedParameters gainSchedP3_singleThreshold_singleInput_bias = new GainSchedParameters
        {
            TimeConstant_s = new double[] { 10, 20 },
            TimeConstantThresholds = new double[] { 2 },
            LinearGains = new List<double[]> { new double[] { -20 }, new double[] { -15 } },
            LinearGainThresholds = new double[] { 1.5 },
            TimeDelay_s = 0,
            OperatingPoint_U = 0,
            OperatingPoint_Y = -15,
            GainSchedParameterIndex = 0
        };
        GainSchedParameters gainSchedP4_singleThreshold_singleInput_bias_and_timedelay= new GainSchedParameters
        {
            TimeConstant_s = new double[] { 10, 20 },
            TimeConstantThresholds = new double[] { 2 },
            LinearGains = new List<double[]> { new double[] { -20 }, new double[] { -15 } },
            LinearGainThresholds = new double[] { 1.5 },
            TimeDelay_s =  2,
            OperatingPoint_U = 0,
            OperatingPoint_Y = -15,
            GainSchedParameterIndex = 0
        };


        GainSchedParameters gainSchedP5_nineThresholds_singleInput= new GainSchedParameters
            {
                TimeConstant_s = null,
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { 0 }, new double[] { 1 }, new double[] { 2 }, new double[] { 3 }, new double[] { 4 }, new double[] { 5 },
                    new double[] { 6 }, new double[] { 7 }, new double[] { 8 }, new double[] { 9 }, new double[] { 10 } },
                LinearGainThresholds = new double[] { 1.5, 2.5, 3.5, 4.5, 5.5, 6.5, 7.5, 8.5, 9.5, 10.5 },
                TimeDelay_s = 0,
                OperatingPoint_U = 0,
                OperatingPoint_Y = 5,
                GainSchedParameterIndex = 0
            }; 

        GainSchedParameters gainSchedP8_singleThreshold_threeInputs = new GainSchedParameters
        {
            TimeConstant_s = new double[] { 10, 20 },
            TimeConstantThresholds = null,
            LinearGains = new List<double[]> { new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 } },
            LinearGainThresholds = new double[] { 2.5 },
            TimeDelay_s = 0,
            OperatingPoint_U = 0,
            OperatingPoint_Y = 0,
            GainSchedParameterIndex = 0
        };

        GainSchedParameters gainSchedP9_singleThreshold_threeInputs_nonzeroGainSchedIdx_bias_and_time_delay =
            new GainSchedParameters
            {
                TimeConstant_s = new double[] { 10, 20 },
                TimeConstantThresholds = new double[] { 2 },
                LinearGains = new List<double[]> { new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 } },
                LinearGainThresholds = new double[] { 2.5 },
                TimeDelay_s = 2,
                OperatingPoint_U = 0,
                OperatingPoint_Y = 0,
                GainSchedParameterIndex = 1
            };

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }

        [Test]

        public void TenThresholds_DifferentGainsAboveEachThreshold()
        {
            // Arrange
            GainSchedModel refModel = new GainSchedModel(gainSchedP5_nineThresholds_singleInput, "GainSched4"); ;
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

            bool doPlot = true;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                     simY1,
                     inputData.GetValues(refModel.GetID(),SignalType.External_U,0),
                     },
                    new List<string> { "y1=y_sim", "y3=u1" },
                    timeBase_s, TestContext.CurrentContext.Test.Name);
                Shared.DisablePlots();
            }

            for (int stepIdx = 0; stepIdx < gainSchedP5_nineThresholds_singleInput.LinearGains.Count; stepIdx++)
            {
                //assume that steps happen every N/4, data points
                int idxBefore = (stepIdx) * N / 4 + 3;
                int idxAfter = (stepIdx+1) * N / 4 + 3;
                var uValAfter = inputData.GetValue(
                    SignalNamer.GetSignalName(refModel.GetID(), SignalType.External_U), idxAfter);
                var uValBefore = inputData.GetValue(
                      SignalNamer.GetSignalName(refModel.GetID(), SignalType.External_U), idxAfter);
                double observedGain = (simY1[idxAfter]- simY1[idxBefore]) ; // all steps are exactly 1.
                Assert.AreEqual( gainSchedP5_nineThresholds_singleInput.LinearGains[stepIdx].First(), observedGain , "step idx:"+stepIdx);
            }


        }

        [TestCase(1, "up", Description = "static, two gains, no timedelay, no bias")]
        [TestCase(5, "up", Description = "nine gains")]
        [TestCase(1, "down", Description = "static, two gains, no timedelay, no bias")]
        [TestCase(5, "down", Description = "nine gains")]
        public void ContinousGradualRamp_ModelOutputShouldIncreaseGradually(int ver, string upOrDown)
        {
            //    var tolerance = 0.2;
            // Arrange
            GainSchedParameters gsParams = new GainSchedParameters();
            if (ver == 1)
            {
                gsParams = gainSchedP1_singleThreshold_singleInput_static;
            }
            if (ver == 5)
            {
                gsParams = gainSchedP5_nineThresholds_singleInput;
            }

            GainSchedModel refModel = new GainSchedModel(gsParams);
            int N = 300;
            var plantSim = new PlantSimulator(new List<ISimulatableModel> { refModel });
            var inputData = new TimeSeriesDataSet();
            double[] input = new double[0];
            if (upOrDown == "up")
                input = TimeSeriesCreator.Ramp(N, 0, 11, 10, 10);
            else
                input = TimeSeriesCreator.Ramp(N, 11, 0, 10, 10);
            inputData.Add(plantSim.AddExternalSignal(refModel, SignalType.External_U, (int)INDEX.FIRST), input);
            inputData.CreateTimestamps(timeBase_s);

            var isSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            double[] simY1 = simData.GetValues(refModel.GetID(), SignalType.Output_Y);

            bool doPlot = true;// should be false unless debugging
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                     simY1,
                     inputData.GetValues(refModel.GetID(),SignalType.External_U,0),
                     },
                    new List<string> { "y1=y_sim", "y3=u1" },
                    timeBase_s, "test"); 
                 //   TestContext.CurrentContext.Test.Name.Replace(',', '_').Replace('(','_').Replace(')','_'));// TestContext.CurrentContext.Test.Name
                Shared.DisablePlots();
            }
            SISOTests.CommonAsserts(inputData, simData, plantSim);

            // assert that step increase is gradual, even at the boundaries between different 
            double maxChg = (11 * 10) / N;
            for (int stepIdx = 1; stepIdx < N; stepIdx++)
            {
                double observedChg = Math.Abs(simY1[stepIdx] - simY1[stepIdx - 1]);
                Assert.IsTrue(observedChg < maxChg, "step idx:" + stepIdx);
            }
        }



        [TestCase(1, 5, 30, Description = "static, two gains, no timedelay, no bias")]
        [TestCase(2, 5, 15, Description = "dynamic, two timeconstants, one gain, no timedelay, no bias")]
        [TestCase(3, -30, -55, Description = "dynamic, two timeconstants, two gains, bias, no timedelay ")]
       // [TestCase(4, -15, -40, Description = "dynamic, two timeconstants, two gains, bias and timedelay")]
        public void SingleThreshold_DifferentGainsAboveAndBelowThreshold(int ver, double step1Out, double step3Out)
        {
            var tolerance = 0.2;
            // Arrange
            GainSchedModel gainSched = null;

            if (ver == 1)
            {
                gainSched = new GainSchedModel(gainSchedP1_singleThreshold_singleInput_static, "GainSched1"); ;
            }
            else if (ver == 2)
            {
                gainSched = new GainSchedModel(gainSchedP2_singleThreshold_singleInput, "GainSched2"); ;
            }
            else if (ver == 3)
            {
                gainSched = new GainSchedModel(gainSchedP3_singleThreshold_singleInput_bias, "GainSched3"); ; ;
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

             bool doPlot = true;// should be false unless debugging.
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                    simY1,
                    inputData.GetValues(gainSched.GetID(),SignalType.External_U,0),
                    },
                    new List<string> { "y1=y_sim" + ver.ToString(), "y3=u1" },
                    timeBase_s, "GainSched_Single");
                Shared.DisablePlots();
            }

            // Assert
            Assert.IsTrue(isSimulatable);
            Assert.IsTrue(Math.Abs(simY1[N/3-2] - step1Out) < tolerance, "first step should have a gain of " + step1Out.ToString());
            Assert.IsTrue(Math.Abs(simY1.Last() - step3Out) < tolerance, "third step should have a gain of " + step3Out.ToString());
            //  Assert.IsTrue(Math.Abs(simY.Last() - (1 * 55 + 0.5 * 45 + 5)) < 0.01);

        }

        [TestCase(8, 48,18)]
        [TestCase(9, 23,47)]
        public void SingleThreshold_ThreeInputs_DifferentGainsAboveAndBelowThreshold(int ver, double step1Out,double step3Out)
        {
            // Arrange
            GainSchedModel gainSched = null;
            if (ver == 8)
            {
                gainSched = new GainSchedModel(gainSchedP8_singleThreshold_threeInputs, "GainSched8"); 
            }
            else if (ver == 9)
            {
                gainSched = new GainSchedModel(gainSchedP9_singleThreshold_threeInputs_nonzeroGainSchedIdx_bias_and_time_delay, "GainSched9");
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

            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                    simY1,
                    inputData.GetValues(gainSched.GetID(),SignalType.External_U,0),
                    inputData.GetValues(gainSched.GetID(),SignalType.External_U,1),
                    inputData.GetValues(gainSched.GetID(),SignalType.External_U,2)},
                    new List<string> { "y1=y_sim" + ver.ToString(), "y3=u1", "y3=u2", "y3=u3"},
                    timeBase_s, "SingleThreshold_ThreeInputs");
                Shared.DisablePlots();
            }
        }


    }
}
