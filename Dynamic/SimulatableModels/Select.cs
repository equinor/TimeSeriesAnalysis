using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// An enum of the type of Select model
    /// </summary>
    public enum SelectType
    { 
        /// <summary>
        /// This value should not occur
        /// </summary>
        NOT_SET=0,
        /// <summary>
        /// Min-select
        /// </summary>
        MIN=1,

        /// <summary>
        /// Max-select
        /// </summary>
        MAX =2,
    }

    /// <summary>
    /// Simulatable select block
    /// <para>
    /// This block can function either as "minimum" or "maximum" selector, mainly inteded
    /// for simulating "min select" or "max selct" pid-control by combining with
    /// <seealso cref="PIDModel"/>
    /// </para>
    /// </summary>
    public class Select : ModelBaseClass, ISimulatableModel 
    {
        private SelectType type;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ID"></param>
        public Select(SelectType type, string ID)
        {
            processModelType = ProcessModelType.Select;
            this.type = type;
            SetID(ID);
        }

        /// <summary>
        /// Iterate simulation
        /// </summary>
        /// <param name="inputsU"></param>
        /// <param name="badDataID"></param>
        /// <returns></returns>
        public double Iterate(double[] inputsU, double badDataID = -9999)
        {
            if (type == SelectType.MAX)
            {
                return (new Vec(badDataID)).Max(inputsU);
            }
            else
            {
                return (new Vec(badDataID)).Min(inputsU);
            }
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
        /// <returns>the steady-state value, if it is not possible to calculate, a <c>null</c> is returned</returns>
        public  double? GetSteadyStateOutput(double[] u0)
        {
            return Iterate(u0);
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
