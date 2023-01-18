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
        [TestCase(1, 0.0)]
        [TestCase(1, 0.1)]
        [TestCase(2, 0.1)]
        [TestCase(5, 0.1)]

        public void YsetpointStepChange_KpAndTiEstimatedOk(double ySetAmplitude, double yNoiseAmplitude)
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
            simData.AddNoiseToSignal("SubProcess1-Output_Y", yNoiseAmplitude);
            Assert.IsTrue(isOk);

            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);

            Assert.AreEqual(idResult.GetWarnings().Count(),0);

            Shared.EnablePlots();
            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                Replace(")", "_").Replace(",", "_") + "y";
            Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),pidDataSet.U_sim.GetColumn(0)}, 
                new List<string> { "y1=y meas", "y1=y set", "y3=u","y3=u_sim" },
                pidDataSet.GetTimeBase(), caseId);
            Shared.DisablePlots();

            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp)< pidParameters1.Kp/10,"Kp too far off!:"+ idResult.Kp);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s / 10,"Ti_s too far off!:"+ idResult.Ti_s);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
        }

        [TestCase(5)]
        [TestCase(-5)]
        [TestCase(1)]
        [TestCase(-1)]


        public void DisturbanceStepChange_KpAndTiEstimatedOk(double stepAmplitude)
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
            Assert.IsTrue(isOk);

            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);

            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp) < pidParameters1.Kp / 10,"Kp too far off:"+ idResult.Kp);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s / 10, "Ti_S too far off:" + idResult.Ti_s);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
        }
        [TestCase(1)]
        public void EstimationWithKpScaling_KpAndTiEstimatedOk(double stepAmplitude)
        {
        }

        // want to see how robust PidIdentifier is when it has to find Kp and Ti on a lower sampling rate than the "actual" rate
        [TestCase(4)]
        [TestCase(20)]
        public void DownsampleNoisyData_KpAndTiEstimatedOk(int downsampleFactor)
        {
            int N = 1000;
            double stepAmplitude = 1;
            double noiseAmplitude = 0.05;//should be much smaller than stepamplitude

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
            simData.AddNoiseToSignal("SubProcess1-Output_Y", noiseAmplitude);
            var combinedData = inputData.Combine(simData);
            var downsampleData = combinedData.CreateDownsampledCopy(downsampleFactor);
            // ----do not use inputData or simData below this line----
            var pidDataSet = processSim.GetUnitDataSetForPID(downsampleData, pidModel1);
            var idResult = new PidIdentifier().Identify(ref pidDataSet);
            Assert.IsTrue(Math.Abs(pidParameters1.Kp - idResult.Kp) < pidParameters1.Kp / 10,"Kp too far off!:"+ idResult.Kp);
            if (pidParameters1.Ti_s > 0)
            {
                Assert.IsTrue(Math.Abs(pidParameters1.Ti_s - idResult.Ti_s) < pidParameters1.Ti_s / 10, "Ti_s too far off!"+ idResult.Ti_s);
            }
            else
            {
                Assert.IsTrue(idResult.Ti_s < 1);
            }
        
        }






    }
}
