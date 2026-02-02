using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Moving-average low-pass filter
    /// <para>
    /// This filter is causal, meaning that for calculating the filtered value at time <c>k</c>
    /// it does not use future values such as <c>k+1</c>, this is at the expense of introducing a time-shift/phase-shift.
    /// </para>
    /// <para>
    /// This is a finite-impulse-response type filter.
    /// </para>
    /// <para>
    /// An alterative to this filter is <seealso cref="LowPass"/>, which is infinite-impulse-response, and also 
    /// requires less working memory(this filter needs to hold a buffer equal to <c>bufferSize</c>, but 
    /// <seealso cref="LowPass"/> only needs to keep its last value in memory). 
    /// <seealso cref="LowPass"/> will have less phase-shift/time-shift, because it 
    /// places most weight on the last datapoint, whereas this filter will weight all data points in its buffer equally and
    /// thus responds sluggish. 
    /// </para>
    /// <para>
    /// The advantage of this filter is that it allows you to control precisely how many past values are weighted.
    /// </para>
    /// <seealso cref="LowPass"/>
    /// <seealso cref="HighPass"/>
    /// <seealso cref="BandPass"/>
    /// </summary>
    public class MovingAvg
    {
        int bufferSize;
        double[] buffer;
        int valuesWritten;
        int bufferPos;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bufferSize"> the number of samples to average, which determines the size of the buffer to create</param>
        public MovingAvg(int bufferSize)
        {
            this.bufferSize = bufferSize;
            bufferPos =-1;
            valuesWritten = 0;
            buffer = new double[bufferSize];
        }

        /// <summary>
        /// Add value to the moving-average filter.
        /// </summary>
        /// <param name="signal">the scalar value to be added</param>
        /// <returns>the output of the filter, given the new value</returns>
        public double Filter(double signal)
        {
            valuesWritten = valuesWritten + 1;
            if (bufferPos < bufferSize-1)
                bufferPos = bufferPos + 1;
            else
                bufferPos = 0;
            buffer[bufferPos] = signal;

            int valuesInMean, lastIdx ;
            if (valuesWritten >= bufferSize)
            {
                valuesInMean = bufferSize;
                lastIdx = bufferSize;
            }
            else
            {
                valuesInMean = valuesWritten;
                lastIdx = valuesWritten;
            }
            double ret = 0;
            for (int i = 0;i<lastIdx;i++)
            {
                ret += buffer[i];
            }
            ret = ret / valuesInMean;
            return ret;
        }

        /// <summary>
        /// Run filter over a vector of values
        /// </summary>
        /// <param name="signal">vector of values to be filtered</param>
        /// <returns>the moving-average filtered version of <c>values</c></returns>
        public double[] Filter(double[] signal)
        {
            List<double> ret = new List<double>();
            foreach (var value in signal)
            {
                ret.Add(Filter(value));                
            }
            return ret.ToArray();
        }


    }
}
