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


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }

        [TestCase(5,1.0, 10)]
        [TestCase(1,1.0, 10)] 
   //     [TestCase(1,5.0, 10)]
        public void StepDisturbanceANDSetpointStep(double distStepAmplitude, double ysetStepAmplitude, double precisionPrc)
        {
            double noiseAmplitude = 0.001;
            var locParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.5 },
                TimeDelay_s = 0,
                Bias = 5
            };

            var trueDisturbance = TimeSeriesCreator.Step(160, N, 0, distStepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude)); ;
            var yset = TimeSeriesCreator.Step(50, N, 50, 50+ ysetStepAmplitude);//do step before disturbance
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc,false, true);
        }

        [TestCase(5, 1.0,30 )]
        [TestCase(1, 1.0,30)]
        [TestCase(1, 5.0,30 )]

        public void SinusDisturbanceANDSetpointStep(double distSinusAmplitude, double ysetStepAmplitude, double gainPrecisionPrc)
        {
            var trueDisturbance = TimeSeriesCreator.Sinus(distSinusAmplitude,N/8,timeBase_s,N );
            var yset = TimeSeriesCreator.Step(N/2, N, 50, 50 + ysetStepAmplitude);//do step before disturbance
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,
                false, true, yset, gainPrecisionPrc,false, isStatic);
        }


        // 0.25: saturates the controller
        // gain of 5 starts giving huge oscillations...

        // generally, the smaller the process gain, the lower the precision of the estimated process gain.
        //(halfing the gain-> halving the precision)
        // first seed
        [TestCase(1, 0.1, 25, 105)]
       // [TestCase(1, 1, 25, 105)]
        [TestCase(2, 0.1, 15, 105)]
       // [TestCase(2, 1, 15, 105)]
        // second seed 
       // [TestCase(1, 0.1, 25, 50)]
        [TestCase(1, 1, 25, 50)]
      //  [TestCase(2, 0.1, 12, 50)]
        [TestCase(2, 1, 12, 50)]
        // third  seed 
        [TestCase(1, 0.1, 25, 71)]
      //  [TestCase(1, 1, 25, 71)]
        [TestCase(2, 0.1, 12, 70)]
      //  [TestCase(2, 1, 12, 70)]


        public void RandomWalkDisturbance(double procGain, double distAmplitude, double precisionPrc,int seed)
        {
            int N = 2000;
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

        [TestCase(5, 1.0,5)]
        public void StepDisturbanceANDSetpointSinus(double distStepAmplitude, double ysetStepAmplitude, double precisionPrc )
        {
            var vec = new Vec();
         
            var locParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, distStepAmplitude);
            var yset = vec.Add(TimeSeriesCreator.Sinus(ysetStepAmplitude, N / 4, timeBase_s, N),TimeSeriesCreator.Constant(50,N));

            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "StaticProcess"), trueDisturbance,
                false, true, yset, precisionPrc,false, true);
        }

        [TestCase(5,5)]
        [TestCase(-5,5)]         
        public void LongStepDisturbance_EstimatesOk(double stepAmplitude, double gainPrecisionPrc)
        {
            bool doInvertGain = false;
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "StaticProcess"), trueDisturbance,doInvertGain,true, null, gainPrecisionPrc, false, true);

        }


        // this works as long as only static identifiation is used in the closed-loop identifier,
       // issue! this hangs for some reason
        [Test]
        public void FlatData_DoesNotCrash()
        {
            double stepAmplitude = 0;
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), 
                trueDisturbance,false,false);
        }

        [Test()]
        public void StepDisturbanceWITHBadDataPoints_IsExcludedFromAnalysis()
        {
            double stepAmplitude = 10;
            double gainTolPrc = 10;
            bool doAddBadData = false;
            N = 350;
            var locParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.5 },
                TimeDelay_s = 0,
                Bias = 5
            };

            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"),
                trueDisturbance, false, true,null,gainTolPrc, doAddBadData,true);
        }

        [TestCase(-5,5, false)]
        [TestCase(5, 5, false)]
        [TestCase(10, 5 ,false )]
        [TestCase(5, 5, true)]

        public void StepDisturbance_EstimatesOk(double stepAmplitude, double gainTolPrc, bool doNegativeGain =false)
        {
             //Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), 
                trueDisturbance, doNegativeGain,true,null, gainTolPrc, false, true);
            //Shared.DisablePlots();
        }




    }
}
