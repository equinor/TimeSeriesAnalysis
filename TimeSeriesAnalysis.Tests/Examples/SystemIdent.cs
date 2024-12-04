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
            Shared.EnablePlots();
            NonlinearUnitModel();
            Shared.DisablePlots();
        }

        [TestCase, Explicit]
        public void PidIdent_Ex()
        {
            Shared.EnablePlots();
            PidModelId();
            Shared.DisablePlots();
        }

        [TestCase, Explicit]
        public void ClosedLoop_Ex()
        {
            Shared.EnablePlots();
            ClosedLoopId();
            Shared.DisablePlots();
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
            var refData = new UnitDataSet();
            refData.U = U;
            refData.CreateTimeStamps(timeBase_s);
            PlantSimulator.SimulateSingle(refData, refModel,true);

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
            var idDataSet = new UnitDataSet();
            idDataSet.U = U;
            idDataSet.CreateTimeStamps(timeBase_s);
            PlantSimulator.SimulateSingleToYmeas(idDataSet, trueModel,noiseAmplitude);

            // do identification of unit model
            FittingSpecs fittingSpecs = new FittingSpecs(designParameters.U0, designParameters.UNorm);
            UnitModel idModel = UnitIdentifier.Identify(ref idDataSet, fittingSpecs);

            Plot.FromList(new List<double[]> { idModel.GetFittedDataSet().Y_sim,
                idModel.GetFittedDataSet().Y_meas,
                refData.Y_sim,
                u1, u2 },
                 new List<string> { "y1=ysim", "y1=ymeas", "y1=yref(linear)", "y3=u1", "y3=u2" },
                 (int)timeBase_s, "NonlinearUnitModelEx", default);

            PlotGain.PlotSteadyState(idModel, trueModel);

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

        //adopted from Dynamic_DistStep_EstiamtesOk(5,5) unit test
        #region ex_CLOSED_LOOP
        void ClosedLoopId()
        {
            PidParameters pidParameters1 = new PidParameters()
            {
                Kp = 0.2,
                Ti_s = 20
            };
            UnitParameters trueModelParameters = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1.5 },
                TimeDelay_s = 5,
                Bias = 5
            };

            double stepAmplitude = 5;
            int timeBase_s = 1;
            int N = 300;
            var trueDisturbance = TimeSeriesCreator.Step(100, N, 0, stepAmplitude);

            var trueProcessModel = new UnitModel(trueModelParameters, "TrueProcessModel");

            // create synthetic dataset
            var pidModel1 = new PidModel(pidParameters1, "PID1");

            var processSim = new PlantSimulator(
             new List<ISimulatableModel> { pidModel1, trueProcessModel });
            processSim.ConnectModels(trueProcessModel, pidModel1);
            processSim.ConnectModels(pidModel1, trueProcessModel);
            var inputData = new TimeSeriesDataSet();

            inputData.Add(processSim.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));

            inputData.Add(processSim.AddExternalSignal(trueProcessModel, SignalType.Disturbance_D), trueDisturbance);
            inputData.CreateTimestamps(timeBase_s);
            var isOk = processSim.Simulate(inputData, out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            var pidDataSet = processSim.GetUnitDataSetForPID(inputData.Combine(simData), pidModel1);

            (var identifiedModel, var estDisturbance) = ClosedLoopUnitIdentifier.Identify(pidDataSet, pidModel1.GetModelParameters());

            Console.WriteLine(identifiedModel.ToString());

            Shared.EnablePlots();
            Plot.FromList(new List<double[]>{ pidDataSet.Y_meas, pidDataSet.Y_setpoint,
                pidDataSet.U.GetColumn(0),  trueDisturbance },
                new List<string> { "y1=y meas", "y1=y set", "y2=u(right)", "y3=true disturbance" },
                pidDataSet.GetTimeBase(), "ClosedLoopId_dataset");
            Plot.FromList(new List<double[]>{ estDisturbance,  trueDisturbance },
                new List<string> { "y1=est disturbance", "y1=true disturbance" },
                pidDataSet.GetTimeBase(), "ClosedLoopId_disturbances");
            Plot.FromList(new List<double[]> { pidDataSet.Y_meas, pidDataSet.Y_sim },
                 new List<string> { "y1=y_meas", "y1=y_sim" },
                 pidDataSet.GetTimeBase(), "ClosedLoopId_ysim");
            Plot.FromList(new List<double[]> { pidDataSet.U.GetColumn(0), pidDataSet.U_sim.GetColumn(0) },
                 new List<string> { "y1=u_meas", "y1=u_sim" },
                 pidDataSet.GetTimeBase(), "ClosedLoopId_usim");
            Shared.DisablePlots();
        }
        #endregion
    }
}
