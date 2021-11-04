namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Parameters describing PID-controller parameters for feed-forward
    /// <seealso cref="PIDModel"/>
    /// <seealso cref="PidController"/>
    /// </summary>
    public class PidFeedForward
    {
        /// <summary>
        ///  if true, then the feed-forward term is added to the output
        /// </summary>
        public bool isFFActive = false;

        /// <summary>
        /// feed-forward high pass filter order (0, 1 or 2)
        /// </summary>
        public int FFHP_filter_order = 1; // 
        /// <summary>
        /// feed-forward low pass filter order (0, 1 or 2)
        /// </summary>
        public int FFLP_filter_order = 1;

        /// <summary>
        /// feed-forward low-pass time constant in seconds (should be larger than FF_HP_Tc_s or zero.)
        /// </summary>
        public double FF_LP_Tc_s = 0;

        /// <summary>
        /// feed-forward high-pass time constant in seconds(should be smaller than FF_HP_Tc_s or zero.)
        /// </summary>
        public double FF_HP_Tc_s = 0;

        /// <summary>
        /// feed-forward gain. If variable is zero then no feed-forward is added to output.
        /// </summary>
        public double FF_Gain = 0; 

    }
}