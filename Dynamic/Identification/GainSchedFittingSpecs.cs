namespace TimeSeriesAnalysis.Dynamic
{


    /// <summary>
    /// variables that are set prior to fitting. 
    /// </summary>
    public class GainSchedFittingSpecs: FittingSpecs
    {
        /// <summary>
        /// Default constructor
        /// </summary>
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

        /// <summary>
        /// If set to false, the model starts in the same value as the tuning set starts in, 
        /// if set to true, the model passes through the mean of the dataset.
        /// </summary>
        public bool DoSetOperatingPointToDatasetMean = false;



    }
}
