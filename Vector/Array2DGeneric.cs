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
        /// converts a 2d array into a jagged array
        ///</summary>

        static public T[][] Convert2DtoJagged(T[,] matrix)
        {
            T[][] ret = new T[matrix.GetLength(0)][];

            for (int curRow = 0; curRow < matrix.GetLength(0); curRow++)
            {
                ret[curRow] = GetRow(matrix, curRow);
            }
            return ret;
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


    }
}
