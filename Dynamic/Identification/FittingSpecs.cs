namespace TimeSeriesAnalysis.Dynamic
{


    /// <summary>
    /// variables that are set prior to fitting. 
    /// </summary>
    public class FittingSpecs
    {
        public FittingSpecs()
        { 
        
        }
        public FittingSpecs(double[] u0, double[] uNorm)
        { 
            this.u0 = u0;
            this.uNorm = uNorm; 
        }


        public double[] u0 = null;

        public double[] uNorm = null;

        /// <summary>
        ///  all values below this threshold are ignored during fitting(if set to NaN, no minimum is applied)
        /// </summary>
        public double? Y_min_fit = null;

        /// <summary>
        /// all values above this threshold are ignored during fitting(if set to NaN, no maximum is applied)
        /// </summary>
        public double? Y_max_fit = null;


        /// <summary>
        /// The minimum input value(if set to NaN,then fitting considers all data)
        /// </summary>
        public double[] U_min_fit = null;

        /// <summary>
        /// the maximum allowed input value(if set to NaN, then fitting considers all data)
        /// </summary>
        public double[] U_max_fit = null;



    }
}
