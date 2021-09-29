using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    ///  Moving-average filter of time-series
    /// </summary>
    public class MovingAvg
    {
        int bufferSize;
        double[] buffer;
        int valuesWritten;
        int bufferPos;

        /// <summary>
        /// constructor
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
        /// Add one more value to the moving-average filter, call this method iterativly.
        /// </summary>
        /// <param name="val">the scalar value to be added</param>
        /// <returns></returns>
        public double Add(double val)
        {
            valuesWritten = valuesWritten + 1;
            if (bufferPos < bufferSize-1)
                bufferPos = bufferPos + 1;
            else
                bufferPos = 0;
            buffer[bufferPos] = val;

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
    }
}
