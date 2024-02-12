namespace TimeSeriesAnalysis.Dynamic
{


    /// <summary>
    /// Class that contains variables that are specified prior to fitting, such as working point, minima and maxima. 
    /// </summary>
    public class FittingSpecs
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="u0">an optional working point, a vector of u around which to localile</param>
        /// <param name="uNorm">an optional vector of values with which to normalize inputs</param>
        public FittingSpecs(double[] u0=null, double[] uNorm=null)
        { 
            this.u0 = u0;
            this.uNorm = uNorm; 
        }

        /// <summary>
        /// A vector of values subtracted form u to set the local opertating point: (u-u0)/uNorm
        /// Can be set to null, in which case no value is subtracted from u
        /// </summary>
        public double[] u0 = null;

        /// <summary>
        /// A vector of values used to normalize u internally in the model by the equation: (u-u0)/uNorm
        /// Can be set to null, in which case u is left un-normalized.
        /// </summary>
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
