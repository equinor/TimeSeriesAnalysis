using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Delays a signal by a specific number of time steps, keeping an internal buffer of delayed values 
    /// between iterations.
    /// <para>
    /// This is a reusable class for providing time-delay functionality to simulateable models.
    /// </para>
    /// <seealso cref="UnitModel"/>
    /// </summary>
    public class TimeDelaySamples
    {
        private double[] delayBuffer;
        private int delayBufferPosition;
        private int delaySignalCounter;
        private int nBufferSize;


        /// <summary>
        /// Time delay, set in terms of number of samples
        /// </summary>
        /// <param name="samples"></param>
        public TimeDelaySamples(int samples)
        {
            this.nBufferSize = samples;
            if (samples > 0) 
            { 
                if (delayBuffer == null)
                {
                    delayBuffer = new double[this.nBufferSize];
                    delayBufferPosition = 0;
                }
            }
        }

        /// <summary>
        /// Delays output by a certain number of time steps 
        /// </summary>
        /// <param name="inputSignal">input signal to be delayed</param>
        /// <returns>a version of <c>inputSignal</c> that is delayed</returns>
        public double Delay(double inputSignal)
        {
            // fail-to-safe
            if (nBufferSize == 0)
            {
                return inputSignal;
            }

            // handle first nBufferSize timesteps differently
            if (delaySignalCounter < nBufferSize)
            {
                delayBuffer[delayBufferPosition] = inputSignal;
                delaySignalCounter++;
                delayBufferPosition++;
                return inputSignal;
            }
            if (delayBufferPosition >= nBufferSize -1)
            {
                delayBufferPosition = 0;
            }
            else
            {
                delayBufferPosition++;
            }
            double returnValue = delayBuffer[delayBufferPosition];
            delayBuffer[delayBufferPosition] = inputSignal;
            return returnValue;
        }


    }
}
