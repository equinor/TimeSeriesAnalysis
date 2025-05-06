using System;
using System.Linq;
using System.Numerics;
using Accord.Math;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Finds an estimate of the period of a signal.
    /// </summary>
    public class SignalPeriodEstimator
    {
        /// <summary>
        /// Uses the largest-in-magnitude index of the FFT of the signal to estimate the period.
        /// </summary>
        public static double? EstimatePeriod(double[] signal, double timeBase_s, double significantPeakThreshold = 0.3)
        {
            if (signal == null || signal.Length == 0)
            {
                return null;
            }

            int fft_length = (int) Math.Pow(
                2,
                Math.Floor(
                    Math.Log(
                        signal.Length, 2
                    )
                )
            );

            double[] fft = CalculateFFT(signal, fft_length);

            // Remove DC / 0 Hz term before further analysis
            fft = fft.Skip(1).ToArray();

            double[] fft_descending = fft.OrderByDescending(x => x).ToArray();
            bool significantPeak = false;
            for (int i = 1; i < fft_descending.Length; i++)
            {
                if (fft_descending[i] < (1 - significantPeakThreshold) * fft_descending[i - 1])
                {
                    // Ignore frequency peaks that are too low in magnitude
                    if ((i > Math.Floor(fft_descending.Length * significantPeakThreshold)) && !significantPeak)
                    {
                        return null;
                    }
                    significantPeak = true;
                }
            }

            if (!significantPeak)
            {
                return null;
            }

            int highest_freq_index = fft.ArgMax();
            if (highest_freq_index == 0)
            {
                return null;
            }

            return timeBase_s * (fft_length / highest_freq_index);
        }

        private static double[] CalculateFFT(double[] data, int fft_length)
        {
            Complex[] complex_data = new Complex[fft_length];

            int i = 0;
            foreach (double element in data.Take(fft_length))
            {
                complex_data[i] = new Complex(element, 0.0);
                i++;
            }

            FourierTransform.FFT(complex_data, FourierTransform.Direction.Forward);

            double[] result = new double[fft_length / 2];
            for (int j = 0; j < (fft_length / 2); j++)
            {
                result[j] = complex_data[j].Magnitude;
            }

            return result;
        }
    }
}
