using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{

    ///<summary>
    /// Numerical low-pass filtering of time-series.
    /// 
    /// This filter is in a recursive(feedback) IIR form that is simple to implement, has few coefficients, 
    /// requires litte memory and computation. This filter is causal, meaning that
    /// for calcuating the filtered value at time <c>k</c> it does not use future values such as <c>k+1</c>, but 
    /// this is at the expense of introducing a time-shift/phase-shift.
    /// 
    /// ///</summary>
    public class LowPass
    {
        private double timeBase_s;
        private double prevFilteredSignal;
        private double prevFilteredSignalOrder2;
        private int nSignals = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="TimeBase_s">The time base, the time interval between each time step of the dataset, in seconds</param>
        public LowPass(double TimeBase_s)
        {
            this.timeBase_s = TimeBase_s;
            this.nSignals = 0;
        }

        /// <summary>
        /// Adds a single data point to the filter
        /// </summary>
        /// <param name="signal">data point</param>
        /// <param name="FilterTc_s">filter time constant in seconds</param>
        /// <param name="order">filter order, either 1 or 2 is supported</param>
        /// <param name="doReset">usually false, setting to true causes filter to reset to the value of signal</param>
        /// <returns></returns>
        public double Filter(double signal, double FilterTc_s, int order=1, bool doReset = false)
        {
            if (nSignals < 2)
            {
                nSignals++;
            }
            double a;
            double filteredSignal= signal;
            if (FilterTc_s < 0.4 * this.timeBase_s)
                a = 0;// (*/ if cutoff frequency is set close to sampling freq, the filter will fail fail - to - safe and just drop filtering*)
            else
                a = 1 / (1 + this.timeBase_s / FilterTc_s); // (*time constant *)

            if (Double.IsNaN(a))
            {
                a = 0;// turn off filter if 
            }

            if (Double.IsNaN(this.prevFilteredSignal) || Double.IsInfinity(this.prevFilteredSignal) || prevFilteredSignal == -9999)
                filteredSignal = signal;
            else
            {
                if (order == 1)
                {
                    filteredSignal = a * this.prevFilteredSignal + (1 - a) * signal;
                }
                else if (order == 2)
                {
                    double signalOrder2 = a * this.prevFilteredSignalOrder2 + (1 - a) * signal;
                    filteredSignal = a * this.prevFilteredSignal + (1 - a) * signalOrder2;
                    this.prevFilteredSignalOrder2 = signalOrder2;
                }
                else
                    filteredSignal = signal;
            }

            this.prevFilteredSignal = filteredSignal;
            if (nSignals <= 1)
                doReset = true;
            if (doReset)
            {
                this.prevFilteredSignal = signal; // (*Force filter to steady - state *)
                filteredSignal = signal;
            }
            return filteredSignal;
        }

        /// <summary>
        /// Filter an entire time-series in one command
        /// </summary>
        /// <param name="signal">the vector of the entire time-series to be filtered</param>
        /// <param name="FilterTc_s">filter time constant</param>
        /// <param name="order">filter order, either 1 or 2</param>
         /// <returns>a vector of the filtered time-series</returns>
        public double[] Filter(double[] signal, double FilterTc_s, int order=1)
        {
            double[] outSig = new double[signal.Length];
            for (int i = 0; i < signal.Count(); i++)
            {
                outSig[i] = this.Filter(signal[i], FilterTc_s, order,false);
            }
            return outSig;
        }

    }
}
