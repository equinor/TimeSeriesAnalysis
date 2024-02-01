namespace TimeSeriesAnalysis.Dynamic
{


    /// <summary>
    /// variables that are set prior to fitting. 
    /// </summary>
    public class GainSchedFittingSpecs: FittingSpecs
    {
        public GainSchedFittingSpecs()
        { 
        }

        /// <summary>
        /// for gain-scheduling, thresholds of u for changing between gains. 
        /// </summary>
        public double[] uGainThresholds = null;

        /// <summary>
        /// for gain-scheduling, thresholds of u for changing between gains. 
        /// </summary>
        public double[] uTimeConstantThresholds = null;

        /// <summary>
        /// for gain-scheduling, thresholds of u for changing between gains. 
        /// Default is 0, i.e. first input is used for gain-scheduling.
        /// </summary>
        public int uGainScheduledInputIndex = 0;
    }
}
