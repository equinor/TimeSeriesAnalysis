using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.PidID
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
        [TestCase(1, 0.0,1)]
        [TestCase(1, 0.1,8)]
        [TestCase(2, 0.1,5)]
        [TestCase(5, 0.1,2)]

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
            inputData.CreateTimestamps(timeBase_s,t0);
            var isOk = processSim.Simulate(inputData,out TimeSeriesDataSet simData);
            simData.AddNoiseToSignal("SubProcess1-Output_Y", yNoiseAmplitude,0);
            Assert.IsTrue(isOk);

            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);

            Assert.AreEqual(idResult.GetWarnings().Count(),0);
            Console.WriteLine("Kp:" + idResult.Kp.ToString("F2") + " Ti:" + idResult.Ti_s.ToString("F2"));

            //   Shared.EnablePlots();
            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                Replace(")", "_").Replace(",", "_") + "y";
            Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),pidDataSet.U_sim.GetColumn(0)}, 
                new List<string> { "y1=y meas", "y1=y set", "y3=u","y3=u_sim" },
                pidDataSet.GetTimeBase(), caseId);
            //Shared.DisablePlots();

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
            inputData.CreateTimestamps(timeBase_s, t0);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            simData.AddNoiseToSignal("SubProcess1-Output_Y", yNoiseAmplitude,0);
            Assert.IsTrue(isOk);

            var combinedData = inputData.Combine(simData);
            var downsampleData = combinedData.CreateDownsampledCopy(downsampleFactor);
            var pidDataSet = processSim.GetUnitDataSetForPID(downsampleData, pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);
            Console.WriteLine("Kp:" + idResult.Kp.ToString("F2") + " Ti:" + idResult.Ti_s.ToString("F2"));

            Assert.AreEqual(idResult.GetWarnings().Count(), 0);

            //   Shared.EnablePlots();
            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                Replace(")", "_").Replace(",", "_") + "y";
            Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),pidDataSet.U_sim.GetColumn(0)},
                new List<string> { "y1=y meas", "y1=y set", "y3=u", "y3=u_sim" },
                pidDataSet.GetTimeBase(), caseId);
            //Shared.DisablePlots();

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
        /*
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(50)]
        public void Pcontroller_estimateU0(double u0)
        {
            double yNoiseAmplitude = 0.1; 
            double stepAmplitude = 1;
            double tolerancePrc = 10;
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 0,
                u0 = u0,
                
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
            simData.AddNoiseToSignal("SubProcess1-Output_Y", yNoiseAmplitude);
            Assert.IsTrue(isOk);

            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);

            Shared.EnablePlots();
            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                Replace(")", "_").Replace(",", "_") + "y";
            Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),pidDataSet.U_sim.GetColumn(0)},
                new List<string> { "y1=y meas", "y1=y set", "y3=u", "y3=u_sim" },
                pidDataSet.GetTimeBase(), caseId);
            Shared.DisablePlots();


            Console.WriteLine("Kp:" + idResult.Kp.ToString("F2") + " Ti:" + idResult.Ti_s.ToString("F2"));
            Console.WriteLine("u0:" + idResult.u0.ToString("F2"));
            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp) < pidParameters1.Kp * tolerancePrc / 100, "Kp too far off:" + idResult.Kp);
            Assert.IsTrue(idResult.Ti_s < 1);
            Assert.IsTrue(Math.Abs(pidParameters1.u0 - idResult.u0) < pidParameters1.u0 * tolerancePrc / 100, "u0 too far off:" + idResult.u0);


        }*/

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
         //   Shared.EnablePlots();
            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                Replace(")", "_").Replace(",", "_") + "y";
            Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),pidDataSet.U_sim.GetColumn(0)},
                new List<string> { "y1=y meas", "y1=y set", "y3=u", "y3=u_sim" },
                pidDataSet.GetTimeBase(), caseId);
           // Shared.DisablePlots();

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






    }
}
