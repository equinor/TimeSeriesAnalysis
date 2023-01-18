using System;
using System.Collections.Generic;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Containing the information related to how a pid-controller
    /// should low-pass filter its input signal 
    /// </summary>
    public class PidFilterParams
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

        public PidFilterParams(bool IsEnabled = false, int FilterOrder = 0, double TimeConstant_s = 0)
        {
            this.IsEnabled = IsEnabled;
            this.FilterOrder = FilterOrder;
            this.TimeConstant_s = TimeConstant_s;
        }
    }

    /// <summary>
    /// Class that handles the filtering of inputs y to a pid-controller
    /// Put into separate class as this is both a component of the PidController and of PidIdentifier,
    /// and important that implementation is equal.
    /// </summary>
    public class PidFilter
    {
        private PidFilterParams fParams;
        private double timebase_s;
        LowPass yFilt1,yFilt2;
        public PidFilter(PidFilterParams filterParams, double timebase_s)
        {
            this.fParams = filterParams;
            this.timebase_s = timebase_s;
            this.yFilt1 = new LowPass(timebase_s);
            this.yFilt2 = new LowPass(timebase_s);
        }

        internal double Filter(double y_process_prc)
        {
            double y_processFilt_prc = y_process_prc;
            if (fParams.IsEnabled)
            {
                if (fParams.FilterOrder == 1 && fParams.TimeConstant_s > 0)
                    y_processFilt_prc = yFilt1.Filter(y_process_prc, fParams.TimeConstant_s, 1, false);
                else if (fParams.FilterOrder == 2 && fParams.TimeConstant_s > 0)
                {
                    double y_processFilt1_prc = yFilt1.Filter(y_process_prc, fParams.TimeConstant_s, 1, false);
                    y_processFilt_prc = yFilt2.Filter(y_processFilt1_prc, fParams.TimeConstant_s, 1, false);
                }
                else
                {
                    y_processFilt_prc = y_process_prc;
                }
            }
            else
            {
                y_processFilt_prc = y_process_prc;
            }
            return y_processFilt_prc;
        }

    }


}
