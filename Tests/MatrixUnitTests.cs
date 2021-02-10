using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.UnitTests
{
    [TestFixture]
    class MatrixUnitTests
    {
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

            matrix = Matrix.ReplaceColumn(matrix, 4 , new double[] { 10, 20,30 });

        }


    }
}
