namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Enum of recognized warning or error states during identification of process model
    /// </summary>
    public enum ProcessIdentWarnings
    {
        /// <summary>
        /// 
        /// </summary>
        Nothing = 0, 
        DataSetVeryShortComparedtoTMax = 1,
        RegressionProblemFailedToYieldSolution = 2,
        TimeDelayAtMaximumConstraint = 3,
        TimeDelayInternalInconsistencyBetweenObjFunAndUncertainty = 4,
        TimeConstantNotIdentifiable = 5,
        TimeConstantEstimateNotConsistent = 6, // validation did not match estimation
        NotPossibleToIdentify = 7,
        TimeConstantEstimateTooBig = 8
    }
}
