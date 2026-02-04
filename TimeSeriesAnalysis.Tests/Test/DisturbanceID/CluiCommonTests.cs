using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Tests.DisturbanceID
{
    static class CluiCommonTests
    {
        static PidParameters pidParameters1 = new PidParameters()
        {
            Kp = 0.2,
            Ti_s = 20
        };

        static double timeBase_s = 1;


        static public void GenericDisturbanceTest(UnitModel trueProcessModel, double[] trueDisturbance, bool doNegativeGain,
            bool doAssertResult = true, double[] yset = null, double processGainAllowedOffsetPrc = 10, bool doAddBadData = false, bool isStatic = false)
        {
            var usedProcParameters = trueProcessModel.GetModelParameters().CreateCopy();
            var usedProcessModel = new UnitModel(usedProcParameters, "UsedProcessModel");
            var usedPidParameter = new PidParameters(pidParameters1);
            if (doNegativeGain)
            {
                usedProcParameters.LinearGains[0] = -usedProcParameters.LinearGains[0];
                usedProcParameters.Bias = 100;
                usedProcessModel.SetModelParameters(usedProcParameters);
                usedPidParameter.Kp = -usedPidParameter.Kp;

                UnitParameters trueParams = trueProcessModel.GetModelParameters();
                trueParams.LinearGains[0] = -trueParams.LinearGains[0];
                trueProcessModel.SetModelParameters(trueParams); ;
            }

            // create synthetic dataset
            var pidModel1 = new PidModel(usedPidParameter, "PID1");

            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, usedProcessModel });
            processSim.ConnectModels(usedProcessModel, pidModel1);
            processSim.ConnectModels(pidModel1, usedProcessModel);
            var inputData = new TimeSeriesDataSet();

            if (yset == null)
                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, trueDisturbance.Length));
            else
                inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), yset);

            inputData.Add(processSim.AddExternalSignal(usedProcessModel, SignalType.Disturbance_D), trueDisturbance);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            if (doAddBadData)
            {
                pidDataSet.Y_setpoint[255] = Double.NaN;
                pidDataSet.Y_setpoint[155] = Double.NaN;
                pidDataSet.Y_setpoint[55] = Double.NaN;
                pidDataSet.Y_meas[300] = Double.NaN;
                pidDataSet.Y_meas[200] = Double.NaN;
                pidDataSet.Y_meas[100] = Double.NaN;
                pidDataSet.U[50, 0] = Double.NaN;
                pidDataSet.U[20, 0] = Double.NaN;
                pidDataSet.U[5, 0] = Double.NaN;
                Console.WriteLine("---------NB!!! bad data added to Ymeas, Y_setpoint and U !!--------");
            }
            // NB! uses the "perfect" pid-model in the identification process

            var estPidParam = new PidParameters(pidModel1.GetModelParameters());

            var identifiedModel = new UnitModel();
            var estDisturbance = ClosedLoopUnitIdentifier.Identify(ref identifiedModel, pidDataSet, estPidParam);
            if (identifiedModel != null)
            {
                Console.WriteLine(identifiedModel.ToString());
                Console.WriteLine();
                DisturbancesToString(estDisturbance, trueDisturbance);
                if (doAssertResult)
                {
                    CommonPlotAndAsserts(pidDataSet, estDisturbance, trueDisturbance, identifiedModel, trueProcessModel, processGainAllowedOffsetPrc, 30, isStatic);
                }
            }


        }

        static void DisturbancesToString(double[] estDisturbance, double[] trueDisturbance)
        {
            if (estDisturbance == null)
            {
                Console.WriteLine("disturbance:NULL");
                return;
            }

            StringBuilder sb = new StringBuilder();
            Vec vec = new Vec();

            var estMin = vec.Min(estDisturbance).ToString("F1");
            var estMax = vec.Max(estDisturbance).ToString("F1");
            var trueMin = vec.Min(trueDisturbance).ToString("F1");
            var trueMax = vec.Max(trueDisturbance).ToString("F1");

            sb.AppendLine("disturbance min:" + estMin + " max:" + estMax + "(actual min:" + trueMin + "max:" + trueMax + ")");
            Console.WriteLine(sb.ToString());
        }



        static public void CommonPlotAndAsserts(UnitDataSet unitDataSet, double[] estDisturbance, double[] trueDisturbance,
            UnitModel identifiedModel, UnitModel trueModel, double maxAllowedGainOffsetPrc, 
            double maxAllowedMeanDisturbanceOffsetPrc = 30, bool isStatic=false)
        {
            Vec vec = new Vec();

            Assert.IsTrue(estDisturbance != null);
            string caseId = TestContext.CurrentContext.Test.Name + "y";
            bool doDebugPlot = false;
            if (doDebugPlot)
            {
                double[] d_HF = vec.Subtract(unitDataSet.Y_meas, unitDataSet.Y_setpoint);
               // this gives "perfectly" the d_LF, that together with d_HF gives d_est
                double[] d_LF = vec.Multiply(vec.Subtract(unitDataSet.Y_proc, unitDataSet.Y_proc[0]), -1);
                // experimental code, comment out
               // var factor = 1.97/1.5*1.10;
               // double[] d_LF = vec.Multiply(vec.Subtract(unitDataSet.Y_proc, unitDataSet.Y_proc[0]), -1/factor);

                Shared.EnablePlots();
                Plot.FromList(new List<double[]>{ unitDataSet.Y_meas, unitDataSet.Y_setpoint,unitDataSet.Y_proc,
                unitDataSet.U.GetColumn(0), unitDataSet.U_sim.GetColumn(0), estDisturbance, trueDisturbance, d_HF,d_LF,vec.Add(d_LF,d_HF) },
                    new List<string> { "y1=y_meas", "y1=y_set", "y1=y_proc", "y2=u_meas(right)","y2=u_sim(right)", 
                        "y3=est disturbance", "y3=true disturbance","y3= d_HF","y3=d_LF","y3=d_LF+d_HF"},
                    unitDataSet.GetTimeBase(), caseId + "_CommonPlotsAndAsserts");
                Shared.DisablePlots();
            }

            // estimated gain
            double estGain = identifiedModel.modelParameters.GetTotalCombinedProcessGain(0);
            double trueGain = trueModel.modelParameters.GetTotalCombinedProcessGain(0);
            double gainOffsetPrc = Math.Abs(estGain - trueGain) / Math.Abs(trueGain) * 100;
            Assert.IsTrue(gainOffsetPrc < maxAllowedGainOffsetPrc, "est.gain:" + estGain + "|true gain:" + trueGain);

            // time constant
            if (!isStatic)
            {
                const double TC_TOL_PRC = 30;
                double estTimeConstant_s = identifiedModel.modelParameters.TimeConstant_s;
                double trueTimeConstant_s = trueModel.modelParameters.TimeConstant_s;
                double timeConstant_tol_s = trueTimeConstant_s * TC_TOL_PRC / 100;
                Assert.IsTrue(Math.Abs(estTimeConstant_s - trueTimeConstant_s) <= timeConstant_tol_s, "est time constant " + estTimeConstant_s + " too far off " + trueTimeConstant_s);
            }
        }

    }
}
