using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Warnings that arise during identification of PidModels
    /// </summary>
    public enum PidIdentWarning
    {
        /// <summary>
        /// No warnings
        /// </summary>
        Nothing = 0,
        /// <summary>
        /// u and y are uncorrelated
        /// </summary>
        NotPossibleToIdentifyPIDcontroller_UAppearsUncorrelatedWithY = 1,
        /// <summary>
        /// The input data was tagged "bad" and could not be used for id
        /// </summary>
        NotPossibleToIdentifyPIDcontroller_BadInputData = 2,
        /// <summary>
        /// The setpoint data provided was tagged "bad"
        /// </summary>
        NotPossibleToIdentifyPIDcontroller_YsetIsBad = 3,
        /// <summary>
        /// The input u remains saturated at its maximum
        /// </summary>
        InputSaturation_UcloseToUmax = 4,
        /// <summary>
        /// The input u remains saturated at its minimum
        /// </summary>
        InputSaturation_UcloseToUmin = 5,
        /// <summary>
        /// Negative time constant. A non-causal model is an inidication of data error usually. 
        /// </summary>
        NegativeEstimatedTi = 6,
        /// <summary>
        /// Regression returned an error
        /// </summary>
        RegressionProblemFailedToYieldSolution = 7,
        /// <summary>
        /// The PID controller does not appear to move to changes in ymeas-yset, is it in manual?
        /// </summary>
        PIDControllerDoesNotAppearToBeInAuto = 8,
        /// <summary>
        /// The dataset needs to be sufficiently long compared to the longest time constatnt to be estimated
        /// </summary>
        DataSetVeryShortComparedtoTMax = 9,
        /// <summary>
        ///  u changes, but pid model has extremely low rSquared, so it does not appear to be in control
        /// </summary>
        PIDControllerPossiblyInTracking = 11,
        /// <summary>
        /// The pid controller is in cascade or is otherwise repsonding to extremely frequent setpoint changes that makes id very challenging.
        /// </summary>
        PIDControllerIsInCascadeModeWithFrequentSetpointChanges = 12,
    }
}
