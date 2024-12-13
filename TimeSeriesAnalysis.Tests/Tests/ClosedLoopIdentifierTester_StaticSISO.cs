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

        [TestCase(5,1.0, 5)]
        [TestCase(1,1.0, 5)] 
        [TestCase(1,5.0, 5)]
        public void StepDisturbanceANDSetpointStep(double distStepAmplitude, double ysetStepAmplitude, double precisionPrc)
        {
            var locParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.5 },
                TimeDelay_s = 0,
                Bias = 5
            };

            var trueDisturbance = TimeSeriesCreator.Step(160, N, 0, distStepAmplitude);
            var yset = TimeSeriesCreator.Step(50, N, 50, 50+ ysetStepAmplitude);//do step before disturbance
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc,false, true);
        }

        /*
        I think the reason this test struggles is that closedloopestimator tries to find the process model that results in the
        disturbance with the smallest average change, but for continously acting disturbances like this sinus disturbance, 
        this may not be as good an assumption as for the step disturbances considered in other tests. 
        */

        [TestCase(5, 1.0,5 )]
        [TestCase(1, 1.0,5)]
        [TestCase(1, 5.0,5 )]// this only works when the step change is much bigger than the disturbance

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

        // first seed
        [TestCase(0.5, 0.1,50, 105)]// unacceptable precision!
        [TestCase(0.5, 1, 40, 105)]// unacceptable precision!
        [TestCase(1, 0.1, 26, 105)]
        [TestCase(1, 1, 26, 105)]
        [TestCase(2, 0.1, 15, 105)]
        [TestCase(2, 1, 15, 105)]
        // second seed 
        [TestCase(0.5, 0.1, 40, 50)]// unacceptable precision!
        [TestCase(0.5, 1, 40, 50)]// unacceptable precision!
        [TestCase(1, 0.1, 26, 50)]
        [TestCase(1, 1, 26, 50)]
        [TestCase(2, 0.1, 15, 50)]
        [TestCase(2, 1, 15, 50)]
        // third  seed 
        [TestCase(0.5, 0.1,50, 70)] // unacceptable precision!
        [TestCase(0.5, 1, 50, 70)]// unacceptable precision!
        [TestCase(1, 0.1, 26, 70)]
        [TestCase(1, 1, 26, 70)]
        [TestCase(2, 0.1, 15, 70)]
        [TestCase(2, 1, 15, 70)]


        public void RandomWalkDisturbance(double procGain, double distAmplitude, double precisionPrc,int seed)
        {
         //   int seed = 50;// works fairly well..
         //   int seed = 100;// much harder for some reason

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


        // this works as long as only static identifiation is used in the closed-looop identifier,
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

        [TestCase(Category = "NotWorking_AcceptanceTest")]
        public void BadDataPoints_IsExcludedFromAnalysis()
        {
            double stepAmplitude = 1;
            bool doAddBadData = true;
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"),
                trueDisturbance, false, true,null,10, doAddBadData);
        }

        [TestCase(-5,10, false)]
        [TestCase(5, 10, false)]
        [TestCase(10, 10 ,false )]
        [TestCase(5, 10, true)]

        public void StepDisturbance_EstimatesOk(double stepAmplitude, double processGainAllowedOffsetPrc, 
            bool doNegativeGain =false)
        {
             //Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,
                doNegativeGain,true,null,  processGainAllowedOffsetPrc,false, true);
            //Shared.DisablePlots();
        }




    }
}
