using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.SysId
{

    public class DefaultProcessModel : IProcessModel
    {
        private DefaultProcessModelParameters modelParameters;
        private LowPass lowPass;
        private double timeBase_s;


        public DefaultProcessModel(DefaultProcessModelParameters modelParameters, double timeBase_s)
        {
            this.modelParameters = modelParameters;
            this.timeBase_s = timeBase_s;
            InitSim(timeBase_s);
        }

        /// <summary>
        /// Initalizer of model that for the given dataSet also creates the resulting y_sim
        /// </summary>
        /// <param name="modelParameters"></param>
        /// <param name="dataSet"></param>
        public DefaultProcessModel(DefaultProcessModelParameters modelParameters, ProcessDataSet dataSet)
        {
            this.modelParameters = modelParameters;
            InitSim(dataSet.TimeBase_s);
        }




        //  public DefaultProcessModelParameters GetModelParameters()
        public IModelParameters GetModelParameters()
        {
            return modelParameters;
        }

        public void InitSim(double timeBase_s)
        {
            this.lowPass = new LowPass(timeBase_s);
        }

        /// <summary>
        /// Iterates the process model state one time step, based on the inputs given
        /// </summary>
        /// <param name="inputsU">vector of inputs</param>
        /// <returns>the updated process model output</returns>
        public double Iterate(double[] inputsU)
        {
            double y_static = modelParameters.Bias;
            for (int curInput = 0; curInput < inputsU.Length; curInput++)
            {
                if (modelParameters.U0 != null)
                {
                    y_static += modelParameters.ProcessGains[curInput] *
                        (inputsU[curInput] - modelParameters.U0[curInput]);
                }
                else
                {
                    y_static += modelParameters.ProcessGains[curInput] *
                            inputsU[curInput];
                }

                if (modelParameters.ProcessGainCurvatures != null)
                { 
                    //TODO
                }
            }
            double y = lowPass.Filter(y_static, modelParameters.TimeConstant_s);
            // TODO: add time-delay
            return y;
        }

        /// <summary>
        /// Is the model static or dynamic?
        /// </summary>
        /// <returns>Returns true if the model is static(no time constant or time delay terms),otherwise false.</returns>
        public bool IsModelStatic()
        {
           return modelParameters.TimeConstant_s == 0 && modelParameters.TimeDelay_s == 0;
        }
        /*
        /// <summary>
        /// Simulates the process model over a period of time, based on a matrix of input vectors
        /// </summary>
        /// <param name="inputsU">a 2D matrix, where each column represents the intputs at each progressive time step to be simulated</param>
        /// <param name="timeBase_s"> the time step in seconds of the simulation. This can be omitted if the model is static.
        /// <returns>null in inputsU is null or if dT_s is not specified and the model is not static</returns>
        public double[] Simulate(double[,] inputsU, double? timeBase_s= null)
        {
            if (timeBase_s.HasValue)
            {
                InitSim(timeBase_s.Value);
            }
            if (inputsU == null)
                return null;
            if (lowPass == null && !IsModelStatic())
            {
                return null;
            }
            int N = inputsU.GetNRows();
            double[] output = new double[N];
            for (int rowIdx = 0; rowIdx < N; rowIdx++)
            {
                output[rowIdx] = Iterate(inputsU.GetRow(rowIdx));
            }
            return output;
        }
        */


    }
}
