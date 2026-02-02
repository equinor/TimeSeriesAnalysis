using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{

    ///<summary>
    /// second-order filter
    /// </summary>
    public class SecondOrder
    {
        private double timeBase_s;
        private double prevFilteredSignal;
        private double prevPrevFilteredSignal;
        private int nSignals = 0;
        private double nanValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="TimeBase_s">The time base, the time interval between each time step of the dataset, in seconds</param>
        /// <param name="nanValue">value that is to be treated as NaN and ignored</param>
        public SecondOrder(double TimeBase_s, double nanValue = -9999)
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
        /// <param name="DampingZeta">filter damping factor zeta (0.3-1 results a single overshoot peak, less than 0.3 results in multiple peaks, over 1 results in a damped response with no overshoot) </param> 
        /// <param name="doReset">usually false, setting to true causes filter to reset to the value of signal</param>
        /// <returns></returns>
        public double Filter(double signal, double FilterTc_s, double DampingZeta=0, bool doReset = false)
        {
            double startUpSignals = 2;
            if (nSignals < startUpSignals)
            {
                nSignals++;
                this.prevPrevFilteredSignal = signal;// (*Force filter to steady - state *)
                this.prevFilteredSignal = signal; // (*Force filter to steady - state *)
            }

            double filteredSignal= signal;

            // The "FilterTc_s is supposed to be the first-order time-constant, but this 
            // needs to be translated to omega_n using a function that will also depend on DampingZeta

            var factor = 1.0;
            if (DampingZeta <= 0)
            {
                factor = 0;
            }
            else if (DampingZeta < 1)
            {
                factor = Math.Max(1, 1 + Math.Pow(DampingZeta - 0.5, 1 / 2));
            }
            else
                factor = DampingZeta * 2;


            double omega_n = 1 / (FilterTc_s)*factor; // 

            double divisor = (1 / timeBase_s + 2 * DampingZeta * omega_n / timeBase_s + Math.Pow(omega_n,2));
            if (divisor <= 0)
                return signal;

            double a_1 = (2 + 2 * DampingZeta * omega_n) / timeBase_s / divisor;
            double a_2 = -1 / timeBase_s / divisor;
            double b = 1-a_1-a_2 ;

            if (Double.IsNaN(this.prevFilteredSignal) || Double.IsInfinity(this.prevFilteredSignal) 
                || prevFilteredSignal == nanValue || Double.IsNaN(this.prevPrevFilteredSignal) 
                || Double.IsInfinity(this.prevPrevFilteredSignal)
                || prevPrevFilteredSignal == nanValue)
                filteredSignal = signal;
            else
            {
                filteredSignal = a_1 * this.prevFilteredSignal + a_2  * this.prevPrevFilteredSignal + b* signal;
            }
            this.prevPrevFilteredSignal = this.prevFilteredSignal;
            this.prevFilteredSignal = filteredSignal;
           // if (nSignals <= startUpSignals-1)
           //     doReset = true;
            if (doReset)
            {
                this.prevPrevFilteredSignal = signal;// (*Force filter to steady - state *)
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
