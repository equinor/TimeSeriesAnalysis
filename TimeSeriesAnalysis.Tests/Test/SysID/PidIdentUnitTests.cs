using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.SysID
{
    [TestFixture]
    class PidIdentUnitTests
    {
        UnitParameters modelParameters1 = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 5,
                Bias = 5
            };

        int timeBase_s = 1;
        int N = 200;
        DateTime t0 = new DateTime(2010,1,1);
        UnitModel processModel1;
        UnitModel processModel2;


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
            processModel1 = new UnitModel(modelParameters1, "SubProcess1");
            processModel2 = new UnitModel(modelParameters1, "SubProcess2");
        }



        // Tendency of Kp and Ti to be biased lower when there is noise in Y
        [TestCase(1, 0.0,10)]
        [TestCase(1, 0.1,10)]
        [TestCase(2, 0.1,10)]
        [TestCase(5, 0.1,10)]

        public void SetpointStep_WNoise_KpAndTiEstimatedOk(double ySetAmplitude, double yNoiseAmplitude, double tolerancePrc)
        {
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Step(N/7, N,50,50+ySetAmplitude));
            inputData.CreateTimestamps(timeBase_s,0,t0);
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);
            simData.AddNoiseToSignal("SubProcess1-Output_Y", yNoiseAmplitude,0);
            Assert.IsTrue(isOk);

            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);

            Assert.AreEqual(idResult.GetWarnings().Count(),0);
            Console.WriteLine("Kp:" + idResult.Kp.ToString("F2") + " Ti:" + idResult.Ti_s.ToString("F2"));

     
            if (false)
            {
                Shared.EnablePlots();
                string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                    Replace(")", "_").Replace(",", "_") + "y";
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),pidDataSet.U_sim.GetColumn(0)},
                    new List<string> { "y1=y meas", "y1=y set", "y3=u", "y3=u_sim" },
                    pidDataSet.GetTimeBase(), caseId);
                Shared.DisablePlots();
            }


            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp)< pidParameters1.Kp * tolerancePrc / 100, "Estimated Kp:"+ idResult.Kp + "True Kp:" + pidParameters1.Kp);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s * tolerancePrc / 100, "Estimated Ti_s:" + idResult.Ti_s + "True Ti_s:" + pidParameters1.Ti_s);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
        }

        // Tendency of Kp and Ti to be biased lower when there is noise in Y
        [TestCase(1, 0.1,10)]
        [TestCase(2, 0.1,10)]
        [TestCase(4, 0.1,20)]// very strange that the biggest setpoint step has the poorest estimation.

        public void SetpointStep_WNoise_Downsampled_KpAndTiEstimatedOk(int downsampleFactor, 
            double yNoiseAmplitude, double tolerancePrc)
        {
            double ySetAmplitude = 1;
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Step(N / 7, N, 50, 50 + ySetAmplitude));
            inputData.CreateTimestamps(timeBase_s, 0,t0);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            simData.AddNoiseToSignal("SubProcess1-Output_Y", yNoiseAmplitude,0);
            Assert.IsTrue(isOk);

            var combinedData = inputData.Combine(simData);
            var downsampleData = combinedData.CreateDownsampledCopy(downsampleFactor);
            var pidDataSet = processSim.GetUnitDataSetForPID(downsampleData, pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);
            Console.WriteLine("Kp:" + idResult.Kp.ToString("F2") + " Ti:" + idResult.Ti_s.ToString("F2"));

            Assert.AreEqual(idResult.GetWarnings().Count(), 0);

            if (false)
            {
                Shared.EnablePlots();
                string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                    Replace(")", "_").Replace(",", "_") + "y";
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),pidDataSet.U_sim.GetColumn(0)},
                    new List<string> { "y1=y meas", "y1=y set", "y3=u", "y3=u_sim" },
                    pidDataSet.GetTimeBase(), caseId);
                Shared.DisablePlots();
            }
            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp) < pidParameters1.Kp * tolerancePrc / 100, "Kp too far off!:" + idResult.Kp);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s * tolerancePrc / 100, "Ti_s too far off!:" + idResult.Ti_s);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
        }


        [TestCase(5,0,5)]
        [TestCase(1,0,5)]
        [TestCase(5, 0.05,5)]
        [TestCase(1, 0.05,10)]

        public void DistStep_WNoise_KpAndTiEstimatedOk(double stepAmplitude, double yNoiseAmplitude, double tolerancePrc )
        {
            
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50,N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Step(N/2,N,0,stepAmplitude));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            simData.AddNoiseToSignal("SubProcess1-Output_Y", yNoiseAmplitude,890978);
            Assert.IsTrue(isOk);

            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);

            Console.WriteLine("Kp:" + idResult.Kp.ToString("F2") + " Ti:" + idResult.Ti_s.ToString("F2"));
            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp) < pidParameters1.Kp * tolerancePrc / 100, "Kp too far off:"+ idResult.Kp);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s *tolerancePrc / 100, "Ti_S too far off:" + idResult.Ti_s);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
        }

        // want to see how robust PidIdentifier is when it has to find Kp and Ti on a lower sampling rate than the "actual" rate

        // when noise is added in the fully sampled, case the solver uses a low-pass filtering of ymeas as a key
        // tactic to improve estimates of Kp and Ti. In the downsampled case,it is not possible to use filtering in the same 
        // way. It may be that instead the solver should run the pid-controller at its original time sampling,
        // maybe this will casue the noise to smoothe out

        [TestCase(2,0,10)]
        [TestCase(2,0.05,35)]// this is poor
        [TestCase(4,0,10)]
        [TestCase(4,0.05,20)]
        public void DistStep_WNoise_Downsampled_KpAndTiEstimatedOk(int downsampleFactor, double noiseAmplitude, double tolerancePrc)
        {
            int N = 1000;
            double stepAmplitude = 1;

            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Step(N / 2, N, 0, stepAmplitude));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            simData.AddNoiseToSignal("SubProcess1-Output_Y", noiseAmplitude,495495);
            var combinedData = inputData.Combine(simData);
            var downsampleData = combinedData.CreateDownsampledCopy(downsampleFactor);
            // ----do not use inputData or simData below this line----
            var pidDataSet = processSim.GetUnitDataSetForPID(downsampleData, pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);

            Console.WriteLine("Kp:" + idResult.Kp.ToString("F2") + " Ti:" + idResult.Ti_s.ToString("F2"));
            if (false)
            {
                Shared.EnablePlots();
                string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                    Replace(")", "_").Replace(",", "_") + "y";
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),pidDataSet.U_sim.GetColumn(0)},
                    new List<string> { "y1=y meas", "y1=y set", "y3=u", "y3=u_sim" },
                    pidDataSet.GetTimeBase(), caseId);
                Shared.DisablePlots();
            }
            // asserts
            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp) < pidParameters1.Kp * tolerancePrc / 100, "Kp estimate:"+ idResult.Kp + "versus true :" + pidParameters1.Kp);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s * tolerancePrc / 100, "Ti_s estimate"+ idResult.Ti_s + "versus true :" + pidParameters1.Ti_s);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }

        }



        [TestCase(1)]
        public void OscillatingDisturbanceCorrectKpSign(double timeBase_s)
        {
            // Define parameters
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 50
            };

            // Create plant model
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
            new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);

            // Create synthetic data 
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Sinus(10, timeBase_s * 20, timeBase_s, N));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk, "simulate did not run");
            var combinedData = inputData.Combine(simData);
            var pidDataSet = processSim.GetUnitDataSetForPID(combinedData, pidModel1);

            var combinedDataFlatLines = new TimeSeriesDataSet(combinedData);
            var pidDataSetWithFlatlines = processSim.GetUnitDataSetForPID(combinedDataFlatLines, pidModel1);

            // Identify on both original and flatlined datasets
            var idParameters = new PidIdentifier().Identify(ref pidDataSet, false);
           
            // Plot results
            if (false)
            {
                Shared.EnablePlots();
                string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                    Replace(")", "_").Replace(",", "_") + "y";
                // plot the dataset without "flatlines" 
                Plot.FromList(new List<double[]> { pidDataSet.Y_meas, pidDataSet.Y_setpoint, pidDataSet.U.GetColumn(0), pidDataSet.U_sim.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u", "y3=u_sim" },
                    pidDataSet.GetTimeBase(), caseId + "_raw");
                Shared.DisablePlots();
            }

            // Assert that identification on datasets with flatlines yields the same parameters as identification on original data
            // and check that it also finds parameters that are somewhat close to the original values.
            // Also assert that the identification yields better fits when attempting to ignore flatlines than when not doing so.
            // Finally, assert that the fit is better when taking flatline handling into account.
            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idParameters.Kp) < 0.02 * pidParameters1.Kp, "Kp too far off: "+ idParameters.Kp); // Allow 2% slack on Kp
            Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idParameters.Ti_s) < 0.05 * pidParameters1.Ti_s, "Ti too far off:"+ idParameters.Ti_s); // Allow 5% slack on Ti
        }


        /// <summary>
        /// It is not uncommon for datasets to have flatlines for various reasons. Such periods should be filtered out in the indicesToIgnore.
        /// </summary>
        /// <param name="N">number of samples in the stored dataset,</param>
        /// <param name="timebase">Timebase of the signals.</param>
        /// <param name="flatlinePeriods">Number of periods with flatlined data.</param>
        /// <param name="flatlineProportion">Proportion of the dataset that should be flatlines.</param>
        [TestCase(1000,1,1,0.1)]// There is one flatline period covering 10%.
       /* [TestCase(1000,10.0,1,0.25)]// There is one flatline period covering 25%.
        [TestCase(1000,10.0,2,0.1)]// There are two flatline periods covering 10%.
        [TestCase(1000,10.0,2,0.25)]// There are two flatline periods covering 25%.
        [TestCase(1000,10.0,3,0.1)]// There are three flatline periods covering 10%.
        [TestCase(1000,10.0,3,0.25)]// There are three flatline periods covering 25%.
        [TestCase(1000,10.0,4,0.1)]// There are four flatline periods covering 10%.
        [TestCase(1000,10.0,4,0.25)]// There are four flatline periods covering 25%.*/
        public void IndicesToIgnore_WFlatLines(int N, double timebase, int flatlinePeriods, double flatlineProportion)
        {
            // Define parameters
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 50
            };

            // Create plant model
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
            new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);

            // Create synthetic data
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Sinus(10, timebase*20, timebase, N));
            inputData.CreateTimestamps(timebase);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk,"simulate did not run");
            var combinedData = inputData.Combine(simData);
            var pidDataSet = processSim.GetUnitDataSetForPID(combinedData, pidModel1);

            var combinedDataFlatLines = new TimeSeriesDataSet(combinedData);
            // Identify on both original and flatlined datasets
            var idParameters = new PidIdentifier().Identify(ref pidDataSet, false);
            // Create synthetic data with flatlines (Create them anew to avoid shallow copies / references)
            int flatlinePeriodLength = (int)(flatlineProportion * N / flatlinePeriods);
            var pidDataSetWithFlatlines = processSim.GetUnitDataSetForPID(combinedDataFlatLines, pidModel1);
            /// create the flat data sets.
            for (int i = 0; i < flatlinePeriods; i++)
            {
                int flatlineStartIndex = (int)(N * ((double)i + 0.5) / flatlinePeriods - flatlinePeriodLength / 2);
                for (int j = 1; j < flatlinePeriodLength; j++)
                {
                    pidDataSetWithFlatlines.U[flatlineStartIndex + j, 0] = pidDataSetWithFlatlines.U[flatlineStartIndex, 0];
                    pidDataSetWithFlatlines.Y_meas[flatlineStartIndex + j] = pidDataSetWithFlatlines.Y_meas[flatlineStartIndex];
                    pidDataSetWithFlatlines.Y_setpoint[flatlineStartIndex + j] = pidDataSetWithFlatlines.Y_setpoint[flatlineStartIndex];
                }
            }

            // experimental: just detect frozen data and ignore those samples.(todo: consider moving below code into identify)
            var frozenIdx = FrozenDataDetector.DetectFrozenSamples(pidDataSetWithFlatlines);
           // pidDataSetWithFlatlines_control.IndicesToIgnore = frozenIdx;
            pidDataSetWithFlatlines.IndicesToIgnore = frozenIdx;

        //    var modelParametersWithFlatlines_control = new PidIdentifier().Identify(ref pidDataSetWithFlatlines_control/*, ignoreFlatLines: false*/);
            var idParametersWithFlatlines = new PidIdentifier().Identify(ref pidDataSetWithFlatlines);

            // Plot results
            if (false)
            {
                Shared.EnablePlots();
                string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                    Replace(")", "_").Replace(",", "_") + "y";
                // plot the dataset without "flatlines" 
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint, pidDataSet.U.GetColumn(0), pidDataSet.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u", "y3=u_sim" },
                    pidDataSet.GetTimeBase(), caseId+"_raw");
                Plot.FromList(new List<double[]>{ pidDataSetWithFlatlines.Y_meas, pidDataSetWithFlatlines.Y_setpoint, 
                    pidDataSetWithFlatlines.U.GetColumn(0), pidDataSetWithFlatlines.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_with_flatlines", "y1=y_setpoint_with_flatlines", "y3=u_with_flatlines", "y3=u_sim_with_flatlines" },
                    pidDataSetWithFlatlines.GetTimeBase(), caseId+"_with_flatlines");
                Shared.DisablePlots();
            }

            // Assert that identification on datasets with flatlines yields the same parameters as identification on original data
            // and check that it also finds parameters that are somewhat close to the original values.
            // Also assert that the identification yields better fits when attempting to ignore flatlines than when not doing so.
            // Finally, assert that the fit is better when taking flatline handling into account.
            Assert.IsTrue(Math.Abs(idParametersWithFlatlines.Kp - idParameters.Kp) < 0.02 * idParametersWithFlatlines.Kp,"Kp too far off 1 "); // Allow 2% slack on Kp
            Assert.IsTrue(Math.Abs(idParametersWithFlatlines.Ti_s - idParameters.Ti_s) < 0.05 * idParametersWithFlatlines.Ti_s,"Ti too far off"); // Allow 5% slack on Ti
            Assert.IsTrue(Math.Abs(idParametersWithFlatlines.Kp - pidParameters1.Kp) < 0.02 * pidParameters1.Kp, "Kp too far off 2"); // Allow 2% slack on Kp
            Assert.IsTrue(Math.Abs(idParametersWithFlatlines.Ti_s - pidParameters1.Ti_s) < 0.05 * pidParameters1.Ti_s, "Ti too far off"); // Allow 5% slack on Ti
     /*       Assert.IsTrue(Math.Abs(modelParametersWithFlatlines.Kp - pidParameters1.Kp) < Math.Abs(modelParametersWithFlatlines_control.Kp - pidParameters1.Kp), "Kp too far off 3");
            Assert.IsTrue(Math.Abs(modelParametersWithFlatlines.Ti_s - pidParameters1.Ti_s) < Math.Abs(modelParametersWithFlatlines_control.Ti_s - pidParameters1.Ti_s), "Ti too far off");
            Assert.IsTrue(modelParametersWithFlatlines.Fitting.FitScorePrc > modelParametersWithFlatlines_control.Fitting.FitScorePrc,"Fit score should improve");
            Assert.IsTrue(modelParametersWithFlatlines.Fitting.RsqDiff > modelParametersWithFlatlines_control.Fitting.RsqDiff, "Rsq should improve");
     */
        }

     /*   /// <summary>
        /// It is not uncommon for datasets to have flatlines for various reasons. Such periods should be filtered out in the indicesToIgnore. This should also work when there is a delay between the process variable signal and the regulator output signal.
        /// </summary>
        /// <param name="N">number of samples in the stored dataset,</param>
        /// <param name="timebase">Timebase of the signals.</param>
        /// <param name="flatlinePeriods">Number of periods with flatlined data.</param>
        /// <param name="flatlineProportion">Proportion of the dataset that should be flatlines.</param>
        [TestCase(1000,10.0,1,0.1)]// There is one flatline period covering 10%.
        [TestCase(1000,10.0,1,0.25)]// There is one flatline period covering 25%.
        [TestCase(1000,10.0,2,0.1)]// There are two flatline periods covering 10%.
        [TestCase(1000,10.0,2,0.25)]// There are two flatline periods covering 25%.
        [TestCase(1000,10.0,3,0.1)]// There are three flatline periods covering 10%.
        [TestCase(1000,10.0,3,0.25)]// There are three flatline periods covering 25%.
        [TestCase(1000,10.0,4,0.1)]// There are four flatline periods covering 10%.
        [TestCase(1000,10.0,4,0.25)]// There are four flatline periods covering 25%.
        public void IndicesToIgnore_WFlatLines_WDelay_WNoise(int N, double timebase, int flatlinePeriods, double flatlineProportion)
        {
            // Define parameters
            var pidParameters1 = new PidParameters()
            {
                Kp = 1.42,
                Ti_s = 87,
                DelayOutputOneSample = true
            };
            double noiseAmplitude = 0.5;

            // Create plant model
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
            new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);

            // Create synthetic data
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Sinus(10, timebase*20, timebase, N));
            inputData.CreateTimestamps(timebase);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            simData.AddNoiseToSignal("SubProcess1-Output_Y", noiseAmplitude, 123456);
            var combinedData = inputData.Combine(simData);
            var pidDataSet = processSim.GetUnitDataSetForPID(combinedData, pidModel1);

            // Create synthetic data with flatlines (Create them anew to avoid shallow copies / references)
            var pidModel2 = new PidModel(pidParameters1, "PID2");
            var processSim2 = new PlantSimulator(
            new List<ISimulatableModel> { pidModel2, processModel2 });
            processSim2.ConnectModels(processModel2, pidModel2);
            processSim2.ConnectModels(pidModel2, processModel2);
            int flatlinePeriodLength = (int)(flatlineProportion * N / flatlinePeriods);
            var inputDataFlatlines = new TimeSeriesDataSet();
            inputDataFlatlines.Add(processSim2.AddExternalSignal(pidModel2, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputDataFlatlines.Add(processSim2.AddExternalSignal(processModel2, SignalType.Disturbance_D), TimeSeriesCreator.Sinus(10, timebase*20, timebase, N));
            inputDataFlatlines.CreateTimestamps(timebase);
            var isOkFlatlines = processSim2.Simulate(inputDataFlatlines, out TimeSeriesDataSet simDataFlatlines);
            simDataFlatlines.AddNoiseToSignal("SubProcess2-Output_Y", noiseAmplitude, 123456);
            var combinedDataFlatlines = inputDataFlatlines.Combine(simDataFlatlines);
            var pidDataSetWithFlatlines_control = processSim2.GetUnitDataSetForPID(combinedDataFlatlines, pidModel2);
            var pidDataSetWithFlatlines = processSim2.GetUnitDataSetForPID(combinedDataFlatlines, pidModel2);
            for (int i = 0; i < flatlinePeriods; i++)
            {
                int flatlineStartIndex = (int)(N * ((double)i + 0.5) / flatlinePeriods - flatlinePeriodLength / 2);
                for (int j = 1; j < flatlinePeriodLength; j++)
                {
                    pidDataSetWithFlatlines_control.U[flatlineStartIndex + j, 0] = pidDataSetWithFlatlines_control.U[flatlineStartIndex, 0];
                    pidDataSetWithFlatlines_control.Y_meas[flatlineStartIndex + j] = pidDataSetWithFlatlines_control.Y_meas[flatlineStartIndex];
                    pidDataSetWithFlatlines_control.Y_setpoint[flatlineStartIndex + j] = pidDataSetWithFlatlines_control.Y_setpoint[flatlineStartIndex];
                    pidDataSetWithFlatlines.U[flatlineStartIndex + j, 0] = pidDataSetWithFlatlines.U[flatlineStartIndex, 0];
                    pidDataSetWithFlatlines.Y_meas[flatlineStartIndex + j] = pidDataSetWithFlatlines.Y_meas[flatlineStartIndex];
                    pidDataSetWithFlatlines.Y_setpoint[flatlineStartIndex + j] = pidDataSetWithFlatlines.Y_setpoint[flatlineStartIndex];
                }
            }

            // Identify on both original and flatlined datasets
            var modelParameters = new PidIdentifier().Identify(ref pidDataSet);
            var modelParametersWithFlatlines_control = new PidIdentifier().Identify(ref pidDataSetWithFlatlines_control); //, ignoreFlatLines: false
            var modelParametersWithFlatlines = new PidIdentifier().Identify(ref pidDataSetWithFlatlines);

            // Plot results
            if (false)
            {
                Shared.EnablePlots();
                string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                    Replace(")", "_").Replace(",", "_") + "y";
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint, pidDataSet.U.GetColumn(0), pidDataSet.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u", "y3=u_sim" },
                    pidDataSet.GetTimeBase(), caseId+"_raw");
                Plot.FromList(new List<double[]>{ pidDataSetWithFlatlines.Y_meas, pidDataSetWithFlatlines.Y_setpoint, pidDataSetWithFlatlines.U.GetColumn(0), pidDataSetWithFlatlines.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_with_flatlines", "y1=y_setpoint_with_flatlines", "y3=u_with_flatlines", "y3=u_sim_with_flatlines" },
                    pidDataSetWithFlatlines.GetTimeBase(), caseId+"_with_flatlines");
                Shared.DisablePlots();
            }

            // Assert that identification on datasets with flatlines yields the same parameters as identification on original data
            // and check that it also finds parameters that are somewhat close to the original values.
            // Also assert that the identification yields better fits when attempting to ignore flatlines than when not doing so.
            // Finally, assert that the fit is better when taking flatline handling into account.
            Assert.IsTrue(Math.Abs(modelParametersWithFlatlines.Kp - modelParameters.Kp) < 0.02 * modelParametersWithFlatlines.Kp); // Allow 2% slack on Kp
            Assert.IsTrue(Math.Abs(modelParametersWithFlatlines.Ti_s - modelParameters.Ti_s) < 0.05 * modelParametersWithFlatlines.Ti_s); // Allow 5% slack on Ti
            Assert.IsTrue(Math.Abs(modelParametersWithFlatlines.Kp - pidParameters1.Kp) < 0.02 * pidParameters1.Kp); // Allow 2% slack on Kp
            Assert.IsTrue(Math.Abs(modelParametersWithFlatlines.Ti_s - pidParameters1.Ti_s) < 0.05 * pidParameters1.Ti_s); // Allow 5% slack on Ti
            Assert.IsTrue(!((Math.Abs(modelParametersWithFlatlines.Kp - pidParameters1.Kp) > Math.Abs(modelParametersWithFlatlines_control.Kp - pidParameters1.Kp)) & (Math.Abs(modelParametersWithFlatlines.Ti_s - pidParameters1.Ti_s) > Math.Abs(modelParametersWithFlatlines_control.Ti_s - pidParameters1.Ti_s))));
            // Assert.IsTrue(Math.Abs(modelParametersWithFlatlines.Ti_s - pidParameters1.Ti_s) < Math.Abs(modelParametersWithFlatlines_control.Ti_s - pidParameters1.Ti_s));
            Assert.IsTrue((modelParametersWithFlatlines.Fitting.FitScorePrc > modelParametersWithFlatlines_control.Fitting.FitScorePrc) | (modelParametersWithFlatlines.Fitting.RsqDiff > modelParametersWithFlatlines_control.Fitting.RsqDiff));
        }
      */
     /*   /// <summary>
        /// It is not uncommon for datasets to be oversampled from the resolution of the stored timeseries.
        /// The identification should then automatically attempt downsampling to improve the fitscore.
        /// </summary>
        /// <param name="N">number of samples in the stored dataset,</param>
        /// <param name="timebaseStored">Timebase of the stored signals.</param>
        /// <param name="timebaseOversampled">Timebase of the oversampled data.</param>
        [TestCase(1000,50.0,1.0)]// the stored signal is oversampled by a factor 50
        [TestCase(1000,50.0,7.0)]// the stored signal is oversampled by a factor 50/7
        [TestCase(1000,50.0,37.0)]// the stored signal is oversampled by a factor 50/37
        [TestCase(1000,10.0,1.0)]// the stored signal is oversampled by a factor 10
        [TestCase(1000,10.0,2.0)]// the stored signal is oversampled by a factor 5
        [TestCase(1000,10.0,3.0)]// the stored signal is oversampled by a factor 10/3
        [TestCase(1000,10.0,4.0)]// the stored signal is oversampled by a factor 2.5
        [TestCase(1000,10.0,5.0)]// the stored signal is oversampled by a factor 2
        [TestCase(1000,7.0,4.0)]// the stored signal is oversampled by a factor 7/4
        [TestCase(1000,5.0,3.0)]// the stored signal is oversampled by a factor 5/3
        [TestCase(1000,5.0,4.0)]// the stored signal is oversampled by a factor 1.25
        public void DownsampleOversampledData(int N, double timebaseStored, double timebaseOversampled)
        {
            // Define parameters
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = timebaseStored*2
            };

            double oversampleFactor = timebaseStored / timebaseOversampled;

            // Create plant model
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
            new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);

            // Create synthetic data
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Sinus(10, timebaseStored*20, timebaseStored, N));
            inputData.CreateTimestamps(timebaseStored);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            var combinedData = inputData.Combine(simData);
            var pidDataSet = processSim.GetUnitDataSetForPID(combinedData, pidModel1);
            var modelParameters = new PidIdentifier().Identify(ref pidDataSet);

            // Oversample synthetic data
            var dataSetOversampled = inputData.CreateOversampledCopy(oversampleFactor);
            var simDataOversampled = simData.CreateOversampledCopy(oversampleFactor);
            var combinedDataOversampled = dataSetOversampled.Combine(simDataOversampled);

            // Identify model on oversampled data
            var pidDataSetOversampled_control = processSim.GetUnitDataSetForPID(combinedDataOversampled, pidModel1);
            var pidDataSetOversampled = processSim.GetUnitDataSetForPID(combinedDataOversampled, pidModel1);
            var oversampledModelParameters_control = new PidIdentifier().Identify(ref pidDataSetOversampled_control, downsampleOversampledData: false );//, ignoreFlatLines: false);
            var oversampledModelParameters = new PidIdentifier().Identify(ref pidDataSetOversampled);

            // Plot results
            if (false)
            {
                Shared.EnablePlots();
                string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                    Replace(")", "_").Replace(",", "_") + "y";
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint, pidDataSet.U.GetColumn(0), pidDataSet.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u", "y3=u_sim" },
                    pidDataSet.GetTimeBase(), caseId+"_raw");
                Plot.FromList(new List<double[]>{ pidDataSetOversampled_control.Y_meas, pidDataSetOversampled_control.Y_setpoint, pidDataSetOversampled_control.U.GetColumn(0), pidDataSetOversampled_control.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_oversampled_control", "y1=y_setpoint_oversampled_control", "y3=u_oversampled_control", "y3=u_sim_oversampled_control" },
                    pidDataSetOversampled_control.GetTimeBase(), caseId+"_oversampled_control");
                Plot.FromList(new List<double[]>{ pidDataSetOversampled.Y_meas, pidDataSetOversampled.Y_setpoint, pidDataSetOversampled.U.GetColumn(0), pidDataSetOversampled.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_oversampled", "y1=y_setpoint_oversampled", "y3=u_oversampled", "y3=u_sim_oversampled" },
                    pidDataSetOversampled.GetTimeBase(), caseId+"_oversampled");
                Shared.DisablePlots();
            }

            // Assert that identification on oversampled data yields the same parameters as identification on original data
            // Then assert that the identification using the downsampling on oversampled data gave a better fit than the identification on just oversampled data
            // Then assert that the correct timebase was identified.
            Assert.IsTrue(Math.Abs(oversampledModelParameters.Kp - modelParameters.Kp) < 0.01 * oversampledModelParameters.Kp);
            Assert.IsTrue(Math.Abs(oversampledModelParameters.Ti_s - modelParameters.Ti_s) < 0.01 * oversampledModelParameters.Ti_s);
            Assert.IsTrue(Math.Abs(oversampledModelParameters.Kp - pidParameters1.Kp) < 0.01 * pidParameters1.Kp); // A bit of slack here; the test is mainly for Ti.
            Assert.IsTrue(Math.Abs(oversampledModelParameters.Ti_s - pidParameters1.Ti_s) < Math.Abs(oversampledModelParameters_control.Ti_s - pidParameters1.Ti_s));
            Assert.IsTrue((oversampledModelParameters.Fitting.FitScorePrc > oversampledModelParameters_control.Fitting.FitScorePrc) | (oversampledModelParameters.Fitting.RsqDiff > oversampledModelParameters_control.Fitting.RsqDiff));
            Assert.IsTrue(Math.Abs(oversampledModelParameters.Fitting.TimeBase_s - timebaseStored) < 0.01 * timebaseStored);
        }*/
        
      /*  /// <summary>
        /// It is not uncommon for datasets to be oversampled from the resolution of the stored timeseries.
        /// The identification should then automatically attempt downsampling to improve the fitscore.
        /// Here noise is added before the oversampling, and the identification should be able to find
        /// approximately the correct parameters from before the noise.
        /// </summary>
        /// <param name="N">number of samples in the stored dataset,</param>
        /// <param name="timebaseStored">Timebase of the stored signals.</param>
        /// <param name="timebaseOversampled">Timebase of the oversampled data.</param>
        [TestCase(1000,50.0,1.0)]// the stored signal is oversampled by a factor 50
        [TestCase(1000,50.0,7.0)]// the stored signal is oversampled by a factor 50/7
        [TestCase(1000,50.0,37.0)]// the stored signal is oversampled by a factor 50/37
        [TestCase(1000,10.0,1.0)]// the stored signal is oversampled by a factor 10
        [TestCase(1000,10.0,2.0)]// the stored signal is oversampled by a factor 5
        [TestCase(1000,10.0,3.0)]// the stored signal is oversampled by a factor 10/3
        [TestCase(1000,10.0,4.0)]// the stored signal is oversampled by a factor 2.5
        [TestCase(1000,10.0,5.0)]// the stored signal is oversampled by a factor 2
        [TestCase(1000,5.0,4.0)]// the stored signal is oversampled by a factor 1.25
        public void DownsampleOversampledData_WNoise(int N, double timebaseStored, double timebaseOversampled)
        {
            // Define parameters
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = timebaseStored*2
            };

            double oversampleFactor = timebaseStored / timebaseOversampled;
            double noiseAmplitude = 1;

            // Create plant model
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
            new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);

            // Create synthetic data
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Sinus(10, timebaseStored*20, timebaseStored, N));
            inputData.CreateTimestamps(timebaseStored);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            simData.AddNoiseToSignal("SubProcess1-Output_Y", noiseAmplitude,123456);
            var combinedData = inputData.Combine(simData);
            var pidDataSet = processSim.GetUnitDataSetForPID(combinedData, pidModel1);
            var modelParameters = new PidIdentifier().Identify(ref pidDataSet);

            // Oversample synthetic data
            var dataSetOversampled = inputData.CreateOversampledCopy(oversampleFactor);
            var simDataOversampled = simData.CreateOversampledCopy(oversampleFactor);
            var combinedDataOversampled = dataSetOversampled.Combine(simDataOversampled);

            // Identify model on oversampled data
            var pidDataSetOversampled_control = processSim.GetUnitDataSetForPID(combinedDataOversampled, pidModel1);
            var pidDataSetOversampled = processSim.GetUnitDataSetForPID(combinedDataOversampled, pidModel1);
            var oversampledModelParameters_control = new PidIdentifier().Identify(ref pidDataSetOversampled_control, downsampleOversampledData: false); //, ignoreFlatLines: false);
            var oversampledModelParameters = new PidIdentifier().Identify(ref pidDataSetOversampled);

            // Plot results
            if (false)
            {
                Shared.EnablePlots();
                string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                    Replace(")", "_").Replace(",", "_") + "y";
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint, pidDataSet.U.GetColumn(0), pidDataSet.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u", "y3=u_sim" },
                    pidDataSet.GetTimeBase(), caseId+"_raw");
                Plot.FromList(new List<double[]>{ pidDataSetOversampled_control.Y_meas, pidDataSetOversampled_control.Y_setpoint, pidDataSetOversampled_control.U.GetColumn(0), pidDataSetOversampled_control.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_oversampled_control", "y1=y_setpoint_oversampled_control", "y3=u_oversampled_control", "y3=u_sim_oversampled_control" },
                    pidDataSetOversampled_control.GetTimeBase(), caseId+"_oversampled_control");
                Plot.FromList(new List<double[]>{ pidDataSetOversampled.Y_meas, pidDataSetOversampled.Y_setpoint, pidDataSetOversampled.U.GetColumn(0), pidDataSetOversampled.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_oversampled", "y1=y_setpoint_oversampled", "y3=u_oversampled", "y3=u_sim_oversampled" },
                    pidDataSetOversampled.GetTimeBase(), caseId+"_oversampled");
                Shared.DisablePlots();
            }

            // Assert that identification on oversampled data yields approximately the same parameters as the data were created with
            Assert.IsTrue(Math.Abs(oversampledModelParameters.Kp - pidParameters1.Kp) < 0.1 * pidParameters1.Kp);
            Assert.IsTrue(Math.Abs(oversampledModelParameters.Ti_s - pidParameters1.Ti_s) < 0.1 * pidParameters1.Ti_s);
            Assert.IsTrue(Math.Abs(oversampledModelParameters.Kp - pidParameters1.Kp) < Math.Abs(oversampledModelParameters_control.Kp - pidParameters1.Kp));
            Assert.IsTrue(Math.Abs(oversampledModelParameters.Ti_s - pidParameters1.Ti_s) < Math.Abs(oversampledModelParameters_control.Ti_s - pidParameters1.Ti_s));
            Assert.IsTrue((oversampledModelParameters.Fitting.FitScorePrc > 0.9 * oversampledModelParameters_control.Fitting.FitScorePrc) | (oversampledModelParameters.Fitting.RsqDiff > 0.9 * oversampledModelParameters_control.Fitting.RsqDiff)); // Allow a little slack due to noise effects
        }
     /*   /// <summary>
        /// A comprehensive case can include situations where both noise, downsampling, oversampling, and data loss
        /// occurs in the same dataset. These tests ensure that the identification is able to find the key parameters
        /// in such situations as well.
        /// </summary>
        /// <param name="N">number of samples in the original dataset.</param>
        /// <param name="timebaseOriginal">Timebase of the original signals.</param>
        /// <param name="timebaseStored">Timebase of the stored signals.</param>
        /// <param name="timebaseOversampled">Timebase of the oversampled data.</param>
        /// <param name="flatlinePeriods">Number of periods with flatlined data.</param>
        /// <param name="flatlineProportion">Proportion of the dataset that should be flatlines.</param>
        [TestCase(10000,1.0,8.0,2.0,1,0.1)]
        [TestCase(10000,1.0,8.0,2.0,1,0.25)]
        [TestCase(10000,1.0,8.0,2.0,4,0.1)]
        [TestCase(10000,1.0,8.0,2.0,4,0.25)]
        [TestCase(10000,1.0,8.0,5.0,1,0.1)]
        [TestCase(10000,1.0,8.0,5.0,1,0.25)]
        [TestCase(10000,1.0,8.0,5.0,4,0.1)]
        [TestCase(10000,1.0,8.0,5.0,4,0.25)]
        [TestCase(10000,1.0,15.0,2.0,1,0.1)]
        [TestCase(10000,1.0,15.0,2.0,1,0.25)]
        [TestCase(10000,1.0,15.0,2.0,4,0.1)]
        [TestCase(10000,1.0,15.0,2.0,4,0.25)]
        [TestCase(10000,1.0,15.0,5.0,1,0.1)]
        [TestCase(10000,1.0,15.0,5.0,1,0.25)]
        [TestCase(10000,1.0,15.0,5.0,4,0.1)]
        [TestCase(10000,1.0,15.0,5.0,4,0.25)]
        [TestCase(10000,3.0,8.0,2.0,1,0.1)]
        [TestCase(10000,3.0,8.0,2.0,1,0.25)]
        [TestCase(10000,3.0,8.0,2.0,4,0.1)]
        [TestCase(10000,3.0,8.0,2.0,4,0.25)]
        [TestCase(10000,3.0,8.0,5.0,1,0.1)]
        [TestCase(10000,3.0,8.0,5.0,1,0.25)]
        [TestCase(10000,3.0,8.0,5.0,4,0.1)]
        [TestCase(10000,3.0,8.0,5.0,4,0.25)]
        [TestCase(10000,3.0,15.0,2.0,1,0.1)]
        [TestCase(10000,3.0,15.0,2.0,1,0.25)]
        [TestCase(10000,3.0,15.0,2.0,4,0.1)]
        [TestCase(10000,3.0,15.0,2.0,4,0.25)]
        [TestCase(10000,3.0,15.0,5.0,1,0.1)]
        [TestCase(10000,3.0,15.0,5.0,1,0.25)]
        [TestCase(10000,3.0,15.0,5.0,4,0.1)]
        [TestCase(10000,3.0,15.0,5.0,4,0.25)]
        public void DownsampleOversampledDownsampledData_WNoise_WFlatLines(int N, double timebaseOriginal, double timebaseStored, double timebaseOversampled, int flatlinePeriods, double flatlineProportion)
        {
            // Define parameters
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.2,
                Ti_s = timebaseOriginal*60
            };

            double downsampleFactor = timebaseStored / timebaseOriginal;
            double oversampleFactor = timebaseStored / timebaseOversampled;
            double noiseAmplitude = 1;

            // Create plant model
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
            new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);

            // Create synthetic data
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Sinus(10, timebaseOriginal*100, timebaseOriginal, N));
            inputData.CreateTimestamps(timebaseOriginal);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);

            // Add noise to measured data
            simData.AddNoiseToSignal("SubProcess1-Output_Y", noiseAmplitude,123456);

            // Identify the model on original, high-resolution data
            var combinedData = inputData.Combine(simData);
            var pidDataSet = processSim.GetUnitDataSetForPID(combinedData, pidModel1);

            // The stored signal is then downsampled to a lower, stored resolution
            var combinedDataDownsampled = combinedData.CreateDownsampledCopy(downsampleFactor);
            var pidDataSetDownsampled = processSim.GetUnitDataSetForPID(combinedDataDownsampled, pidModel1);

            // At some point these signals are oversampled
            var combinedDataDownsampledOversampled = combinedDataDownsampled.CreateOversampledCopy(oversampleFactor);
            var pidDataSetDownsampledOversampled = processSim.GetUnitDataSetForPID(combinedDataDownsampledOversampled, pidModel1);

            // Something occurs somewhere along the way, causing sporadic data loss
            // Recreate data to avoid shallow copies
            var inputDataWithFlatlines = new TimeSeriesDataSet();
            inputDataWithFlatlines.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputDataWithFlatlines.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Sinus(10, timebaseOriginal*100, timebaseOriginal, N));
            inputDataWithFlatlines.CreateTimestamps(timebaseOriginal);
            var isOkWithFlatlines = processSim.Simulate(inputDataWithFlatlines, out TimeSeriesDataSet simDataWithFlatlines);
            simDataWithFlatlines.AddNoiseToSignal("SubProcess1-Output_Y", noiseAmplitude,123456);
            var combinedDataWithFlatlines = inputData.Combine(simDataWithFlatlines);
            var combinedDataDownsampledWithFlatlines = combinedDataWithFlatlines.CreateDownsampledCopy(downsampleFactor);
            var combinedDataDownsampledOversampledWithFlatlines = combinedDataDownsampledWithFlatlines.CreateOversampledCopy(oversampleFactor);
            var pidDataSetDownsampledOversampledWithFlatlines = processSim.GetUnitDataSetForPID(combinedDataDownsampledOversampledWithFlatlines, pidModel1);

            int N_downsampled_oversampled = pidDataSetDownsampledOversampled.Y_meas.Count();
            int flatlinePeriodLength = (int)(flatlineProportion * N_downsampled_oversampled / flatlinePeriods);
            for (int i = 0; i < flatlinePeriods; i++)
            {
                int flatlineStartIndex = (int)(N_downsampled_oversampled * ((double)i + 0.5) / flatlinePeriods - flatlinePeriodLength / 2);
                for (int j = 1; j < flatlinePeriodLength; j++)
                {
                    pidDataSetDownsampledOversampledWithFlatlines.U[flatlineStartIndex + j, 0] = pidDataSetDownsampledOversampledWithFlatlines.U[flatlineStartIndex, 0];
                    pidDataSetDownsampledOversampledWithFlatlines.Y_meas[flatlineStartIndex + j] = pidDataSetDownsampledOversampledWithFlatlines.Y_meas[flatlineStartIndex];
                    pidDataSetDownsampledOversampledWithFlatlines.Y_setpoint[flatlineStartIndex + j] = pidDataSetDownsampledOversampledWithFlatlines.Y_setpoint[flatlineStartIndex];
                }
            }
            var modelParametersOriginal = new PidIdentifier().Identify(ref pidDataSet);
            var modelParametersDownsampled = new PidIdentifier().Identify(ref pidDataSetDownsampled);
            var modelParametersDownsampledOversampled = new PidIdentifier().Identify(ref pidDataSetDownsampledOversampled);
            var modelParametersDownsampledOversampledWithFlatlines = new PidIdentifier().Identify(ref pidDataSetDownsampledOversampledWithFlatlines);

            // Plot results
            if (false)
            {
                Shared.EnablePlots();
                string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                    Replace(")", "_").Replace(",", "_") + "y";
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint, pidDataSet.U.GetColumn(0), pidDataSet.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u", "y3=u_sim" },
                    pidDataSet.GetTimeBase(), caseId+"_raw");
                Plot.FromList(new List<double[]>{ pidDataSetDownsampled.Y_meas, pidDataSetDownsampled.Y_setpoint, pidDataSetDownsampled.U.GetColumn(0), pidDataSetDownsampled.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_downsampled", "y1=y_setpoint_downsampled", "y3=u_downsampled", "y3=u_sim_downsampled" },
                    pidDataSetDownsampled.GetTimeBase(), caseId+"_downsampled");
                Plot.FromList(new List<double[]>{ pidDataSetDownsampledOversampled.Y_meas, pidDataSetDownsampledOversampled.Y_setpoint, pidDataSetDownsampledOversampled.U.GetColumn(0), pidDataSetDownsampledOversampled.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_downsampled_oversampled", "y1=y_setpoint_downsampled_oversampled", "y3=u_downsampled_oversampled", "y3=u_sim_downsampled_oversampled" },
                    pidDataSetDownsampledOversampled.GetTimeBase(), caseId+"_downsampled_oversampled");
                Plot.FromList(new List<double[]>{ pidDataSetDownsampledOversampledWithFlatlines.Y_meas, pidDataSetDownsampledOversampledWithFlatlines.Y_setpoint, pidDataSetDownsampledOversampledWithFlatlines.U.GetColumn(0), pidDataSetDownsampledOversampledWithFlatlines.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_downsampled_oversampled_withFlatlines", "y1=y_setpoint_downsampled_oversampled_withFlatlines", "y3=u_downsampled_oversampled_withFlatlines", "y3=u_sim_downsampled_oversampled_withFlatlines" },
                    pidDataSetDownsampledOversampledWithFlatlines.GetTimeBase(), caseId+"_downsampled_oversampled_withFlatlines");
                Shared.DisablePlots();
            }

            // Assert that the identification on the final dataset is able to approximately identify the parameters of the original data
            // This is a case where the original dataset is significantly altered before identification, so significant slack is allowed for now.
            Assert.IsTrue(Math.Abs(modelParametersDownsampledOversampledWithFlatlines.Kp - pidParameters1.Kp) < 0.15 * pidParameters1.Kp);
            Assert.IsTrue(Math.Abs(modelParametersDownsampledOversampledWithFlatlines.Ti_s - pidParameters1.Ti_s) < 0.2 * pidParameters1.Ti_s);
            Assert.IsTrue(Math.Abs(modelParametersDownsampledOversampledWithFlatlines.Fitting.TimeBase_s - modelParametersOriginal.Fitting.TimeBase_s * downsampleFactor) < 0.85 * modelParametersOriginal.Fitting.TimeBase_s * downsampleFactor);
        }*/



    }
}
