using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Tests.DisturbanceID;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceID
{
    [TestFixture]
    class ClosedLoopIdentifierTester_DynamicSISO
    {
        const bool doPlot = true;

        UnitParameters modelParameters = new UnitParameters
        {
            TimeConstant_s = 10,
            LinearGains = new double[] { 1.5 },
            TimeDelay_s = 0,//was: 5
            Bias = 5
        };

        static PidParameters pidParameters1 = new PidParameters()
        {
            Kp = 0.2,
            Ti_s = 20
        };


        int timeBase_s = 1;
        int N = 300;
        DateTime t0 = new DateTime(2010,1,1);


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }


        [TestCase(5,1.0,5)]
        [TestCase(1,1.0,5)] 
        public void StepDisturbanceANDSetpointStep(double distStepAmplitude, double ysetStepAmplitude,double precisionPrc)
        {
            var locParams = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1.2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.Step(160, N, 0, distStepAmplitude);
            var yset = TimeSeriesCreator.Step(50, N, 50, 50+ ysetStepAmplitude);//do step before disturbance
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParams, "DynProcess"), trueDisturbance,
                false, true, yset, precisionPrc);
        }

        // 0.25: saturates the controller
        [TestCase(1, 0.1, 30, Explicit = true,Category = "NotWorking_AcceptanceTest")]
        [TestCase(1, 1, 30, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(2, 0.1, 30, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(2, 1, 30, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        // gain of 5 starts giving huge oscillations...

        public void RandomWalkDisturbance(double procGain, double distAmplitude, double gainPrecisionPrc)
        {
            int seed = 50; // important: this seed will be able to find a model when time constant is zero
            int N = 2000;
            var locParameters = new UnitParameters
            {
                TimeConstant_s = 10, // should be nonzero for dynamic test
                LinearGains = new double[] { procGain },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.RandomWalk(N,distAmplitude, 0, seed);
            var yset = TimeSeriesCreator.Constant( 50, N);
            Shared.EnablePlots();
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"), trueDisturbance,
                false, true, yset, gainPrecisionPrc);
            Shared.DisablePlots();
        }

        [TestCase(5, 1.0,500, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(1, 5.0,500,Explicit = true, Category = "NotWorking_AcceptanceTest")]

        public void SinusDisturbance(double distSinusAmplitude, double gainPrecisionPrc, int N)
        {
            var period = N / 2;
           
            var  modelParametersLoc = new UnitParameters
            {
                TimeConstant_s = 20,
                LinearGains = new double[] { 1.5 },
                TimeDelay_s = 0,
                Bias = 5
            };
          // try with steady-state before and after to see if this improves estimates
          var trueDisturbance = TimeSeriesCreator.Concat(TimeSeriesCreator.Constant(0,100), TimeSeriesCreator.Concat(TimeSeriesCreator.Sinus(distSinusAmplitude,period,timeBase_s,N ), TimeSeriesCreator.Constant(0,200)));
          var yset = TimeSeriesCreator.Constant( 50,trueDisturbance.Length);
          CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParametersLoc, "Process"), trueDisturbance,
            false, true, yset, gainPrecisionPrc,false, isStatic:false);
        }



        // this test works when run alone, but fails when all test are run together.
        // likely a race condition in the GenericDisturbanceTest
        
        [TestCase(5, 1.0, Explicit = true, Category = "NotWorking_AcceptanceTest"), NonParallelizable]
        //[TestCase(0, 1.0)]//debug, test performance if no disturbance

        public void StepDistANDSetpointSinus(double distStepAmplitude, double ysetSinusAmplitude)
        {
            Vec vec = new Vec();
            double precisionPrc = 20;
            var  locParameters = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1.5 },
                TimeDelay_s = 0,
                Bias = 5
            };

            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, distStepAmplitude);
            var yset = vec.Add(TimeSeriesCreator.Sinus(ysetSinusAmplitude, N / 2, timeBase_s, N),TimeSeriesCreator.Constant(50,N));

            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc);
        }



        [TestCase(5,5)]
        [TestCase(-5,5)]         
        public void LongStepDist_EstimatesOk(double stepAmplitude,double procGainAllowedOffSetPrc)
        {
            double noiseAmplitude = 0.01;
            bool doInvertGain = false;
            N = 400;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude).
                Add(TimeSeriesCreator.Noise(N, noiseAmplitude));
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,doInvertGain,true,null,procGainAllowedOffSetPrc);
        }

        /*
        [TestCase()]
        public void StepAtStartOfDataset_IsExcludedFromAnalysis()
        {
            double stepAmplitude = 1;
            bool doAddBadData = true;
            int N = 100;
            var trueDisturbance = TimeSeriesCreator.Step(10, N, 0, stepAmplitude);
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"),
                trueDisturbance, false, true,null,10, doAddBadData);
        }*/

        [TestCase(-5,5),NonParallelizable]
        [TestCase(5, 5)]
        [TestCase(10, 5)]
        public void StepDisturbance_EstimatesOk(double stepAmplitude, double processGainAllowedOffsetPrc, 
            bool doNegativeGain =false)
        {
            int N = 100;
            double noiseAmplitude = 0.01;
            Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.Step(40, N, 0, stepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude));
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,
                doNegativeGain,true,null,  processGainAllowedOffsetPrc);
            Shared.DisablePlots();
        }




    }
}
