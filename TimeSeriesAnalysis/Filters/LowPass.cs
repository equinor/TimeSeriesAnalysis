using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{

    ///<summary>
    /// Low-pass filtering of time-series.
    /// <para>
    /// This filter is in a recursive(feedback) IIR form that is simple to implement, has few coefficients, 
    /// requires litte memory and computation. This filter is causal, meaning that
    /// for calcuating the filtered value at time <c>k</c> it does not use future values such as <c>k+1</c>, but 
    /// this is at the expense of introducing a time-shift/phase-shift.
    /// </para>
    /// <seealso cref="HighPass"/>
    /// <seealso cref="BandPass"/>
    /// <seealso cref="MovingAvg"/>
    /// </summary>
    public class LowPass
    {
        private double timeBase_s;
        private double prevFilteredSignal;
        private double prevFilteredSignalOrder2;
        private int nSignals = 0;
        private double nanValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="TimeBase_s">The time base, the time interval between each time step of the dataset, in seconds</param>
        /// <param name="nanValue">value that is to be treated as NaN and ignored</param>
        public LowPass(double TimeBase_s, double nanValue = -9999)
        {
            this.timeBase_s = TimeBase_s;
            this.nSignals = 0;
            this.nanValue = nanValue;
        }

        /// <summary>
        /// Adds a single data point to the filter
        /// </summary>
        /// <param name="signal">data point</param>
        /// <param name="FilterTc_s">filter time constant in seconds</param>
        /// <param name="order">filter order, either 1 or 2 is supported</param>
        /// <param name="doReset">usually false, setting to true causes filter to reset to the value of signal</param>
        /// <param name="stepLength_s">if given, the filter ignores any "timebase" and uses the stepLength_ provided that can vary for each step</param>
        /// <returns></returns>
        public double Filter(double signal, double FilterTc_s, int order=1, bool doReset = false, double? stepLength_s = null)
        {
            double internalStepLength_s = this.timeBase_s;
            if (stepLength_s.HasValue)
            {
                internalStepLength_s = stepLength_s.Value;
            }

            if (nSignals < 2)
            {
                nSignals++;
            }
            double a;
            double filteredSignal= signal;
            if (FilterTc_s < 0.4 * internalStepLength_s)
                a = 0;// (*/ if cutoff frequency is set close to sampling freq, the filter will fail fail - to - safe and just drop filtering*)
            else
                a = 1 / (1 + internalStepLength_s / FilterTc_s); // (*time constant *)

            if (Double.IsNaN(a))
            {
                a = 0;// turn off filter if 
            }

            if (Double.IsNaN(this.prevFilteredSignal) || Double.IsInfinity(this.prevFilteredSignal) 
                || prevFilteredSignal == nanValue)
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
        /// <param name="indicesToIgnore">for these indices the of the signal, the filter should just "freeze" the value(can be null)</param>
        /// <returns>a vector of the filtered time-series</returns>
        public double[] Filter(double[] signal, double FilterTc_s, int order=1, List<int> indicesToIgnore= null)
        {
            double[] outSig = new double[signal.Length];

            if (indicesToIgnore == null)
            {
                for (int i = 0; i < signal.Count(); i++)
                {
                    outSig[i] = this.Filter(signal[i], FilterTc_s, order, false);
                }
            }
            else
            {
                int lastGoodIndex = 0;
                for (int i = 0; i < signal.Count(); i++)
                {
                    if (indicesToIgnore.Contains(i))
                    {
                        if (i == 0)
                        {
                            int i_forward = 1;
                            while (i_forward < signal.Count()-1 && indicesToIgnore.Contains(i_forward))
                            {
                                i_forward++;
                            }
                            lastGoodIndex = i_forward;
                        }
                    }
                    else
                    {
                        lastGoodIndex = i;
                    }
                    outSig[i] = this.Filter(signal[lastGoodIndex], FilterTc_s, order, false);
                }
            }
            return outSig;
        }

    }
}
