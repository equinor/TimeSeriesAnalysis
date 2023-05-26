using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using NUnit.Framework;
using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceID
{
    [TestFixture]
    class FindDisturbanceAndModelSimultanouslyTester_MISO
    {
        const bool doPlot = true;

        UnitParameters staticModelParameters = new UnitParameters
        {
            TimeConstant_s = 0,
            LinearGains = new double[] { 0.5, 0.25 },
            TimeDelay_s = 0,
            Bias = 10
        };

        UnitParameters dynamicModelParameters = new UnitParameters
        {
            TimeConstant_s = 15,
            LinearGains = new double[] { 0.5, 0.25 },
            TimeDelay_s = 5,
            Bias = 5
        };


        PidParameters pidParameters1 = new PidParameters()
        {
            Kp = 0.5,
            Ti_s = 20
        };

        int timeBase_s = 1;
        int N = 300;
        DateTime t0 = new DateTime(2010, 1, 1);


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
            bool doDebugPlot = true;
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
        }
        [TestCase]
        public void StaticMISO_no_disturbance_detectsProcessOk()
        {
            var trueDisturbance = TimeSeriesCreator.Constant(0, N);
            var externalU = TimeSeriesCreator.Step(N/8, N, 5, 10);
            var yset = TimeSeriesCreator.Step(N*3/8, N, 20, 18);
            GenericMISODisturbanceTest(new UnitModel(staticModelParameters, "StaticProcess"), trueDisturbance, externalU, false,true,yset);
        }

        [TestCase]
        public void DynamicMISO_no_disturbance_detectsProcessOk()
        {
            var trueDisturbance = TimeSeriesCreator.Constant(0, N);
            var externalU = TimeSeriesCreator.Step(0, N,0,1);
            GenericMISODisturbanceTest(new UnitModel(dynamicModelParameters, "DynamicProcess"), trueDisturbance, externalU,false);
        }

        public void GenericMISODisturbanceTest  (UnitModel trueProcessModel, double[] trueDisturbance, double[] externalU, bool doNegativeGain,
            bool doAssertResult=true, double[] yset=null, double processGainAllowedOffsetPrc=10, bool doAddBadData = false, int pidInputIdx=0)
        {
            var usedProcParameters = trueProcessModel.GetModelParameters().CreateCopy();
            var usedProcessModel = new UnitModel(usedProcParameters,"UsedProcessModel");
            var extInputIdx = 1;
            if (pidInputIdx == 1)
                extInputIdx = 0;
            var extSignalId = SignalNamer.GetSignalName(usedProcessModel.GetID(), SignalType.External_U, extInputIdx);
            var pidSignalId = SignalNamer.GetSignalName(usedProcessModel.GetID(), SignalType.PID_U, pidInputIdx);

            if (pidInputIdx == 0)
            {
                usedProcessModel.ModelInputIDs = new string[] { pidSignalId, extSignalId };
            }
            else
            {
                usedProcessModel.ModelInputIDs = new string[] { extSignalId, pidSignalId  };
            }

            // create synthetic dataset
            var pidModel1 = new PidModel(pidParameters1, "PID1");

            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, usedProcessModel });
            processSim.ConnectModels(usedProcessModel, pidModel1);
            processSim.ConnectModels(pidModel1, usedProcessModel, pidInputIdx);
            var inputData = new TimeSeriesDataSet();
           
            if (yset == null)
                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            else
                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), yset);

  
            var extInputID = processSim.AddExternalSignal(usedProcessModel, SignalType.External_U, extInputIdx);
            inputData.Add(extInputID, externalU);
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
            (var identifiedModel, var estDisturbance) = modelId.Identify(pidDataSet, pidModel1.GetModelParameters(), pidInputIdx);

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
