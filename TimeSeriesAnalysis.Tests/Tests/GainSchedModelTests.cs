using Accord;
using NuGet.Frameworks;
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

        GainSchedParameters gainSchedP1_singleThreshold_singleInput_static = new GainSchedParameters(0,0)
          {
                TimeConstant_s = null,
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { 5 }, new double[] { 10 } },
                LinearGainThresholds = new double[] { 2.5 },
                TimeDelay_s = 0,
                GainSchedParameterIndex = 0
            };
        GainSchedParameters gainSchedP2_singleThreshold_singleInput = new GainSchedParameters(0,0)
        {
            TimeConstant_s = new double[] { 0,50 }, //nb! large jump that can create "bump" in model output
            TimeConstantThresholds = new double[] { 2.5 },
            LinearGains = new List<double[]> { new double[] { 5 }, new double[] { 10 } },
            LinearGainThresholds = new double[] { 2.5 },
            TimeDelay_s = 0,
            GainSchedParameterIndex = 0
        };
        GainSchedParameters gainSchedP3_singleThreshold_singleInput_bias = new GainSchedParameters(0,-15)
        {
            TimeConstant_s = new double[] { 5, 10 },
            TimeConstantThresholds = new double[] { 2 },
            LinearGains = new List<double[]> { new double[] { 5 }, new double[] { 10 } },
            LinearGainThresholds = new double[] { 2.5 },
            TimeDelay_s = 1,
            GainSchedParameterIndex = 0
        };
        GainSchedParameters gainSchedP4_singleThreshold_singleInput_bias_and_timedelay= new GainSchedParameters(0,-15)
        {
            TimeConstant_s = new double[] { 10, 20 },
            TimeConstantThresholds = new double[] { 2 },
            LinearGains = new List<double[]> { new double[] { -20 }, new double[] { -15 } },
            LinearGainThresholds = new double[] { 1.5 },
            TimeDelay_s =  2,
            GainSchedParameterIndex = 0
        };


        GainSchedParameters gainSchedP5_nineThresholds_singleInput= new GainSchedParameters(0,5)
            {
                TimeConstant_s = null,
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { 0 }, new double[] { 1 }, new double[] { 2 }, new double[] { 3 }, new double[] { 4 }, new double[] { 5 },
                    new double[] { 6 }, new double[] { 7 }, new double[] { 8 }, new double[] { 9 }, new double[] { 10 } },
                LinearGainThresholds = new double[] { 1.5, 2.5, 3.5, 4.5, 5.5, 6.5, 7.5, 8.5, 9.5, 10.5 },
                TimeDelay_s = 0,
                GainSchedParameterIndex = 0
           };

        // similar to P1 except for operating point.
        GainSchedParameters gainSchedP6_singleThreshold_singleInput_static_nonzeroOperatingPointU = new GainSchedParameters(2,2)
        {
            TimeConstant_s = null,
            TimeConstantThresholds = null,
            LinearGains = new List<double[]> { new double[] { 5 }, new double[] { 10 } },
            LinearGainThresholds = new double[] { 2.5 },
            TimeDelay_s = 0,
            GainSchedParameterIndex = 0
        };


        GainSchedParameters gainSchedP8_singleThreshold_threeInputs = new GainSchedParameters(0,0)
        {
            TimeConstant_s = new double[] { 10, 20 },
            TimeConstantThresholds = null,
            LinearGains = new List<double[]> { new double[] { 1, 2, 3 }, new double[] { 3, 6, 9 } },
            LinearGainThresholds = new double[] { 1.5 },
            TimeDelay_s = 0,
            GainSchedParameterIndex = 0
        };

        GainSchedParameters gainSchedP9_singleThreshold_threeInputs_bias_and_time_delay =
            new GainSchedParameters(0,1)
            {
                TimeConstant_s = new double[] { 10, 20 },
                TimeConstantThresholds = new double[] { 2 },
                LinearGains = new List<double[]> { new double[] { 1, 2, 3 }, new double[] { 3, 6, 9 } },
                LinearGainThresholds = new double[] { 1.5 },
                TimeDelay_s = 2,
                GainSchedParameterIndex = 0
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

            bool doPlot = false;
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

            double prevGain = -1;
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
                
                Assert.IsTrue( observedGain >prevGain, "step idx:"+stepIdx);
                prevGain = observedGain;
            }
        }

        [TestCase(1, "up", Description = "static, two gains, no timedelay, no bias")]
        [TestCase(5, "up", Description = "nine gains")]
        [TestCase(1, "down", Description = "static, two gains, no timedelay, no bias")]
        [TestCase(5, "down", Description = "nine gains")]
   //     [TestCase(2, "down", Description = "two gains, two time-constants very different, creates bump in simulated y")]

        public void ContinousGradualRamp_BumplessModelOutput(int ver, string upOrDown)
        {
            int padBeginIdx = 10;
            int padEndIdx = 40; 
            //    var tolerance = 0.2;
            // Arrange
            GainSchedParameters gsParams = new GainSchedParameters();
            if (ver == 1)
            {
                gsParams = gainSchedP1_singleThreshold_singleInput_static;
            }
            if (ver == 2) // in this case the time constant is large enough that the transient in the one model is 
                // so large that thre it can create a bump when the model transitions.
            {
                gsParams = gainSchedP2_singleThreshold_singleInput;
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
                input = TimeSeriesCreator.Ramp(N, 0, 11, padBeginIdx, padEndIdx);
            else
                input = TimeSeriesCreator.Ramp(N, 11, 0, padBeginIdx, padEndIdx);
            inputData.Add(plantSim.AddExternalSignal(refModel, SignalType.External_U, (int)INDEX.FIRST), input);
            inputData.CreateTimestamps(timeBase_s);

            var isSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            double[] simY1 = simData.GetValues(refModel.GetID(), SignalType.Output_Y);

            bool doPlot = false;// should be false unless debugging
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                     simY1,
                     inputData.GetValues(refModel.GetID(),SignalType.External_U,0),
                     },
                    new List<string> { "y1=y_sim", "y3=u1" },
                    timeBase_s, "ContinousGradualRamp_BumplessModelOutput" + ver + upOrDown); 
                Shared.DisablePlots();
            }
            SISOTests.CommonAsserts(inputData, simData, plantSim);

            // assert that step increase is gradual, even at the boundaries between different 
            double maxChg = ((11.0 +1)* 10.0) / N;
            for (int stepIdx = padBeginIdx+2; stepIdx < N-padEndIdx-1; stepIdx++)
            {
                double observedChg = Math.Abs(simY1[stepIdx] - simY1[stepIdx - 1]);
                Assert.IsTrue(observedChg < maxChg, "step idx:" + stepIdx 
                    + "chg:"+observedChg + "max:"+ maxChg);
            }
        }


        [TestCase(1, 5, 17.5, Description = "static, two gains, no timedelay, no bias")]
        [TestCase(2, 5, 17.5, Description = "dynamic, two timeconstants, two gain, no timedelay, no bias")]
        [TestCase(3, -10, 2.5, Description = "dynamic, two timeconstants, two gains, bias, timedelay ")]
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
            var U0 = gainSched.GetModelParameters().GetOperatingPointU();
            inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.FIRST),
                TimeSeriesCreator.ThreeSteps(N/5, N/3, N/2, N, U0, U0+1, U0+2, U0+3));
            inputData.CreateTimestamps(timeBase_s);

            // Act
            var isSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
                        
            SISOTests.CommonAsserts(inputData, simData, plantSim);
            double[] simY1 = simData.GetValues(gainSched.GetID(), SignalType.Output_Y); 

            bool doPlot = false;// should be false unless debugging.
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
            Assert.AreEqual(gainSched.modelParameters.GetOperatingPointY(), simY1[0], "at time zero the u is zero and the model output should match the operating point.");
            Assert.IsTrue(Math.Abs(simY1[N/3-2] - step1Out) < tolerance, "first step should have a gain of " + step1Out.ToString() + "was:" + simY1[N / 3 - 2]);
            Assert.IsTrue(Math.Abs(simY1.Last() - step3Out) < tolerance, "third step should have a gain of " + step3Out.ToString() + "was:" + simY1.Last());
        }

        [TestCase(20, 70)]
        [TestCase(50, 120)]

        public void NonzeroOperatingPoint_SimulationStartsInOpPointOk(double uOperatingPoint, double yOperatingPoint)
        {
            double noiseAmp = 0.0;
            int N = 400;
            double gainSchedThreshold = uOperatingPoint+5;//note: the threshold will not match the operating point.
            // Arrange
            var unitData = new UnitDataSet();
            double[] u1 = TimeSeriesCreator.ThreeSteps((int)(N*0.1), (int)(N*0.3), (int)(N*0.5), (int)(N*0.7), uOperatingPoint, uOperatingPoint + 10, uOperatingPoint, uOperatingPoint - 10);
            double[] u = u1; 
            unitData.U = Array2D<double>.CreateFromList(new List<double[]> { u });
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(new DateTime(2000, 1, 1), timeBase_s, N);

            //reference model
            GainSchedParameters trueGSparams = new GainSchedParameters(uOperatingPoint, yOperatingPoint)
            {
                TimeConstant_s = new double[] { 10, 2 },
                TimeConstantThresholds = new double[] { gainSchedThreshold },
                LinearGains = new List<double[]> { new double[] { 2 }, new double[] { 4 } },
                LinearGainThresholds = new double[] { gainSchedThreshold },
                TimeDelay_s = 0,
            };

            GainSchedModel trueModel = new GainSchedModel(trueGSparams, "True  model");
            var truePlantSim = new PlantSimulator(new List<ISimulatableModel> { trueModel });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(truePlantSim.AddExternalSignal(trueModel, SignalType.External_U, (int)INDEX.FIRST), u);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = truePlantSim.Simulate(inputData, out TimeSeriesDataSet refSimData);
  
            double[] simY1 = refSimData.GetValues(trueModel.GetID(), SignalType.Output_Y);
            unitData.Y_meas = (new Vec()).Add(Vec.Rand(simY1.Length, -noiseAmp, noiseAmp, (int)Math.Ceiling(2 * gainSchedThreshold + 45)), simY1);

            // plot
            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                        unitData.Y_meas ,
                        unitData.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y3=u1" },
                    timeBase_s,
                    "NonzeroOpPointSimulationU="+ uOperatingPoint);
                Shared.DisablePlots();
            }

            SISOTests.CommonAsserts(inputData, refSimData, truePlantSim);
            Assert.That(unitData.Y_meas.First() == yOperatingPoint, "since time series starts in uOperatingPoint, simulation should start in yOperatingPoint");

        }



        [TestCase(8, 0, 1,3,6, Description = "steps below the threshold")]
        [TestCase(8, 1, 3, 9, 18, Description = "steps above the treshold")]
        [TestCase(9, 0, 2, 4, 7,Description = "nonzero bias of one, also time-delay added, steps below the trehoshold")]
        [TestCase(9, 1, 4, 10, 19, Description = "nonzero bias of one, also time-delay added, steps below the trehoshold")]
        public void SingleThreshold_ThreeInputs_DifferentGainsAboveAndBelowThreshold(int modelVer, int inputVer, double exp_ystep1,
            double exp_ystep2, double exp_ystep3 )
        {
            var tolerance = 0.1;

            // Arrange
            GainSchedModel gainSched = null;
            if (modelVer == 8)
            {
                gainSched = new GainSchedModel(gainSchedP8_singleThreshold_threeInputs, "GainSched8"); 
            }
            else if (modelVer == 9)
            {
                gainSched = new GainSchedModel(gainSchedP9_singleThreshold_threeInputs_bias_and_time_delay, "GainSched9");
            }

            var plantSim = new PlantSimulator(new List<ISimulatableModel> { gainSched });
            var inputData = new TimeSeriesDataSet();

            //below threshold
            if (inputVer == 0)
            {
                inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.FIRST),
                    TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 0, 1, 1, 1));
                inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.SECOND),
                    TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 0, 0, 1, 1));
                inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.THIRD),
                    TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 0, 0, 0, 1));
            }
            else if (inputVer == 1)// above threshold of 1.5
            {
                inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.FIRST),
                    TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 0, 2, 2, 2));
                inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.SECOND),
                    TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 0, 0, 2, 2));
                inputData.Add(plantSim.AddExternalSignal(gainSched, SignalType.External_U, (int)INDEX.THIRD),
                    TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 0, 0, 0, 2));
            }
            inputData.CreateTimestamps(timeBase_s);

            // Act
            var isSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            double[] simY1 = simData.GetValues(gainSched.GetID(), SignalType.Output_Y);


            bool doPlot = false;//should be false unless debugging.
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                    simY1,
                    inputData.GetValues(gainSched.GetID(),SignalType.External_U,0),
                    inputData.GetValues(gainSched.GetID(),SignalType.External_U,1),
                    inputData.GetValues(gainSched.GetID(),SignalType.External_U,2)},
                    new List<string> { "y1=y_sim" + modelVer.ToString(), "y3=u1", "y3=u2", "y3=u3" },
                    timeBase_s, "SingleThreshold_ThreeInputs");
                Shared.DisablePlots();
            }

            // Assert
            Assert.IsTrue(isSimulatable);

            var simBeforeStep = simY1[0];
            var simStep1 = simY1[N/2-2];
            var simStep2 = simY1[N*3/ 4-2];
            var simStep3 = simY1[N-2];

            Assert.IsTrue(Math.Abs(simBeforeStep - gainSched.modelParameters.GetOperatingPointY()) < tolerance, "before step");
            Assert.IsTrue(Math.Abs(simStep1 - exp_ystep1) < tolerance, "step1 was:"+ simStep1);
            Assert.IsTrue(Math.Abs(simStep2 - exp_ystep2) < tolerance, "step2 was:" + simStep2);
            Assert.IsTrue(Math.Abs(simStep3 - exp_ystep3) < tolerance, "step3 was:" + simStep3);

           }

    }
}
