﻿namespace TimeSeriesAnalysis.Dynamic
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

        ReEstimateBiasDisabledDueToNonzeroDisturbance = 10,


        /// <summary>
        /// One or more inputs were constant and not identifiable, this can affect other parameters as well, and consider removing the input
        /// or using a different dataset
        /// </summary>
        ConstantInputU = 11,

        /// <summary>
        /// Correlated inputs
        /// </summary>
        CorrelatedInputsU = 12,

        /// <summary>
        /// Some of the parameters returned were NaN, this can happen in the input and output vectors are all constants, such as all-zero
        /// </summary>
        /// 
        RegressionProblemNaNSolution = 14,

        /// <summary>
        /// If this is a closed loop system where setpoints of PID-controller change, and the "global search" at step1 of the ClosedLoopIdentifier failed to find a local minima when
        /// trying different gains. This warning likely means that the linear gain can be totally off the mark and may be too low. 
        /// </summary>
        /// 
        ClosedLoopEst_GlobalSearchFailedToFindLocalMinima = 15,
        /// <summary>
        /// UnitIdentify was able to solve the given estimation problem for the linear/static case, but when adding dynamics, the estimation failed for some reason.
        /// </summary>
        /// 
       DynamicModelEstimationFailed = 16,
        /// <summary>
        /// A negative time constant was returned, this is "non-causal", and could be due to a time-shift between u and y in the dataset.
        /// </summary>
       NonCausalNegativeTimeConstant = 17




    }
}
