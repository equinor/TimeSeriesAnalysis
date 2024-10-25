using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using System.Collections.Generic;

using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Test.PlantSimulations;
using Accord;
using Accord.Math;
using System.Diagnostics;
using System;
using System.Reflection;

namespace TimeSeriesAnalysis.Test.SysID
{
    public class GainSchedIdentifyTests
    {
        const int timeBase_s = 1;
        const double TimeConstantAllowedDev_s = 3.5;



        // note that the tolerance seems to be linear with the noise in the data
        // five varying gains
        [TestCase(1, 0, 1, 0.0, Description = "Two steps for every threshold(five thresholds)")]
        [TestCase(1, 0, 10, 1.0, Description ="Two steps for every threshold(five thresholds)")]
        [TestCase(1, 0, 20, 2.0, Description = "Two steps for every threshold(five thresholds)")]
        // five gains that are the same (note that the tolerance can be much lower in this case)
        [TestCase(2, 0, 1, 0.0, Description = "Two steps for every threshold(five thresholds)")]
        [TestCase(2, 0, 5, 1.0, Description = "Two steps for every threshold(five thresholds)")]//note here that the tolerance can be set much lower!
        [TestCase(2, 0, 10, 2.0, Description = "Two steps for every threshold(five thresholds)")]//note here that the tolerance can be set much lower!
        public void GainEst_FiveGains_CorrectGainsReturned(int ver, int expectedNumWarnings, double gainTolerancePrc, double noiseAmplitude )
        {
            const int N = 100;//Note, that the actual dataset is four times this value.
            GainSchedParameters refParams = new GainSchedParameters(); 
            // below: 5 gains
            if (ver == 1)
            {
                refParams = new GainSchedParameters
                {
                    TimeConstant_s = null,
                    TimeConstantThresholds = null,
                    LinearGains = new List<double[]> { new double[] { 0.5 }, new double[] { 1 }, new double[] { 3 }, new double[] { 4.5 }, new double[] { 6 }, new double[] { 9 } },
                    LinearGainThresholds = new double[] { 2.5, 4.5, 6.5, 8.5, 10.5 },
                    TimeDelay_s = 0,
                    OperatingPoint_U = 5,
                    OperatingPoint_Y = 4,
                    GainSchedParameterIndex = 0
                };
            }
            else if (ver == 2)
            {
                refParams = new GainSchedParameters // let all gains be the same, and check that estimation does not falsely suggest a gain-scheduled model
                {
                    TimeConstant_s = null,
                    TimeConstantThresholds = null,
                    LinearGains = new List<double[]> { new double[] { 2 }, new double[] { 2 }, new double[] { 2}, new double[] { 2 }, new double[] { 2 }, new double[] { 2 } },
                    LinearGainThresholds = new double[] { 2.5, 4.5, 6.5, 8.5, 10.5 },
                    TimeDelay_s = 0,
                    OperatingPoint_U = 5,
                    OperatingPoint_Y = 4,
                    GainSchedParameterIndex = 0
                };
            }
            var refModel = new GainSchedModel(refParams,"ref_model");
            var gsFittingSpecs= new GainSchedFittingSpecs();
            gsFittingSpecs.uGainThresholds = refModel.GetModelParameters().LinearGainThresholds;

            var plantSim = new PlantSimulator(new List<ISimulatableModel> { refModel });
            var inputData = new TimeSeriesDataSet();
            var input = (TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N,  0,  1,  2,  3).
                  Concat(TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N,  4,  5,  6,  7)).
                  Concat(TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N,  8,  9, 10, 11)).
                  Concat(TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 12, 13, 14, 15))
                 .ToArray());
            inputData.Add(plantSim.AddExternalSignal(refModel, SignalType.External_U, (int)INDEX.FIRST), input);
            inputData.CreateTimestamps(timeBase_s);

            // Act
            var isSimulatable = plantSim.Simulate(inputData, out TimeSeriesDataSet simData);
            simData.AddNoiseToSignal(SignalNamer.GetSignalName(refModel.ID, SignalType.Output_Y, 0),noiseAmplitude);

            Assert.IsTrue(isSimulatable);
            var dataSet = new UnitDataSet();
            dataSet.Y_meas = simData.GetValues(refModel.ID, SignalType.Output_Y);
            dataSet.U = Array2D<double>.CreateFromList(new List<double[]> { inputData.GetValues(refModel.ID,SignalType.External_U)});
            dataSet.Times = inputData.GetTimeStamps();
            var gsParams = GainSchedIdentifier.IdentifyForGivenThresholds(dataSet, gsFittingSpecs);
            Assert.IsTrue(gsParams.Fitting.WasAbleToIdentify);
            Assert.AreEqual(expectedNumWarnings, gsParams.GetWarningList().Count());

            // Plotting gains(debugging)
            bool doPlots = false;
            if (doPlots)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                dataSet.Y_meas,
                dataSet.Y_sim, 
                dataSet.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_ident", "y3=u1" },
                    timeBase_s,
                    "GainEstOnly_CorrectGainsReturned");

                GainSchedModel gsModel = new GainSchedModel(gsParams,"ident_model");
                gsModel.SetOutputID("y_meas");
                gsModel.SetInputIDs((new List<string> { "u_1" }).ToArray());
                GainSchedModel referenceModel = new GainSchedModel(refParams,"ref_model");
                referenceModel.SetInputIDs(gsModel.GetModelInputIDs());
                referenceModel.SetOutputID(gsModel.GetOutputID());

                PlotGain.Plot(gsModel, referenceModel);
                Shared.DisablePlots();
            }
            // asserts
            for (int i = 0; i < refParams.LinearGains.Count; i++)
            {
                DiffLessThan(refParams.LinearGains[i][0], gsParams.LinearGains[i][0], gainTolerancePrc,i);
            }
        }

        private void DiffLessThan(double trueVal, double testVal, double tolerancePrc,int index)
        {
            var diff = trueVal - testVal;
            if (trueVal > 0)
            {
                var diffPrc = diff / trueVal*100;
                Assert.IsTrue(Math.Abs(diffPrc) < tolerancePrc, "diffPrc:" + diffPrc.ToString("F1") + " above tolerance:" + tolerancePrc+ " value:"+testVal+" vs. true: "+trueVal+"at index:"+index);
            }
            else
            {
                Assert.IsTrue(diff < 0.1);
            }
        }

        [TestCase()]
        public void GainAndThreshold_GainsNotLargerThanTheBiggestPossibleGain()
        {
            int N = 500;

            // Arrange
            var unitData = new UnitDataSet("test"); /* Create an instance of TimeSeries with test data */
            double[] u1 = TimeSeriesCreator.ThreeSteps(N / 5, N / 3, N / 2, N, 0, 1, 2, 3);
            double[] u2 = TimeSeriesCreator.ThreeSteps(3 * N / 5, 2 * N / 3, 4 * N / 5, N, 0, 1, 2, 3);
            double[] u = u1.Zip(u2, (x, y) => x + y).ToArray();
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u });
            unitData.U = U;
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            var correct_gain_sched_parameters = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 10 },
                TimeConstantThresholds = new double[] { },
                LinearGains = new List<double[]> { new double[] { 1 }, new double[] { 6 } },
                LinearGainThresholds = new double[] { 3.1 },
                TimeDelay_s = 0,
            };
            GainSchedModel correct_model = new GainSchedModel(correct_gain_sched_parameters, "Correct gain sched model");
            var correct_plantSim = new PlantSimulator(new List<ISimulatableModel> { correct_model });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(correct_plantSim.AddExternalSignal(correct_model, SignalType.External_U, (int)INDEX.FIRST), u);
            inputData.CreateTimestamps(timeBase_s);
            var CorrectisSimulatable = correct_plantSim.Simulate(inputData, out TimeSeriesDataSet CorrectsimData);
            SISOTests.CommonAsserts(inputData, CorrectsimData, correct_plantSim);
            double[] simY1 = CorrectsimData.GetValues(correct_model.GetID(), SignalType.Output_Y);
            unitData.Y_meas = simY1;

            // Act
            var best_params = GainSchedIdentifier.Identify(unitData);
            double current_abs_value = 0;
            double largest_gain_amplitude = 0;
            for (int k = 0; k < best_params.LinearGains.Count; k++)
            {
                current_abs_value = Math.Sqrt(best_params.LinearGains[k][0] * best_params.LinearGains[k][0]);
                if (current_abs_value > largest_gain_amplitude)
                {
                    largest_gain_amplitude = current_abs_value;
                }
            }

            double largest_correct_gain_amplitude = 0;
            for (int k = 0; k < correct_gain_sched_parameters.LinearGains.Count; k++)
            {
                current_abs_value = Math.Sqrt(correct_gain_sched_parameters.LinearGains[k][0] * correct_gain_sched_parameters.LinearGains[k][0]);
                if (current_abs_value > largest_correct_gain_amplitude)
                {
                    largest_correct_gain_amplitude = current_abs_value;
                }
            }


            bool doPlot = true;
            if (doPlot)
            { 
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                    simY1,
                    unitData.U.GetColumn(0) },
                    new List<string> { "y1=correct_model", "y3=u1" },
                    timeBase_s,
                    "GainSched - Max Threshold");
                Shared.DisablePlots();
            }
            // Assert
            Assert.That(largest_gain_amplitude, Is.LessThanOrEqualTo(largest_correct_gain_amplitude),
                "The largest gain in the best fitting model cannot exceed the largest gain amplitude of the correct model");

        }

   /*     [TestCase(1, -1.5)]
        [TestCase(2, -1.0)]
        [TestCase(3, -0.5)]
        [TestCase(4, 1.0)]
        [TestCase(5, 2.5)]
       [TestCase(6, 3.0)]*/
        [TestCase(7, 4.0)]
        public void GainAndThreshold_LinearGainThresholdAtReasonablePlace(int ver, double gain_sched_threshold)
        {
            int N = 300;
            // Arrange
            var unitData = new UnitDataSet("test"); 
            double[] u1 = TimeSeriesCreator.ThreeSteps(N/5, N/3, N/2, N, -2, -1, 0, 1);
            double[] u2 = TimeSeriesCreator.ThreeSteps(3*N/5, 2*N/3, 4*N/5, N, 0, 1, 2, 3);
            double[] u = u1.Zip(u2, (x, y) => x+y).ToArray();
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u });
            unitData.U = U;
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            //reference model
            GainSchedParameters correct_gain_sched_parameters = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 3, 10 },
                TimeConstantThresholds = new double[] { gain_sched_threshold },
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { gain_sched_threshold },
                TimeDelay_s = 0,
            };
            GainSchedModel correct_model = new GainSchedModel(correct_gain_sched_parameters, "Correct gain sched model");
            var correct_plantSim = new PlantSimulator(new List<ISimulatableModel> { correct_model });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(correct_plantSim.AddExternalSignal(correct_model, SignalType.External_U, (int)INDEX.FIRST), u);
            inputData.CreateTimestamps(timeBase_s);
            var CorrectisSimulatable = correct_plantSim.Simulate(inputData, out TimeSeriesDataSet CorrectsimData);
            SISOTests.CommonAsserts(inputData, CorrectsimData, correct_plantSim);
            double[] simY1 = CorrectsimData.GetValues(correct_model.GetID(), SignalType.Output_Y);
            unitData.Y_meas = simY1;

            // Act
            GainSchedParameters best_params = GainSchedIdentifier.Identify(unitData);
            GainSchedModel best_model = new GainSchedModel(best_params, "Best fitting model");
            var best_plantSim = new PlantSimulator(new List<ISimulatableModel> { best_model });
            inputData.Add(best_plantSim.AddExternalSignal(best_model, SignalType.External_U, (int)INDEX.FIRST), u);
            var IdentifiedisSimulatable = best_plantSim.Simulate(inputData, out TimeSeriesDataSet IdentifiedsimData);
            SISOTests.CommonAsserts(inputData, IdentifiedsimData, best_plantSim);
            double[] simY2 = IdentifiedsimData.GetValues(best_model.GetID(), SignalType.Output_Y);
            // Number of inputs can be determined from the TimeSeries object, assuming it provides a way to determine this
            int numberOfInputs = unitData.U.GetNColumns(); // Example property, replace with actual implementation
            // Assert
            int min_number_of_gains = Math.Min(best_params.LinearGainThresholds.Length, correct_gain_sched_parameters.LinearGainThresholds.Length);
            for (int k = 0; k < min_number_of_gains; k++)
            {
                Assert.That(Math.Pow(best_params.LinearGainThresholds[k] - correct_gain_sched_parameters.LinearGainThresholds[k], 2), Is.LessThanOrEqualTo(0.5),
                "There are too large differences in the linear gain threshold " + k.ToString());
            }
            /*
            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                    simY1,
                    simY2,
                    unitData.U.GetColumn(0) },
                new List<string> { "y1=correct_model", "y1=best_model", "y3=u1" },
                timeBase_s,
                "GainSched - Threshold at reasonable place - " + ver.ToString());
            Shared.DisablePlots();*/
        }

        [TestCase(3, 10, 0, Description= "identify gain and time constants, zero bias, thresholds are GIVEN")]
        [TestCase(3, 10, 1, Description = "same as ver1, except non-zero bias")]
        [TestCase(3, 10, 2, Description = "identify gain and time constants AND THRESHOLDS, zero bias, ")]
        [TestCase(3, 10, 3, Description = "same as ver2, except non-zero bias")]

        public void TimeConstant_TwoGains_TCAndThresholdFoundOk(double TimeConstant1_s, 
            double TimeConstant2_s, int ver)
        {
            int N = 300;
            var threshold_tol = 0.03;
            var operatingy_tol = 0.03;

            // Arrange
            var unitData = new UnitDataSet("test"); 
            double[] u1 = TimeSeriesCreator.ThreeSteps(N/5, N/3, N/2, N, -2, -1, 0, 1);
            double[] u2 = TimeSeriesCreator.ThreeSteps(3*N/5, 2*N/3, 4*N/5, N, 0, 1, 2, 3);
            double[] u = u1.Zip(u2, (x, y) => x+y).ToArray();
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u });
            unitData.U = U;
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            GainSchedParameters trueParams = new GainSchedParameters
            {
                TimeConstant_s = new double[] { TimeConstant1_s, TimeConstant2_s },
                TimeConstantThresholds = new double[] { 1.035 },
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { 1.035 },
                TimeDelay_s = 0,
            };


            // make the bias nonzero to test that the operating point estimation works.
            trueParams.OperatingPoint_Y = 1.34;

            GainSchedModel true_model = new GainSchedModel(trueParams, "Correct gain sched model");
            var truePlantSim = new PlantSimulator(new List<ISimulatableModel> { true_model });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(truePlantSim.AddExternalSignal(true_model, SignalType.External_U, (int)INDEX.FIRST), u);
            inputData.CreateTimestamps(timeBase_s);
            var trueModelIsSimulatable = truePlantSim.Simulate(inputData, out TimeSeriesDataSet trueSimData);
            SISOTests.CommonAsserts(inputData, trueSimData, truePlantSim);
            double[] y_meas = trueSimData.GetValues(true_model.GetID(), SignalType.Output_Y);
            unitData.Y_meas = y_meas;

            // Act
            GainSchedParameters est_params = new GainSchedParameters();
            if (ver == 2 || ver == 3)
            {
                // this will include determining thresholds, unlike below
                est_params = GainSchedIdentifier.Identify(unitData);
            }
            else if (ver == 0 || ver == 1)
            {
                var gsFittingSpecs = new GainSchedFittingSpecs();
                gsFittingSpecs.uGainThresholds = trueParams.LinearGainThresholds;
                gsFittingSpecs.uTimeConstantThresholds = trueParams.TimeConstantThresholds;
                est_params = GainSchedIdentifier.IdentifyForGivenThresholds(unitData, gsFittingSpecs);
            }
            GainSchedModel est_model = new GainSchedModel(est_params, "fitted model");
            var estPlantSim = new PlantSimulator(new List<ISimulatableModel> { est_model });
            inputData.Add(estPlantSim.AddExternalSignal(est_model, SignalType.External_U, (int)INDEX.FIRST), u);

            var IdentifiedisSimulatable = estPlantSim.Simulate(inputData, out TimeSeriesDataSet IdentifiedsimData);

            double[] simY2 = IdentifiedsimData.GetValues(est_model.GetID(), SignalType.Output_Y);
            // plot
            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                    y_meas,
                    simY2,
                    unitData.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_sim(est_model)", "y3=u1" },
                    timeBase_s,
                    "GainSchedTest ver_"+ver);
                Shared.DisablePlots();
            }
            Console.Write("Est timeconstant 1:" + TimeConstant1_s + " | 2:" + TimeConstant2_s + "\r\n");
            Console.Write("Est gain 1:" + est_params.LinearGains.ElementAt(0).ElementAt(0)
                + " | 2:" + est_params.LinearGains.ElementAt(1).ElementAt(0) + "\r\n");
            Console.Write("Op point 1:" + est_params.OperatingPoint_Y.ToString("F2")
                + " vs true value: " + trueParams.OperatingPoint_Y + "\r\n");
            Console.Write("threshold :" + est_params.LinearGainThresholds.First().ToString("F2")
                + " vs true value: " + trueParams.LinearGainThresholds.First() + "\r\n");



            // Asserts

            Assert.IsTrue(Math.Abs(est_params.LinearGainThresholds.First() - trueParams.LinearGainThresholds.First()) < threshold_tol);
            SISOTests.CommonAsserts(inputData, IdentifiedsimData, estPlantSim);

            int min_number_of_time_constants = Math.Min(est_params.TimeConstant_s.Length, trueParams.TimeConstant_s.Length);
            for (int k = 0; k < min_number_of_time_constants; k++)
            {
                Assert.That(est_params.TimeConstant_s[k].IsGreaterThanOrEqual(Math.Max(0,Math.Min(TimeConstant1_s,TimeConstant2_s) - TimeConstantAllowedDev_s)),
                "Too low time constant " + k.ToString());
                Assert.That(est_params.TimeConstant_s[k].IsLessThanOrEqual(Math.Max(TimeConstant1_s, TimeConstant2_s) + TimeConstantAllowedDev_s),
                "Too high time constant " + k.ToString());
            }
            Assert.That(Math.Abs(est_params.OperatingPoint_Y - trueParams.OperatingPoint_Y) < operatingy_tol);


        }

        [TestCase(1, 1.5)]
        /*[TestCase(2, 2.0)]
        [TestCase(3, 2.5)]
        [TestCase(4, 3.0)]
        [TestCase(5, 3.5)]
        [TestCase(6, 4.0)]*/
     //   [TestCase(7, 4.5)]
        public void GainAndThreshold_ThresholdsWithinUminAndUmax(int ver, double gain_sched_threshold)
        {
            int N = 250;
            // Arrange
            var unitData = new UnitDataSet("test"); /* Create an instance of TimeSeries with test data */
            double[] u1 = TimeSeriesCreator.ThreeSteps(N/5, N/3, N/2, N, -2, -1, 0, 1);
            double[] u2 = TimeSeriesCreator.ThreeSteps(3*N/5, 2*N/3, 4*N/5, N, 0, 1, 2, 3);
            double[] u = u1.Zip(u2, (x, y) => x+y).ToArray();
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u });
            unitData.U = U;
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);
        
            // reference model
            GainSchedParameters correct_gain_sched_parameters = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 3, 10 },
                TimeConstantThresholds = new double[] { gain_sched_threshold },
                LinearGains = new List<double[]> { new double[] { 1 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { gain_sched_threshold },
                TimeDelay_s = 0,
            };
            GainSchedModel correct_model = new GainSchedModel(correct_gain_sched_parameters, "Correct gain sched model");
            var correct_plantSim = new PlantSimulator(new List<ISimulatableModel> { correct_model });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(correct_plantSim.AddExternalSignal(correct_model, SignalType.External_U, (int)INDEX.FIRST), u);
            inputData.CreateTimestamps(timeBase_s);
            var CorrectisSimulatable = correct_plantSim.Simulate(inputData, out TimeSeriesDataSet CorrectsimData);
            SISOTests.CommonAsserts(inputData, CorrectsimData, correct_plantSim);
            double[] simY1 = CorrectsimData.GetValues(correct_model.GetID(), SignalType.Output_Y);
            unitData.Y_meas = simY1;

            // Act
            GainSchedParameters best_params = GainSchedIdentifier.Identify(unitData);
            GainSchedModel best_model = new GainSchedModel(best_params, "Best fitting model");
            var best_plantSim = new PlantSimulator(new List<ISimulatableModel> { best_model });
            inputData.Add(best_plantSim.AddExternalSignal(best_model, SignalType.External_U, (int)INDEX.FIRST), u);

            var IdentifiedisSimulatable = best_plantSim.Simulate(inputData, out TimeSeriesDataSet IdentifiedsimData);
            SISOTests.CommonAsserts(inputData, IdentifiedsimData, best_plantSim);
            double[] simY2 = IdentifiedsimData.GetValues(best_model.GetID(), SignalType.Output_Y);
            // Number of inputs can be determined from the TimeSeries object, assuming it provides a way to determine this
            int numberOfInputs = unitData.U.GetNColumns(); // Example property, replace with actual implementation

            // Assert
            int min_number_of_gains = Math.Min(best_params.LinearGainThresholds.Length, correct_gain_sched_parameters.LinearGainThresholds.Length);
            for (int k = 0; k < min_number_of_gains; k++)
            {
                Assert.That(best_params.LinearGainThresholds[k].IsGreaterThanOrEqual(u.First()),
                    "Linear gain threshold below lower bound (umin) " + ver.ToString());
                Assert.That(best_params.LinearGainThresholds[k].IsLessThanOrEqual(u.Last()),
                    "Linear gain threshold above upper bound (umax) " + ver.ToString());
            }

            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                    simY1,
                    simY2,
                    unitData.U.GetColumn(0) },
                    new List<string> { "y1=correct_model", "y1=best_model", "y3=u1" },
                    timeBase_s,
                    "GainSched - Threshold within bounds - " + ver.ToString());
                Shared.DisablePlots();
            }
        }
