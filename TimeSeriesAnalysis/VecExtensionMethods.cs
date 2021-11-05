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


        /// <summary>
        /// Add a scalar to vector
        /// </summary>
        /// <param name="array"></param>
        /// <param name="scalar"></param>
        /// <param name="nanValue"></param>
        /// <returns></returns>
        public static double[] Add(this double[] array, double scalar, double nanValue = -9999)
        {
            return (new Vec(nanValue)).Add(array, scalar);
        }


        /// <summary>
        /// Elementwise subtraction of two arrays of same size
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <param name="nanValue"></param>
        /// <returns></returns>
        public static double[] Sub(this double[] array1, double[] array2, double nanValue=-9999)
        {
            return (new Vec(nanValue)).Subtract(array1, array2);
        }

        /// <summary>
        /// Multiply vector by a scalar
        /// </summary>
        /// <param name="array"></param>
        /// <param name="scalar"></param>
        /// <param name="nanValue"></param>
        /// <returns></returns>
        public static double[] Mult(this double[] array, double scalar, double nanValue = -9999)
        {
            return (new Vec(nanValue)).Multiply(array,scalar);
        }

        /// <summary>
        /// Create a compact string of vector with a certain number of significant digits and a chosen divider
        /// </summary>
        /// <param name="array"></param>
        /// <param name="nSignificantDigits"></param>
        /// <param name="dividerStr"></param>
        /// <returns></returns>
        public static string ToString(this double[] array,int nSignificantDigits,string dividerStr =";")
        {
            return Vec.ToString(array, nSignificantDigits, dividerStr);
        }
    }
}
