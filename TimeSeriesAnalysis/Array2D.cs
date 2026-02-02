using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;


namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Non-generic 2D-array methods
    /// </summary>
    static public class Array2D
    {

        /// <summary>
        /// Parses a column of strings in an array/matrix of strings 
        /// </summary>
        /// <param name="matrix">a 2D-array of strings</param>
        /// <param name="columnNumber">the index of the column to parse</param>
        /// <param name="dateFormat">the DateTime dateformat,For the format of dateFormat, see https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings</param>
        /// <returns></returns>
        static public DateTime[] GetColumnParsedAsDateTime(this string[,] matrix, int columnNumber, string dateFormat)
        {
            string[] strArray = Array2D<string>.GetColumn(matrix, columnNumber);

            List<DateTime> datetimes = new List<DateTime>();
            for (int i = 0; i < strArray.Length; i++)
            {
                datetimes.Add(DateTime.ParseExact(strArray[i], dateFormat, CultureInfo.InvariantCulture));
            }
            return datetimes.ToArray();
        }

    }
}
