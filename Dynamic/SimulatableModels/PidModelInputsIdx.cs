namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// This determines the position in the U-vector given to Iterate for the class <c>PIDModel</c>
    /// </summary>
    public enum PidModelInputsIdx
    { 
        /// <summary>
        /// The index of the measured process variable input signal to the PID-controller
        /// </summary>
        Y_meas =0,
        /// <summary>
        /// The index of the required setpoint input signal to the PID-controller
        /// </summary>
        Y_setpoint = 1,
        /// <summary>
        /// The input index of an optional tracking input signal to the PID-controller
        /// </summary>
        Tracking = 2,
        /// <summary>
        /// The input index of an optional gain-scheduling input variable to the PID-controller
        /// </summary>
        GainScheduling = 3,
        /// <summary>
        /// The input index of an optional a feed-forward input signal to the PID-controller
        /// </summary>
        FeedForward = 4
    }

}
