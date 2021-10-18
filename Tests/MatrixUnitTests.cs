using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Test
{
    [TestFixture]
    class MatrixUnitTests
    {
        [Test]
        public void AppendRow()
        {

            double[,] matrix = new double[,] { { 1, 2, 3 }, { 3, 4, 5}, { 6,7,8 } };
            double[] vec = new double[] {9,10,11 };
            double[,] expResult = new double[,] { { 1, 2, 3 }, { 3, 4, 5 }, { 6, 7, 8 }, { 9, 10, 11 } };
            // (3x2) x (2x1) = 3x1
            double[,] result = Matrix.AppendRow(matrix, vec);
            Assert.AreEqual(expResult,result);

        }

        [Test]
        public void MatrixMult()
        {

            double[,] matrix = new double[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };
            // (3x2) x (2x1) = 3x1
            double[] result = Matrix.Mult(matrix, new double[] { 2, 3 });
            Assert.AreEqual(result,new double[] { 8, 18,28 } );

        }




        [Test]
        public void ReplaceRow()
        {
            double[,] matrix = new double[,]{ { 1,2}, { 3,4}, {5,6 } };
            matrix = Matrix.ReplaceRow(matrix, 2, new double[] { 10, 20 });
            Assert.AreEqual(new double[] { 10 ,20 }, matrix.GetRow(2));

            matrix = Matrix.ReplaceRow(matrix, 4, new double[] { 10, 20 });
        }

        [Test]
        public void ReplaceColumn()
        {
            double[,] matrix = new double[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };
            matrix = Matrix.ReplaceColumn(matrix, 1, new double[] { 10, 20,30 });
            Assert.AreEqual(new double[] { 10, 20,30 }, matrix.GetColumn(1));

       //     matrix = Matrix.ReplaceColumn(matrix, 4 , new double[] { 10, 20,30 });

        }


    }
}
