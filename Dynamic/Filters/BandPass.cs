using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Numerical band-pass filter based on <c>LowPass</c>
    /// 
    /// This filter is in a recursive(feedback) IIR form that is simple to implement, has few coefficients, 
    /// requires litte memory and computation. This filter is causal, meaning that
    /// for calculating the filtered value at time <c>k</c> it does not use future values such as <c>k+1</c>, but 
    /// this is at the expense of introducing a time-shift/phase-shift.
    /// 
    /// </summary>
    public class BandPass
    {
        LowPass lp;
        HighPass hp;

        public BandPass(double TimeBase_s)
        {
            this.lp = new LowPass(TimeBase_s);
            this.hp = new HighPass(TimeBase_s);
        }


        /// <summary>
        /// Adds a single data point to the filter
        /// </summary>
        /// <param name="signal">data point</param>
        /// <param name="lpFilterTc_s">low-passfilter time constant in seconds</param>
        /// <param name="hpFilterTc_s">high-passfilter time constant in seconds</param>
        /// <param name="order">filter order, either 1 or 2 is supported</param>
        /// <param name="doReset">usually false, setting to true causes filter to reset to the value of signal</param>
        /// <returns></returns>
        public double Filter(double signal, double lpFilterTc_s,double hpFilterTc_s, int order = 1, bool doReset = false)
        {
            return signal - lp.Filter(signal, lpFilterTc_s, order, doReset) - hp.Filter(signal, hpFilterTc_s,order,doReset);
        }

        /// <summary>
        /// Filter an entire time-series in one command
        /// </summary>
        /// <param name="signal">the vector of the entire time-series to be filtered</param>
        /// <param name="lpFilterTc_s">filter time constant</param>
        /// <param name="hpFilterTc_s">filter time constant</param>
        /// <param name="order">filter order, either 1 or 2</param>
        /// <returns>a vector of the filtered time-series</returns>
        public double[] Filter(double[] signal, double lpFilterTc_s,double hpFilterTc_s, int order = 1)
        {
            return Vec.Subtract(Vec.Subtract(signal, lp.Filter(signal, lpFilterTc_s, order)),
                hp.Filter(signal,hpFilterTc_s,order));
        }


    }
}
