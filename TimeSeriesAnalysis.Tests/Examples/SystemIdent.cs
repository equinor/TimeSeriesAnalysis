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
                WasAbleToIdentify = true,
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 2, -0.05 },
                U0 = new double[] { 1.1, 1.1 },
                Bias = bias
            };
            var refModel = new UnitModel(paramtersNoCurvature, timeBase_s, "Reference");
            var refSim  = new UnitSimulator(refModel);
            var refData = new UnitDataSet(timeBase_s,U);
            refSim.Simulate(ref refData);

            // simulate the nonlinear model 
            UnitParameters designParameters = new UnitParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = timeConstant_s,
                TimeDelay_s = timeDelay_s,
                LinearGains = new double[] { 1, 0.7 },
                Curvatures = new double[] { 1, -0.8 },
                U0 = new double[] { 1.1, 1.1 },// set this to make results comparable
                UNorm = new double[] { 1, 1 },// set this to make results comparable
                Bias = bias
            };
            var trueModel = new UnitModel(designParameters, timeBase_s, "NonlinearModel1");
            var sim = new UnitSimulator(trueModel);
            var idDataSet = new UnitDataSet(timeBase_s,U);
            sim.SimulateYmeas(ref idDataSet, noiseAmplitude);

            // do identification
            var modelId = new UnitIdentifier();
            UnitModel idModel = modelId.Identify(ref idDataSet, designParameters.U0, designParameters.UNorm);

            Plot.FromList(new List<double[]> { idModel.FittedDataSet.Y_sim,
                idModel.FittedDataSet.Y_meas,
                refData.Y_sim,
                u1, u2 },
                 new List<string> { "y1=ysim", "y1=ymeas", "y1=yref(linear)", "y3=u1", "y3=u2" },
                 (int)timeBase_s, "NonlinearUnitModelEx", default);

            Console.WriteLine(idModel.ToString());


        }

        #endregion 


    }
}
