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
    class ArrayUnitTests
    {

        [Test]
        public void InitFromColumnList()
        {
            List<double[]> inputList = new List<double[]> {new  double[] {  5, 6 },new  double[]{ 4, 3 }};
            var result=  Array2D<double>.CreateFromList(inputList);
            Assert.AreEqual(new double[,] { { 5, 4 },{6,3 } }, result) ;
        }

        

        [Test]
        public void ArrayGetColumn()
        {
            double[,] matrix = new double[,]{ { 1,2}, { 3,4}, {5,6 } };
            Assert.AreEqual(new double[] { 1 ,3, 5 }, matrix.GetColumn(0));
            Assert.AreEqual(new double[] { 2, 4, 6 }, matrix.GetColumn(1));
            Assert.AreEqual(null, matrix.GetColumn(2));
        }

        [Test]
        public void ArrayGetRow()
        {
            double[,] matrix = new double[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };
            Assert.AreEqual(new double[] { 1, 2 }, matrix.GetRow(0));
            Assert.AreEqual(new double[] { 3, 4 }, matrix.GetRow(1));
            Assert.AreEqual(new double[] { 5, 6 }, matrix.GetRow(2));
            Assert.AreEqual(null, matrix.GetRow(3));
        }

        [Test]
        public void ArrayGetNColumns()
        {
            double[,] matrix = new double[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };
            Assert.AreEqual(2, matrix.GetNColumns());
        }
        [Test]
        public void ArrayGetNRows()
        {
            double[,] matrix = new double[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };
            Assert.AreEqual(3, matrix.GetNRows());
        }
        [Test]
        public void ArrayGetRowsAfterIndex()
        {
            DateTime[] vec = new DateTime[] { new DateTime(2000,1,1),new DateTime(2000,1,2)};
            DateTime[] result = vec.GetRowsAfterIndex(1);
            Assert.AreEqual(new List<DateTime> { new DateTime(2000, 1, 2) }, result);
        }

        [Test]
        public void Combine()
        {
            double[][] matrix1 = new double[][] { new double[]{ 1, 2 ,3}, new double[] { 4, 5,6} };
            double[][] matrix2 = new double[][] { new double[] { 7, 8,9 }, new double[] { 10, 11,12 } };
            double[][] exp  = new double[][] { new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 },
                new double[] { 7,8,9}, new double[] { 10,11,12 }};

            var comb = Array2D<double>.Combine(matrix1,matrix2);
            Assert.AreEqual(exp, comb);
        }




        [Test]
        public void Downsample()
        {
            double[,] matrix = new double[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };
            double[,] expResult = new double[,] { { 1, 2 }, { 5, 6 } };

            double[,] result = Array2D<double>.Downsample(matrix, 2);

            Assert.AreEqual(expResult, result);

        }





    }
}
