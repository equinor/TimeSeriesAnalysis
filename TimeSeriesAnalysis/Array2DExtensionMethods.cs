﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis
{
    ///<summary>
    /// Extension methods based on Array2D
    ///</summary>

    public static class Array2DExtensionMethods
    {
        ///<summary>
        /// overwrites the column in matrix with the new column newColumnValues
        ///</summary>
        static public double[,] WriteColumn(this double[,] matrix, int colIdx, double[] newColumnValues)
        {
            for (int rowIdx = 0; rowIdx < newColumnValues.Count(); rowIdx++)
            {
                matrix[rowIdx,colIdx] = newColumnValues[rowIdx];
            }
            return matrix;
        }

        ///<summary>
        /// returns the column of a 2d-array of strings corresponding to columnIndex (starts at zero)
        ///</summary>

        static public string[] GetColumn(this string[,] matrix, int columnIndex)
        {
            return Array2D<string>.GetColumn(matrix, columnIndex);
        }

        ///<summary>
        /// returns the column of a 2d-array of doubles corresponding to columnIndex (starts at zero)
        ///</summary>
        static public double[] GetColumn(this double[,] matrix, int columnIndex)
        {
            return Array2D<double>.GetColumn(matrix, columnIndex);
        }

        ///<summary>
        /// returns the row of a 2d-array of strings corresponding to rowNumber (starts at zero)
        ///</summary>
        static public string[] GetRow(this string[,] matrix, int rowNumber)
        {
            return Array2D<string>.GetRow(matrix, rowNumber);
        }

        ///<summary>
        /// returns the row of a 2d-array of doubles corresponding to rowNumber (starts at zero)
        ///</summary>

        static public double[] GetRow(this double[,] matrix, int rowNumber)
        {
            return Array2D<double>.GetRow(matrix, rowNumber);
        }

        ///<summary>
        /// Parses a column in a 2d-array and returns the results as a vector of date-times.
        ///</summary>
       
        static public DateTime[] GetColumnParsedAsDateTime(this string[,] matrix, int columnNumber, string dateFormat)
        {
            return Array2D.GetColumnParsedAsDateTime(matrix, columnNumber, dateFormat);
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

        ///<summary>
        /// Returns the columns corresponding to columnNumbers as a 2d-array
        ///</summary>

        static public double[,] GetColumns(this double[,] matrix, int[] columnNumbers)
        {
            return Array2D<double>.GetColumns(matrix, columnNumbers);
        }

        ///<summary>
        /// Converts a 2d-array to a jagged array
        ///</summary>

        static public double[][] Convert2DtoJagged(this double[,] matrix)
        {
            return Array2D<double>.CreateJaggedFrom2D(matrix);
        }

        ///<summary>
        /// Return the number of columns of a 2d-matrix
        ///</summary>

        static public int GetNColumns(this double[,] matrix)
        {
            if (matrix == null)
                return 0;
            return matrix.GetLength(1);
        }

        /// <summary>
        /// Get number of columns 
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        static public int GetNColumns(this double[][] matrix)
        {
            if (matrix == null)
                return 0;
            return matrix.ElementAt(0).GetLength(0);
        }

        ///<summary>
        /// Return the number of rows of a 2d-matrix
        ///</summary>

        static public int GetNRows(this double[,] matrix)
        {
            if (matrix == null)
                return 0;
            return matrix.GetLength(0);
        }

        /// <summary>
        /// Get nubmer of rows
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        static public int GetNRows(this double[][] matrix)
        {
            if (matrix == null)
                return 0;
            return matrix.GetLength(0);
        }

        ///<summary>
        /// Transposes a 2d-matrix.
        ///</summary>

        static public double[,] Transpose(this double[,] matrix)
        {
            return Array2D<double>.Transpose(matrix);
        }

    }
}
