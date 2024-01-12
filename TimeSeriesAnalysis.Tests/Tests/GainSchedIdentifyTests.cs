using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using System.Collections.Generic;

using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Test.PlantSimulations;

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
                TimeConstant_s = new double[] { 3, 10 },
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

            // Assert
            Assert.That(best_params.LinearGains.Count, Is.LessThanOrEqualTo(2),
                "The length of the LinearGains list must not exceed 2.");

            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simY1,
                simY2,
                unitData.U.GetColumn(0) },
                new List<string> { "y1=correct_model", "y1=best_model", "y3=u1" },
                timeBase_s,
                "GainSched split");
            Shared.DisablePlots();
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

            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simY1,
                unitData.U.GetColumn(0) },
                new List<string> { "y1=correct_model", "y3=u1" },
                timeBase_s,
                "GainSched - Max Threshold");
            Shared.DisablePlots();

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

            // Assert
            Assert.That(largest_gain_amplitude, Is.LessThanOrEqualTo(largest_correct_gain_amplitude),
                "The largest gain in the best fitting model cannot exceed the largest gain amplitude of the correct model");

        }


        [TestCase()]
        public void GainSchedIdentify_EstimatedTimeConstantsAreCloseToCorrectTimeConstants()
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
                TimeConstant_s = new double[] { 3, 10 },
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

            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simY1,
                unitData.U.GetColumn(0) },
                new List<string> { "y1=correct_model", "y3=u1" },
                timeBase_s,
                "GainSched - Time constant inspection");
            Shared.DisablePlots();

            // Act
            GainSchedParameters best_params = gainSchedIdentifier.GainSchedIdentify(unitData);

            // Assert
            for (int k = 0; k < correct_gain_sched_parameters.TimeConstant_s.Length; k++)
            {
                Assert.That(Math.Pow(best_params.TimeConstant_s[k] - correct_gain_sched_parameters.TimeConstant_s[k], 2), Is.LessThanOrEqualTo(1),
                "There are too large differences in the time constant on index " + k.ToString());
            }
        }
        // TODO: Additional test cases as needed...

        // Helper methods for creating test TimeSeries instances, gain scheduling slices, and other setup tasks...
    }
}