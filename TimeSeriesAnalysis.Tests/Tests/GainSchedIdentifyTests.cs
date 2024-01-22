using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using System.Collections.Generic;

using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Test.PlantSimulations;
using Accord;

namespace TimeSeriesAnalysis.Tests.Dynamic
{
    [TestFixture]
    public class GainSchedIdentifyTests
    {
        int timeBase_s = 1;
        int N = 2500;

        [TestCase()]
        public void GainSchedIdentify_ReturnsParametersWithNumberOfLinearGainsNotExceeding2()
        {
            // Arrange
            var unitData = new UnitDataSet("test"); /* Create an instance of TimeSeries with test data */
            double[] u1 = TimeSeriesCreator.ThreeSteps(N/5, N/3, N/2, N, 0, 1, 2, 3);
            double[] u2 = TimeSeriesCreator.ThreeSteps(3*N/5, 2*N/3, 4*N/5, N, 0, 1, 2, 3);
            double[] u = u1.Zip(u2, (x, y) => x+y).ToArray();
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u });
            unitData.U = U;
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            var gainSchedIdentifier = new GainSchedIdentifier();

            GainSchedParameters correct_gain_sched_parameters = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 3, 15 },
                TimeConstantThresholds = new double[] { 3.1 },
                LinearGains = new List<double[]> { new double[] { 1 }, new double[] { 5 } },
                LinearGainThresholds = new double[] { 3.1 },
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

            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simY1,
                simY2,
                unitData.U.GetColumn(0) },
                new List<string> { "y1=correct_model", "y1=best_model", "y3=u1" },
                timeBase_s,
                "GainSched split");
            Shared.DisablePlots();

            // Assert
            Assert.That(best_params.LinearGains.Count, Is.LessThanOrEqualTo(2),
                "The length of the LinearGains list must not exceed 2.");
        }

        [TestCase()]
        public void GainSchedIdentify_GainsNotLargerThanTheBiggestPossibleGain()
        {
            // Arrange
            var unitData = new UnitDataSet("test"); /* Create an instance of TimeSeries with test data */
            double[] u1 = TimeSeriesCreator.ThreeSteps(N/5, N/3, N/2, N, 0, 1, 2, 3);
            double[] u2 = TimeSeriesCreator.ThreeSteps(3*N/5, 2*N/3, 4*N/5, N, 0, 1, 2, 3);
            double[] u = u1.Zip(u2, (x, y) => x+y).ToArray();
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u });
            unitData.U = U;
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            var gainSchedIdentifier = new GainSchedIdentifier();

            GainSchedParameters correct_gain_sched_parameters = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 10 },
                TimeConstantThresholds = new double[] { },
                LinearGains = new List<double[]> { new double[] { 1 }, new double[] { 6 } },
                LinearGainThresholds = new double[] { 3.1 },
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
            double current_abs_value = 0;
            double largest_gain_amplitude = 0;
            for (int k = 0; k < best_params.LinearGains.Count; k++)
            {
                current_abs_value = Math.Sqrt(best_params.LinearGains[k][0] *best_params.LinearGains[k][0]);
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

            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simY1,
                unitData.U.GetColumn(0) },
                new List<string> { "y1=correct_model", "y3=u1" },
                timeBase_s,
                "GainSched - Max Threshold");
            Shared.DisablePlots();

            // Assert
            Assert.That(largest_gain_amplitude, Is.LessThanOrEqualTo(largest_correct_gain_amplitude),
                "The largest gain in the best fitting model cannot exceed the largest gain amplitude of the correct model");

        }

        [TestCase(1, -1.5)]
        [TestCase(2, -1.0)]
        [TestCase(3, -0.5)]
        [TestCase(4, 1.0)]
        [TestCase(5, 2.5)]
        [TestCase(6, 3.0)]
        [TestCase(7, 4.0)]
        public void GainSchedIdentify_LinearGainThresholdAtReasonablePlace(int ver, double gain_sched_threshold)
        {
            // Arrange
            var unitData = new UnitDataSet("test"); /* Create an instance of TimeSeries with test data */
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
                TimeConstant_s = new double[] { 3, 10 },
                TimeConstantThresholds = new double[] { gain_sched_threshold },
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
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
            int min_number_of_gains = Math.Min(best_params.LinearGainThresholds.Length, correct_gain_sched_parameters.LinearGainThresholds.Length);
            for (int k = 0; k < min_number_of_gains; k++)
            {
                Assert.That(Math.Pow(best_params.LinearGainThresholds[k] - correct_gain_sched_parameters.LinearGainThresholds[k], 2), Is.LessThanOrEqualTo(0.5),
                "There are too large differences in the linear gain threshold " + k.ToString());
            }

            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                    simY1,
                    simY2,
                    unitData.U.GetColumn(0) },
                new List<string> { "y1=correct_model", "y1=best_model", "y3=u1" },
                timeBase_s,
                "GainSched - Threshold at reasonable place - " + ver.ToString());
            Shared.DisablePlots();
        }

        [TestCase(1, 3, 10)]
        [TestCase(2, 3, 15)]
        [TestCase(3, 15, 10)]
        [TestCase(4, 20, 30)]
        [TestCase(5, 1, 35)]
        [TestCase(6, 38, 40)]
        [TestCase(7, 40, 20)]
        public void GainSchedIdentify_TimeConstantsWithinReasonableRange(int ver, double t1, double t2)
        {
            // Arrange
            var unitData = new UnitDataSet("test"); /* Create an instance of TimeSeries with test data */
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
                TimeConstant_s = new double[] { t1, t2 },
                TimeConstantThresholds = new double[] { 1.1 },
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { 1.1 },
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
            int min_number_of_time_constants = Math.Min(best_params.TimeConstant_s.Length, correct_gain_sched_parameters.TimeConstant_s.Length);
            for (int k = 0; k < min_number_of_time_constants; k++)
            {
                Assert.That(best_params.TimeConstant_s[k].IsGreaterThanOrEqual(Math.Max(0,Math.Min(t1,t2) - 0.5)),
                "Too low time constant " + k.ToString());
                Assert.That(best_params.TimeConstant_s[k].IsLessThanOrEqual(Math.Max(t1, t2) + 0.5),
                "Too high time constant " + k.ToString());
            }

            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                    simY1,
                    simY2,
                    unitData.U.GetColumn(0) },
                new List<string> { "y1=correct_model", "y1=best_model", "y3=u1" },
                timeBase_s,
                "GainSched - Threshold at reasonable place - " + ver.ToString());
            Shared.DisablePlots();
        }

        [TestCase(1, 1.5)]
        [TestCase(2, 2.0)]
        [TestCase(3, 2.5)]
        [TestCase(4, 3.0)]
        [TestCase(5, 3.5)]
        [TestCase(6, 4.0)]
        [TestCase(7, 4.5)]
        public void GainSchedIdentify_ThresholdsWithinUminAndUmax(int ver, double gain_sched_threshold)
        {
            // Arrange
            var unitData = new UnitDataSet("test"); /* Create an instance of TimeSeries with test data */
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
                TimeConstant_s = new double[] { 3, 10 },
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
            int min_number_of_gains = Math.Min(best_params.LinearGainThresholds.Length, correct_gain_sched_parameters.LinearGainThresholds.Length);
            for (int k = 0; k < min_number_of_gains; k++)
            {
                Assert.That(best_params.LinearGainThresholds[k].IsGreaterThanOrEqual(u.First()),
                    "Linear gain threshold below lower bound (umin) " + ver.ToString());
                Assert.That(best_params.LinearGainThresholds[k].IsLessThanOrEqual(u.Last()),
                    "Linear gain threshold above upper bound (umax) " + ver.ToString());
            }

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
            var unitData = new UnitDataSet("test"); /* Create an instance of TimeSeries with test data */
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

            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                    simY1,
                    simY2,
                    unitData.U.GetColumn(0) },
                new List<string> { "y1=correct_model", "y1=best_model", "y3=u1" },
                timeBase_s,
                "GainSched - Positive time constants - " + ver.ToString());
            Shared.DisablePlots();
        }
        // TODO: Additional test cases as needed...

        // Helper methods for creating test TimeSeries instances, gain scheduling slices, and other setup tasks...
    }
}
