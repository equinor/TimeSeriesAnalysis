namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Class that contains special pid-controller parameters for anti-surge controllers
    ///  <seealso cref="PIDModel"/>
    /// <seealso cref="PIDcontroller"/>
    /// </summary>
    public class PIDAntiSurgeParams
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="kickPrc"></param>
        /// <param name="ffRampDownRatePrcPerMin"></param>
        /// <param name="nKicksFound"></param>
        /// <param name="kickBelowThresholdE"></param>
        public PIDAntiSurgeParams(double kickPrc, double? ffRampDownRatePrcPerMin,
            int nKicksFound=0,double kickBelowThresholdE=-5 )
        {
            this.kickPrcPerSec              = kickPrc;
            this.ffRampDownRatePrcPerMin    = ffRampDownRatePrcPerMin;
            this.kickBelowThresholdE        = kickBelowThresholdE;
            this.nKicksFound                = nKicksFound;
        }
        /// <summary>
        ///  if kick is "ctrldev" below this value (often zero)
        /// </summary>
        public double kickBelowThresholdE;

        /// <summary>
        /// how many percent to kick controller open if it closes 
        /// </summary>
        public double kickPrcPerSec;

        /// <summary>
        /// after a kick, valve closure will be rate-limited.
        /// </summary>
        public double? ffRampDownRatePrcPerMin; 

        /// <summary>
        /// Kicks counter
        /// </summary>
        public int nKicksFound;
    }
}
