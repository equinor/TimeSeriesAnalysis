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
        public double[] LinearGains { get; set; } = null;

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

        /// <summary>
        /// Return the process gain for a given index at u=u0
        /// <para>
        /// Note that for nonlinear processes, the process gain is given by a combination of 
        /// the linear and curvature terms of the model : dy/du(u=u0)
        /// </para>
        /// </summary>
        /// <param name="inputIdx"></param>
        /// <returns></returns>
        public double GetProcessGain(int inputIdx)
        {
            if (inputIdx > LinearGains.Length-1)
            {
                return double.NaN;
            }
            if (Curvatures == null)
                return LinearGains[inputIdx];
            if (inputIdx <= Curvatures.Length - 1)
            {
                if ( UNorm== null)
                    return LinearGains[inputIdx] + 2 * Curvatures[inputIdx];
                else
                    return LinearGains[inputIdx] + 2 * Curvatures[inputIdx] / UNorm[inputIdx];
            }
            else
            {
                return LinearGains[inputIdx];
            }
        }

        /// <summary>
        /// Get all process gains (including both linear and any nonlinear terms)
        /// </summary>
        /// <returns></returns>
        public double[] GetProcessGains()
        {
            var list = new List<double>();
            for (int inputIdx = 0; inputIdx < U0.Length; inputIdx++)
            {
                list.Add(GetProcessGain(inputIdx));
            }
            return list.ToArray();
        }



        /// <summary>
        /// Adds a identifiation warning to the object
        /// </summary>
        /// <param name="warning"></param>
        public void AddWarning(ProcessIdentWarnings warning)
        {
            if (!errorsAndWarningMessages.Contains(warning))
                errorsAndWarningMessages.Add(warning);
        }

        /// <summary>
        /// Get the list of all warnings given during identification of the model
        /// </summary>
        /// <returns></returns>
        public List<ProcessIdentWarnings> GetWarningList()
        {
            return errorsAndWarningMessages;
        }

    }
}