/*
        [TestCase(1, -0.5)]
        [TestCase(2, 2.0)]
        [TestCase(3, -1.5)]
        [TestCase(4, 3.0)]
        [TestCase(5, -3.5)]
        [TestCase(6, 4.0)]
        [TestCase(7, -4.5)]
        public void GainSchedIdentify_AllTimeConstantsArePositive(int ver, double gain_sched_threshold)
        {
            // Arrange
            var unitData = new UnitDataSet("test"); 
            double[] u1 = TimeSeriesCreator.ThreeSteps(N/5, N/3, N/2, N, -2, -1, 0, 1);
            double[] u2 = TimeSeriesCreator.ThreeSteps(3*N/5, 2*N/3, 4*N/5, N, 0, 1, 2, 3);
            double[] u = u1.Zip(u2, (x, y) => x+y).ToArray();
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u });
            unitData.U = U;
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            var gainSchedIdentifier = new GainSchedIdentifier();

            GainSchedParameters correct_gain_sched_parameters = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 15, 5 },
                TimeConstantThresholds = new double[] { gain_sched_threshold },
                LinearGains = new List<double[]> { new double[] { 1 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { gain_sched_threshold },
                TimeDelay_s = 0,
                Bias = 0
            };
            GainSchedModel correct_model = new GainSchedModel(correct_gain_sched_parameters, "Correct gain sched model");
            var correct_plantSim = new PlantSimulator(new List<ISimulatableModel> { correct_model });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(correct_plantSim.AddExternalSignal(correct_model, SignalType.External_U, (int)INDEX.FIRST), u);
            inputData.CreateTimestamps(timeBase_s);
            var CorrectisSimulatable = correct_plantSim.Simulate(inputData, out TimeSeriesDataSet CorrectsimData);
            SISOTests.CommonAsserts(inputData, CorrectsimData, correct_plantSim);
            double[] simY1 = CorrectsimData.GetValues(correct_model.GetID(), SignalType.Output_Y);
            unitData.Y_meas = simY1;

            // Act
            GainSchedParameters best_params = gainSchedIdentifier.GainSchedIdentify(unitData);
            GainSchedModel best_model = new GainSchedModel(best_params, "Best fitting model");
            var best_plantSim = new PlantSimulator(new List<ISimulatableModel> { best_model });
            inputData.Add(best_plantSim.AddExternalSignal(best_model, SignalType.External_U, (int)INDEX.FIRST), u);

            var IdentifiedisSimulatable = best_plantSim.Simulate(inputData, out TimeSeriesDataSet IdentifiedsimData);

            SISOTests.CommonAsserts(inputData, IdentifiedsimData, best_plantSim);

            double[] simY2 = IdentifiedsimData.GetValues(best_model.GetID(), SignalType.Output_Y);

            // Number of inputs can be determined from the TimeSeries object, assuming it provides a way to determine this
            int numberOfInputs = unitData.U.GetNColumns(); // Example property, replace with actual implementation
            
            // Assert
            int min_number_of_time_constants = best_params.TimeConstant_s.Length;
            for (int k = 0; k < min_number_of_time_constants; k++)
            {
                Assert.That(best_params.TimeConstant_s[k].IsGreaterThanOrEqual((double)0),
                    "Negative time constant - " + ver.ToString());
            }

        //    Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                    simY1,
                    simY2,
                    unitData.U.GetColumn(0) },
                new List<string> { "y1=correct_model", "y1=best_model", "y3=u1" },
                timeBase_s,
                "GainSched - Positive time constants - " + ver.ToString());
          //  Shared.DisablePlots();
        }
        */




    }
}
