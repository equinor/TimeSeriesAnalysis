using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Tests.DisturbanceID;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceID
{
    [TestFixture]
    class ClosedLoopIdentifierTester_StaticSISO
    {
        const bool doPlot = true;
        const bool isStatic = true;

        UnitParameters modelParameters = new UnitParameters
        {
            TimeConstant_s = 0,
            LinearGains = new double[] { 1.5 },
            TimeDelay_s = 0,
            Bias = 5
        };

        int N = 300;
        DateTime t0 = new DateTime(2010,1,1);
        int timeBase_s = 1;

       double noiseAmplitude = 0.01;

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }

        [TestCase(5,1.0, 10)]
        [TestCase(1,1.0, 10)] 

        public void StepDisturbanceANDSetpointStep(double distStepAmplitude, double ysetStepAmplitude, double precisionPrc)
        {
            int N = 100;

            var locParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.5 },
                TimeDelay_s = 0,
                Bias = 5
            };
            // this unit test is quite sensitive to the noise in the disturbance
            var trueDisturbance = TimeSeriesCreator.Step(80, N, 0, distStepAmplitude).Add(TimeSeriesCreator.Noise(N, 0.001)); ;
            var yset = TimeSeriesCreator.Step(10, N, 50, 50+ ysetStepAmplitude);//do step before disturbance
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc,false, true);
        }

        [TestCase(5, 1.0,20,500 )]
        [TestCase(1, 5.0,5,500 )]

        public void SinusDisturbanceANDSetpointStep(double distSinusAmplitude, double ysetStepAmplitude, 
            double gainPrecisionPrc, int N)
        {
            var trueDisturbance = TimeSeriesCreator.Sinus(distSinusAmplitude,N/8,timeBase_s,N );
            var yset = TimeSeriesCreator.Step(N/2, N, 50, 50 + ysetStepAmplitude);//do step before disturbance
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,
                false, true, yset, gainPrecisionPrc,false, isStatic);
        }

        [TestCase(5, 1.0,500,Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(1, 5.0,500,Explicit = true, Category = "NotWorking_AcceptanceTest")]

        public void SinusDisturbance(double distSinusAmplitude, double gainPrecisionPrc, int N)
        {
            var period = N / 2;
           
            var  modelParametersLoc = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.5 },
                TimeDelay_s = 0,
                Bias = 5
            };
          // try with steady-state before and after to see if this improves estimates
          var trueDisturbance = TimeSeriesCreator.Concat(TimeSeriesCreator.Constant(0,100), 
          TimeSeriesCreator.Concat(TimeSeriesCreator.Sinus(distSinusAmplitude,period,timeBase_s,N ), TimeSeriesCreator.Constant(0,200)));
            trueDisturbance = trueDisturbance.Add(TimeSeriesCreator.Noise(trueDisturbance.Length, noiseAmplitude));

          var yset = TimeSeriesCreator.Constant( 50,trueDisturbance.Length);
          CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParametersLoc, "Process"), trueDisturbance,
            false, true, yset, gainPrecisionPrc,false, isStatic);
        }




        // 0.25: saturates the controller
        // gain of 5 starts giving huge oscillations...

        // generally, the smaller the process gain, the lower the precision of the estimated process gain.
        //(halving the gain-> halving the precision)
        // first seed
        [TestCase(1, 0.1, 25, 105,1000)]
        [TestCase(2, 0.1, 15, 105,1500)]
        // second seed 
        [TestCase(1, 1, 10, 50,1000)]

        [TestCase(2, 1, 12, 50, 2000)]
        // third  seed 
        [TestCase(1, 0.1, 16, 71,1500)]
        [TestCase(2, 0.1, 12, 70, 2000)]



        public void RandomWalkDisturbance(double procGain, double distAmplitude, double precisionPrc,int seed, int N)
        {
          //  int N = 2000;
            bool doBadData = false;
            var locParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { procGain },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.RandomWalk(N,distAmplitude, 0, seed);
            var yset = TimeSeriesCreator.Constant( 50, N);
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc,doBadData,isStatic);
        }

        [TestCase(5, 1.0,5),NonParallelizable]
        public void StepDisturbanceANDSetpointSinus(double distStepAmplitude, double ysetStepAmplitude,
            double precisionPrc )
        {
            int N = 300;
            var vec = new Vec();
         
            var locParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, distStepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude));
            var yset = vec.Add(TimeSeriesCreator.Sinus(ysetStepAmplitude, N / 8, timeBase_s, N),TimeSeriesCreator.Constant(50,N));

            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "StaticProcess"), trueDisturbance,
                false, true, yset, precisionPrc,false, true);
        }

        [TestCase(5,5)]
        [TestCase(-5,5)]         
        public void LongStepDisturbance_EstimatesOk(double stepAmplitude, double gainPrecisionPrc)
        {
            bool doInvertGain = false;
            N = 300;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude));;
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "StaticProcess"), trueDisturbance,doInvertGain,true, null, gainPrecisionPrc, false, true);
        }


        // this works as long as only static identification is used in the closed-loop identifier,
       // issue! this hangs for some reason
        [Test]
        public void FlatData_DoesNotCrash()
        {
            double stepAmplitude = 0;
            N = 300;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude));;
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), 
                trueDisturbance,false,false);
        }

        [Test()]
        public void StepDisturbanceWITHBadDataPoints_IsExcludedFromAnalysis()
        {
            double stepAmplitude = 10;
            double gainTolPrc = 10;
            bool doAddBadData = true;
            N = 350;
            var locParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.5 },
                TimeDelay_s = 0,
                Bias = 5
            };

            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude));;
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"),
                trueDisturbance, false, true,null,gainTolPrc, doAddBadData,true);
        }

        [TestCase(-5,5, false)]
        [TestCase(5, 5, false)]
        [TestCase(10, 5 ,false )]
        [TestCase(5, 5, true)]

        public void StepDisturbance_EstimatesOk(double stepAmplitude, double gainTolPrc, bool doNegativeGain =false)
        {
            int N = 50;

            var trueDisturbance = TimeSeriesCreator.Step(10, N, 0, stepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude));
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), 
                trueDisturbance, doNegativeGain,true,null, gainTolPrc, false, true);
        }




    }
}
