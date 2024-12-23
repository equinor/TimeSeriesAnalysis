using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Accord.Math;
using NUnit.Framework;
using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceID
{
    [TestFixture]
    class ClosedLoopIdentifierTester_MISO
    {
        const bool doPlot = true;
        const bool isStatic = true;

        UnitParameters staticModelParameters = new UnitParameters
        {
            TimeConstant_s = 0,
            LinearGains = new double[] { 0.5, 0.25,0.15 },
            TimeDelay_s = 0,
            Bias = 10
        };



        UnitParameters dynamicModelParameters = new UnitParameters
        {
            TimeConstant_s = 8,
            LinearGains = new double[] { 0.5, 0.25,-0.55 },
            TimeDelay_s = 0,
            Bias = 5
        };


        PidParameters pidParameters1 = new PidParameters()
        {
            Kp = 0.5,
            Ti_s = 20
        };

        int timeBase_s = 2;
        int N = 500;
        DateTime t0 = new DateTime(2010, 1, 1);


        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }

        public void CommonPlotAndAsserts(UnitDataSet pidDataSet, double[] estDisturbance, double[] trueDisturbance,
            UnitModel identifiedModel, UnitModel trueModel, double maxAllowedGainOffsetPrc, double maxAllowedMeanDisturbanceOffsetPrc = 30, bool isStatic = true)
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
                    pidDataSet.U.GetColumn(0),pidDataSet.U.GetColumn(1),  estDisturbance, trueDisturbance },
                    new List<string> { "y1=y meas", "y1=y set", "y2=u_1(right)", "y2=u_2(right)", "y3=est disturbance", "y3=true disturbance" },
                    pidDataSet.GetTimeBase(), caseId + "commonplotandasserts");
                Shared.DisablePlots();
            }

            for (int gainIdx = 0; gainIdx < trueModel.modelParameters.LinearGains.Length; gainIdx++)
            {
                double estGain = identifiedModel.modelParameters.GetTotalCombinedProcessGain(gainIdx);
                double trueGain = trueModel.modelParameters.GetTotalCombinedProcessGain(gainIdx);
                double gainOffsetPrc = Math.Abs(estGain - trueGain) / Math.Abs(trueGain) * 100;
                if (Vec.IsAllValue(trueDisturbance, 0))
                {

                }
                else
                {
                    double disturbanceOffset = vec.Mean(vec.Abs(vec.Subtract(trueDisturbance, estDisturbance))).Value;
                    Assert.IsTrue(disturbanceOffset < distTrueAmplitude * maxAllowedMeanDisturbanceOffsetPrc / 100, "true disturbance and actual disturbance too far apart");
                }
                Assert.IsTrue(gainOffsetPrc < maxAllowedGainOffsetPrc, "est.gain:" + estGain + "|true gain:" + trueGain);
            }

            // time constant:
            if (!isStatic)
            {
                double estTimeConstant_s = identifiedModel.modelParameters.TimeConstant_s;
                double trueTimeConstant_s = trueModel.modelParameters.TimeConstant_s;
                double timeConstant_tol_s = 1;
                Assert.IsTrue(Math.Abs(estTimeConstant_s - trueTimeConstant_s) < timeConstant_tol_s, "est time constant " + estTimeConstant_s + " too far off " + trueTimeConstant_s);
            }

        }

        [TestCase(0, false,20)]
        [TestCase(0, true,20)]

        public void Static2Input_SetpointAndExtUChanges_NOdisturbance_detectsProcessOk(int pidInputIdx, bool doNegative, double gainTolPrc)
        {
            var trueDisturbance = TimeSeriesCreator.Constant(0, N);
            var externalU1 = TimeSeriesCreator.Step(N / 8, N, 15, 20);
            var yset = TimeSeriesCreator.Step(N * 3 / 8, N, 20, 18);
            UnitParameters twoInputModelParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 0.5, 0.25 },
                TimeDelay_s = 0,
                Bias = 10
            };
            GenericMISODisturbanceTest(new UnitModel(twoInputModelParameters, "StaticTwoInputsProcess"),
                trueDisturbance, externalU1, null, doNegative, true, yset, pidInputIdx, gainTolPrc, false, isStatic);
        }


        [TestCase(0, false, false), NonParallelizable]
        [TestCase(1, false, false)]
        // when a third input is added, estimation seems to fail.
        [TestCase(0, false, true)]
        [TestCase(1, false, true)]
        public void StaticMISO_SetpointChanges_WITH_disturbance_detectsProcessOk(int pidInputIdx,
            bool doNegative, bool addThirdInput)
        {
            UnitParameters trueParamters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 0.5, 0.25 },
                TimeDelay_s = 0,
                Bias = -10
            };
            double[] externalU2 = null;
            if (addThirdInput)
            {
                trueParamters = new UnitParameters
                {
                    TimeConstant_s = 0,
                    LinearGains = new double[] { 0.5, 0.25, 0.2 },
                    TimeDelay_s = 0,
                    Bias = -10
                };
                externalU2 = TimeSeriesCreator.Step(N * 5 / 8, N, 3, 1);
            }
            if (pidInputIdx == 1)
            {
                double[] oldGains = new double[trueParamters.LinearGains.Length];
                trueParamters.LinearGains.CopyTo(oldGains);
                trueParamters.LinearGains = new double[trueParamters.LinearGains.Length];
                trueParamters.LinearGains[0] = oldGains[1];
                trueParamters.LinearGains[1] = oldGains[0];
                if (addThirdInput)
                {
                    trueParamters.LinearGains[2] = oldGains[2];
                }
            }
            var trueDisturbance = TimeSeriesCreator.Step(N / 2, N, 0, 1);
            var externalU1 = TimeSeriesCreator.Step(N / 8, N, 15, 20);
            var yset = TimeSeriesCreator.Step(N * 3 / 8, N, 20, 18);
            GenericMISODisturbanceTest(new UnitModel(trueParamters, "StaticProcess"), trueDisturbance, externalU1, externalU2,
                doNegative, true, yset, pidInputIdx,10,false,isStatic);
        }
        // be aware that adding any sort of dynamics to the "true" model here seems to destroy the 
        // model estimate. 
        [TestCase(0, false)]
        [TestCase(0, true)]
        [TestCase(1, false)]
        public void StaticMISO_externalUchanges_NOsetpointChange_detectsProcessOk(int pidInputIdx, bool doNegative)
        {
            UnitParameters trueParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 0.5, 0.25, 0.15 },
                TimeDelay_s = 0,
                Bias = 10
            };

            var trueDisturbance = TimeSeriesCreator.Step(N / 2, N, 0, 1);
            var externalU1 = TimeSeriesCreator.Step(N / 8, N, 35, 40);
            var externalU2 = TimeSeriesCreator.Step(N * 5 / 8, N, 2, 1);
            var yset = TimeSeriesCreator.Constant(20,N);

            if (pidInputIdx == 1)
            {
                double[] oldGains = new double[3];
                trueParameters.LinearGains.CopyTo(oldGains);
                trueParameters.LinearGains = new double[] { oldGains[1], oldGains[0], oldGains[2] };
            }
            GenericMISODisturbanceTest(new UnitModel(trueParameters, "StaticProcess"), trueDisturbance, externalU1, externalU2,
                doNegative, true, yset, pidInputIdx);
        }

        [TestCase(0,5)]
        [TestCase(1,5)]
        public void DynamicMISO_SetpointAndExtUChanges_NoDisturbance_detectsProcessOk(int pidInputIdx, double gainTolPrc)
        {
            UnitParameters trueParameters = new UnitParameters
            {
                TimeConstant_s = 8,
                LinearGains = new double[] { 0.5, 0.25, -0.55 },
                TimeDelay_s = 0,
                Bias = 5
            };

            var trueDisturbance = TimeSeriesCreator.Constant(0, N);
            var externalU1 = TimeSeriesCreator.Step(N / 8, N, 5, 10);
            var externalU2 = TimeSeriesCreator.Step(N *5 / 8, N, 2, 1);
            var yset = TimeSeriesCreator.Step(N * 3 / 8, N, 20, 18);
            GenericMISODisturbanceTest(new UnitModel(trueParameters, "DynamicProcess"), trueDisturbance, externalU1,externalU2,false,true,
                yset, pidInputIdx, gainTolPrc);
        }

        // TODO:
        // in closed loop identification, both "diff" and "Abs" return a linear gain for the pid control input 
        // that is very close to zero- indicating something is wrong. They also have the warning message
        // "constant input U" - indicating there may be an error in the programming this test.
        
        [TestCase(0, Category = "NotWorking_AcceptanceTest"),Explicit]
        [TestCase(1, Category = "NotWorking_AcceptanceTest")]
        public void DynamicMISO_externalUchanges_NoDisturbance_NOsetpointchange_DetectsProcessOk(int pidInputIdx)
        {
            // similar to PidSingle demo case in front end
            UnitParameters trueParameters = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1, 0.5},
                TimeDelay_s = 0,
                Bias = 5
            };

            var trueDisturbance = TimeSeriesCreator.Constant(0, N);
            var externalU1 = TimeSeriesCreator.Step(N / 8, N, 50, 45);
            var yset = TimeSeriesCreator.Constant(60, N);
            GenericMISODisturbanceTest(new UnitModel(trueParameters, "DynamicProcess"), trueDisturbance, 
                externalU1, null, false, true, yset, pidInputIdx, 20);
        }


        public void GenericMISODisturbanceTest  (UnitModel trueProcessModel, double[] trueDisturbance,
            double[] externalU1, double[] externalU2, bool doNegativeGain,
            bool doAssertResult=true, double[] yset=null, int pidInputIdx = 0,
            double processGainAllowedOffsetPrc=10, bool doAddBadData = false,bool isStatic = true)
        {
            var usedProcParameters = trueProcessModel.GetModelParameters().CreateCopy();
            var usedProcessModel = new UnitModel(usedProcParameters,"UsedProcessModel");
            var usedPidParams = new PidParameters(pidParameters1);
            var extInputIdx1 = 1;
            var extInputIdx2 = 2;
            if (pidInputIdx == 1)
            {
                extInputIdx1 = 0;
                extInputIdx2 = 2;
            }
            if (pidInputIdx == 2)
            {
                extInputIdx1 = 0;
                extInputIdx2 = 1;
            }
            var extSignalId1 = SignalNamer.GetSignalName(usedProcessModel.GetID(), SignalType.External_U, extInputIdx1);
            var extSignalId2 = SignalNamer.GetSignalName(usedProcessModel.GetID(), SignalType.External_U, extInputIdx2);
            var pidSignalId = SignalNamer.GetSignalName(usedProcessModel.GetID(), SignalType.PID_U, pidInputIdx);

            if (pidInputIdx == 0)
            {
                if (externalU2 == null)
                    usedProcessModel.ModelInputIDs = new string[] { pidSignalId, extSignalId1 };
                else
                    usedProcessModel.ModelInputIDs = new string[] { pidSignalId, extSignalId1, extSignalId2 };
            }
            else if (pidInputIdx == 1)
            {
                if (externalU2 == null)
                    usedProcessModel.ModelInputIDs = new string[] { extSignalId1, pidSignalId };
                else
                    usedProcessModel.ModelInputIDs = new string[] { extSignalId1, pidSignalId, extSignalId2 };
            }
            else if (pidInputIdx == 2)
            {
                usedProcessModel.ModelInputIDs = new string[] { extSignalId1,  extSignalId2, pidSignalId };
            }

            if (doNegativeGain)
            {
                usedProcParameters.LinearGains[pidInputIdx] = -usedProcParameters.LinearGains[pidInputIdx];
                usedProcParameters.Bias = 50;// 
                usedProcessModel.SetModelParameters(usedProcParameters);
                usedPidParams.Kp = -usedPidParams.Kp;
                trueProcessModel.SetModelParameters(usedProcParameters);
            }
            // create synthetic dataset
            var pidModel1 = new PidModel(usedPidParams, "PID1");

            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, usedProcessModel });
            processSim.ConnectModels(usedProcessModel, pidModel1);
            processSim.ConnectModels(pidModel1, usedProcessModel, pidInputIdx);
            var inputData = new TimeSeriesDataSet();
           
            if (yset == null)
                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            else
                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), yset);
  
            var extInputID1 = processSim.AddExternalSignal(usedProcessModel, SignalType.External_U, extInputIdx1);
            inputData.Add(extInputID1, externalU1);

            if (externalU2 != null)
            {
                var extInputID2 = processSim.AddExternalSignal(usedProcessModel, SignalType.External_U, extInputIdx2);
                inputData.Add(extInputID2, externalU2);
            }
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
                Console.WriteLine("BAD DATA ADDED!!!");
            }

            (var identifiedModel, var estDisturbance) = ClosedLoopUnitIdentifier.Identify(pidDataSet, pidModel1.GetModelParameters(), pidInputIdx);

            Console.WriteLine(identifiedModel.ToString());
            Console.WriteLine();

            DisturbancesToString(estDisturbance, trueDisturbance);
            if (doAssertResult)
            {
                CommonPlotAndAsserts(pidDataSet, estDisturbance, trueDisturbance, identifiedModel,
                    trueProcessModel, processGainAllowedOffsetPrc, 30,isStatic);
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
