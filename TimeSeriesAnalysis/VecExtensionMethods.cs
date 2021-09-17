using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis
{
    ///<summary>
    /// Utility functions and operations for treating arrays as mathetmatical vectors
    ///</summary>
    public static class VecExtensionMethods
    {

        ///<comment>
        /// Add scalar to vector
        ///</summary>
        public static double[] Add(this double[] array, double scalar)
        {
            return Vec.Add(array, scalar);
        }

        ///<comment>
        /// elementwise subtraction of two arrays of same size
        ///</summary>
        public static double[] Sub(this double[] array1, double[] array2)
        {
            return Vec.Sub(array1, array2);
        }

        ///<comment>
        /// Multiply vector by a scalar
        ///</summary>
        public static double[] Mult(this double[] array, double scalar)
        {
            return Vec.Mult(array,scalar);
        }

        ///<comment>
        /// Create a compact string of vector with a certain number of significant digits and a chosen divider
        ///</summary>
        public static string ToString(this double[] array,int nSignificantDigits,string dividerStr =";")
        {
            return Vec.ToString(array, nSignificantDigits, dividerStr);
        }
    }
}
