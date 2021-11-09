using System.Collections.Generic;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Parameters data class of the <seealso cref="UnitModel"/>
    /// </summary>
    public class UnitParameters : ModelParametersBaseClass
    {
        /// <summary>
        /// ID of the solver used to identify the model
        /// </summary>
        public string SolverID;

        /// <summary>
        /// The minimum allowed output value(if set to NaN, no minimum is applied)
        /// </summary>
        public double Y_min = double.NaN;

        /// <summary>
        /// the maximum allowed output value(if set to NaN, no maximum is applied)
        /// </summary>
        public double Y_max = double.NaN;

        /// <summary>
        /// A time constant in seconds, the time a 1. order linear system requires to do 63% of a step response.
        /// Set to zero to turn off time constant in model.
        /// </summary>
        public double TimeConstant_s { get; set; } = 0;

        /// <summary>
        /// The time delay in seconds.This number needs to be a multiple of the sampling rate.
        /// Set to zero to turn of time delay in model.
        /// </summary>
        public double TimeDelay_s { get; set; } = 0;
        /// <summary>
        /// An array of gains that determine how much in the steady state each input change affects the output(multiplied with (u-u0))
        /// </summary>
        public double[] ProcessGains { get; set; } = null;

        /// <summary>
        /// The nonlinear curvature of the process gain, this paramter is multiplied + Curvatures*((u-u0)/Unorm)^2.
        /// If value is <c>null</c>c> then no curvatures are added to the model
        /// </summary>
        public double[] Curvatures { get; set; } = null;

        /// <summary>
        /// The working point of the model, the value of each U around which the model is localized.
        /// If value is <c>null</c>c> then no U0 is used in the model
        /// </summary>
        public  double[] U0 { get; set; } = null;

        /// <summary>
        /// A "normal range" of U that is used in the nonlinear curvature term ((u-u0)/Unorm)^2.
        /// If value is <c>null</c>c> then no Unorm is used in the model
        /// </summary>
        public double[] UNorm { get; set; } = null;

        /// <summary>
        /// The constant bias that is added so that models and dataset match on average, this value will depend on U0 and other parameters.
        /// </summary>
        public  double Bias { get; set; } = 0;

        private List<ProcessIdentWarnings> errorsAndWarningMessages;
        internal List<ProcessTimeDelayIdentWarnings> TimeDelayEstimationWarnings;
     
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public UnitParameters()
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
