using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// A high-pass recursive time-series filter.
    /// Internally the filter is based on LowPass.
    /// </summary>
    public class HighPass
    {
        LowPass lp;

        public HighPass(double TimeBase_s)
        {
            this.lp = new LowPass(TimeBase_s);
        }

        /// <summary>
        /// Adds a single data point to the filter
        /// </summary>
        /// <param name="signal">data point</param>
        /// <param name="FilterTc_s">filter time constant in seconds</param>
        /// <param name="order">filter order, eitehr 1 or 2 is supported</param>
        /// <param name="doReset">usually false, setting to true causes filter to reset to the value of signal</param>
        /// <returns></returns>
        public double Filter(double signal, double FilterTc_s, int order = 1, bool doReset = false)
        {
            return signal-lp.Filter(signal,FilterTc_s,order, doReset);
        }

        /// <summary>
        /// Filter an entire time-series in one command
        /// </summary>
        /// <param name="signal">the vector of the entire time-series to be filtered</param>
        /// <param name="FilterTc_s">filter time constant</param>
        /// <param name="order">filter order, either 1 or 2</param>
        /// <returns>a vector of the filtered time-series</returns>
        public double[] Filter(double[] signal, double FilterTc_s, int order = 1)
        {
            return Vec.Subtract(signal,lp.Filter(signal,FilterTc_s,order)); 
        }


    }
}
