namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Class that contains special pid-controller parameters for anti-surge controllers
    /// </summary>
    public class PIDAntiSurgeParams
    {
        public PIDAntiSurgeParams(double kickPrc, double? ffRampDownRatePrcPerMin,
            int nKicksFound=0,double kickBelowThresholdE=-5 )
        {
            this.kickPrcPerSec              = kickPrc;
            this.ffRampDownRatePrcPerMin    = ffRampDownRatePrcPerMin;
            this.kickBelowThresholdE        = kickBelowThresholdE;
            this.nKicksFound                = nKicksFound;
        }
        public double kickBelowThresholdE; // if kick is "ctrldev" below this value (often zero)
        public double kickPrcPerSec;// how many percent to kick controller open if it closes 
        public double? ffRampDownRatePrcPerMin; // after a kick, valve closure will be rate-limited.
        public int nKicksFound;
    }
}
