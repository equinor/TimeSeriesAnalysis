using Accord.Math;
using System.Collections.Generic;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Parameters data class of the <seealso cref="UnitModel"/>
    /// </summary>
    public class UnitParameters : ModelParametersBaseClass
    {
        /// <summary>
        /// Fitting object
        /// </summary>
       // public FittingInfo Fitting { get; internal set; }

        /// <summary>
        /// The minimum allowed output value(if set to NaN, no minimum is applied)
        /// </summary>
        public double Y_min = double.NaN;

        /// <summary>
        /// the maximum allowed output value(if set to NaN, no maximum is applied)
        /// </summary>
        public double Y_max = double.NaN;


        /// <summary>
        /// The minimum input value(if set to NaN, no minimum is applied)
        /// </summary>
        public double U_min = double.NaN;

        /// <summary>
        /// the maximum allowed input value(if set to NaN, no maximum is applied)
        /// </summary>
        public double U_max = double.NaN;

        /// <summary>
        /// A time constant in seconds, the time a 1. order linear system requires to do 63% of a step response.
        /// Set to zero to turn off time constant in model.
        /// </summary>
        public double TimeConstant_s { get; set; } = 0;


        /// <summary>
        /// The uncertinty of the time constant estimate
        /// </summary>
        public double? TimeConstantUnc_s { get; set; } = null;

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
        /// An array of 95%  uncertatinty in the linear gains  (u-u0))
        /// </summary>
        public double[] LinearGainUnc { get; set; } = null;


        /// <summary>
        /// The nonlinear curvature of the process gain, this paramter is multiplied + Curvatures*((u-u0)/Unorm)^2.
        /// If value is <c>null</c>c> then no curvatures are added to the model
        /// </summary>
        public double[] Curvatures { get; set; } = null;

        /// <summary>
        /// The unceratainties of the curvature term of the process gains 
        /// </summary>
        public double[] CurvatureUnc { get; set; } = null;


        /// <summary>
        /// The working point of the model, the value of each U around which the model is localized.
        /// If value is <c>null</c>c> then no U0 is used in the model
        /// </summary>
        public double[] U0 { get; set; } = null;

        /// <summary>
        /// A "normal range" of U that is used in the nonlinear curvature term ((u-u0)/Unorm)^2.
        /// If value is <c>null</c>c> then no Unorm is used in the model
        /// </summary>
        public double[] UNorm { get; set; } = null;

        /// <summary>
        /// The constant bias that is added so that models and dataset match on average, this value will depend on U0 and other parameters.
        /// </summary>
        public  double Bias { get; set; } = 0;

        /// <summary>
        /// The 95% uncertainty of the bias
        /// </summary>
        public double? BiasUnc { get; set; } = null;


        private List<UnitdentWarnings> errorsAndWarningMessages;
        internal List<ProcessTimeDelayIdentWarnings> TimeDelayEstimationWarnings;

        /// <summary>
        /// Default constructor
        /// </summary>
        public UnitParameters()
        {
            Fitting = null;
            errorsAndWarningMessages = new List<UnitdentWarnings>();
        }


        /// <summary>
        /// Get the number of inputs U to the model.
        /// </summary>
        /// <returns></returns>
        public int GetNumInputs()
        {
            if (LinearGains != null)
                return LinearGains.Length;
            else
                return 0;
        }

        public UnitParameters CreateCopy()
        {
            // arrays are reference types, so by default only the reference is copied, use
            // .clone here to make an actual new object with a new reference
            UnitParameters newP = new UnitParameters();
            newP.Y_min = Y_min;
            newP.Y_max = Y_max;
            newP.TimeConstant_s = TimeConstant_s;
            newP.TimeDelay_s = TimeDelay_s;
            newP.LinearGains = (double[])LinearGains.Clone();
            if (LinearGainUnc == null)
                newP.LinearGainUnc = null;
            else
                newP.LinearGainUnc = (double[])LinearGainUnc.Clone();
            if (Curvatures == null)
                newP.Curvatures = null;
            else
                newP.Curvatures = (double[])Curvatures.Clone();
            if (CurvatureUnc == null)
                newP.CurvatureUnc = null;
            else
                newP.CurvatureUnc = (double[])CurvatureUnc.Clone();
            newP.U0 = U0;
            newP.UNorm = UNorm;
            newP.Bias = Bias;
            newP.BiasUnc = BiasUnc;
            newP.errorsAndWarningMessages = errorsAndWarningMessages;
            newP.TimeDelayEstimationWarnings = TimeDelayEstimationWarnings;
            return newP;
        }

        /// <summary>
        /// Return the "total combined" process gain for a given index at u=u0, a combination of lineargain and curvature gain
        /// <para>
        /// Note that for nonlinear processes, the process gain is given by a combination of 
        /// the linear and curvature terms of the model : dy/du(u=u0)
        /// </para>
        /// </summary>
        /// <param name="inputIdx"></param>
        /// <returns></returns>
        public double GetTotalCombinedProcessGain(int inputIdx)
        {
            if (LinearGains == null)
            {
                return double.NaN;
            }
            if (inputIdx > LinearGains.Length-1)
            {
                return double.NaN;
            }
            if (Curvatures == null)
                return LinearGains[inputIdx];
            if (Curvatures.Length<LinearGains.Length)
                return LinearGains[inputIdx];
            if (double.IsNaN(Curvatures[inputIdx]) )
                return LinearGains[inputIdx];

            // the "gain" of u is dy/du
            // d/du [c/(1-a)*(u-u0)^2)]=
            //      d/du [c/(1-a)*(u^2-2*u*u0 +2u0))]= 
            //      d/du [c/(1-a)*(u^2-2u*u0)] =
            //      c/(1-a)*[2*u-2u*u0]

            if (inputIdx <= Curvatures.Length - 1)
            {
                if ( UNorm== null)
                    return LinearGains[inputIdx] +  Curvatures[inputIdx];
                else
                    return LinearGains[inputIdx] +  Curvatures[inputIdx] / UNorm[inputIdx];//todo - this is wrong!!!
            }
            else
            {
                return LinearGains[inputIdx];
            }
        }

        /// <summary>
        /// Return the process gain uncertatinty for a given input index at u=u0
        /// <para>
        /// Note that for nonlinear processes, the process gain is given by a combination of 
        /// the linear and curvature terms of the model : dy/du(u=u0)
        /// </para>
        /// </summary>
        /// <param name="inputIdx"></param>
        /// <returns></returns>
        public double GetTotalCombinedProcessGainUncertainty(int inputIdx)
        {
            if (LinearGainUnc == null)
                return double.NaN;

            if (inputIdx > LinearGainUnc.Length - 1)
            {
                return double.NaN;
            }
            if (Curvatures == null|| CurvatureUnc == null)
                return LinearGainUnc[inputIdx];
            if (double.IsNaN(CurvatureUnc[inputIdx]))
                return LinearGainUnc[inputIdx];

            if (inputIdx <= CurvatureUnc.Length - 1)
            {
                return LinearGainUnc[inputIdx] + CurvatureUnc[inputIdx];
            }
            else
            {
                return LinearGainUnc[inputIdx];
            }
        }




        /// <summary>
        /// Get all process gains (including both linear and any nonlinear terms)
        /// </summary>
        /// <returns>may return null if no process gains given</returns>
        public double[] GetProcessGains()
        {
            var list = new List<double>();

            if (LinearGains == null)
            {
                return null;
            }

            var nInputs = LinearGains.Length;

            for (int inputIdx = 0; inputIdx < nInputs; inputIdx++)
            {
                list.Add(GetTotalCombinedProcessGain(inputIdx));
            }
            return list.ToArray();
        }

        /// <summary>
        /// Get all the process gain uncertainties
        /// </summary>
        /// <returns></returns>
        public double[] GetProcessGainUncertainties()
        {
            var list = new List<double>();
            for (int inputIdx = 0; inputIdx < U0.Length; inputIdx++)
            {
                list.Add(GetTotalCombinedProcessGainUncertainty(inputIdx));
            }
            return list.ToArray();
        }


        /// <summary>
        /// Adds a identifiation warning to the object
        /// </summary>
        /// <param name="warning"></param>
        public void AddWarning(UnitdentWarnings warning)
        {
            if (!errorsAndWarningMessages.Contains(warning))
                errorsAndWarningMessages.Add(warning);
        }

        /// <summary>
        /// Get the list of all warnings given during identification of the model
        /// </summary>
        /// <returns></returns>
        public List<UnitdentWarnings> GetWarningList()
        {
            return errorsAndWarningMessages;
        }

    }
}
