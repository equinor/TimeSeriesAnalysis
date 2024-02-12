using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Paramters of the divide model
    /// </summary>
    public class DivideParameters:ModelParametersBaseClass 
    {


        /// <summary>
        /// Default constructor
        /// </summary>
        public DivideParameters()
        {
        }

        /// <summary>
        /// minimum output value
        /// </summary>
        public double Y_min { get; set; } = double.NegativeInfinity;
        /// <summary>
        /// maximum output value
        /// </summary>
        public double Y_max { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// protect from specific bad value
        /// </summary>
        public double NanValueIn { get; set; } = -9999;

        /// <summary>
        /// value to give if divideByZero
        /// </summary>
        public double NanValueOut { get; set; } = -9999;


    }
}
