namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Enum of recognized warning or error states during identification of process model
    /// </summary>
    public enum GainSchedWarnings
    {
        /// <summary>
        /// No errors or warnings
        /// </summary>
        Nothing = 0, 

        /// <summary>
        /// Failed to initalize the gain scheduling model
        /// </summary>
        FailedToInitializeGainSchedModel = 1,


    }
}
