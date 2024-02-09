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

        /// <summary>
        ///  Returns the portion of array1 starting and indStart, and ending at indEnd(or at the end if third paramter is omitted)
        ///</summary>
        /// <param name="array1">array to get subarray from</param>
        /// <param name="indStart">starting index</param>
        /// <param name="indEnd">ending index(or to the end if omitted)</param>
        /// <returns>null if indStart and indEnd are the same, otherwise the subarray</returns>
        public static double[] SubArray(this double[] array1, int indStart, int indEnd = -9999)
        {
            if (array1 == null)
                return null;

            if (indEnd > array1.Length - 1 || indEnd == -9999)
                indEnd = array1.Length - 1;
            else if (indEnd < 0)
            {
                indEnd = 0;
                return new double[0];
            }
            if (indStart < 0)
                indStart = 0;
            int length = indEnd - indStart + 1;
            if (length > 0)
            {
                double[] retArray = new double[length];
                int outInd = 0;
                for (int i = indStart; i <= indEnd; i++)
                {
                    retArray[outInd] = array1[i];
                    outInd++;
                }
                return retArray;
            }
            else
                return null;
        }



    }
}
