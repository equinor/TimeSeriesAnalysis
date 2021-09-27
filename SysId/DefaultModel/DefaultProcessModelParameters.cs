using System.Collections.Generic;

namespace TimeSeriesAnalysis.SysId
{
    /// <summary>
    /// Parameters data class of the "Default" process model 
    /// </summary>
    public class DefaultProcessModelParameters : IProcessModelParameters
    {
        public double TimeConstant_s { get; set; } = 0;
        public int TimeDelay_s { get; set; } = 0;
        public double[] ProcessGains { get; set; } = null;
        public double[] ProcessGainCurvatures { get; set; } = null;
        public  double[] U0 { get; set; } = null;
        public  double Bias { get; set; } = 0;

        public bool WasAbleToIdentify { get; set; }
        public double FittingR2 { get; set; }

        private List<ProcessIdentWarnings> errorsAndWarningMessages;

        public double GetFittingR2()
        {
            return FittingR2;
        }

        public double GetFittingObjFunVal()
        {
            return FittingR2;
        }

        public bool AbleToIdentify()
        {
            return WasAbleToIdentify;
        }
        
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
