﻿using System;
using System.Collections.Generic;
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
            var trueDisturbance = TimeSeriesCreator.Step(160, N, 0, distStepAmplitude);
            var yset = TimeSeriesCreator.Step(50, N, 50, 50+ ysetStepAmplitude);//do step before disturbance
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc);
        }

        /*
        I think the reason this test struggles is that closedloopestimator tries to find the process model that results in the
        disturbance with the smallest average change, but for continously acting disturbances like this sinus disturbance, 
        this may not be as good an assumption as for the step disturbances considered in other tests. 
        */

        [TestCase(5, 1.0), NonParallelizable]
        [TestCase(1, 1.0)]
        [TestCase(1, 5.0 )]// this only works when the step change is much bigger than the disturbance

        public void SinusDisturbanceANDSetpointStep(double distSinusAmplitude, double ysetStepAmplitude)
        {
            double precisionPrc = 20;

            var trueDisturbance = TimeSeriesCreator.Sinus(distSinusAmplitude,N/8,timeBase_s,N );
            var yset = TimeSeriesCreator.Step(N/2, N, 50, 50 + ysetStepAmplitude);//do step before disturbance
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc,false, isStatic);
        }

        [TestCase(2, Category = "NotWorking_AcceptanceTest")]
        [TestCase(1,  Category = "NotWorking_AcceptanceTest")]
        [TestCase(0.5, Category = "NotWorking_AcceptanceTest")]
        public void SinusDisturbance(double distSinusAmplitude)
        {
            double precisionPrc = 20;

            var trueDisturbance = TimeSeriesCreator.Sinus(distSinusAmplitude, N / 8, timeBase_s, N);
            var yset = TimeSeriesCreator.Step(N / 2, N, 50, 50 );
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc,isStatic);
        }


        // 0.25: saturates the controller
        [TestCase(0.5, 0.1,20, Category = "NotWorking_AcceptanceTest")]
        [TestCase(0.5, 1, 20, Category = "NotWorking_AcceptanceTest")]
        [TestCase(1, 0.1, 20, Category = "NotWorking_AcceptanceTest")]
        [TestCase(1, 1, 20, Category = "NotWorking_AcceptanceTest")]
        [TestCase(2, 0.1, 20)]
        [TestCase(2, 1, 20)]
        // gain of 5 starts giving huge oscillations...

        public void RandomWalkDisturbance(double procGain, double distAmplitude, double precisionPrc)
        {
         //   int seed = 50;// works fairly well..
         //   int seed = 100;// much harder for some reason
            int seed = 105;

            int N = 2000;

            UnitParameters staticModelParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { procGain },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.RandomWalk(N,distAmplitude, 0, seed);
            var yset = TimeSeriesCreator.Constant( 50, N);
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(staticModelParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc);
        }

        // these tests seem to run well when run individuall, but when "run all" they seem to fail
        // this is likely related to a test architecture issue, not to anything wiht the actual algorithm.

        [TestCase(5, 1.0),NonParallelizable]// most difficult
        public void StepDisturbanceANDSetpointSinus(double distStepAmplitude, double ysetStepAmplitude)
        {
            Vec vec = new Vec();

            double precisionPrc = 20;// works when run indivdually
            UnitParameters modelParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, distStepAmplitude);
            var yset = vec.Add(TimeSeriesCreator.Sinus(ysetStepAmplitude, N / 4, timeBase_s, N),TimeSeriesCreator.Constant(50,N));

            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "StaticProcess"), trueDisturbance,
                false, true, yset, precisionPrc);
        }

        [TestCase(5)]
        [TestCase(-5)]         
        public void LongStepDisturbance_EstimatesOk(double stepAmplitude)
        {
            bool doInvertGain = false;
            //    Shared.EnablePlots();
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "StaticProcess"), trueDisturbance,doInvertGain);
          //  Shared.DisablePlots();
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

        [Test]
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
                doNegativeGain,true,null,  processGainAllowedOffsetPrc);
            //Shared.DisablePlots();
        }




    }
}