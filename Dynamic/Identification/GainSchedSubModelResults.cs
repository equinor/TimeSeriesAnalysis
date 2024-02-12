namespace TimeSeriesAnalysis.Dynamic
{
    class GainSchedSubModelResults
    {
        public double[] LinearGains = null;
        public double TimeConstant_s =0;
        public bool NotEnoughExitationBetweenAllThresholds = false;
        public bool WasAbleToIdentfiy = false;
    }
}
