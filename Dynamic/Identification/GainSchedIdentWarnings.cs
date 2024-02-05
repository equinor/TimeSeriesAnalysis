namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Enum of recognized warning or error states during identification of process model
    /// </summary>
    public enum GainSchedIdentWarnings
    {
        /// <summary>
        /// No errors or warnings
        /// </summary>
        Nothing = 0,

        /// <summary>
        /// Identifying some of the sub-models failed (possibly a sign of insufficient excitation)
        /// </summary>
        UnableToIdentifySomeSubmodels = 1,

        /// <summary>
        /// In order to identify model, the range of data used in the estimation of certain sub-models had to be increased
        /// beyond the threshold ranges. This can cause imprecise gains, and you may consider reducing the number of threshold, or
        /// obtaining a tuning data set with more information/exctiation.
        /// </summary>
        InsufficientExcitationBetweenEachThresholdToBeCertainOfGains = 2


    }
}
