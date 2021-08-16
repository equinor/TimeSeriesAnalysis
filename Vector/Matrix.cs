using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TimeSeriesAnalysis
{
    ///<summary>
    /// Utility functions and operations for treating 2D-arrays as mathetmatical matrices
    ///</summary>

    public static class Matrix
    {
        ///<summary>
        ///  Appends another row onto an existing matrix. Returns null if this was not possible(ie. dimnesions dont agree). 
        ///</summary>
        static public double[,] AppendRow(double[,] matrix , double[] newRowVec)
        {
            if (matrix == null)
                return null;
            if (newRowVec == null)
                return null;

            if (matrix.GetNColumns() == newRowVec.Length)
            {
                double[,] newMatrix = new double[matrix.GetNRows() + 1, matrix.GetNColumns()];
                for (int curRow = 0; curRow < matrix.GetNRows(); curRow++)
                {
                    newMatrix = Matrix.ReplaceRow(newMatrix,curRow, matrix.GetRow(curRow));
                }
                newMatrix = Matrix.ReplaceRow(newMatrix, matrix.GetNRows(), newRowVec);
                return newMatrix;
            }
            else
                return null;
        }




            ///<summary>
            ///  Replace a single row of a matrix 
            ///</summary>

            static public double[,] ReplaceRow(double[,] matrix, int rowIndex, double[] newRowVec)
        {
            if (newRowVec.Length != matrix.GetNColumns())
                return null;
            if (rowIndex > matrix.GetNRows() - 1)
                return null;
            for (int colIdx = 0; colIdx < newRowVec.Length; colIdx++)
            {
                matrix[rowIndex, colIdx] = newRowVec[colIdx];
            }
            return matrix;
        }

        ///<summary>
        ///  Replace a single column of a matrix 
        ///</summary>

        static public double[,] ReplaceColumn(double[,] matrix, int colIndex, double[] newColVec)
        {
            if (newColVec.Length != matrix.GetNRows())
                return null;
            if (colIndex > matrix.GetNColumns() - 1)
                return null;
            for (int rowIdx = 0; rowIdx < newColVec.Length; rowIdx++)
            {
                matrix[rowIdx, colIndex] = newColVec[rowIdx];
            }
            return matrix;
        }


        ///<summary>
        ///  Multipliy either entire matrix or single row(optional third input) by a scalar
        ///</summary>

        static public double[,] Mult(double[,] matrix, double scalar, int singleMatrixRowToMult = -1)
        {
            if (singleMatrixRowToMult >= 0)
            {
                for (int curMatrixCol = 0; curMatrixCol < matrix.GetNColumns(); curMatrixCol++)
                {
                    matrix[singleMatrixRowToMult, curMatrixCol] = matrix[singleMatrixRowToMult, curMatrixCol] * scalar;
                }
                return matrix;
            }
            for (int curMatrixRow = 0; curMatrixRow < matrix.GetNRows(); curMatrixRow++)
            {
                for (int curMatrixCol = 0; curMatrixCol < matrix.GetNColumns(); curMatrixCol++)
                {
                    matrix[curMatrixRow, curMatrixCol] = matrix[curMatrixRow, curMatrixCol] * scalar;
                } 
            }
            return matrix;
        }

        ///<summary>
        ///  Multipliy either entire matrix or single row(optional third input) by a vector (returns vector)
        ///</summary>

        static public double[] Mult(double[,] matrix, double[] vector, int singleMatrixRowToMult=-1)
        {
            if (matrix.GetNColumns() != vector.Count())
            {
                return null;//incompatible matrix and vector lengths
            }
            /*
            if (singleMatrixRowToMult >= 0)
            {
                if (singleMatrixRowToMult > matrix.GetNRows() - 1)
                {
                    return null;// matrix row index out of range
                }
                for (int curMatrixCol = 0; curMatrixCol < vector.Count(); curMatrixCol++)
                {
                    matrix[singleMatrixRowToMult, curMatrixCol] = matrix[singleMatrixRowToMult, curMatrixCol] * vector[curMatrixCol];
                }
                return matrix;
            }*/

            double[] returnVec = new double[matrix.GetNRows()];


            for (int curMatrixRow = 0; curMatrixRow < matrix.GetNRows(); curMatrixRow++)
            {
                returnVec[curMatrixRow] = 0;
                for (int curMatrixCol = 0; curMatrixCol < vector.Count(); curMatrixCol++)
                {
                    returnVec[curMatrixRow] += matrix[curMatrixRow, curMatrixCol] * vector[curMatrixCol];
                }
            }
            return returnVec;
        }


        ///<summary>
        ///  Multipliy either entire matrix or single row(optional third input) by a vector (returns a matrix)
        ///</summary>

        static public double[,] ComponentMult(double[,] matrix, double[] vector, int singleMatrixRowToMult = -1)
        {
            if (matrix.GetNColumns() != vector.Count())
            {
                return null;//incompatible matrix and vector lengths
            }

            if (singleMatrixRowToMult >= 0)
            {
                if (singleMatrixRowToMult > matrix.GetNRows() - 1)
                {
                    return null;// matrix row index out of range
                }
                for (int curMatrixCol = 0; curMatrixCol < vector.Count(); curMatrixCol++)
                {
                    matrix[singleMatrixRowToMult, curMatrixCol] = matrix[singleMatrixRowToMult, curMatrixCol] * vector[curMatrixCol];
                }
                return matrix;
            }

            for (int curMatrixRow = 0; curMatrixRow < matrix.GetNRows(); curMatrixRow++)
            {
                for (int curMatrixCol = 0; curMatrixCol < vector.Count(); curMatrixCol++)
                {
                    matrix[curMatrixRow, curMatrixCol] = matrix[curMatrixRow, curMatrixCol] * vector[curMatrixCol];
                }
            }
            return matrix;
        }



    }
}
