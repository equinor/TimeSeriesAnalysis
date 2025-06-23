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


        [TestCase(5,1.0), NonParallelizable]
        [TestCase(1,1.0)] 
     //   [TestCase(1,5.0)]
        public void StepDisturbanceANDSetpointStep(double distStepAmplitude, double ysetStepAmplitude)
        {
            double precisionPrc = 30;

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

        /*
        I think the reason this test struggles is that closedloopestimator tries to find the process model that results in the
        disturbance with the smallest average change, but for continously acting disturbances like this sinus disturbance, 
        this may not be as good an assumption as for the step disturbances considered in other tests. 
        */
        /*
        [TestCase(5, 1.0, Category="NotWorking_AcceptanceTest"), NonParallelizable]
        [TestCase(1, 1.0, Category = "NotWorking_AcceptanceTest")]
        [TestCase(1, 5.0 )]// this only works when the step change is much bigger than the disturbance

        public void SinusDisturbanceANDSetpointStep(double distSinusAmplitude, double ysetStepAmplitude)
        {
            double precisionPrc = 20;

            UnitParameters staticModelParameters = new UnitParameters
            {
                TimeConstant_s = 15,
                LinearGains = new double[] { 1.2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.Sinus(distSinusAmplitude,N/8,timeBase_s,N );
            var yset = TimeSeriesCreator.Step(N/2, N, 50, 50 + ysetStepAmplitude);//do step before disturbance
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(staticModelParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc);
        }
        */

        /*
        [TestCase(2, Category = "NotWorking_AcceptanceTest")]
        [TestCase(1,  Category = "NotWorking_AcceptanceTest")]
        [TestCase(0.5, Category = "NotWorking_AcceptanceTest")]
        public void SinusDisturbance(double distSinusAmplitude)
        {
            double precisionPrc = 20;

            var trueDisturbance = TimeSeriesCreator.Sinus(distSinusAmplitude, N / 8, timeBase_s, N);
            var yset = TimeSeriesCreator.Step(N / 2, N, 50, 50 );
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc);
        }
        */

        // 0.25: saturates the controller
        [TestCase(1, 0.1, 30, Category = "NotWorking_AcceptanceTest")]
        [TestCase(1, 1, 30, Category = "NotWorking_AcceptanceTest")]
        [TestCase(2, 0.1, 30, Category = "NotWorking_AcceptanceTest")]
        [TestCase(2, 1, 30, Category = "NotWorking_AcceptanceTest")]
        // gain of 5 starts giving huge oscillations...

        public void RandomWalkDisturbance(double procGain, double distAmplitude, double gainPrecisionPrc)
        {
            int seed = 50; // important: this seend will if time constant is zero be able to find a model 
            int N = 2000;
            var locParameters = new UnitParameters
            {
                TimeConstant_s = 2,//should be nonzero for dynamic test (fails if  timconstant_s i larger than about 2)
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

        [TestCase(5, 1.0)]

        public void StepDistANDSetpointSinus(double distStepAmplitude, double ysetStepAmplitude)
        {
            Vec vec = new Vec();
            double precisionPrc = 20;
            var  locParameters = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1.5 },
                TimeDelay_s = 0,//was: 5
                Bias = 5
            };

            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, distStepAmplitude);
            var yset = vec.Add(TimeSeriesCreator.Sinus(ysetStepAmplitude, N / 4, timeBase_s, N),TimeSeriesCreator.Constant(50,N));

            CluiCommonTests.GenericDisturbanceTest(new UnitModel(locParameters, "Process"), trueDisturbance,
                false, true, yset, precisionPrc);
        }



        [TestCase(5,5)]
        [TestCase(-5,5)]         
        public void LongStepDist_EstimatesOk(double stepAmplitude,double procGainAllowedOffSetPrc)
        {
            double noiseAmplitude = 0.01;
            bool doInvertGain = false;
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude).
                Add(TimeSeriesCreator.Noise(N, noiseAmplitude));
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,doInvertGain,true,null,procGainAllowedOffSetPrc);
        }


        [TestCase()]
        public void StepAtStartOfDataset_IsExcludedFromAnalysis()
        {
            double stepAmplitude = 1;
            double noiseAmplitude = 0.01;
            bool doAddBadData = true;
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);//.Add(TimeSeriesCreator.Noise(N, noiseAmplitude)); ;
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"),
                trueDisturbance, false, true,null,10, doAddBadData);
        }

        [TestCase(-5,10)]
        [TestCase(5, 5)]
        [TestCase(10, 5)]
        public void StepDisturbance_EstimatesOk(double stepAmplitude, double processGainAllowedOffsetPrc, 
            bool doNegativeGain =false)
        {
            double noiseAmplitude = 0.01;
             //Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude).Add(TimeSeriesCreator.Noise(N, noiseAmplitude));
            CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParameters, "Process"), trueDisturbance,
                doNegativeGain,true,null,  processGainAllowedOffsetPrc);
            //Shared.DisablePlots();
        }




    }
}
