using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis
{
    ///<summary>
    ///  Generic array operations that can be done on arrays of any type, for operators specific to numerical arrays(matrices)
    ///  see Matrix.cs
    ///</summary>
    public class Array2D<T>
    {
        ///<summary>
        /// Convert a 2D array into a jagged array
        ///</summary>
        static public T[][] CreateJaggedFrom2D(T[,] matrix)
        {
            T[][] ret = new T[matrix.GetLength(0)][];

            for (int curRow = 0; curRow < matrix.GetLength(0); curRow++)
            {
                ret[curRow] = GetRow(matrix, curRow);
            }
            return ret;
        }

        /// <summary>
        /// Convert a jagged array to a 2d-array
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        static public T[,] Created2DFromJagged(T[][] matrix)
        {
            try
            {
                int FirstDim = matrix.Length;
                int SecondDim = matrix.GroupBy(row => row.Length).Single().Key; // throws InvalidOperationException if source is not rectangular

                var result = new T[FirstDim, SecondDim];
                for (int i = 0; i < FirstDim; ++i)
                    for (int j = 0; j < SecondDim; ++j)
                        result[i, j] = matrix[i][j];

                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Convert a list of 1D arrays to a jagged array
        /// </summary>
        /// <param name="listOfArrays"></param>
        /// <param name="indicesToIgnore">optionally, indices in each variable to ignore</param>
        /// <returns>null if unsuccessful</returns>
        static public T[][] CreateJaggedFromList(List<T[]> listOfArrays, List<int> indicesToIgnore= null )
        {
            try
            {
                T[][] ret = new T[listOfArrays.Count][];
                for (int i = 0; i < listOfArrays.Count; i++)
                {
                    if (indicesToIgnore == null)
                    {
                        ret[i] = listOfArrays.ElementAt(i);
                    }
                    else
                    {
                        ret[i] = Vec<T>.GetValuesExcludingIndices(listOfArrays.ElementAt(i),indicesToIgnore);
                    }
                }
                return ret;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Combine two arrays into a single array(increasing the number of rows)
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns>null if unsuccessful</returns>
        static public T[][] Combine(T[][] array1, T[][] array2)
        {
            try
            {
                T[][] ret = new T[array1.GetLength(0) + array2.GetLength(0)][];
                for (int i = 0; i < array1.GetLength(0); i++)
                {
                    ret[i] = array1.ElementAt(i);
                }
                int j0 = array1.GetLength(0);
                int k = 0;
                for (int j = j0; j < j0+ array2.GetLength(0); j++)
                {
                    ret[j] = array2.ElementAt(k);
                    k++;
                }
                return ret;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Combine two arrays into a single array(increasing the number of rows)
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="vector">a vector </param>
        /// <returns>null if unsuccessful</returns>
        static public T[][] Append(T[][] array1, T[] vector)
        {
            try
            {
                T[][] ret = new T[array1.GetLength(0) + 1][];
                for (int i = 0; i < array1.GetLength(0); i++)
                {
                    ret[i] = array1.ElementAt(i);
                }
                int j0 = array1.GetLength(0);
                ret[j0] = vector;
                return ret;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Initializes a 2D array from a list of arrays representing each column in the array
        /// </summary>
        /// <param name="columnList"></param>
        /// <returns>null if list columnList dimensions do not match</returns>
        static public T[,] CreateFromList(List<T[]> columnList)
        {
            int nColumns = columnList.Count;
            if (columnList.ElementAt(0) == null)
                return null;
            int nRows    = columnList.ElementAt(0).Length;
            for (int k = 1; k < columnList.Count; k++)
            {
                if (columnList.ElementAt(k).Length != nRows)
                {
                    return null;
                }
            }

            T[,] retArray = new T[nRows,nColumns];

            for (int curRow = 0; curRow < nRows; curRow++)
                for (int curCol = 0; curCol < nColumns; curCol++)
                { 
                    retArray[curRow,curCol] = columnList.ElementAt(curCol).ElementAt(curRow);
                }

            return retArray;
        }

        /// <summary>
        /// Create 2d-array with only a single column
        /// </summary>
        /// <param name="columnArray"></param>
        /// <returns></returns>
        static public T[,] Create(T[] columnArray)
        {
            return CreateFromList(new List<T[]> { columnArray });
        }

        /// <summary>
        /// Downsample a matrix where times are rows and variables are columns
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="downsampleFactor"></param>
        /// <returns></returns>
        static public T[,] Downsample(T[,] matrix, int downsampleFactor)
        {
            if (matrix == null)
                return null;
            int nCols = matrix.GetLength(1);

            T[][] ret = new T[nCols][];

            for (int colIdx =0; colIdx< nCols; colIdx++)
            {
                T[] curCol = Array2D<T>.GetColumn(matrix, colIdx);

                ret[colIdx] = Vec<T>.Downsample(curCol, downsampleFactor);
            }
            var jaggedRet = Created2DFromJagged(ret);
            return Array2D<T>.Transpose(jaggedRet);
        }

        ///<summary>
        /// returns the column of the matrix with the given index
        ///</summary>
        static public T[] GetColumn(T[,] matrix, int columnNumber)
        {
            if (matrix == null)
                return null;

            if (columnNumber < matrix.GetLength(1))
            {
                return Enumerable.Range(0, matrix.GetLength(0))
                        .Select(x => matrix[x, columnNumber])
                        .ToArray();
            }
            else
                return null;
        }

        ///<summary>
        /// returns the column of the matrix with the given index
        ///</summary>
        static public T[] GetColumn(T[][] matrix, int columnNumber)
        {
            if (matrix == null)
                return null;

            List<T> retList = new List<T>();

            if (columnNumber < matrix[0].GetLength(0))
            {
                for (int i = 0; i < matrix.GetLength(0);i++)
                {
                    retList.Add(matrix[i][columnNumber]);
                }
                return retList.ToArray() ;
            }
            else
                return null;
        }


        ///<summary>
        /// returns all the columns correspoding to columnNumbers in a 2d-array
        ///</summary>
        static public T[,] GetColumns(T[,] matrix, int[] columnNumbers)
        {
            if (matrix == null)
                return null;

            List<T[]> retList = new List<T[]>();

            for (int i = 0; i < columnNumbers.Length; i++)
            {
                int columnNumber = columnNumbers[i];

                if (columnNumber <= matrix.GetLength(0))
                {
                    retList.Add(Enumerable.Range(0, matrix.GetLength(0))
                          .Select(x => matrix[x, columnNumber])
                          .ToArray());
                }
                else
                    return null;
            }

            int nCols = retList.Count();
            int nRows = retList.First().GetLength(0);

            T[,] retArray = new T[nRows, nCols];
            int curCol = 0;
            foreach (T[] column in retList)
            {
                for (int i = 0; i < column.Length; i++)
                    retArray[i, curCol] = column[i];
                curCol++;
            }
            return retArray;
        }


        ///<summary>
        /// returns the row of the matrix with the given index as an vector
        ///</summary>

        static public T[] GetRow(T[,] matrix, int rowNumber)
        {
            if (matrix == null)
                return null;

        
            if (rowNumber < matrix.GetLength(0))
            {
                return Enumerable.Range(0, matrix.GetLength(1))
                    .Select(x => matrix[rowNumber, x])
                    .ToArray();
            }
            else
                return null;
        }


        ///<summary>
        /// transposes a 2d-array (rows are turned into columns and vice versa)
        ///</summary>
        static public T[,] Transpose(T[,] matrix)
        {
            T[,] ret = new T[matrix.GetLength(1), matrix.GetLength(0)];

            for (int curRow = 0; curRow < matrix.GetLength(0); curRow++)
            {
                for (int curCol = 0; curCol < matrix.GetLength(1); curCol++)
                {
                    ret[curCol, curRow] = matrix[curRow, curCol];
                }
            }
            return ret;
        }



    }
}
