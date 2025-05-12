using System;
using System.Collections.Generic;
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

            List<(double magnitude, int originalIndex)> indexedFft = fft.Select((magnitude, index) => (magnitude, index)).ToList();

            var fft_descending = indexedFft.OrderByDescending(x => x.magnitude).ToList();
            List<int> significantPeakIndices = new List<int>();

            for (int i = 0; i < fft_descending.Count - 1; i++)
            {
                if (fft_descending[i + 1].magnitude < (1 - significantPeakThreshold) * fft_descending[i].magnitude)
                {
                    // Ignore frequency peaks that are too low in magnitude
                    if ((i > Math.Floor(fft_descending.Count * significantPeakThreshold)) && !significantPeakIndices.Any())
                    {
                        return null;
                    }

                    significantPeakIndices.Add(fft_descending[i].originalIndex);

                }
            }

            if (!significantPeakIndices.Any())
            {
                return null;
            }

            int highest_freq_index = significantPeakIndices[0];
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
