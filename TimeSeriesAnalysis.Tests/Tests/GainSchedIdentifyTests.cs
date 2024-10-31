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
using System.Runtime.ConstrainedExecution;

namespace TimeSeriesAnalysis.Test.SysID
{
    public class GainSchedIdentifyTests
    {
        const int timeBase_s = 1;
        const double TimeConstantAllowedDev_s = 3.5;

        private void ConoleOutResult(GainSchedParameters trueParams, GainSchedParameters estParams)
        {
            string delimTxt = " vs ";
            string accuracy = "F2";

            Console.Write("[estimate]" + delimTxt + "[true]"+"\r\n");

            for (int i = 0; i < estParams.TimeConstant_s.Length; i++)
            {
                if (trueParams.TimeConstant_s != null)
                {
                    Console.Write("Tc " + (i + 1) + ": " + estParams.TimeConstant_s.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture)
                        + delimTxt + trueParams.TimeConstant_s.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
                }
                else
                {
                    Console.Write("Tc " + (i + 1) + ": " + estParams.TimeConstant_s.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture)
                    + delimTxt + "[null]" + "\r\n");
                }
            }
            for (int i = 0; i < estParams.LinearGains.Count; i++)
            {
                Console.Write("gain " + (i + 1) + ": " + estParams.LinearGains.ElementAt(i).ElementAt(0).ToString(accuracy, CultureInfo.InvariantCulture) 
                    + delimTxt + trueParams.LinearGains.ElementAt(i).ElementAt(0).ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
            }

            Console.Write("op point Y: " + estParams.OperatingPoint_Y.ToString(accuracy, CultureInfo.InvariantCulture)
                + delimTxt + trueParams.OperatingPoint_Y.ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");

            Console.Write("op point U: " + estParams.OperatingPoint_U.ToString(accuracy, CultureInfo.InvariantCulture)
                 + delimTxt + trueParams.OperatingPoint_U.ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");

            if (estParams.LinearGainThresholds.Count() > 0)
            {
                Console.Write("threshold : " + estParams.LinearGainThresholds.First().ToString(accuracy,CultureInfo.InvariantCulture)
                   + delimTxt + trueParams.LinearGainThresholds.First().ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
            }
            else
            {
                Console.Write("threshold : " +"[none!]"  + delimTxt + trueParams.LinearGainThresholds.First().ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
            }
            Console.Write("time-delay : " + estParams.TimeDelay_s.ToString(accuracy, CultureInfo.InvariantCulture)
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
        public void FiveGains_StepChange_CorrectGainsReturned(int ver, int expectedNumWarnings, double gainTolerancePrc, double noiseAmplitude )
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


            var input = (TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 0, 1, 2, 3).
              Concat(TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 4, 5, 6, 7)).
              Concat(TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 8, 9, 10, 11)).
              Concat(TimeSeriesCreator.ThreeSteps(N / 4, N * 2 / 4, N * 3 / 4, N, 12, 13, 14, 15))
             .ToArray());

            var dataSet = new UnitDataSet();
            dataSet.SetU(input);
            dataSet.CreateTimeStamps(timeBase_s);
            (bool isOk, double[] y_sim) = PlantSimulator.SimulateSingleToYmeas(dataSet, refModel,noiseAmplitude);

            var gsModel = GainSchedIdentifier.IdentifyForGivenThresholds(dataSet, gsFittingSpecs);

            // console out
            ConoleOutResult(refParams, gsModel.GetModelParameters());
            Console.WriteLine(gsModel);

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

                gsModel.SetOutputID("y_meas");
                gsModel.SetInputIDs((new List<string> { "u_1" }).ToArray());
                GainSchedModel referenceModel = new GainSchedModel(refParams,"ref_model");
                referenceModel.SetInputIDs(gsModel.GetModelInputIDs());
                referenceModel.SetOutputID(gsModel.GetOutputID());
                PlotGain.PlotSteadyState(gsModel, referenceModel, "steady state gains", new double[] { 0}, new double[] { 15});
                PlotGain.PlotGainSched(gsModel, referenceModel,"gain-scheduling");
                Shared.DisablePlots();
            }
            // asserts
            Assert.IsTrue(gsModel.GetModelParameters().Fitting.WasAbleToIdentify);
            Assert.AreEqual(expectedNumWarnings, gsModel.GetModelParameters().GetWarningList().Count());

            for (int i = 0; i < refParams.LinearGains.Count; i++)
            {
                DiffLessThan(refParams.LinearGains[i][0], gsModel.GetModelParameters().LinearGains[i][0], gainTolerancePrc,i);
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

        public void TimeDelay_StepChange_TDEstOk(int timeDelaySamples)
        {
            double td_tol = 0.04;

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

            GainSchedModel trueModel = new GainSchedModel(trueGSparams, "true gain sched model");
            PlantSimulator.SimulateSingleToYmeas(unitData, trueModel, noiseAmp, 454);

            // Act
            var idModel = GainSchedIdentifier.Identify(unitData);

            // plot
            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                        unitData.Y_meas ,
                     //   unitData.Y_sim,
                        unitData.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y3=u1" },
                    timeBase_s,
                    "GainSched - TimeDelay - ");
                Shared.DisablePlots();
            }

            ConoleOutResult(trueGSparams, idModel.GetModelParameters());
           Assert.That(Math.Abs(idModel.GetModelParameters().TimeDelay_s - trueGSparams.TimeDelay_s)<td_tol );
        }


        // note that the input varies from -2 to 4 here, so threshold beyond that are not identifiable, and at the edges they are also hard to identify.
        [TestCase(20, 60,true)]
        [TestCase(20, 60,false)]


        public void NonzeroOperatingPointU_EstimatesStillOk(double uOperatingPoint, double yOperatingPoint,bool estimateThresholds)
        {
            double noiseAmp = 0.0;
            int N = 50;
            double gainSchedThreshold = uOperatingPoint;

            // Arrange
            var unitData = new UnitDataSet();
            double[] u1 = TimeSeriesCreator.ThreeSteps(N / 5, N / 3, N / 2, N, uOperatingPoint, uOperatingPoint + 10, uOperatingPoint, uOperatingPoint - 10);
            double[] u = u1; //u1.Zip(u2, (x, y) => x + y).ToArray();
            unitData.U = Array2D<double>.CreateFromList(new List<double[]> { u });
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(new DateTime(2000, 1, 1), timeBase_s, N);

            //reference model
            GainSchedParameters trueGSparams = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 0, 0 },
                TimeConstantThresholds = new double[] { gainSchedThreshold },
                LinearGains = new List<double[]> { new double[] { 2}, new double[] { 4 } },
                LinearGainThresholds = new double[] { gainSchedThreshold },
                TimeDelay_s = 0,
            };
            trueGSparams.OperatingPoint_Y = yOperatingPoint;
            trueGSparams.OperatingPoint_U = uOperatingPoint;

