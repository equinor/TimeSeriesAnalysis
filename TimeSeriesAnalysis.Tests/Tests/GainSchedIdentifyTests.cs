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
using Accord.Math.Transforms;

using System.Globalization;

namespace TimeSeriesAnalysis.Test.SysID
{
    public class GainSchedIdentifyTests
    {
        const int timeBase_s = 1;
        const double TimeConstantAllowedDev_s = 3.5;

        private void ConoleOutResult(GainSchedParameters trueParams, GainSchedParameters estParams)
        {
            string delimTxt = " vs true value: ";
            string accuracy = "F2";

            for (int i = 0; i < estParams.TimeConstant_s.Length; i++)
            {
                Console.Write("Est timeconstant "+(i+1)+":" + estParams.TimeConstant_s.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture) 
                    + delimTxt + trueParams.TimeConstant_s.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
            }
            for (int i = 0; i < estParams.LinearGains.Count; i++)
            {
                Console.Write("Est gain " + (i + 1) + ":" + estParams.LinearGains.ElementAt(i).ElementAt(0).ToString(accuracy, CultureInfo.InvariantCulture) 
                    + delimTxt + estParams.LinearGains.ElementAt(i).ElementAt(0).ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
            }

            Console.Write("Op point Y:" + estParams.OperatingPoint_Y.ToString(accuracy, CultureInfo.InvariantCulture)
                + delimTxt + trueParams.OperatingPoint_Y.ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
            Console.Write("threshold :" + estParams.LinearGainThresholds.First().ToString(accuracy)
                + delimTxt + trueParams.LinearGainThresholds.First().ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
            Console.Write("time-delay :" + estParams.TimeDelay_s.ToString(accuracy, CultureInfo.InvariantCulture)
                + delimTxt  + trueParams.TimeDelay_s.ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
        }

        // note that the tolerance seems to be linear with the noise in the data
        // five varying gains
        [TestCase(1, 0, 1, 0.0, Description = "Two steps for every threshold(five thresholds)")]
        [TestCase(1, 0, 10, 1.0, Description ="Two steps for every threshold(five thresholds)")]
        [TestCase(1, 0, 20, 2.0, Description = "Two steps for every threshold(five thresholds)")]
        // five gains that are the same (note that the tolerance can be much lower in this case)
        [TestCase(2, 0, 1, 0.0, Description = "Two steps for every threshold(five thresholds)")]
        [TestCase(2, 0, 5, 1.0, Description = "Two steps for every threshold(five thresholds)")]//note here that the tolerance can be set much lower!
        [TestCase(2, 0, 10, 2.0, Description = "Two steps for every threshold(five thresholds)")]//note here that the tolerance can be set much lower!
        public void FiveGains_CorrectGainsReturned(int ver, int expectedNumWarnings, double gainTolerancePrc, double noiseAmplitude )
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
                    OperatingPoint_U = 0,
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
                    OperatingPoint_U = 0,
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
            simData.AddNoiseToSignal(SignalNamer.GetSignalName(refModel.ID, SignalType.Output_Y, 0),noiseAmplitude,123);

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

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(7)]

        public void TimeDelay_TDEstOk(int timeDelaySamples)
        {
            double noiseAmp = 0.25;
            int N = 300;
            // Arrange
            var unitData = new UnitDataSet("test");
            double[] u1 = TimeSeriesCreator.ThreeSteps(N / 5, N / 3, N / 2, N, -2, -1, 0, 1);
            double[] u2 = TimeSeriesCreator.ThreeSteps(3 * N / 5, 2 * N / 3, 4 * N / 5, N, 0, 1, 2, 3);
            double[] u = u1.Zip(u2, (x, y) => x + y).ToArray();
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u });
            unitData.U = U;
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            double threshold =2;

            //reference model
            GainSchedParameters trueGSparams = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 3, 10 },
                TimeConstantThresholds = new double[] { threshold },
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { threshold },
                TimeDelay_s = timeBase_s* timeDelaySamples,
            };
            trueGSparams.OperatingPoint_Y = -1.34;

            GainSchedModel trueModel = new GainSchedModel(trueGSparams, "Correct gain sched model");
            var correct_plantSim = new PlantSimulator(new List<ISimulatableModel> { trueModel });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(correct_plantSim.AddExternalSignal(trueModel, SignalType.External_U, (int)INDEX.FIRST), u);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = correct_plantSim.Simulate(inputData, out TimeSeriesDataSet refSimData);
            SISOTests.CommonAsserts(inputData, refSimData, correct_plantSim);
            double[] simY1 = refSimData.GetValues(trueModel.GetID(), SignalType.Output_Y);
            unitData.Y_meas = (new Vec()).Add(Vec.Rand(simY1.Length, -noiseAmp, noiseAmp, 454), simY1);

