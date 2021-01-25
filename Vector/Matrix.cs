using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TimeSeriesAnalysis
{
    static class Matrix
    {
        static public double[,] ReplaceRow(double[,] matrix, int rowIndex, double[] newRowVec)
        {
            if (newRowVec.Length != matrix.GetNColumns())
                return null;
            for (int colIdx = 0; colIdx < newRowVec.Length; colIdx++)
            {
                matrix[rowIndex, colIdx] = newRowVec[colIdx];
            }
            return matrix;
        }

        ///<summary>
        ///  Multipliy either entire matrix or single row(optional third input) by a scalara 
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
        ///  Multipliy either entire matrix or single row(optional third input) by a vector 
        ///</summary>

        static public double[,] Mult(double[,] matrix, double[] vector, int singleMatrixRowToMult=-1)
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
