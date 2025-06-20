namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Enum of types of signals that ProcessSimulator must differentiate between
    /// </summary>
    public enum SignalType
    { 
        /// <summary>
        /// Unset, should not occur
        /// </summary>
        Unset = 0,
        /// <summary>
        /// The output/manipulated variable of a PID-controller
        /// </summary>
        PID_U = 1,//PID
        /// <summary>
        /// Setpoint of a PID-controller
        /// </summary>
        Setpoint_Yset = 2,//PID
        /// <summary>
        /// An input to a model that is an output of another process-model
        /// </summary>
        NonPIDInternal_U = 3,
        /// <summary>
        /// An input to a model that is not from a simulated PID-controller
        /// </summary>
        External_U = 4,
        /// <summary>
        /// The disturbance on the output of a "Process"
        /// </summary>
        Disturbance_D = 5,//SubProcess
        /// <summary>
        /// The output of a "SubProcess"
        /// </summary>
        Output_Y = 6,
        /// <summary>
        /// The output of a select block
        /// </summary>
        SelectorOut =7, // Select
        /// <summary>
        /// A noise signal
        /// </summary>
        OutputNoise = 8 


    }
}
