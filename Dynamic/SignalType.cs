namespace TimeSeriesAnalysis.Dynamic
{
    public enum SignalType
    { 
        Unset = 0,
        PID_U = 1,//PID
        Setpoint_Yset = 2,//PID
        NonPIDInternal_U = 3, // the input U to a subprocess is the output of another subprocess 
        External_U = 4,// the input U to a subprocess is externally provided
        Distubance_D = 5,//SubProcess
        Output_Y_sim = 6,//SubProcess
        //    State_X = 6,//SubProcess
//        Output_Y_sim = 7,//SubProcess
      //  Output_Y_meas = 6//SubProcess
    }
}
