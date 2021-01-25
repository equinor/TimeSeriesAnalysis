using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{
    public class MovingAvg
    {
        int bufferSize;
        double[] buffer;
        int valuesWritten;
        int bufferPos;

        public MovingAvg(int bufferSize)
        {
            this.bufferSize = bufferSize;
            bufferPos =-1;
            valuesWritten = 0;
            buffer = new double[bufferSize];
        }

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
