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
        [TestCase(0.8, 10,Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(1.5, 10,Explicit = true, Category = "NotWorking_AcceptanceTest")]
        [TestCase(3, 10,Explicit = true, Category = "NotWorking_AcceptanceTest")]

        public void SinusDisturbance(double procGain, double gainPrecisionPrc )
        {
            int N = 500;
            var period = N / 3;
            var distSinusAmplitude = 1;
            var noiseAmplitude = 0.01;

            var  modelParametersLoc = new UnitParameters
            {
                TimeConstant_s = 0,//nb! static
                LinearGains = new double[] { procGain },
                TimeDelay_s = 0,
                Bias = 5
            };
          // try with steady-state before and after to see if this improves estimates
          var trueDisturbance = TimeSeriesCreator.Concat(TimeSeriesCreator.Noise(50,noiseAmplitude), 
          TimeSeriesCreator.Concat(TimeSeriesCreator.Sinus(distSinusAmplitude,period,timeBase_s,N ), TimeSeriesCreator.Noise(100,noiseAmplitude)));
 
          var yset = TimeSeriesCreator.Constant( 50,trueDisturbance.Length);
          CluiCommonTests.GenericDisturbanceTest(new UnitModel(modelParametersLoc, "Process"), trueDisturbance,
            false, true, yset, gainPrecisionPrc,false, isStatic);
        }

        // idea is to compare two different gains in the sinus case and see how different the time-series simulation look
        // not really a test, but a way to generate a figure for the documentation.
       
        [TestCase(Explicit = true, Category = "Documentation")]
        public void DOCUMENTATION_CompareDataForDifferentGainsInSinusCase()
        {
            
            int N = 500;
            var period = N / 3;
            var distSinusAmplitude = 1;
            var noiseAmplitude = 0.01;

            PidParameters pidParameters1 = new PidParameters()
            {
                Kp = 0.2,
                Ti_s = 20
            };

            var unitDataSetList = new List<UnitDataSet> ();


            var trueDisturbance = TimeSeriesCreator.Concat(TimeSeriesCreator.Noise(50,noiseAmplitude), 
                TimeSeriesCreator.Concat(TimeSeriesCreator.Sinus(distSinusAmplitude,period,timeBase_s,N ), TimeSeriesCreator.Noise(100,noiseAmplitude)));

            for (double procGain =1;procGain<2.5; procGain +=1)
            {

                var  modelParametersLoc = new UnitParameters
                {
                    TimeConstant_s = 0,//nb! static
                    LinearGains = new double[] { procGain },
                    U0 = new double[]{50},
                    TimeDelay_s = 0,
                    Bias = 50
                };
                // try with steady-state before and after to see if this improves estimates

                var usedProcParameters = modelParametersLoc.CreateCopy();
                var usedProcessModel = new UnitModel(usedProcParameters, "UsedProcessModel");
                var usedPidParameter = new PidParameters(pidParameters1);

                // create synthetic dataset
                var pidModel1 = new PidModel(usedPidParameter, "PID1");

                var processSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, usedProcessModel });
                processSim.ConnectModels(usedProcessModel, pidModel1);
                processSim.ConnectModels(pidModel1, usedProcessModel);
                var inputData = new TimeSeriesDataSet();
                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, trueDisturbance.Length));
                inputData.Add(processSim.AddExternalSignal(usedProcessModel, SignalType.Disturbance_D), trueDisturbance);
                inputData.CreateTimestamps(timeBase_s);
                var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);

                Assert.IsTrue(isOk);
                var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
                unitDataSetList.Add(pidDataSet);
            }

            Shared.EnablePlots();
            Plot.FromList(new List<double[]>{ unitDataSetList[0].Y_meas, unitDataSetList[1].Y_meas, unitDataSetList[0].Y_setpoint, 
            unitDataSetList[0].U.GetColumn(0), unitDataSetList[1].U.GetColumn(0) , trueDisturbance },
                new List<string> { "y1=y_meas(G=1)","y1=y_meas(G=2)", "y1=y_set", "y3=u_pid(G=1)","y3=u_pid(G=2)","y4=d_true" },
                unitDataSetList[0].GetTimeBase(),TestContext.CurrentContext.Test.Name );
            Shared.DisablePlots();
        }


 [TestCase(Explicit = true, Category = "Documentation")]
        public void DOCUMENTATION_CompareDataForDifferentDisturbanceAmplitudesInSinusCase()
        {
            
            int N = 500;
            var period = N / 3;
            var noiseAmplitude = 0.01;

            PidParameters pidParameters1 = new PidParameters()
            {
                Kp = 0.2,
                Ti_s = 20
            };

            var unitDataSetList = new List<UnitDataSet> ();


            var disturbanceList = new List<double[]>();


            double procGain  = 1.5;

            for (double distSinusAmplitude =1;distSinusAmplitude<2.0; distSinusAmplitude +=0.6)
            {

                var  modelParametersLoc = new UnitParameters
                {
                    TimeConstant_s = 0,//nb! static
                    LinearGains = new double[] { procGain },// scale gain with disturbance amplitude to get similar output amplitude
                    U0 = new double[]{50},
                    TimeDelay_s = 0,
                    Bias = 50
                };
                if(distSinusAmplitude>1)
                    modelParametersLoc.LinearGains[0] = 4;
                // try with steady-state before and after to see if this improves estimates

                var usedProcParameters = modelParametersLoc.CreateCopy();
                var usedProcessModel = new UnitModel(usedProcParameters, "UsedProcessModel");
                var usedPidParameter = new PidParameters(pidParameters1);

                // create synthetic dataset
                var pidModel1 = new PidModel(usedPidParameter, "PID1");

                var processSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, usedProcessModel });
                processSim.ConnectModels(usedProcessModel, pidModel1);
                processSim.ConnectModels(pidModel1, usedProcessModel);
                var inputData = new TimeSeriesDataSet();

                var trueDisturbance = TimeSeriesCreator.Concat(TimeSeriesCreator.Noise(50,noiseAmplitude), 
                TimeSeriesCreator.Concat(TimeSeriesCreator.Sinus(distSinusAmplitude,period,timeBase_s,N ), TimeSeriesCreator.Noise(100,noiseAmplitude)));
                disturbanceList.Add(trueDisturbance);

                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, trueDisturbance.Length));
                inputData.Add(processSim.AddExternalSignal(usedProcessModel, SignalType.Disturbance_D), trueDisturbance);
                inputData.CreateTimestamps(timeBase_s);
                var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);

                Assert.IsTrue(isOk);
                var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
                unitDataSetList.Add(pidDataSet);
            }

            Shared.EnablePlots();
            Plot.FromList(new List<double[]>{ unitDataSetList[0].Y_meas, unitDataSetList[1].Y_meas, unitDataSetList[0].Y_setpoint, 
            unitDataSetList[0].U.GetColumn(0), unitDataSetList[1].U.GetColumn(0) , disturbanceList[0],disturbanceList[1] },
                new List<string> { "y1=y_meas(d1)", "y1=y_meas(d2)","y1=y_set", "y3=u_pid(d1)","y3=u_pid(d2)","y4=d1","y4=d2" },
                unitDataSetList[0].GetTimeBase(),TestContext.CurrentContext.Test.Name );
            Shared.DisablePlots();
        }





        // 0.25: saturates the controller
        // gain of 5 starts giving huge oscillations...

        // generally, the smaller the process gain, the lower the precision of the estimated process gain.
        //(halving the gain-> halving the precision)
        // first seed
        [TestCase(1, 0.1, 28, 105,1000)]
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

        // for whatever reason, this test seems to sometimes fail if run with other test, but not if run alone. 
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
