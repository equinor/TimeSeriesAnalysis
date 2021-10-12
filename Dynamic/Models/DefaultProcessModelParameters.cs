using System.Collections.Generic;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Parameters data class of the "Default" process model 
    /// </summary>
    public class DefaultProcessModelParameters : ModelParametersBaseClass
    {
        public string SolverID;

        public double TimeConstant_s { get; set; } = 0;
        public double TimeDelay_s { get; set; } = 0;
        public double[] ProcessGains { get; set; } = null;
        public double[] ProcessGainCurvatures { get; set; } = null;
        public  double[] U0 { get; set; } = null;
        public  double Bias { get; set; } = 0;

        private List<ProcessIdentWarnings> errorsAndWarningMessages;
        internal List<ProcessTimeDelayIdentWarnings> TimeDelayEstimationWarnings;


     
        
        public DefaultProcessModelParameters()
        {
            errorsAndWarningMessages = new List<ProcessIdentWarnings>();
        }

        public void AddWarning(ProcessIdentWarnings warning)
        {
            if (!errorsAndWarningMessages.Contains(warning))
                errorsAndWarningMessages.Add(warning);
        }

        public List<ProcessIdentWarnings> GetWarningList()
        {
            return errorsAndWarningMessages;
        }

    }
}
