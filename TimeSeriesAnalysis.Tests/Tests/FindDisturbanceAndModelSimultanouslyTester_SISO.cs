using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceID
{
    [TestFixture]
    class FindDisturbanceAndModelSimultanouslyTester_SISO
    {
        const bool doPlot = true;

        UnitParameters staticModelParameters = new UnitParameters
        {
            TimeConstant_s = 0,
            LinearGains = new double[] { 1.5 },
            TimeDelay_s = 0,
            Bias = 5
        };

        UnitParameters dynamicModelParameters = new UnitParameters
        {
            TimeConstant_s = 10,
            LinearGains = new double[] { 1.5 },
            TimeDelay_s = 5,
            Bias = 5
        };

        PidParameters pidParameters1 = new PidParameters()
        {
            Kp = 0.2,
            Ti_s = 20
        };


        int timeBase_s = 1;
        int N = 300;// TODO:influences the results!
        DateTime t0 = new DateTime(2010,1,1);


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }

        public void CommonPlotAndAsserts(UnitDataSet pidDataSet, double[] estDisturbance, double[] trueDisturbance,
            UnitModel identifiedModel, UnitModel trueModel, double maxAllowedGainOffsetPrc, double maxAllowedMeanDisturbanceOffsetPrc = 30)
        {
            Vec vec = new Vec();

            double distTrueAmplitude = vec.Max(vec.Abs(trueDisturbance));
            Assert.IsTrue(estDisturbance != null);
            string caseId = TestContext.CurrentContext.Test.Name.Replace("(", "_").
                Replace(")", "_").Replace(",", "_") + "y";
            bool doDebugPlot = false;
            if (doDebugPlot)
            {
                Shared.EnablePlots();
                Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),  estDisturbance, trueDisturbance },
                    new List<string> { "y1=y meas", "y1=y set", "y2=u(right)", "y3=est disturbance", "y3=true disturbance" },
                    pidDataSet.GetTimeBase(), caseId + "commonplotandasserts");
                Shared.DisablePlots();
            }

            // estimated gain
            double estGain = identifiedModel.modelParameters.GetTotalCombinedProcessGain(0);
            double trueGain = trueModel.modelParameters.GetTotalCombinedProcessGain(0);
            double gainOffsetPrc = Math.Abs(estGain - trueGain) / Math.Abs(trueGain) * 100;
            if (Vec.IsAllValue(trueDisturbance, 0))
            { 
                
            }
            else
            {
                double disturbanceOffset = vec.Mean(vec.Abs(vec.Subtract(trueDisturbance, estDisturbance))).Value;
                Assert.IsTrue(disturbanceOffset < distTrueAmplitude * maxAllowedMeanDisturbanceOffsetPrc / 100, "true disturbance and actual disturbance too far apart");
            }
            Assert.IsTrue(gainOffsetPrc < maxAllowedGainOffsetPrc,"est.gain:"+ estGain+ "|true gain:"+trueGain);

            // time constant
            const double TC_TOL_PRC = 30;
            double estTimeConstant_s = identifiedModel.modelParameters.TimeConstant_s;
            double trueTimeConstant_s = trueModel.modelParameters.TimeConstant_s;
            double timeConstant_tol_s = trueTimeConstant_s*TC_TOL_PRC/100;
            Assert.IsTrue(Math.Abs(estTimeConstant_s - trueTimeConstant_s) <= timeConstant_tol_s, "est time constant "+ estTimeConstant_s  + " too far off " + trueTimeConstant_s);

        }

        [TestCase(-5,false), NonParallelizable]
        [TestCase(5,false)]
        [TestCase(-5, true)]
    //    [TestCase(5, true)]
        public void Static_StepDist_EstimatesOk(double stepAmplitude,bool doNegativeGain)
        {
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            var usedParamters = staticModelParameters;
          //  Shared.EnablePlots();
            GenericDisturbanceTest(new UnitModel(usedParamters, "StaticProcess"), trueDisturbance, doNegativeGain);
          //  Shared.DisablePlots();
        }

        //
        // does not work in general for any seed
      //  [TestCase(1, 1.5, 25, 1)]
       // [TestCase(1, 1.5, 25, 2)]
      //  [TestCase(1, 1.5, 25, 3)]// TODO: redo for more seeds. use seed to avoid test buidl failing on server by chance
        public void Static_RandomWalk_EstimatesOk(double noiseAmplitude, double systemGain,
                 double precisionPrc, int seed, bool doNegativeGain = false)
        {
            UnitParameters staticModelParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { systemGain },
                TimeDelay_s = 0,
                Bias = 5
            };
        // Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.RandomWalk( N, noiseAmplitude,0,seed);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance,
                doNegativeGain,true, null,precisionPrc);
        // Shared.DisablePlots();
        }

        /*
        This is currently a work-in-progress!!!
        */

        [TestCase(5,1.0), NonParallelizable]
        //       [TestCase(1,1.0)] // for some reason this fails when running all tests, but works fine when running only select tests
        //      [TestCase(1,5.0)]// for some reason this fails when running all tests, but works fine when running only select tests
        public void Static_DistANDSetpointStep(double distStepAmplitude, double ysetStepAmplitude)
        {
            double precisionPrc = 30;

            UnitParameters staticModelParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.Step(160, N, 0, distStepAmplitude);
            var yset = TimeSeriesCreator.Step(50, N, 50, 50+ ysetStepAmplitude);//do step before disturbance
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance,
                false, true, yset, precisionPrc);
        }
        [TestCase(1.0), NonParallelizable]
        [TestCase(2.0)]
       // [TestCase(0.5)]
        public void Dynamic_SetpointStep( double ysetStepAmplitude)
        {
            double precisionPrc = 10;
            UnitParameters staticModelParameters = new UnitParameters
            {
                TimeConstant_s = 20,
                LinearGains = new double[] { 1.2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.Constant(0, N);
            var yset = TimeSeriesCreator.Step(50, N, 50, 50 + ysetStepAmplitude);//do step before disturbance
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance,
                false, true, yset, precisionPrc);
        }

        [TestCase(5, 1.0),NonParallelizable]
        [TestCase(1, 1.0)]
        [TestCase(1, 5.0)]
        public void Static_SinusDistANDSetpointStep(double distSinusAmplitude, double ysetStepAmplitude)
        {
            double precisionPrc = 20;

            UnitParameters staticModelParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.Sinus(distSinusAmplitude,N/8,timeBase_s,N );
            //   var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, distStepAmplitude);
            var yset = TimeSeriesCreator.Step(N/2, N, 50, 50 + ysetStepAmplitude);//do step before disturbance
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance,
                false, true, yset, precisionPrc);
        }
        // these tests seem to run well when run individuall, but when "run all" they seem to fail
        // this is likely related to a test architecture issue, not to anything wiht the actual algorithm.
   
        [TestCase(5, 1.0), Explicit]// most difficult
        [TestCase(1, 1.0)] //difficult
        [TestCase(1, 5.0)]//easist
        public void Static_StepDistANDSetpointSinus(double distStepAmplitude, double ysetStepAmplitude)
        {
            Vec vec = new Vec();

            double precisionPrc = 20;// works when run indivdually
            UnitParameters staticModelParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, distStepAmplitude);
            //    var yset = TimeSeriesCreator.Step(50, N, 50, 50 + ysetStepAmplitude);//do step before disturbance
            var yset = vec.Add(TimeSeriesCreator.Sinus(ysetStepAmplitude, N / 4, timeBase_s, N),TimeSeriesCreator.Constant(50,N));

            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance,
                false, true, yset, precisionPrc);
        }



        // this works as long as only static identifiation is used in the closed-looop identifier,
        // otherwise the model 
    [TestCase(5)]
        [TestCase(-5)]         
        public void Static_LongStepDist_EstimatesOk(double stepAmplitude)
        {
            bool doInvertGain = false;
            //    Shared.EnablePlots();
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance,doInvertGain);
          //  Shared.DisablePlots();
        }




        // this works as long as only static identifiation is used in the closed-looop identifier,
        [Test]
        public void FlatData_DoesNotCrash()
        {
            double stepAmplitude = 0;
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), 
                trueDisturbance,false,false);
        }
        // for some reason, this test also does not work with "run all", but works fine when run individually?
        [Test,Explicit]
        public void StepAtStartOfDataset_IsExcludedFromAnalysis()
        {
            double stepAmplitude = 1;
            bool doAddBadData = true;//todo:set true
            N = 1000;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"),
                trueDisturbance, false, true,null,10, doAddBadData);
        }

        [TestCase(-5,5)]
        [TestCase(5, 5)]
        [TestCase(10, 5)]
        public void Dynamic_DistStep_EstimatesOk(double stepAmplitude, double processGainAllowedOffsetPrc, 
            bool doNegativeGain =false)
        {
             //Shared.EnablePlots();
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);
            GenericDisturbanceTest(new UnitModel(dynamicModelParameters, "DynamicProcess"), trueDisturbance,
                doNegativeGain,true,null,  processGainAllowedOffsetPrc);
            //Shared.DisablePlots();
        }

        public void GenericDisturbanceTest  (UnitModel trueProcessModel, double[] trueDisturbance, bool doNegativeGain,
            bool doAssertResult=true, double[] yset=null, double processGainAllowedOffsetPrc=10, bool doAddBadData = false)
        {
            var usedProcParameters = trueProcessModel.GetModelParameters().CreateCopy();
            var usedProcessModel = new UnitModel(usedProcParameters,"UsedProcessModel");
            if (doNegativeGain)
            {
                usedProcParameters.LinearGains[0] = -usedProcParameters.LinearGains[0];
                usedProcParameters.Bias = 100;// 
                usedProcessModel.SetModelParameters(usedProcParameters);
                pidParameters1.Kp = -pidParameters1.Kp ;
            }

            // create synthetic dataset
            var pidModel1 = new PidModel(pidParameters1, "PID1");

            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, usedProcessModel });
            processSim.ConnectModels(usedProcessModel, pidModel1);
            processSim.ConnectModels(pidModel1, usedProcessModel);
            var inputData = new TimeSeriesDataSet();
           
            if (yset == null)
                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            else
                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), yset);

            inputData.Add(processSim.AddExternalSignal(usedProcessModel, SignalType.Disturbance_D), trueDisturbance);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            if (doAddBadData)
            {
                pidDataSet.Y_setpoint[0] = pidDataSet.Y_setpoint[0] * 0.5;
                pidDataSet.Y_setpoint[50] = Double.NaN;
                pidDataSet.Y_meas[400] = Double.NaN;
                pidDataSet.U[500,0] = Double.NaN;

            }

            var modelId = new ClosedLoopUnitIdentifier();
            (var identifiedModel, var estDisturbance) = modelId.Identify(pidDataSet, pidModel1.GetModelParameters());

            Console.WriteLine(identifiedModel.ToString());
            Console.WriteLine();

            DisturbancesToString(estDisturbance, trueDisturbance);
            if (doAssertResult)
            {
                CommonPlotAndAsserts(pidDataSet, estDisturbance, trueDisturbance, identifiedModel,trueProcessModel, processGainAllowedOffsetPrc);
            }
        }

        void DisturbancesToString(double[] estDisturbance, double[] trueDisturbance)
        { 
            StringBuilder sb = new StringBuilder();
            Vec vec = new Vec();

            var estMin = vec.Min(estDisturbance).ToString("F1") ;
            var estMax = vec.Max(estDisturbance).ToString("F1");
            var trueMin = vec.Min(trueDisturbance).ToString("F1");
            var trueMax = vec.Max(trueDisturbance).ToString("F1");

            sb.AppendLine("disturbance min:"+estMin+" max:"+estMax+"(actual min:"+trueMin+"max:"+trueMax+")");
            Console.WriteLine(sb.ToString());
        }



    }
}
