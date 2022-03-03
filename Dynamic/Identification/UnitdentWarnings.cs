namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Enum of recognized warning or error states during identification of process model
    /// </summary>
    public enum UnitdentWarnings
    {
        /// <summary>
        /// No errors or warnings
        /// </summary>
        Nothing = 0, 

        /// <summary>
        /// The dataset is time span is very short compared to the maximal time-constant given as input to the algorithm
        /// </summary>
        DataSetVeryShortComparedtoTMax = 1,

        /// <summary>
        /// 
        /// </summary>
        RegressionProblemFailedToYieldSolution = 2,

        /// <summary>
        /// The time delay which gave the lowest objective function is the biggest allowed time delay 
        /// - consider increasing this limit or if something is wrong
        /// </summary>
        TimeDelayAtMaximumConstraint = 3,

        /// <summary>
        /// When considering different time delays internally, you expect the "best" to have both
        /// the lowest objective functino _and_ the lowest paramter uncertainty 
        /// - but for some reason this is not the case
        /// </summary>
        TimeDelayInternalInconsistencyBetweenObjFunAndUncertainty = 4,

        /// <summary>
        /// Time constant is not identifiable from dataset
        /// </summary>
        TimeConstantNotIdentifiable = 5,

        /// <summary>
        /// Time constant estimates vary significantly across dataset, indicating that something is wrong 
        /// </summary>
        TimeConstantEstimateNotConsistent = 6, // validation did not match estimation

        /// <summary>
        /// It was not possible to identify the 
        /// </summary>
        NotPossibleToIdentify = 7,

        /// <summary>
        /// Estimation returned an enourmous time constant, this is an indication of something is wrong
        /// </summary>
        TimeConstantEstimateTooBig = 8,
        
        /// <summary>
        /// Re-estimating bias method returned null, so the bias from the intial estimation is used, be careful the bias 
        /// estimate may be off!
        /// </summary>
        ReEstimateBiasFailed = 9,

        /// <summary>
        /// If disturbance is nonzero, then re-estimation of bias is turned off
        /// </summary>

        ReEstimateBiasDisabled = 10,


        /// <summary>
        /// One or more inputs were constant and not identifiable, this can affect other parameters as well, and consider removing the input
        /// or using a different dataset
        /// </summary>
        ConstantInputU = 11,

        /// <summary>
        /// Correlated inputs
        /// </summary>
        CorrelatedInputsU = 12

    }
}
