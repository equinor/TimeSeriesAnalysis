using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

using NUnit.Framework;

namespace TimeSeriesAnalysis._Examples
{
    [TestFixture]
    class SystemIdent
    {

        [TestCase,Explicit]
        public void NonlinearUnitModel_Ex()
        {
            NonlinearUnitModel();
        }

        [TestCase, Explicit]
        public void PidIdent_Ex()
        {
            PidModelId();
        }


        #region ex_NONLINEAR_UNIT_MODEL
        public void NonlinearUnitModel()
        {
            // constants of the designed 
            double timeConstant_s = 10;
            double timeDelay_s = 4;
            double timeBase_s = 1;
            double bias = 1;
            double noiseAmplitude = 0.1;

            // create the input U as a matrix
            double[] u1 = TimeSeriesCreator.ThreeSteps(60, 120, 180, 240, 0, 1, 2, 3);
            double[] u2 = TimeSeriesCreator.ThreeSteps(90, 150, 210, 240, 2, 1, 3, 2);
            double[,] U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2 });

            // simulate a linear reference model for comparison
            UnitParameters paramtersNoCurvature = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 2, -0.05 },
                U0 = new double[] { 1.1, 1.1 },
                Bias = bias
            };
            var refModel = new UnitModel(paramtersNoCurvature, "Reference");
            var refSim  = new UnitSimulator(refModel);
            var refData = new UnitDataSet();
            refData.U = U;
            refSim.Simulate(ref refData);

            // simulate the nonlinear model 
            UnitParameters designParameters = new UnitParameters
            {
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1, 0.7 },
                Curvatures = new double[] { 1, -0.8 },
                U0 = new double[] { 1.1, 1.1 },// set this to make results comparable
                UNorm = new double[] { 1, 1 },// set this to make results comparable
                Bias = bias
            };
            var trueModel = new UnitModel(designParameters, "NonlinearModel1");
            var sim = new UnitSimulator(trueModel);
            var idDataSet = new UnitDataSet();
            idDataSet.U = U;
            idDataSet.CreateTimeStamps(timeBase_s);
            sim.SimulateYmeas(ref idDataSet, noiseAmplitude);

            // do identification
            FittingSpecs fittingSpecs = new FittingSpecs(designParameters.U0, designParameters.UNorm);
            UnitModel idModel = UnitIdentifier.Identify(ref idDataSet, fittingSpecs);

            Plot.FromList(new List<double[]> { idModel.GetFittedDataSet().Y_sim,
                idModel.GetFittedDataSet().Y_meas,
                refData.Y_sim,
                u1, u2 },
                 new List<string> { "y1=ysim", "y1=ymeas", "y1=yref(linear)", "y3=u1", "y3=u2" },
                 (int)timeBase_s, "NonlinearUnitModelEx", default);

            Console.WriteLine(idModel.ToString());
        }

        #endregion


        #region ex_PID_ID
        void PidModelId()
        {
            // create a PlantSimulator simulated dataset with known paramters
            double timeBase_s = 1;
            int N = 200;
            double noiseAmplitude = 0.1;
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            UnitParameters modelParameters1 = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 5,
                Bias = 5
            };
            var processModel1 = new UnitModel(modelParameters1, "Process1");
            var pidModel1 = new PidModel(pidParameters1, "PID1");
            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, processModel1 });
            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1);
            var inputData = new TimeSeriesDataSet();
            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), 
                TimeSeriesCreator.Step(N / 4, N, 50, 55));
            inputData.Add(processSim.AddExternalSignal(processModel1, SignalType.Disturbance_D), 
                TimeSeriesCreator.Noise(N, noiseAmplitude)) ;
            inputData.CreateTimestamps(timeBase_s);
            processSim.Simulate(inputData, out TimeSeriesDataSet simData);

            // do the actual identification
            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);
            var pidId = new PidIdentifier();
            var idResult = pidId.Identify(ref pidDataSet);

            // view results
            Console.WriteLine(idResult.ToString());
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y),
                inputData.GetValues(pidModel1.GetID(),SignalType.Setpoint_Yset),
                simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                Array2D<double>.GetColumn(pidDataSet.U_sim,(int)INDEX.FIRST) },
                new List<string>{ "y1=y_sim", "y1=y_set","y3=u_pid","y3=u_pid(id)" },timeBase_s); 
        }
        #endregion 

    }
}
