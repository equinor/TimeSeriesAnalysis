using System;
using System.Collections.Generic;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Containing the information related to how a pid-controller
    /// should low-pass filter its input signal 
    /// </summary>
    public class PidFiltering
    {
        /// <summary>
        /// Time-constant in seconds of the low-pass filter
        /// </summary>
        public double TimeConstant_s;

        /// <summary>
        /// The order of the low-pass filter. Values 0(off), 1 and 2 are supported
        /// </summary>
        public int FilterOrder;

        /// <summary>
        /// Enable or disable the filtering with this variable
        /// </summary>
        public bool IsEnabled;

        public PidFiltering(bool IsEnabled = false, int FilterOrder = 0, double TimeConstant_s = 0)
        {
            this.IsEnabled = IsEnabled;
            this.FilterOrder = FilterOrder;
            this.TimeConstant_s = TimeConstant_s;
        }




    }
}
