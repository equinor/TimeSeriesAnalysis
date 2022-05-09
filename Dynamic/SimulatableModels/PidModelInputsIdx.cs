namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// This determines the position in the U-vector given to Iterate for the class <c>PIDModel</c>
    /// </summary>
    public enum PidModelInputsIdx
    { 
        Y_meas =0,
        Y_setpoint=1,
        Tracking=2,
        GainScheduling=3,
        FeedForward=4
    }

}
