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
    /// This is a reasuble class for providing time-delay functionality to simulatable models.
    /// </para>
    /// <seealo cref="DefaultProcessModel"/>
    /// </summary>
    public class TimeDelay
    {
        private double timeBase_s;
        private double[] delayBuffer;
        private int delayBufferPosition;
        private int delaySignalCounter;

        private int nBufferSize;

        /// <summary>
        /// Initalize
        /// </summary>
        /// <param name="timeBase_s">the simulation time interval between each subsequent call to Delay (in seconds)</param>
        /// <param name="timeDelay_s">the time delay to be simulated(in seconds). Note that the time delay will be rounded up to neares whole number factor of <c>timeBase_s</c></param>
        public TimeDelay(double timeBase_s, double timeDelay_s)
        {
            this.timeBase_s = timeBase_s;
            this.nBufferSize = (int)Math.Ceiling((double)(timeDelay_s / timeBase_s));
            if (delayBuffer == null)
            {
                delayBuffer = new double[this.nBufferSize];
                delayBufferPosition = 0;
            }
        }

        /// <summary>
        /// Delays output by a certain number of time steps 
        /// </summary>
        /// <param name="inputSignal">input signal to be delayed</param>
        /// <returns>a version of <c>inputSignal</c> that is delayed</returns>
        public double Delay(double inputSignal)
        {
          
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
