using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{


    /// <summary>
    /// Simulatable divide block, requires exactly two inputs
    /// <para>
    /// </para>
    /// </summary>
    public class Divide : ModelBaseClass, ISimulatableModel 
    {
        /// <summary>
        /// Paramters that define the Divide model
        /// </summary>
        public DivideParameters divideParameters;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="divideParameters"></param>
        /// <param name="ID"></param>
        public Divide(DivideParameters divideParameters, string ID)
        {
            this.processModelType = ModelType.Divide;
            this.divideParameters = divideParameters;
            this.ID = ID;
        }

        /// <summary>
        /// Required model which answers if model will be able to simulate given current input
        /// </summary>
        /// <param name="explain">a string explaining reason for a false return, if applicable</param>
        /// <returns></returns>
        public bool IsModelSimulatable(out string explain)
        {
            if (divideParameters == null)
            {
                explain = "divideParameters is null";
                return false;
            }
            explain = "";
            return true;
        }


        /// <summary>
        /// Iterate simulation
        /// </summary>
        /// <param name="inputsU"></param>
        /// <param name="timeBase_s"></param>
        /// <param name="badDataID"></param>
        /// <returns></returns>
        public double[] Iterate(double[] inputsU, double timeBase_s,double badDataID = -9999)
        {
            if (divideParameters == null)
                return new double[] { divideParameters.NanValueOut };

            if (inputsU.Length == 2)
            {
                double ret = 0;
                if (inputsU[1] == 0 || // divide by zero
                    inputsU[1]== divideParameters.NanValueIn||
                    inputsU[0] == divideParameters.NanValueIn||
                    inputsU[1] == badDataID ||
                    inputsU[0] == badDataID ||
                    Double.IsNaN(inputsU[0]) ||
                    Double.IsNaN(inputsU[1]) ||
                    Double.IsInfinity(inputsU[0]) ||
                    Double.IsInfinity(inputsU[1]) 
                    )
                {
                    ret = divideParameters.NanValueOut;
                }
                else
                {
                    ret = inputsU[0] / inputsU[1];
                    ret = Math.Min(ret, divideParameters.Y_max);
                    ret = Math.Max(ret, divideParameters.Y_min);
                }
                return new double[] { ret };
            }
            return new double[] { divideParameters.NanValueOut };
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="output"></param>
        public void WarmStart(double[] inputs, double output)
        { 
        
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="y0"></param>
        /// <param name="inputIdx"></param>
        /// <param name="givenInputValues"></param>
        /// <returns></returns>
        public  double? GetSteadyStateInput(double y0, int inputIdx = 0, 
            double[] givenInputValues = null)
        {
            return null;//todo?
        }

        /// <summary>
        /// Get the steady state value of the model output
        /// </summary>
        /// <param name="u0">vector of inputs for which the steady state is to be calculated</param>
        /// <param name="badDataID">optional special value that indicates "Nan"</param>
        /// <returns>the steady-state value, if it is not possible to calculate, a <c>null</c> is returned</returns>
        public double? GetSteadyStateOutput(double[] u0, double badDataID = -9999)
        {
            return Iterate(u0,1, badDataID).First();
        }

        /// <summary>
        /// Gives the type of the output signal
        /// </summary>
        /// <returns></returns>
        public override SignalType GetOutputSignalType()
        { 
            return SignalType.SelectorOut; 
        }
    }
}
