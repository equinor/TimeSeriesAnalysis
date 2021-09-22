using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.SysId
{
    public class ProcessModelParamters
    {
        public double TimeConstant_s { get; set; } = 0;
        public int TimeDelay_s { get; set; } = 0;
        public double[] ProcessGain { get; set; } = null;
        public double[] ProcessGain_CurvatureTerm { get; set; } = null;//TODO: nonlinear curvature term
        public  double[] u0 { get; set; } = null;
        public  double Bias { get; set; } = 0;
    }

    public class ProcessModel
    {
        private ProcessModelParamters modelParameters;
        private LowPass lp;

        public ProcessModel(ProcessModelParamters modelParamters)
        {
            this.modelParameters = modelParamters;
        }

        public void InitSim(double dT_s)
        {
            this.lp = new LowPass(dT_s);
        }

        public double IterateSimulation(double[] inputsU)
        {
            double y_static = modelParameters.Bias;
            for (int curInput = 0; curInput < inputsU.Length; curInput++)
            {
                if (modelParameters.u0 != null)
                {
                    y_static += modelParameters.ProcessGain[curInput] *
                        (inputsU[curInput] - modelParameters.u0[curInput]);
                }
                else
                {
                    y_static += modelParameters.ProcessGain[curInput] *
                            inputsU[curInput];
                }

                if (modelParameters.ProcessGain_CurvatureTerm != null)
                { 
                    //TODO
                }

    
            }
            double y = lp.Filter(y_static, modelParameters.TimeConstant_s);
            return y;
        }

        public double[] Simulate(double[,] inputsU, double? dT_s= null)
        {
            if (dT_s.HasValue)
            {
                InitSim(dT_s.Value);
            }
            if (inputsU == null)
                return null;
            this.lp = new LowPass(modelParameters.TimeConstant_s);
            int N = inputsU.GetNRows();
            double[] output = new double[N];
            for (int rowIdx = 0; rowIdx < N; rowIdx++)
            {
                output[rowIdx] = IterateSimulation(inputsU.GetRow(rowIdx));
            }
            return output;
        }



    }
}
