using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{

    public enum SelectType
    { 
        NOT_SET=0,
        MIN=1,
        MAX =2
    }

    /// <summary>
    /// Selects "Min" or "Max" select block
    /// </summary>
    public class Select : ModelBaseClass, ISimulatableModel 
    {
        private SelectType type;

        public Select(SelectType type, string ID)
        { 
            this.type = type;
            SetID(ID);
        }


        public double Iterate(double[] inputsU, double badDataID = -9999)
        {
            if (type == SelectType.MAX)
            {
                return (new Vec(badDataID)).Max(inputsU);
            }
            // default: min
         //   else if (type == SelectType.MAX)
            {
                return (new Vec(badDataID)).Max(inputsU);
            }
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


        public override SignalType GetOutputSignalType()
        { 
            return SignalType.SelectorOut; 
        }

        

    }
}
