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
using System.Linq;
using Accord.Statistics.Kernels;

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

            Console.Write("[estimate]" + delimTxt + "[true]" + "\r\n");

            // time-delay
            Console.Write("t_d : " + estParams.TimeDelay_s.ToString(accuracy, CultureInfo.InvariantCulture)
                 + delimTxt + trueParams.TimeDelay_s.ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");

            // time-constant
            for (int i = 0; i < estParams.TimeConstant_s.Length; i++)
            {
                if (trueParams.TimeConstant_s != null)
                {
                    if (estParams.TimeConstant_s != null )
                    {
                        if (trueParams.TimeConstant_s.Length >= i+1 )
                        {
                            Console.Write("Tc " + (i + 1) + ": " + estParams.TimeConstant_s.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture)
                                + delimTxt + trueParams.TimeConstant_s.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
                        }
                    }
                    else
                    {
                        Console.Write("Tc " + (i + 1) + ": " + "[null]"
                    + delimTxt + trueParams.TimeConstant_s.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
                    }
                }
                else
                {
                    Console.Write("Tc " + (i + 1) + ": " + estParams.TimeConstant_s.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture)
                    + delimTxt + "[null]" + "\r\n");
                }
            }
            // tc threshold
            if (estParams.TimeConstantThresholds != null)
            {
                if (estParams.TimeConstantThresholds.Count() > 0)
                {
                    Console.Write("Tc threshold : " + estParams.TimeConstantThresholds.First().ToString(accuracy, CultureInfo.InvariantCulture)
                       + delimTxt + trueParams.LinearGainThresholds.First().ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
                }
                else
                {
                    Console.Write("Tc threshold : " + "[none!]" + delimTxt + trueParams.TimeConstantThresholds.First().ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
                }
            }
            else
            {
                Console.WriteLine("Tc threshold : " + "null");
            }

            //Linear gains
            for (int i = 0; i < estParams.LinearGains.Count; i++)
            {
                Console.Write("gain " + (i + 1) + ": " + estParams.LinearGains.ElementAt(i).ElementAt(0).ToString(accuracy, CultureInfo.InvariantCulture) 
                    + delimTxt + trueParams.LinearGains.ElementAt(i).ElementAt(0).ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
            }
            // linear gains threshold
            if (estParams.LinearGainThresholds.Count() > 0)
            {
                for (int i = 0; i < Math.Min(estParams.LinearGainThresholds.Count(), trueParams.LinearGainThresholds.Count()); i++)
                {
                    Console.Write("gain threshold "+i+": " + estParams.LinearGainThresholds.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture)
                       + delimTxt + trueParams.LinearGainThresholds.ElementAt(i).ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
                }
            }
            else
            {
                Console.Write("gain threshold : " + "[none!]" + delimTxt + trueParams.LinearGainThresholds.First().ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");
            }

            Console.Write("op point Y: " + estParams.GetOperatingPointY().ToString(accuracy, CultureInfo.InvariantCulture)
                + delimTxt + trueParams.GetOperatingPointY().ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");

            Console.Write("op point U: " + estParams.GetOperatingPointU().ToString(accuracy, CultureInfo.InvariantCulture)
                 + delimTxt + trueParams.GetOperatingPointU().ToString(accuracy, CultureInfo.InvariantCulture) + "\r\n");

        }

        // note that the tolerance seems to be linear with the noise in the data
        // five varying gains
        [TestCase(1, 0, 1, 0.0,99, Description = "Two steps for every threshold(five thresholds)")]
        [TestCase(1, 0, 10, 1.0, 95, Description ="Two steps for every threshold(five thresholds)")]
        [TestCase(1, 0, 20, 2.0, 94, Description = "Two steps for every threshold(five thresholds)")]
        // five gains that are the same (note that the tolerance can be much lower in this case)
        [TestCase(2, 0, 1, 0.0, 99, Description = "Two steps for every threshold(five thresholds)")]
        [TestCase(2, 0, 5, 1.0, 92, Description = "Two steps for every threshold(five thresholds)")]//note here that the tolerance can be set much lower!
        [TestCase(2, 0, 10, 2.0, 85, Description = "Two steps for every threshold(five thresholds)")]//note here that the tolerance can be set much lower!
        public void FiveGainsStatic_StepChange_ForGivenThresholds_CorrectGains(int ver, int expectedNumWarnings, double gainTolerancePrc, double noiseAmplitude,
            double fitScoreReq)
        {
            const int N = 100;//Note, that the actual dataset is four times this value.
            GainSchedParameters refParams = new GainSchedParameters(); 
            // below: 5 gains
            if (ver == 1)
            {
                refParams = new GainSchedParameters( 0, 4)
                {
                    TimeConstant_s = null,
                    TimeConstantThresholds = null,
                    LinearGains = new List<double[]> { new double[] { 0.5 }, new double[] { 1 }, new double[] { 3 }, new double[] { 4.5 }, new double[] { 6 }, new double[] { 9 } },
                    LinearGainThresholds = new double[] { 2.5, 4.5, 6.5, 8.5, 10.5 },
                    TimeDelay_s = 0,
                    GainSchedParameterIndex = 0
                };
            }
            else if (ver == 2)
            {
                refParams = new GainSchedParameters(0,4) // let all gains be the same, and check that estimation does not falsely suggest a gain-scheduled model
                {
                    TimeConstant_s = null,
                    TimeConstantThresholds = null,
                    LinearGains = new List<double[]> { new double[] { 2 }, new double[] { 2 }, new double[] { 2}, new double[] { 2 }, new double[] { 2 }, new double[] { 2 } },
                    LinearGainThresholds = new double[] { 2.5, 4.5, 6.5, 8.5, 10.5 },
                    TimeDelay_s = 0,
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
            PlantSimulatorHelper.SimulateSingleToYmeas(dataSet, refModel,noiseAmplitude);

            var idModel = GainSchedIdentifier.IdentifyForGivenThresholds(dataSet, gsFittingSpecs);

            // console out
            ConoleOutResult(refParams, idModel.GetModelParameters());
            Console.WriteLine(idModel);

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
                    TestContext.CurrentContext.Test.Name);

                idModel.SetOutputID("y_meas");
                idModel.SetInputIDs((new List<string> { "u_1" }).ToArray());
                GainSchedModel referenceModel = new GainSchedModel(refParams,"ref_model");
                referenceModel.SetInputIDs(idModel.GetModelInputIDs());
                referenceModel.SetOutputID(idModel.GetOutputID());
                PlotGain.PlotSteadyState(idModel, referenceModel, "steady state gains", new double[] { 0}, new double[] { 15});
                PlotGain.PlotGainSched(idModel, referenceModel,"gain-scheduling");
                Shared.DisablePlots();
            }
            // asserts
            Assert.IsTrue(idModel.GetModelParameters().Fitting.WasAbleToIdentify);
            Assert.AreEqual(expectedNumWarnings, idModel.GetModelParameters().GetWarningList().Count());

            for (int i = 0; i < refParams.LinearGains.Count; i++)
            {
                DiffLessThan(refParams.LinearGains[i][0], idModel.GetModelParameters().LinearGains[i][0], gainTolerancePrc,i);
            }
            Assert.That(idModel.GetModelParameters().Fitting.FitScorePrc > fitScoreReq, "Tripwire: FitScore should not fall past what was observed during design of the test ");


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


        [TestCase( 99, Explicit =true, Category = "NotWorking_AcceptanceTest")]

        public void IgnoreIndicesInMiddleOfDataset_ResultShouldStillBeGood( double fitScoreReq)
        {
            double noiseAmp = 0.05;
            int N = 500;
            double td_tol = 0.04;
            double tc_tol = 2.5;

            // use for dataset 1 and 3:
            GainSchedParameters trueGSparams = new GainSchedParameters(0, -1.34)
            {
                TimeConstant_s = new double[] { 10 },
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { 2 },
                TimeDelay_s = timeBase_s * 0,
            };

            // Dataset 1 
            var unitData1 = new UnitDataSet("dataset1");
            {
                double[] u1 = TimeSeriesCreator.ThreeSteps(N * 1 / 5, N * 2 / 5, N * 3 / 5, N, -2, -1, 0, 1);
                unitData1.SetU(u1);
                unitData1.CreateTimeStamps(timeBase_s);

                GainSchedModel trueModel = new GainSchedModel(trueGSparams, "true gain sched model");
                PlantSimulatorHelper.SimulateSingleToYmeas(unitData1, trueModel, noiseAmp, 454);
            }
            // Dataset 2 
            var unitData2 = new UnitDataSet("dataset2");
            {
                double[] u2 = TimeSeriesCreator.ThreeSteps(N * 1 / 5, N * 2 / 5, N * 3 / 5, N, -2, -1, 0, 1);
                unitData2.SetU(u2);
                unitData2.CreateTimeStamps(timeBase_s);
                GainSchedParameters otherGsParams = new GainSchedParameters(5, -1.34)
                {
                    TimeConstant_s = new double[] { 35 },
                    TimeConstantThresholds = null,
                    LinearGains = new List<double[]> { new double[] { -1 }, new double[] { 2 } },
                    LinearGainThresholds = new double[] { 2 },
                    TimeDelay_s = timeBase_s * 0,
                };
                GainSchedModel trueModel = new GainSchedModel(otherGsParams, "true gain sched model");
                PlantSimulatorHelper.SimulateSingleToYmeas(unitData2, trueModel, noiseAmp, 454);
            }
            // dataset 3
            var unitData3 = new UnitDataSet("dataset3");
            {
                double[] u3 = TimeSeriesCreator.ThreeSteps(N * 1 / 5, N * 2 / 5, N * 3 / 5, N, 0, 1, 2, 3);
                unitData3.SetU(u3);
                unitData3.CreateTimeStamps(timeBase_s);
                GainSchedModel trueModel = new GainSchedModel(trueGSparams, "true gain sched model");
                PlantSimulatorHelper.SimulateSingleToYmeas(unitData3, trueModel, noiseAmp, 454);
            }

            var joinedDataSet = new UnitDataSet(unitData1);
            joinedDataSet.Concat(unitData2);
            joinedDataSet.Concat(unitData3);
            //joinedDataSet.IndicesToIgnore =Index.MakeIndexArray(N-2,N*2+1).ToList();

            // Act
            var idModel = GainSchedIdentifier.Identify(joinedDataSet);

            // plot
            bool doPlot = true;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                        joinedDataSet.Y_meas ,
                        joinedDataSet.Y_sim,
                        joinedDataSet.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_sim", "y3=u1" },
                    timeBase_s,
                    TestContext.CurrentContext.Test.Name);
                Shared.DisablePlots();
            }
            ConoleOutResult(trueGSparams, idModel.GetModelParameters());
            Console.WriteLine(idModel);

            // assert
            Assert.That(Math.Abs(idModel.GetModelParameters().TimeDelay_s - trueGSparams.TimeDelay_s) < td_tol, "time delay too far off");
            Assert.That(Math.Abs(idModel.GetModelParameters().TimeConstant_s.First() - trueGSparams.TimeConstant_s.First()) < tc_tol, "time constant too far off!");
            Assert.That(idModel.GetModelParameters().Fitting.FitScorePrc > fitScoreReq, "Tripwire: FitScore should not fall past what was observed during design of the test ");
        }

        [TestCase(0,99)]
        [TestCase(2,97)]
        [TestCase(5,94)]

        public void TimeDelaySingleTc_StepChange_Identify_TcAndTdEstOk(int timeDelaySamples, double fitScoreReq)
        {
            double td_tol = 0.04;
            double tc_tol = 2.5;

            double noiseAmp = 0.0;
            int N = 500;
            // Arrange
            var unitData = new UnitDataSet("test");
            double[] u1 = TimeSeriesCreator.ThreeSteps(N*1/5, N*2/5, N*3/5, N, -2, -1, 0, 1);
            double[] u2 = TimeSeriesCreator.ThreeSteps(N*1/5, N*2/5, N*3/5, N, 0, 1, 2, 3);
            double[] u = TimeSeriesCreator.Concat(u1,u2);
            unitData.SetU(u); 
            unitData.CreateTimeStamps(timeBase_s);

            double threshold =2;

            //reference model
            GainSchedParameters trueGSparams = new GainSchedParameters(0,-1.34)
            {
                TimeConstant_s = new double[] { 10 },
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { threshold },
                TimeDelay_s = timeBase_s* timeDelaySamples,
            };

            GainSchedModel trueModel = new GainSchedModel(trueGSparams, "true gain sched model");
            PlantSimulatorHelper.SimulateSingleToYmeas(unitData, trueModel, noiseAmp, 454);

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
                    new List<string> { "y1=y_meas", "y1=y_sim", "y3=u1" },
                    timeBase_s,
                    TestContext.CurrentContext.Test.Name);
                Shared.DisablePlots();
            }

            ConoleOutResult(trueGSparams, idModel.GetModelParameters());

            Console.WriteLine(idModel);

            // assert
           Assert.That(Math.Abs(idModel.GetModelParameters().TimeDelay_s - trueGSparams.TimeDelay_s)<td_tol,"time delay too far off" );
           Assert.That(Math.Abs(idModel.GetModelParameters().TimeConstant_s.First() - trueGSparams.TimeConstant_s.First()) < tc_tol, "time constant too far off!");
            Assert.That(idModel.GetModelParameters().Fitting.FitScorePrc > fitScoreReq,"Tripwire: FitScore should not fall past what was observed during design of the test ");
        }


        // note that the input varies from -2 to 4 here, so threshold beyond that are not identifiable, and at the edges they are also hard to identify.
        [TestCase(20, 60,true)]
        [TestCase(20, 60,false)]


        public void NonzeroOperatingPointU_Both_EstimatesStillOk(double uOperatingPoint, double yOperatingPoint,bool estimateThresholds)
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
            GainSchedParameters trueGSparams = new GainSchedParameters(uOperatingPoint, yOperatingPoint)
            {
                TimeConstant_s = new double[] { 0, 0 },
                TimeConstantThresholds = new double[] { gainSchedThreshold },
                LinearGains = new List<double[]> { new double[] { 2 }, new double[] { 4 } },
                LinearGainThresholds = new double[] { gainSchedThreshold },
                TimeDelay_s = 0,
            };

            GainSchedModel trueModel = new GainSchedModel(trueGSparams, "True Model");
            PlantSimulatorHelper.SimulateSingleToYmeas(unitData, trueModel, noiseAmp, (int)Math.Ceiling(2 * gainSchedThreshold + 45));

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
                    TestContext.CurrentContext.Test.Name + uOperatingPoint);

                PlotGain.PlotSteadyState(trueModel, idModel, "NonzeroOperatingPointU");
                Shared.DisablePlots();
            }
            ConoleOutResult(trueGSparams, idModel.GetModelParameters());
        }
        [TestCase("Identify",40,-2,-1,98)]
        [TestCase("Identify",20,-2,-1, 98)]
        [TestCase("Identify", 40, -2, 1, 98)]
        [TestCase("Identify", 20, -2, 1, 98)]
        [TestCase("IdentifyForGivenThresholds", 40, -2, -1,99, Description ="threshold is assumed perfectly known, makes estimation easier")]
        [TestCase("IdentifyForGivenThresholds", 20, -2, -1,99, Description = "threshold is assumed perfectly known, makes estimation easier")]
        [TestCase("IdentifyForGivenThresholds", 40, -2, 1,93, Description = "threshold is assumed perfectly known, makes estimation easier")]
        [TestCase("IdentifyForGivenThresholds", 20, -2, 1,97, Description = "threshold is assumed perfectly known, makes estimation easier")]
        public void TwoGainsConstTc_RampChange_Both_AllParamsEstOk(string solver, double Tc, double gain1, double gain2, double fitScoreReq)
        {
            double tc_tol = 5;
            double gain_tol_prc = 15;
            double threshold_tol = 5;

            double noise_abs = 0.2;

            // Arrange
            GainSchedParameters trueGSparams = new GainSchedParameters
            {
                TimeConstant_s = new double[] { Tc },
                TimeConstantThresholds = new double[] {  },
                LinearGains = new List<double[]> { new double[] { gain1 }, new double[] { gain2 } },
                LinearGainThresholds = new double[] { 50 },
                TimeDelay_s = 0,
            };
            GainSchedModel trueModel = new GainSchedModel(trueGSparams);

            double[]  input = TimeSeriesCreator.Ramp(300, 100, 0, 10, 40);
            var unitData = new UnitDataSet();
            unitData.SetU(input);
            unitData.CreateTimeStamps(timeBase_s);
            bool isOk = PlantSimulatorHelper.SimulateSingleToYmeas(unitData,trueModel, noise_abs);

            GainSchedModel idModel = new GainSchedModel();
            if (solver == "Identify")
            {
                idModel = GainSchedIdentifier.Identify(unitData);
            }
            else if (solver == "IdentifyForGivenThresholds")
            {
                var gsFittingSpecs = new GainSchedFittingSpecs()
                {
                    uGainThresholds = trueGSparams.LinearGainThresholds
                };
                idModel = GainSchedIdentifier.IdentifyForGivenThresholds(unitData, gsFittingSpecs);
            }
            Console.WriteLine(idModel);

            // plotting
            bool doPlot = false;// should be false unless debugging
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                     unitData.Y_meas,
                     unitData.Y_sim,
                     unitData.U.GetColumn(0),
                     },
                    new List<string> { "y1=y_meas", "y1=y_sim", "y3=u1" },
                    timeBase_s, "TwoGains_RampChange(" + solver + "," + Tc + "," + gain1 + "," + gain2 + ")");
                Shared.DisablePlots();
            }

            // assert
            Assert.That(idModel.GetModelParameters().TimeConstant_s.First() - trueModel.GetModelParameters().TimeConstant_s.First() < tc_tol, "Timeconstant 1 too far off");
            if (idModel.GetModelParameters().TimeConstant_s.Count()>1)
                Assert.That(idModel.GetModelParameters().TimeConstant_s.ElementAt(1) - trueModel.GetModelParameters().TimeConstant_s.ElementAt(1) < tc_tol, "Timeconstant 2 too far off");

            Assert.That(Math.Abs(idModel.GetModelParameters().LinearGains.ElementAt(0)[0]/ trueModel.GetModelParameters().LinearGains.ElementAt(0)[0] - 1)*100 < gain_tol_prc, "Linear gain 1 too far off");
            Assert.That(Math.Abs(idModel.GetModelParameters().LinearGains.ElementAt(1)[0] / trueModel.GetModelParameters().LinearGains.ElementAt(1)[0] - 1) * 100 < gain_tol_prc, "Linear gain 2 too far off");

            Assert.That(idModel.GetModelParameters().LinearGainThresholds.First() - trueModel.GetModelParameters().LinearGainThresholds.First() < threshold_tol, "Threshold too far off");

            Assert.That(idModel.GetModelParameters().Fitting.FitScorePrc > fitScoreReq, "Tripwire: FitScore should not fall past what was observed during design of the test ");

        }






        [TestCase(-0.5, 0.055, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(-0.2, 0.058, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(0.2, 0.045, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(0.5, 0.04, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(1.0, 0.01, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(2.0, 0.015, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(2.5, 0.015, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(3.0, 0.015, Explicit = true, Category = "NotWorking_AcceptanceTest") ]

        public void TwoGainsAndTwoTc_StepChange_Identify_TwoTcEstEstOk(double gain_sched_threshold, double linearGainTresholdTol )
        {
            double noiseAmp = 0.0;// TODO: should be above zero
            int N = 300;
            // Arrange
            var unitData = new UnitDataSet("test"); 
            double[] u1 = TimeSeriesCreator.ThreeSteps(N/5, N/3, N/2, N, -2, -1, 0, 1);
            double[] u2 = TimeSeriesCreator.ThreeSteps(3*N/5, 2*N/3, 4*N/5, N, 0, 1, 2, 3);
            double[] u = TimeSeriesCreator.Concat(u1, u2);
            unitData.SetU(u);
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            //reference model
            GainSchedParameters trueGSparams = new GainSchedParameters(0,-1.34)
            {
                TimeConstant_s = new double[] { 3, 10 },
                TimeConstantThresholds = new double[] { gain_sched_threshold },
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { gain_sched_threshold },
                TimeDelay_s = 0,
            };

            GainSchedModel trueModel = new GainSchedModel(trueGSparams, "Correct gain sched model");
            PlantSimulatorHelper.SimulateSingleToYmeas(unitData,trueModel, noiseAmp, (int)Math.Ceiling(2 * gain_sched_threshold + 45));

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
                    TestContext.CurrentContext.Test.Name + gain_sched_threshold.ToString("F2"));
                Shared.DisablePlots();
            }
            ConoleOutResult(trueGSparams, idModel.GetModelParameters());

            Console.WriteLine(idModel);

            // Assert
            int min_number_of_gains = Math.Min(idModel.GetModelParameters().LinearGainThresholds.Length, trueGSparams.LinearGainThresholds.Length);
            for (int k = 0; k < min_number_of_gains; k++)
            {
                Assert.That(Math.Abs(idModel.GetModelParameters().LinearGainThresholds[k] - trueGSparams.LinearGainThresholds[k]), Is.LessThanOrEqualTo(linearGainTresholdTol),
                "There are too large differences in the linear gain threshold " + k.ToString());
            }
            Assert.That(idModel.GetModelParameters().TimeConstantThresholds != null);
            Assert.That(idModel.GetModelParameters().TimeConstantThresholds.Count() == 2,
                "Two time constants in true model but only one model found");




        }
        [TestCase(1,2)]
        public void ChangeOperatingPoint_YsimUnchanged(double origOpU, double origOpY)
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

            GainSchedParameters trueParams = new GainSchedParameters(origOpU,origOpY)
            {
                TimeConstant_s = new double[] { 4, 12 },
                TimeConstantThresholds = new double[] { 1.035 },
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { 1.035 },
                TimeDelay_s = 0,
            };

            // make the bias nonzero to test that the operating point estimation works.
            GainSchedModel trueModel = new GainSchedModel(trueParams, "true");
            PlantSimulatorHelper.SimulateSingleToYsim(unitData, trueModel);

            var alteredParams = new GainSchedParameters(trueModel.GetModelParameters());
            var alteredIdModel = new GainSchedModel(alteredParams,"altered");

            alteredIdModel.GetModelParameters().MoveOperatingPointUWithoutChangingModel(3);
            (bool isOk3, double[] simY2) = PlantSimulatorHelper.SimulateSingle(unitData, alteredIdModel);

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
                    TestContext.CurrentContext.Test.Name);

                PlotGain.PlotSteadyState(trueModel, alteredIdModel, "ChangeOperatingPoint_YsimUnchanged" , 
                    new double[] { (new Vec()).Min(u) }, new double[] { (new Vec()).Max(u) });
                Shared.DisablePlots();
            }
            ConoleOutResult(trueParams, alteredIdModel.GetModelParameters());
            // Asserts
            Assert.IsTrue((new Vec()).Max((new Vec()).Subtract(unitData.Y_sim, simY2))<y_tol);
            Assert.IsTrue(trueParams.GetOperatingPointU() != alteredIdModel.GetModelParameters().GetOperatingPointU());
            Assert.IsTrue(trueParams.GetOperatingPointY() != alteredIdModel.GetModelParameters().GetOperatingPointY());
            Assert.IsTrue(trueParams.GetOperatingPointU() == origOpU);
            Assert.IsTrue(trueParams.GetOperatingPointY() == origOpY);

        }

        [TestCase("IdentifyForGivenThresholds")]
        [TestCase("Identify")]

        public void FlatData_IdTerminatesWithoutCrashing(string solverId)
        {
            int N = 300;
            double noiseAmplitude = 0.40;

            // Arrange
            var unitData = new UnitDataSet("test");
            unitData.SetU(TimeSeriesCreator.Constant(5, N));
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            GainSchedParameters trueParams = new GainSchedParameters(0, 1.34)
            {
                TimeConstant_s = new double[] { 5 },
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { 1.035 },
                TimeDelay_s = 0,
            };

            // make the bias nonzero to test that the operating point estimation works.
            //    trueParams.OperatingPoint_Y = 1.34;
            GainSchedModel trueModel = new GainSchedModel(trueParams, "Correct gain sched model");

            PlantSimulatorHelper.SimulateSingleToYmeas(unitData, trueModel, noiseAmplitude, 123);

            // Act
            var idModel = new GainSchedModel();
            if (solverId == "Identify")
            {
                // this will include determining thresholds, unlike below
                idModel = GainSchedIdentifier.Identify(unitData);
            }
            else if (solverId == "IdentifyForGivenThresholds")
            {
                var gsFittingSpecs = new GainSchedFittingSpecs();
                gsFittingSpecs.uGainThresholds = trueParams.LinearGainThresholds;
                gsFittingSpecs.uTimeConstantThresholds = trueParams.TimeConstantThresholds;
                idModel = GainSchedIdentifier.IdentifyForGivenThresholds(unitData, gsFittingSpecs);
            }
            // plot
            bool doPlot = false;
            if (doPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> {
                    unitData.Y_meas,
                    unitData.Y_sim,
                    unitData.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_sim(est_model)", "y3=u1" },
                    timeBase_s,
                    "GainSchedFlatData" + solverId);
                Shared.DisablePlots();
            }

            Console.WriteLine(idModel);

        }



        [TestCase(10, 0, 99.9, Description= "IdentifyForGivenThresholds(),identify gain and time constants, zero bias, thresholds are GIVEN")]
        [TestCase(10, 1, 99.9, Description = "IdentifyForGivenThresholds(), same as ver1, except non-zero bias")]
        [TestCase(10, 2, 99.9, Description = "Identify(), identify gain and time constants AND THRESHOLDS, zero bias, ")]
        [TestCase(10, 3, 99.9, Description = "Identify(), same as ver2, except non-zero bias")]

        public void TwoGainsConstTc_StepChange_Both_TCAndThresholdFoundOk(double TimeConstant1_s,
            int ver, double fitScoreReq)
        {
            int N = 300;
            var threshold_tol = 0.03;

            double noiseAmplitude = 0.00;

            // Arrange
            var unitData = new UnitDataSet("test"); 
            double[] u1 = TimeSeriesCreator.ThreeSteps(N/5, N/3, N/2, N, -2, -1, 0, 1);
            double[] u2 = TimeSeriesCreator.ThreeSteps(3*N/5, 2*N/3, 4*N/5, N, 0, 1, 2, 3);
            double[] u = TimeSeriesCreator.Concat(u1, u2);
            unitData.SetU(u);
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            GainSchedParameters trueParams = new GainSchedParameters(0,1.34)
            {
                TimeConstant_s = new double[] { TimeConstant1_s, TimeConstant1_s },
                TimeConstantThresholds = null,
                LinearGains = new List<double[]> { new double[] { -2 }, new double[] { 3 } },
                LinearGainThresholds = new double[] { 1.035 },
                TimeDelay_s = 0,
            };

            // make the bias nonzero to test that the operating point estimation works.
        //    trueParams.OperatingPoint_Y = 1.34;
            var trueModel = new GainSchedModel(trueParams, "Correct gain sched model");

            PlantSimulatorHelper.SimulateSingleToYmeas(unitData, trueModel, noiseAmplitude,123);

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
            (bool isOk2, double[] simY2) =  PlantSimulatorHelper.SimulateSingle(unitData, idModel);

            var alteredIdModel = (GainSchedModel)idModel.Clone("altered");
            alteredIdModel.GetModelParameters().MoveOperatingPointUWithoutChangingModel(3);
            (bool isOk3, double[] simY3) = PlantSimulatorHelper.SimulateSingle(unitData, alteredIdModel);

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
                    TestContext.CurrentContext.Test.Name + " ver_"+ver);

                PlotGain.PlotSteadyState(idModel, alteredIdModel, "twogains"+ver, 
                    new double[] { (new Vec()).Min(u) }, new double[] { (new Vec()).Max(u) });

                Shared.DisablePlots();
            }

            ConoleOutResult(trueParams, idModel.GetModelParameters());
            Console.WriteLine(idModel);


            // Asserts
            Assert.IsTrue(Math.Abs(idModel.GetModelParameters().LinearGainThresholds.First() - trueParams.LinearGainThresholds.First()) < threshold_tol);
            //SISOTests.CommonAsserts(inputData, IdentifiedsimData, estPlantSim);

            int min_number_of_time_constants = Math.Min(idModel.GetModelParameters().TimeConstant_s.Length, trueParams.TimeConstant_s.Length);
            for (int k = 0; k < min_number_of_time_constants; k++)
            {
                Assert.That(idModel.GetModelParameters().TimeConstant_s[k].IsGreaterThanOrEqual(Math.Max(0,TimeConstant1_s) - TimeConstantAllowedDev_s),
                "Too low time constant " + k.ToString());
                Assert.That(idModel.GetModelParameters().TimeConstant_s[k].IsLessThanOrEqual(TimeConstant1_s + TimeConstantAllowedDev_s),
                "Too high time constant " + k.ToString());
            }
            Assert.That(idModel.GetModelParameters().Fitting.FitScorePrc > fitScoreReq, "Tripwire: FitScore should not fall past what was observed during design of the test ");

        }


    }
}
