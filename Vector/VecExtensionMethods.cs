using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

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
            StringBuilder sb = new StringBuilder();
            if (array == null)
            {
                return "null";
            }
            if (array.Length > 0)
            {
                sb.Append("[");
                sb.Append(SignificantDigits.Format(array[0], nSignificantDigits).ToString("", CultureInfo.InvariantCulture));
                for (int i = 1; i < array.Length; i++)
                {
                    sb.Append(dividerStr);
                    sb.Append(SignificantDigits.Format(array[i], nSignificantDigits).ToString("",CultureInfo.InvariantCulture) );
                }
                sb.Append("]");
            }
            return sb.ToString();
        }
    }
}
