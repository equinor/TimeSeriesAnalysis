using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace TimeSeriesAnalysis
{
    public static class Array2DExtensionMethods
    {
        static public double[,] WriteColumn(this double[,] matrix, int colIdx, double[] allRowVals)
        {
            for (int rowIdx = 0; rowIdx < allRowVals.Count(); rowIdx++)
            {
                matrix[rowIdx,colIdx] = allRowVals[rowIdx];
            }
            return matrix;
        }


        static public string[] GetColumn(this string[,] matrix, int columnNumber)
        {
            return Array2D<string>.GetColumn(matrix, columnNumber);
        }

        static public double[] GetColumn(this double[,] matrix, int columnNumber)
        {
            return Array2D<double>.GetColumn(matrix, columnNumber);
        }

        static public string[] GetRow(this string[,] matrix, int rowNumber)
        {
            return Array2D<string>.GetRow(matrix, rowNumber);
        }

        static public double[] GetRow(this double[,] matrix, int rowNumber)
        {
            return Array2D<double>.GetRow(matrix, rowNumber);
        }

        ///<summary>
        ///  For the format of dateFormat, see https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings
        ///</summary>
       
        static public DateTime[] GetColumnParsedAsDateTime(this string[,] matrix, int columnNumber, string dateFormat)
        {
            string[] strArray = Array2D<string>.GetColumn(matrix, columnNumber);

            List<DateTime> datetimes = new List<DateTime>();
            for (int i= 0; i < strArray.Length; i++)
            {
                datetimes.Add(DateTime.ParseExact(strArray[i], dateFormat, CultureInfo.InvariantCulture));
            }
            return datetimes.ToArray();
        }

        ///<summary>
        /// Returns rows starting with rowIndex and onwards 
        ///</summary>
        static public double[] GetRowsAfterIndex(this double[] array, int rowIndex)
        {
            return Vec<double>.SubArray(array, rowIndex);
        }

        ///<summary>
        /// Returns rows starting with rowIndex and onwards 
        ///</summary>
        static public DateTime[] GetRowsAfterIndex(this DateTime[] array, int rowIndex)
        {
            return Vec<DateTime>.SubArray(array, rowIndex);
        }



        ///<summary>
        /// Returns rows starting with rowIndex and onwards 
        ///</summary>
        static public double[,] GetRowsAfterIndex(this double[,] array, int rowIndex)
        {
            double[,] result = new double[array.GetNRows() - rowIndex, array.GetNColumns()];

            for (int colIdx = 0; colIdx < array.GetNColumns(); colIdx++)
            {
                result.WriteColumn(colIdx, array.GetColumn(colIdx).GetRowsAfterIndex(rowIndex));
            }
            return result;
        }

        static public double[,] GetColumns(this double[,] matrix, int[] columnNumbers)
        {
            return Array2D<double>.GetColumns(matrix, columnNumbers);
        }

        static public double[][] Convert2DtoJagged(this double[,] matrix)
        {
            return Array2D<double>.Convert2DtoJagged(matrix);
        }

        static public int GetNColumns(this double[,] matrix)
        {
            return matrix.GetLength(1);
        }
        static public int GetNRows(this double[,] matrix)
        {
            return matrix.GetLength(0);
        }

        static public double[,] Transpose(this double[,] matrix)
        {
            return Array2D<double>.Transpose(matrix);
        }





    }
}
