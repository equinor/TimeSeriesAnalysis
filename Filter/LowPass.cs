using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{
    class LowPass
    {
        private double timeBase_s;
        private double prevFilteredSignal;
        private double prevFilteredSignalOrder2;
        private Int64 nSignals = 0;

        public LowPass(double TimeBase_s)
        {
            this.timeBase_s = TimeBase_s;
            this.nSignals = 0;
        }

        public double Filter(double signal, double FilterTc_s, int order=1, bool doReset = false)
        {
            nSignals++;
            double a;
            double filteredSignal= signal;
            if (FilterTc_s < 0.4 * this.timeBase_s)
                a = 0;// (*/ if cutoff frequency is set close to sampling freq, the filter will fail fail - to - safe and just drop filtering*)
            else
                a = 1 / (1 + this.timeBase_s / FilterTc_s); // (*time constant *)

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

        public double[] Filter(double[] signal, double FilterTc_s, int order=1, bool doReset = false)
        {
            double[] outSig = new double[signal.Length];
            for (int i = 0; i < signal.Count(); i++)
            {
                outSig[i] = this.Filter(signal[i], FilterTc_s, order,doReset);
            }
            return outSig;
        }

    }
}
