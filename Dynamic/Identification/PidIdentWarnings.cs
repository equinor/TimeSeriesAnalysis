using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic
{
    public enum PidIdentWarning
    {
        Nothing = 0,
        NotPossibleToIdentifyPIDcontroller_UAppearsUncorrelatedWithY = 1,
        NotPossibleToIdentifyPIDcontroller_BadInputData = 2,
        NotPossibleToIdentifyPIDcontroller_YsetIsBad = 3,
        InputSaturation_UcloseToUmax = 4,
        InputSaturation_UcloseToUmin = 5,
        NegativeEstimatedTi = 6,
        RegressionProblemFailedToYieldSolution = 7,
        PIDControllerDoesNotAppearToBeInAuto = 8,
        DataSetVeryShortComparedtoTMax = 9,
        PoorModelFit = 10,//this is perhaps not neccessary anymore, as the "joint sim test" is added, and model quality is always outputted.
        PIDControllerPossiblyInTracking = 11,// u changes, but pid model has extremely low rSquared, so it does not appear to be in control
        PIDControllerIsInCascadeModeWithFrequentSetpointChanges = 12,
    }
}
