using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceID
{
    [TestFixture]
    class DisturbanceIDUnitTests
    {
        UnitParameters modelParameters1 = new UnitParameters
        {
            TimeConstant_s = 10,
            LinearGains = new double[] { 1 },
            TimeDelay_s = 5,
            Bias = 5
        };

        PidParameters pidParameters1 = new PidParameters()
        {
            Kp = 0.5,
            Ti_s = 20
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

        // todo: test for all four combinations of gain sign and disturbance sign

        // TODO: currently the sign of the gain changes from run1 to run2, indicating 
        // a programming error

        [TestCase(5)]
        public void StepChangeDisturbance_ProcessAndDisturbanceEstimatedOk(double stepAmplitude)
        {
            // create synthetic dataset
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            var trueDisturbance = TimeSeriesCreator.Step(N / 2, N, 0, stepAmplitude);
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50,N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), trueDisturbance);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);

            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);

            var modelId = new ClosedLoopUnitIdentifier();
            (var identifiedModel,var estDisturbance) = modelId.Identify(pidDataSet);

            Assert.IsTrue(estDisturbance != null);

            Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),
                estDisturbance, trueDisturbance },
                new List<string>{"y1=y meas", "y1=y set", "y2=u(right)","y3=est disturbance", "y3=total disturbance" },
                inputData.GetTimeBase());

            Vec vec = new Vec();
            Assert.IsTrue(vec.Mean(vec.Abs(vec.Subtract(trueDisturbance,estDisturbance)))< stepAmplitude/10);

   //         Assert.IsTrue(Math.Abs(modelParameters1.GetTotalCombinedProcessGain()- identifiedModel.modelParameters.GetTotalCombinedProcessGain()));
        }

        [TestCase(5,20)]
        public void SinusDisturbance_ProcessAndDisturbanceEstimatedOk(double sinusAmplitude, double sinusPeriod)
        {
            // create synthetic dataset
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), 
                TimeSeriesCreator.Sinus(sinusAmplitude, sinusPeriod, timeBase_s,N));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);





        }




    }
}
