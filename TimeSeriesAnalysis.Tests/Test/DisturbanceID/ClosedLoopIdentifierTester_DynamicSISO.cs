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

        double noiseAmplitude = 0.01;

        int timeBase_s = 1;
        int N = 300;
        DateTime t0 = new DateTime(2010,1,1);


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
            N = 300;
        }


        [TestCase(5,1.0,10), NonParallelizable]
        [TestCase(1,1.0,10)] 
        public void StepDisturbanceANDSetpointStep(double distStepAmplitude, double ysetStepAmplitude,double precisionPrc)
        {
            var locParams = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1.2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            // note that this unit test is quite sensitive to noise in the disturbance!
            var trueDisturbance = TimeSeriesCreator.Step(160, N, 0, distStepAmplitude).Add(TimeSeriesCreator.Noise(N, 0.001));
            var yset = TimeSeriesCreator.Step(50, N, 50, 50+ ysetStepAmplitude);//do step before disturbance
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParams, "DynProcess"), trueDisturbance,
                false, true, yset, precisionPrc);
        }



        /*
        Note that the performance of these have improved with the implementation of  dHLF/dHF fallback of clui,
        but moreso for the time constant than the gain estimates, which still hover around 1.2-1.5?
        */

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
           // Shared.EnablePlots();
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"), trueDisturbance,
                false, true, yset, gainPrecisionPrc);
           // Shared.DisablePlots();
        }

        /*
            Performance on these seem to be improved, it finds time-constants in the correct ballpark, but for some reason the gain estimates are always around 2?
        */

        [TestCase(1.5, 10,20, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(3, 10,20,Explicit = true, Category = "NotWorking_AcceptanceTest")]

        [TestCase(1.5, 10,10, Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(3, 10,10,Explicit = true, Category = "NotWorking_AcceptanceTest")]
        public void SinusDisturbance(double procGain, double gainPrecisionPrc, double timeConst_s)
        {
            int N = 500;
            var period = N / 3;
           
            var distSinusAmplitude = 2;

            var  modelParametersLoc = new UnitParameters
            {
                TimeConstant_s = timeConst_s,
                LinearGains = new double[] { procGain },
                TimeDelay_s = 0,
                Bias = 5
            };

          var noiseAmplitude = 0.01;

          // try with steady-state before and after to see if this improves estimates
          var trueDisturbance = TimeSeriesCreator.Concat(TimeSeriesCreator.Noise(100,noiseAmplitude), TimeSeriesCreator.Concat(TimeSeriesCreator.Sinus(distSinusAmplitude,period,timeBase_s,N ), 
          TimeSeriesCreator.Noise(200,noiseAmplitude)));
          var yset = TimeSeriesCreator.Constant( 50,trueDisturbance.Length);
          CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParametersLoc, "Process"), trueDisturbance,
            false, true, yset, gainPrecisionPrc,false, isStatic:false);
        }



        // have had some issues with this test suceeeding when run alone but failing when run together "run all"
        // possibly this is due to it being sensitive to the seed of the noise added to the disturbance, as a workaround a specific seed is used.
        
        [TestCase(1, 1.0), NonParallelizable]
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

            int seed = 51001;

            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, distStepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude, seed));
            var yset = vec.Add(TimeSeriesCreator.Sinus(ysetSinusAmplitude, N / 2, timeBase_s, N),TimeSeriesCreator.Constant(50,N));

            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc);
        }



        [TestCase(5,5)]
        [TestCase(-5,5)]         
        public void LongStepDist_EstimatesOk(double stepAmplitude,double procGainAllowedOffSetPrc)
        {

            bool doInvertGain = false;
       //     N = 400;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude));
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

        [TestCase(-5,10),NonParallelizable]
        [TestCase(5, 10)]
        [TestCase(10, 10)]
        public void StepDisturbance_EstimatesOk(double stepAmplitude, double processGainAllowedOffsetPrc, 
            bool doNegativeGain =false)
        {
            int N = 300;
            double noiseAmplitude = 0.01;
            Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.Step(40, N, 0, stepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude));
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,
                doNegativeGain,true,null,  processGainAllowedOffsetPrc);
            Shared.DisablePlots();
        }




    }
}
