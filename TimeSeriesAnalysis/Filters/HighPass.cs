using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// A high-pass recursive time-series filter based on <seealso cref="LowPass"/>.
    /// <para>
    /// This filter is in a recursive(feedback) IIR form that is simple to implement, has few coefficients, 
    /// requires litte memory and computation. This filter is causal, meaning that
    /// for calculating the filtered value at time <c>k</c> it does not use future values such as <c>k+1</c>, but 
    /// this is at the expense of introducing a time-shift/phase-shift.
    /// </para>
    /// <seealso cref="LowPass"/>
    /// <seealso cref="BandPass"/>
    /// <seealso cref="MovingAvg"/>
    /// </summary>
    public class HighPass
    {
        private LowPass lp;
        private double nanValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="TimeBase_s">time base/sampling time in seconds of data to be fed filter</param>
        /// <param name="nanValue">value of input signal to be ignored/treated as NaN</param>
        public HighPass(double TimeBase_s, double nanValue=-9999)
        {
            this.nanValue = nanValue;
            this.lp = new LowPass(TimeBase_s, nanValue);
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
            return (new Vec(nanValue)).Subtract(signal,lp.Filter(signal,FilterTc_s,order)); 
        }


    }
}
