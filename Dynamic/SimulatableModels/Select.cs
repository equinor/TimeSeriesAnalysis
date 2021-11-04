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
        /// <summary>
        /// Range: Forces signal to stay above a given minimum and below a given maximum
        /// </summary>
        RANGE=3

    }

    /// <summary>
    /// Simulatable select block
    /// <para>
    /// This block can function either as "minimum" or "maximum" selector.
    /// It can be used either for enforcing ranges in plant simulations, but also for simulating 
    /// "min select" or "max selct" pid-control by combining with <seealso cref="PIDModel"/>
    /// </para>
    /// </summary>
    public class Select : ModelBaseClass, ISimulatableModel 
    {
        private SelectType type;
        private double threshold1;
        private double threshold2;

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
        /// Constructor with thresholds for range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ID"></param>
        /// <param name="threshold1"></param>
        /// <param name="threshold2"></param>
        public Select(SelectType type, string ID, double threshold1, double threshold2)
        {
            this.threshold1 = threshold1;
            this.threshold2 = threshold2;
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
            // default: min
         //   else if (type == SelectType.MAX)
            return (new Vec(badDataID)).Min(inputsU);
        }

        public  void WarmStart(double[] inputs, double output)
        { 
        
        }

        public  double? GetSteadyStateInput(double y0, int inputIdx = 0, double[] givenInputValues = null)
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
