using System;
using System.Collections.Generic;
using TimeSeriesAnalysis;


namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Class that contains the paramters that determine the disturbance that acts on 
    /// the process/pid system to be simulated by ProcessPIDsim
    /// 
    /// The disturbance can consist of one or more sinuses, steps, pulses or a time-series can be specified.
    /// </summary>
    public class PIDsimDisturbance
    {
        private Dictionary<double, double> sinusPeriodAmplitudeDict = new Dictionary<double, double>();
        private Dictionary<int, double> stepSampleAmplitudeDict  = new Dictionary<int, double>();
        private double timeBase_s = 1;

        private List<int> pulseTimeList = new List<int>();
        private int pulseDisturbanceLength = 0;
        private double pulseAmplitude = 0;
        private int pulseDisturbanceCounter = 0;

        private double[] distubanceTimeSeries = null;

        private bool addRandomWalk=false;
        private double amplitudeRandomWalk=0;
        private Random randomWalkRand;

        /// <summary>
        ///  Constructor - empty constructor => no disturbance
        /// </summary>
        public PIDsimDisturbance()
        { 
        }

        /// <summary>
        ///  Constructor which adds just a random walk with the given amltidue
        /// </summary>
        public PIDsimDisturbance(double randomWalkAmplitude)
        {
            AddRandomWalk(randomWalkAmplitude);
        }

        private int d_iterationCounter = 0;
        private double d_prev = 0;

        /// <summary>
        /// Get the next time step of the distubance d[k] at time 
        /// </summary>
        public double Iterate()
        {
            double d = 0;
            pulseDisturbanceCounter = pulseDisturbanceLength + 1;//set to zero at start of each pulse
            pulseTimeList.Sort();//sort from first to last
            // sinus disturbances
            if (sinusPeriodAmplitudeDict.Count>0)
            {
                foreach (KeyValuePair<double,double> sinusPeriodAmplitude in sinusPeriodAmplitudeDict)
                {
                    double amplitude     = sinusPeriodAmplitude.Value;
                    double sinusPeriod_s = sinusPeriodAmplitude.Key;
                    d = d + amplitude * Math.Sin((d_iterationCounter * timeBase_s) / sinusPeriod_s * Math.PI * 2);
                }
            }
            // step disturbances
            if (stepSampleAmplitudeDict.Count>0)
            {
                foreach (KeyValuePair<int, double> stepSampleAmplitude in stepSampleAmplitudeDict)
                {
                    int stepDisturbanceTime_samples = stepSampleAmplitude.Key;
                    double stepDisturbanceAmplitude = stepSampleAmplitude.Value;
                    if (d_iterationCounter > stepDisturbanceTime_samples)
                    {
                        d = d + stepDisturbanceAmplitude;
                    }
                }
            }
            // pulse disturbances
            if (pulseTimeList.Count > 0)
            {
               //  if (pulseTimeList.First() == d_iterationCounter)
                if (pulseTimeList[0] == d_iterationCounter)
                {
                    pulseDisturbanceCounter = 0;
                    pulseTimeList.RemoveAt(0);
                }
                if (pulseDisturbanceCounter >= pulseDisturbanceLength)
                {
                    //                    d = 0;
                }
                else
                {
                    pulseDisturbanceCounter++;
                    d = pulseAmplitude;
                }
            }
            // random walk  disturbances
            if (addRandomWalk)
            {
                d = d + d_prev + (randomWalkRand.NextDouble() - 0.5) * 2 * amplitudeRandomWalk / 100;
            }
            // time-series  disturbances
            if (distubanceTimeSeries!=null)
            {
                d = distubanceTimeSeries[d_iterationCounter];
            }
            d_prev = d;
            d_iterationCounter++;
            return d;
        }

        /// <summary>
        /// Adds a random-walk component with a given amplitude and seed number to the overall distubance
        /// </summary>

        public void AddRandomWalk(double amplitude,int seedNr = 2323)
        {
            addRandomWalk = true;
            amplitudeRandomWalk = amplitude;
            randomWalkRand = new Random(seedNr);
        }
        /// <summary>
        /// Adds a specific disutbance time series to the overall distubance
        /// </summary>

        public void AddDisturbanceTimeSeries(double[] disturbanceTimeSeries)
        {
            this.distubanceTimeSeries = disturbanceTimeSeries;
        }

        /// <summary>
        /// Adds a sinus component with specificed amplitued and period to the overall distubance
        /// </summary>

        public void AddSinusDisturbance(double sinusAmplitude_prc, double sinusPeriod_s, double timeBase_s)
        {
            sinusPeriodAmplitudeDict.Add(sinusPeriod_s, sinusAmplitude_prc);
            this.timeBase_s = timeBase_s;
        }
        /// <summary>
        /// Adds a step of a given amplitude after a given number of samples to the overall disturbance
        /// </summary>

        public void AddStepDisturbance(int stepDisturbanceTime_samples, double stepDisturbanceAmplitude=1)
        {
            stepSampleAmplitudeDict.Add(stepDisturbanceTime_samples, stepDisturbanceAmplitude);
        }

        /// <summary>
        /// Adds a pulse of a given amplitude and length  after a given number of samples(pulseTimeStepNr) to the overall disturbance
        /// </summary>

        public void AddPulseDisturbance(int pulseDisturbanceLength, double pulseAmplitude, 
            int pulseTimeStepNr)
        {
            this.pulseDisturbanceLength = pulseDisturbanceLength;
            this.pulseAmplitude = pulseAmplitude;
            this.pulseTimeList = new List<int>();
            this.pulseTimeList.Add(pulseTimeStepNr);
        }

    }
}