            GainSchedModel trueModel = new GainSchedModel(trueGSparams, "True Model");
            PlantSimulator.SimulateSingleToYmeas(unitData, trueModel, noiseAmp, (int)Math.Ceiling(2 * gainSchedThreshold + 45));

            // Act
            var idModel = new GainSchedModel();
            if (estimateThresholds)
            {
                idModel = GainSchedIdentifier.Identify(unitData);
            }
            else
            {
                GainSchedFittingSpecs gsFittingSpecs = new GainSchedFittingSpecs { uGainThresholds = new double[] { gainSchedThreshold } };
                idModel = GainSchedIdentifier.IdentifyForGivenThresholds(unitData, gsFittingSpecs);
            }
            


            // plot
            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                        unitData.Y_meas ,
                        unitData.Y_sim,
                        unitData.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_sim", "y3=u1" },
                    timeBase_s,
                    "NonzeroOpPointIdent_U="+ uOperatingPoint);

                PlotGain.PlotSteadyState(trueModel, idModel, "NonzeroOperatingPointU");

                Shared.DisablePlots();
            }
            ConoleOutResult(trueGSparams, idModel.GetModelParameters());
        }
        [TestCase(true, Explicit = true,Description="work in progress")]
        [TestCase(false, Explicit = true, Description = "work in progress")]
        public void TwoGains_RampChange(bool useIdentify)
        {

            //    var tolerance = 0.2;
            // Arrange
            GainSchedParameters trueGSparams = new GainSchedParameters
            {
                TimeConstant_s = new double[] { 40 },
                TimeConstantThresholds = new double[] {  },
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { 30 },
                TimeDelay_s = 0,
            };
            int N = 300;
            int padBeginIdx = 10;
            int padEndIdx = 40;
            double[]  input = TimeSeriesCreator.Ramp(N, 100, 0, padBeginIdx, padEndIdx);

            GainSchedModel trueModel = new GainSchedModel(trueGSparams);
        
            var unitData = new UnitDataSet();
            unitData.SetU(input);
            unitData.CreateTimeStamps(timeBase_s);
            (bool isOk, double[] y_meas)= PlantSimulator.SimulateSingleToYmeas(unitData,trueModel,0);

            GainSchedModel idModel = new GainSchedModel();
            if (useIdentify)
            {
                idModel = GainSchedIdentifier.Identify(unitData);
            }
            else
            {
                var gsFittingSpecs = new GainSchedFittingSpecs()
                {
                    uGainThresholds = new double[] { 30 }
                };
                idModel = GainSchedIdentifier.IdentifyForGivenThresholds(unitData, gsFittingSpecs);
            }
            Console.WriteLine(idModel);

            bool doPlot = true;// should be false unless debugging
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                     y_meas,
                     unitData.Y_sim,
                     unitData.U.GetColumn(0),
                     },
                    new List<string> { "y1=y_meas", "y1=y_sim", "y3=u1" },
                    timeBase_s, "TwoGains_RampChange");
                //   TestContext.CurrentContext.Test.Name.Replace(',', '_').Replace('(','_').Replace(')','_'));// TestContext.CurrentContext.Test.Name
                Shared.DisablePlots();
            }









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

        public void TwoGains_StepChange_ThresholdEstOk(double gain_sched_threshold, double linearGainTresholdTol )
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
            PlantSimulator.SimulateSingleToYmeas(unitData,trueModel, noiseAmp, (int)Math.Ceiling(2 * gain_sched_threshold + 45));

            // Act
            var idModel = GainSchedIdentifier.Identify(unitData);

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
            ConoleOutResult(trueGSparams, idModel.GetModelParameters());
            // Assert
            int min_number_of_gains = Math.Min(idModel.GetModelParameters().LinearGainThresholds.Length, trueGSparams.LinearGainThresholds.Length);
            for (int k = 0; k < min_number_of_gains; k++)
            {
                Assert.That(Math.Abs(idModel.GetModelParameters().LinearGainThresholds[k] - trueGSparams.LinearGainThresholds[k]), Is.LessThanOrEqualTo(linearGainTresholdTol),
                "There are too large differences in the linear gain threshold " + k.ToString());
            }
        }
        [Test]
        public void ChangeOperatingPoint_YsimUnchanged()
        {
            double y_tol = 0.01;

            int N = 50;
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
                TimeConstant_s = new double[] { 4, 12 },
                TimeConstantThresholds = new double[] { 1.035 },
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { 1.035 },
                TimeDelay_s = 0,
            };

            // make the bias nonzero to test that the operating point estimation works.
            trueParams.OperatingPoint_Y = 1.34;
            GainSchedModel trueModel = new GainSchedModel(trueParams, "Correct gain sched model");
            PlantSimulator.SimulateSingle(unitData, trueModel,true);

            var alteredIdModel = (GainSchedModel)trueModel.Clone("altered");
            alteredIdModel.SetOperatingPoint(3);
            (bool isOk3, double[] simY2) = PlantSimulator.SimulateSingle(unitData, alteredIdModel, false);

            // plot
            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                    unitData.Y_sim,
                    simY2,
                    unitData.U.GetColumn(0) },
                    new List<string> { "y1=y_meas",  "y1=y_altered", "y3=u1" },
                    timeBase_s,
                    "GainSchedTest ver_");

                PlotGain.PlotSteadyState(trueModel, alteredIdModel, "ChangeOperatingPoint_YsimUnchanged" , 
                    new double[] { (new Vec()).Min(u) }, new double[] { (new Vec()).Max(u) });
                Shared.DisablePlots();
            }
            ConoleOutResult(trueParams, alteredIdModel.GetModelParameters());
            // Asserts
            Assert.IsTrue((new Vec()).Max((new Vec()).Subtract(unitData.Y_sim, simY2))<y_tol);
        }


        [TestCase(3, 10, 0, Description= "identify gain and time constants, zero bias, thresholds are GIVEN")]
        [TestCase(3, 10, 1, Description = "same as ver1, except non-zero bias")]
        [TestCase(3, 10, 2, Description = "identify gain and time constants AND THRESHOLDS, zero bias, ")]
        [TestCase(3, 10, 3, Description = "same as ver2, except non-zero bias")]

        public void TwoGains_StepChange_TCAndThresholdFoundOk(double TimeConstant1_s, 
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
            GainSchedModel trueModel = new GainSchedModel(trueParams, "Correct gain sched model");

            PlantSimulator.SimulateSingleToYmeas(unitData, trueModel, 0,123);

            // Act
            var idModel = new GainSchedModel();
            if (ver == 2 || ver == 3)
            {
                // this will include determining thresholds, unlike below
                idModel = GainSchedIdentifier.Identify(unitData);
            }
            else if (ver == 0 || ver == 1)
            {
                var gsFittingSpecs = new GainSchedFittingSpecs();
                gsFittingSpecs.uGainThresholds = trueParams.LinearGainThresholds;
                gsFittingSpecs.uTimeConstantThresholds = trueParams.TimeConstantThresholds;
                idModel = GainSchedIdentifier.IdentifyForGivenThresholds(unitData, gsFittingSpecs);
            }
            (bool isOk2, double[] simY2) =  PlantSimulator.SimulateSingle(unitData, idModel, false);

            var alteredIdModel = (GainSchedModel)idModel.Clone("altered");
            alteredIdModel.SetOperatingPoint(3);
            (bool isOk3, double[] simY3) = PlantSimulator.SimulateSingle(unitData, alteredIdModel, false);

            // plot
            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                    unitData.Y_meas,
                    simY2,
                    simY3,
                    unitData.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_sim(est_model)", "y1=y_altered", "y3=u1" },
                    timeBase_s,
                    "GainSchedTest ver_"+ver);

                PlotGain.PlotSteadyState(idModel, alteredIdModel, "twogains"+ver, 
                    new double[] { (new Vec()).Min(u) }, new double[] { (new Vec()).Max(u) });

                Shared.DisablePlots();
            }

            ConoleOutResult(trueParams, idModel.GetModelParameters());

            // Asserts
            Assert.IsTrue(Math.Abs(idModel.GetModelParameters().LinearGainThresholds.First() - trueParams.LinearGainThresholds.First()) < threshold_tol);
            //SISOTests.CommonAsserts(inputData, IdentifiedsimData, estPlantSim);

            int min_number_of_time_constants = Math.Min(idModel.GetModelParameters().TimeConstant_s.Length, trueParams.TimeConstant_s.Length);
            for (int k = 0; k < min_number_of_time_constants; k++)
            {
                Assert.That(idModel.GetModelParameters().TimeConstant_s[k].IsGreaterThanOrEqual(Math.Max(0,Math.Min(TimeConstant1_s,TimeConstant2_s) - TimeConstantAllowedDev_s)),
                "Too low time constant " + k.ToString());
                Assert.That(idModel.GetModelParameters().TimeConstant_s[k].IsLessThanOrEqual(Math.Max(TimeConstant1_s, TimeConstant2_s) + TimeConstantAllowedDev_s),
                "Too high time constant " + k.ToString());
            }
//            Assert.That(Math.Abs(idModel.GetModelParameters().OperatingPoint_Y - trueParams.OperatingPoint_Y) < operatingy_tol);
        }
    

    }
}
