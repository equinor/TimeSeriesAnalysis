using NuGet.Frameworks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Dynamic.CommonDataPreprocessing;
using TimeSeriesAnalysis.Utility;
using CategoryAttribute = NUnit.Framework.CategoryAttribute;

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
        [TestCase(1, 0.01,3)]
        [TestCase(1, 0.01,5)]
        [TestCase(2, 0.01,3)]
        [TestCase(5, 0.01,3)]

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
            Console.WriteLine(idResult);
            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp)< pidParameters1.Kp * tolerancePrc / 100, "Estimated Kp:"+ idResult.Kp + "True Kp:" + pidParameters1.Kp);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s * tolerancePrc / 100, "Estimated Ti_s:" + idResult.Ti_s + "True Ti_s:" + pidParameters1.Ti_s);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
            Assert.Greater(idResult.Fitting.FitScorePrc,91, "fit score be high" );
            Assert.AreEqual(idResult.Fitting.NumSimulatorRestarts, 0, "no sim restarts");

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
            Console.WriteLine(idResult.ToString());

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
            Assert.Greater(idResult.Fitting.FitScorePrc, 80, "fit score be high");
            Assert.AreEqual(idResult.Fitting.NumSimulatorRestarts, 0, "no sim restarts");
        }


        [TestCase(5, 0.01,5)]
        [TestCase(1, 0.01,5)]
        [TestCase(5, 0.05,5)]
        [TestCase(1, 0.05,10)]

        public void DistStep_WNoise_KpAndTiEstimatedOk(double stepAmplitude, double yNoiseAmplitude, double tolerancePrc )
        {

            int timeBaseLoc_s = 2;
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
            inputData.CreateTimestamps(timeBaseLoc_s);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            simData.AddNoiseToSignal("SubProcess1-Output_Y", yNoiseAmplitude,890978);
            Assert.IsTrue(isOk);

            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);
            Console.WriteLine(idResult.ToString());

            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp) < pidParameters1.Kp * tolerancePrc / 100, "Kp too far off:"+ idResult.Kp);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s *tolerancePrc / 100, "Ti_S too far off:" + idResult.Ti_s);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
            Assert.Greater(idResult.Fitting.FitScorePrc, 95, "fit score be high");
            Assert.AreEqual(idResult.Fitting.NumSimulatorRestarts, 0, "no sim restarts");
        }

        // want to see how robust PidIdentifier is when it has to find Kp and Ti on a lower sampling rate than the "actual" rate

        // when noise is added in the fully sampled, case the solver uses a low-pass filtering of ymeas as a key
        // tactic to improve estimates of Kp and Ti. In the downsampled case,it is not possible to use filtering in the same 
        // way. It may be that instead the solver should run the pid-controller at its original time sampling,
        // maybe this will casue the noise to smoothe out

        [TestCase(2,0.01,5)]
        [TestCase(2,0.01,20)]
        [TestCase(4,0.01,10)] 
        [TestCase(4,0.01,20)]
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

            Console.WriteLine(idResult.ToString());
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
            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp) < pidParameters1.Kp * tolerancePrc / 100, 
                "Kp estimate:"+ idResult.Kp + "versus true :" + pidParameters1.Kp);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s * tolerancePrc / 100, 
                    "Ti_s estimate"+ idResult.Ti_s + "versus true :" + pidParameters1.Ti_s);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
            Assert.Greater(idResult.Fitting.FitScorePrc, 96);
        }



        [TestCase(4)]
        public void OscillatingDisturbanceCorrectKpSign(double timeBase_s)
        {
            // Define parameters
            var trueParams = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 50
            };

            // Create plant model
            var pidModel1 = new PidModel(trueParams, "PID1");
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
            var idResult = new PidIdentifier().Identify(ref pidDataSet);

            Console.WriteLine(idResult.ToString());

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
            Assert.IsTrue(Math.Abs(trueParams.Kp - idResult.Kp) < 0.02 * trueParams.Kp, "Kp too far off: "+ idResult.Kp); // Allow 2% slack on Kp
            Assert.IsTrue(Math.Abs(trueParams.Ti_s - idResult.Ti_s) < 0.05 * trueParams.Ti_s, "Ti too far off:"+ idResult.Ti_s); // Allow 5% slack on Ti
            Assert.Greater(idResult.Fitting.FitScorePrc, 99.99, "fit score should be almost 100 because no noise ");

        }


        /// <summary>
        /// It is not uncommon for datasets to have flatlines for various reasons. Such periods should be filtered out in the indicesToIgnore.
        /// </summary>
        /// <param name="N">number of samples in the stored dataset,</param>
        /// <param name="timebase">Timebase of the signals.</param>
        /// <param name="flatlinePeriods">Number of periods with flatlined data.</param>
        /// <param name="flatlineProportion">Proportion of the dataset that should be flatlines.</param>
        [TestCase(180,1,1,0.1)]// There is one flatline period 
        [TestCase(280, 1, 2, 0.15)]// There is two flatline periods (this test could be improved!)

        public void IndicesToIgnore_WFlatLines(int N, double timebase, int flatlinePeriods, double flatlineProportion)
        {
            // Define parameters
            var trueParameters = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 50
            };

            // Create plant model
            var pidModel1 = new PidModel(trueParameters, "PID1");
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

            string caseId = TestContext.CurrentContext.Test.Name;

            if (false)// plot the raw data before flatline is created
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> { pidDataSet.Y_meas, pidDataSet.Y_setpoint, pidDataSet.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u"},
                    pidDataSet.GetTimeBase(), caseId + "_beforeSim");
                Shared.DisablePlots();
            }
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
            var idParams = new PidIdentifier().Identify(ref pidDataSetWithFlatlines, tryDownsampling:true);// also creates a U_sim in pidDataSetWithFlatlines
            Console.WriteLine(idParams.ToString());
            // Plot results
            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]>{ pidDataSetWithFlatlines.Y_meas, pidDataSetWithFlatlines.Y_setpoint, 
                    pidDataSetWithFlatlines.U.GetColumn(0), pidDataSetWithFlatlines.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_with_flatlines", "y1=y_setpoint_with_flatlines", "y3=u_with_flatlines", "y3=u_sim_with_flatlines" },
                    pidDataSetWithFlatlines.GetTimeBase(), caseId+"_with_flatlines");
                Shared.DisablePlots();
            }
       
            Assert.IsTrue(Math.Abs(idParams.Kp - trueParameters.Kp) < 0.02 * trueParameters.Kp, "Kp too far off :"+ idParams.Kp); 
            Assert.IsTrue(Math.Abs(1- idParams.Ti_s/ trueParameters.Ti_s) < 0.15, "Ti too far off"+ idParams.Ti_s); 
            Assert.Greater(idParams.Fitting.FitScorePrc, 70, "fit score should ignore bad data and give a high score:");
     //       Assert.IsTrue(idParams.Fitting.NumSimulatorRestarts == flatlinePeriods, "simulator should restart for each flatline period");
        }

        public enum BadDataEnum { U, Y_set, Y_meas}


        [TestCase(80, 1, 0.1, BadDataEnum.U)]
        [TestCase(80, 1, 0.1, BadDataEnum.Y_set)]
        [TestCase(80, 1, 0.1, BadDataEnum.Y_meas)]

        public void IndicesToIgnore_BadData(int N, double timebase, double badDataProportion, BadDataEnum badDataType)
        {
            // Define parameters
            var trueParameters = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 50
            };

            // Create plant model
            var pidModel1 = new PidModel(trueParameters, "PID1");
            var processSim = new PlantSimulator(
            new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);

            // Create synthetic data
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), TimeSeriesCreator.Sinus(10, timebase * 20, timebase, N));
            inputData.CreateTimestamps(timebase);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk, "simulate did not run");
            var combinedData = inputData.Combine(simData);
            var pidDataSet = processSim.GetUnitDataSetForPID(combinedData, pidModel1);

            var combinedDataFlatLines = new TimeSeriesDataSet(combinedData);

            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").Replace(")", "_").Replace(",", "_") + "y";

            if (false)// plot the raw data before flatline is created
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]> { pidDataSet.Y_meas, pidDataSet.Y_setpoint, pidDataSet.U.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u" },
                    pidDataSet.GetTimeBase(), caseId + "_beforeSim");
                Shared.DisablePlots();
            }
            var pidDataSetWithBadData = processSim.GetUnitDataSetForPID(combinedDataFlatLines, pidModel1);

            int nBadDataPointsAddedCouter = 1; // works if set to one, but simulation struggles if first indices are bad (if set to zero)
            int nBadDataPointsToAdd = (int)Math.Floor(N * badDataProportion);

            var rand = new Random();

            var trueBadDataIdx = new List<int>();
            /// create the flat data sets.
            while(nBadDataPointsAddedCouter< nBadDataPointsToAdd)
            {
                int badDataStartIndex = (int)Math.Floor((double)N * nBadDataPointsAddedCouter/ nBadDataPointsToAdd);
                int badDataPeriodLength = 1;//  (int)Math.Ceiling(rand.NextDouble()* nBadDataPointsToAdd/5);
                for (int j = 0; j < badDataPeriodLength; j++)
                {
                    int curBadDataIdx = badDataStartIndex + j;
                    if (badDataType == BadDataEnum.U)
                        pidDataSetWithBadData.U[curBadDataIdx, 0] = inputData.BadDataID;
                    else if(badDataType == BadDataEnum.Y_meas)
                        pidDataSetWithBadData.Y_meas[curBadDataIdx] = inputData.BadDataID;
                    else if (badDataType == BadDataEnum.Y_set)
                        pidDataSetWithBadData.Y_setpoint[curBadDataIdx] = inputData.BadDataID;
                    nBadDataPointsAddedCouter++;
                    trueBadDataIdx.Add(curBadDataIdx);
                }
            }
            var idParameters = new PidIdentifier().Identify(ref pidDataSetWithBadData);
            Console.WriteLine(idParameters.ToString()); 
            // Plot results
            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]>{ pidDataSetWithBadData.Y_meas, pidDataSetWithBadData.Y_setpoint,
                    pidDataSetWithBadData.U.GetColumn(0), pidDataSetWithBadData.U_sim.GetColumn(0)},
                    new List<string> { "y1=y_meas_with_flatlines", "y1=y_setpoint_with_baddata", "y3=u_with_baddata", "y3=u_sim_with_baddata" },
                    pidDataSetWithBadData.GetTimeBase(), caseId + "_with_baddata");
                Shared.DisablePlots();
            }
            Assert.IsTrue(Math.Abs(idParameters.Kp - trueParameters.Kp) < 0.02 * trueParameters.Kp, "Kp too far off :" + idParameters.Kp); 
            Assert.IsTrue(Math.Abs(idParameters.Ti_s - trueParameters.Ti_s) < 0.05 * trueParameters.Ti_s, "Ti too far off" + idParameters.Ti_s); 
            Assert.Greater(idParameters.Fitting.FitScorePrc, 95, "fit score should ignore bad data and give a high score:");
            Assert.AreEqual(idParameters.Fitting.NumSimulatorRestarts, 0, "no sim restarts");
        }

        /// <summary>
        /// It is not uncommon for datasets to be oversampled from the resolution of the stored timeseries.
        /// The identification should then automatically attempt downsampling to improve the fitscore.
        /// </summary>
        /// <param name="N">number of samples in the stored dataset,</param>
        /// <param name="timebaseTrue">Timebase of the stored signals.</param>
        /// <param name="timebaseOversampled">Timebase of the oversampled data.</param>

        [TestCase(100, 6,5)]// the stored signal is oversampled by a factor 6/5ths (not a whole number)
        [TestCase(100, 6,4)]// the stored signal is oversampled by a factor 1.5(not whole number)
        [TestCase(100, 6,3)]// the stored signal is oversampled by a factor 2(whole number)
        [TestCase(100, 6,2)]// the stored signal is oversampled by a factor 3(whole number)


        public void OversampledData(int N, double timebaseTrue, double timebaseOversampled)
           {
               // Define parameters
               var truePidParams = new PidParameters()
               {
                   Kp = 0.5,
                   Ti_s = timebaseTrue*10
               };
               double oversampleFactor = timebaseTrue / timebaseOversampled;
               Assert.Greater(oversampleFactor, 1,"oversample factor should be above one!");
               // Create plant model
               var pidModel1 = new PidModel(truePidParams, "PID1");
               var processSim = new PlantSimulator( new List<ISimulatableModel> { pidModel1, processModel1 });
               processSim.ConnectModels(processModel1, pidModel1);
               processSim.ConnectModels(pidModel1, processModel1);

               // Create synthetic data
               var inputData = new TimeSeriesDataSet();
               inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
               inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), 
                   TimeSeriesCreator.Sinus(10, timebaseTrue*20, timebaseTrue, N));
               inputData.CreateTimestamps(timebaseTrue);
               var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
               var combinedData = inputData.Combine(simData);
               var pidDataSet = processSim.GetUnitDataSetForPID(combinedData, pidModel1);

               // Oversample synthetic data
               var combinedDataOversampled = combinedData.CreateOversampledCopy(timebaseOversampled);

               // Identify model on oversampled data
               var pidDataSetOversampled = processSim.GetUnitDataSetForPID(combinedDataOversampled, pidModel1);

                // try to create a downsampled copy of the dataset and give that to identification
                PidParameters idModelParams = new PidParameters();

                idModelParams = new PidIdentifier().Identify(ref pidDataSetOversampled);
           
                 if (false)
                {
                    Shared.EnablePlots();
                    string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                        Replace(")", "_").Replace(",", "_") + "y";
                    Plot.FromList(new List<double[]> { pidDataSetOversampled.Y_meas, pidDataSetOversampled.Y_setpoint, 
                        pidDataSetOversampled.U.GetColumn(0),pidDataSetOversampled.U_sim.GetColumn(0) },
                        new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u_meas", "y3=u_sim" },
                        pidDataSet.GetTimeBase(), caseId);
                    Shared.DisablePlots();
                }
                // test that the two methods are equivalent
                Console.WriteLine(idModelParams.ToString());

                // Plot results
                Assert.IsTrue(Math.Abs(idModelParams.Ti_s - truePidParams.Ti_s) < 0.1 * truePidParams.Ti_s,"Ti too far off!!");
                Assert.IsTrue(Math.Abs(idModelParams.Kp - truePidParams.Kp) < 0.1 * truePidParams.Kp, "Kp too far off!!"); 
                Assert.Greater(idModelParams.Fitting.FitScorePrc, 50, "FitScore poor!"); 
           }


        //
        // oversampling introduces phase-shift. 
        // 

        [TestCase(100, 6, 5), Explicit, Category("NotWorking_AcceptanceTest")] // the stored signal is oversampled by a factor 6/5ths (not a whole number)
                                                                              // ;Kp 0.505 Ti 60.8(great) but FitScore is 55%
        [TestCase(100, 6, 4), Category("NotWorking_AcceptanceTest")]// the stored signal is oversampled by a factor 1.5(not whole number)
                             // Kp 0.508 Ti 61.3 (good) but FitScore is 80
        [TestCase(100, 6, 3), Category("NotWorking_AcceptanceTest")]// the stored signal is oversampled by a factor 2(whole number)
                             // Kp 0.517, Ti 93s (not so good) Fit Score 87%
        [TestCase(100, 6, 2), Category("NotWorking_AcceptanceTest")]// the stored signal is oversampled by a factor 3(whole number): 
                             // Kp 0.52 Ti 62s (good)Fit Score 89% 

        public void OversampledData_VariableTimebase(int N, double timebaseTrue, double timebaseOversampled)
        {
            // Define parameters
            var truePidParams = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = timebaseTrue * 10
            };
            double oversampleFactor = timebaseTrue / timebaseOversampled;
            Assert.Greater(oversampleFactor, 1, "oversample factor should be above one!");
            // Create plant model
            var pidModel1 = new PidModel(truePidParams, "PID1");
            var processSim = new PlantSimulator(new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);

            // Create synthetic data
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D),
                TimeSeriesCreator.Sinus(10, timebaseTrue * 20, timebaseTrue, N));
            inputData.CreateTimestamps(timebaseTrue);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            var combinedData = inputData.Combine(simData);
            var pidDataSet = processSim.GetUnitDataSetForPID(combinedData, pidModel1);

            // Oversample synthetic data
            var combinedDataOversampled = combinedData.CreateOversampledCopy(timebaseOversampled);

            // Identify model on oversampled data
            var pidDataSetOversampled = processSim.GetUnitDataSetForPID(combinedDataOversampled, pidModel1);

            // pidIdentifier should itself try to vary the timebase.
            var idModelParams = new PidIdentifier().Identify(ref pidDataSetOversampled,tryVariableTimeBase: true);

            if (true)
            {
                Shared.EnablePlots();
                string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                    Replace(")", "_").Replace(",", "_") + "y";
                Plot.FromList(new List<double[]> { pidDataSetOversampled.Y_meas, pidDataSetOversampled.Y_setpoint, 
                    pidDataSetOversampled.U.GetColumn(0),pidDataSetOversampled.U_sim.GetColumn(0) },
                    new List<string> { "y1=y_meas", "y1=y_setpoint", "y3=u", "y3=u_sim" },
                    pidDataSet.GetTimeBase(), caseId);
                Shared.DisablePlots();
            }

            Console.WriteLine(idModelParams.ToString());

            Assert.IsTrue(Math.Abs(idModelParams.Ti_s - truePidParams.Ti_s) < 0.1 * truePidParams.Ti_s, "Ti too far off!!");
            Assert.IsTrue(Math.Abs(idModelParams.Kp - truePidParams.Kp) < 0.1 * truePidParams.Kp, "Kp too far off!!");
            Assert.Greater(idModelParams.Fitting.FitScorePrc, 99, "FitScore poor!"); 
        }


    }
}