            // Act
            GainSchedParameters idParams = GainSchedIdentifier.Identify(unitData);

            // plot
            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                        unitData.Y_meas ,
                        unitData.Y_sim,
                        unitData.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_ident", "y3=u1" },
                    timeBase_s,
                    "GainSched - timeconstant - ");
                Shared.DisablePlots();
            }

            ConoleOutResult(trueGSparams, idParams);

            // Assert
       /*     int min_number_of_gains = Math.Min(idParams.LinearGainThresholds.Length, trueGSparams.LinearGainThresholds.Length);
            for (int k = 0; k < min_number_of_gains; k++)
            {
                Console.WriteLine("identified threshold: " + idParams.LinearGainThresholds[k].ToString("F3") + "true threshold: " + trueGSparams.LinearGainThresholds[k].ToString("F3"));
                Assert.That(Math.Abs(idParams.LinearGainThresholds[k] - trueGSparams.LinearGainThresholds[k]), Is.LessThanOrEqualTo(linearGainTresholdTol),
                "There are too large differences in the linear gain threshold " + k.ToString());
            }*/
        }




        // note that the input varies from -2 to 4 here, so threshold beyond that are not identifiable, and at the edges they are also hard to identify.
        [TestCase(-0.5, 0.055)]
        [TestCase(-0.2, 0.055)]
        [TestCase(0.2, 0.045)]
        [TestCase(0.5, 0.04)]
        [TestCase(1.0, 0.01)]
        [TestCase(2.0, 0.015)]
        [TestCase(2.5, 0.015)]
        [TestCase(3.0, 0.015) ]

        public void TwoGainsVaryingTreshold_ThresholdEstOk(double gain_sched_threshold, double linearGainTresholdTol )
        {
            double noiseAmp = 0.25;
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
            GainSchedParameters trueGSparams = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 3, 10 },
                TimeConstantThresholds = new double[] { gain_sched_threshold },
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { gain_sched_threshold },
                TimeDelay_s = 0,
            };
            trueGSparams.OperatingPoint_Y = -1.34;

            GainSchedModel trueModel = new GainSchedModel(trueGSparams, "Correct gain sched model");
            var correct_plantSim = new PlantSimulator(new List<ISimulatableModel> { trueModel });
            var inputData = new TimeSeriesDataSet();
            inputData.Add(correct_plantSim.AddExternalSignal(trueModel, SignalType.External_U, (int)INDEX.FIRST), u);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = correct_plantSim.Simulate(inputData, out TimeSeriesDataSet refSimData);
            SISOTests.CommonAsserts(inputData, refSimData, correct_plantSim);
            double[] simY1 = refSimData.GetValues(trueModel.GetID(), SignalType.Output_Y);
            unitData.Y_meas = (new Vec()).Add(Vec.Rand(simY1.Length,-noiseAmp, noiseAmp, (int)Math.Ceiling(2*gain_sched_threshold+45)), simY1);

            // Act
            GainSchedParameters idParams = GainSchedIdentifier.Identify(unitData);

            // plot
            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                        unitData.Y_meas ,
                        unitData.Y_sim,
                        unitData.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_ident", "y3=u1" },
                    timeBase_s,
                    "GainSched - Threshold - " + gain_sched_threshold.ToString("F2"));
                Shared.DisablePlots();
            }

            // Assert
            int min_number_of_gains = Math.Min(idParams.LinearGainThresholds.Length, trueGSparams.LinearGainThresholds.Length);
            for (int k = 0; k < min_number_of_gains; k++)
            {
                Console.WriteLine("identified threshold: " + idParams.LinearGainThresholds[k].ToString("F3") + "true threshold: " + trueGSparams.LinearGainThresholds[k].ToString("F3"));
                Assert.That(Math.Abs(idParams.LinearGainThresholds[k] - trueGSparams.LinearGainThresholds[k]), Is.LessThanOrEqualTo(linearGainTresholdTol),
                "There are too large differences in the linear gain threshold " + k.ToString());
            }
        }

        [TestCase(3, 10, 0, Description= "identify gain and time constants, zero bias, thresholds are GIVEN")]
        [TestCase(3, 10, 1, Description = "same as ver1, except non-zero bias")]
        [TestCase(3, 10, 2, Description = "identify gain and time constants AND THRESHOLDS, zero bias, ")]
        [TestCase(3, 10, 3, Description = "same as ver2, except non-zero bias")]

        public void TwoGains_TCAndThresholdFoundOk(double TimeConstant1_s, 
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
    

    }
}
