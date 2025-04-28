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


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
            processModel1 = new UnitModel(modelParameters1, "SubProcess1");
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


        /// <summary>
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
            var oversampledModelParameters_control = new PidIdentifier().Identify(ref pidDataSetOversampled_control, downsampleOversampledData: false, ignoreFlatLines: false);
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
        }
        
        /// <summary>
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
            var oversampledModelParameters_control = new PidIdentifier().Identify(ref pidDataSetOversampled_control, downsampleOversampledData: false, ignoreFlatLines: false);
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

       



    }
}
