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
    class VecUnitTests
    {
        [TestCase(new int[] { 0, 1 }, 3, new int[] { 2 })]
        [TestCase(new int[] { 0, 2 }, 3, new int[] { 1 })]
        [TestCase(new int[] { 1, 2 }, 3, new int[] { 0 })]
        [TestCase(new int[] { 0, 1, 2 }, 3, new int[] { })]
        [TestCase(new int[] { 1, 5, 8 }, 10, new int[] { 0, 2, 3, 4, 6, 7, 9 })]
        public void InverseIndices_isOK(int[] vec, int N, int[] vecExpectedResult)
        {
            List<int> vecResult = Vec.InverseIndices(N, vec.ToList());
            Assert.AreEqual(vecExpectedResult, vecResult);
        }

        [Test]
        public void SortAscending_isOk()
        {
            double[] vec = new double[] { 1.1, 2.1, 0.1, 3.1, 5.1, 4.1 };
            double[] vecExp = new double[] { 0.1, 1.1, 2.1, 3.1, 4.1, 5.1 };
            double[] idxExp = new double[] { 2, 0, 1, 3, 5, 4 };

            double[] refArr = new double[vec.Length];
            Array.Copy(vec, refArr, vec.Length);
            double[] sorted = Vec.Sort(vec, SortType.Ascending, out int[] idx);
            Assert.AreEqual(sorted, vecExp);
            Assert.AreEqual(idx, idxExp);
        }

        [Test]
        public void SortDescending_isOk()
        {
            double[] vec = new double[] { 1, 2, 0, 3, 5, 4 };
            double[] vecExp = new double[] { 5, 4, 3, 2, 1, 0 };
            double[] idxExp = new double[] { 4, 5, 3, 1, 0, 2 };
            double[] refArr = new double[vec.Length];
            Array.Copy(vec, refArr, vec.Length);
            double[] sorted = Vec.Sort(vec, SortType.Descending, out int[] idx);
            Assert.AreEqual(sorted, vecExp);
            Assert.AreEqual(idx, idxExp);
        }

        [Test]
        public void Mean_isOk()
        {
            double[] vec = Vec.Fill(10, 1);
            double mean = Vec.Mean(vec).Value;
            Assert.AreEqual(10, mean);
        }

        [Test]
        public void Var_ZeroForConstantVector()
        {
            double[] vec = Vec.Fill(10, 1);
            double var = Vec.VarAbs(vec);
            Assert.AreEqual(0, var);
        }

        [Test]
        public void SubArray_GivesCorrectSubArray()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            double[] vecResult = Vec.SubArray(vec, 1, 2);
            double[] vecExpt = { 1, 2 };
            Assert.AreEqual(vecExpt, vecResult);
        }
        [Test]
        public void SubArray_GivesCorrectSubArray2()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            double[] vecResult = Vec.SubArray(vec, 9);
            double[] vecExpt = { 9, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void SubArray_GivesCorrectSubArray3()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            double[] vecResult = Vec.SubArray(vec, -1, 2);
            double[] vecExpt = { 0, 1, 2 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void Cov_ZeroForConstantVectors()
        {
            double[] vec1 = Vec.Fill(10, 1);
            double[] vec2 = Vec.Fill(10, 2);
            double cov = Vec.CovAbs(vec1, vec2);
            Assert.AreEqual(0, cov);
        }

        [Test]
        public void Cov_OrderIrrelevant()
        {
            double[] vec1 = Vec.Rand(10);
            double[] vec2 = Vec.Rand(10);
            double cov1 = Vec.CovAbs(vec1, vec2);
            double cov2 = Vec.CovAbs(vec2, vec1);
            Assert.AreEqual(cov1, cov2);
        }

        [Test]
        public void Regress_givesCorrectValue()
        {
            double[] Y = { 1, 0, 3, 4, 2 };
            double[] X1 = { 1, 0, 1, 0,2 }; // gain:1
            double[] X2 = { 0, 0, 1, 2,0 };// gain:2
            double[][] X = { X1, X2 };
            double[] b = Vec.Regress(Y, X);
            Assert.IsNotNull(b);
            Assert.Less(Math.Abs(1 - b[0]), 0.001);
            Assert.Less(Math.Abs(2 - b[1]), 0.001);
        }

        [Test]
        public void Regress_SingularMatrixCaught()
        {
            double[] Y = { 1, 0, 1, 0 };
            double[] X1 = { 1, 0, 1, 0 };
            double[] X2 = { 1, 0, 1, 0 };
            double[][] X = { X1, X2 };
            double[] b = Vec.Regress(Y, X);
        }

        [Test]
        public void Regress_RankDeficient()
        {
            double[] Y = { 1, 0, 1, 0 };
            double[] X1 = { 1, 0, 1, 0 };
            double[] X2 = { 0, 0, 0, 0 };
            double[][] X = { X1, X2 };
            double[] b = Vec.Regress(Y, X);
        }


        [Test]
        public void Regress_IgnoreIndices()
        {
            double[] Y = { 1, 0, 3, 4, -9999, 1 };
            double[] X1 = { 1, 0, 1, 0, -1, 1, }; // gain:1
            double[] X2 = { 0, 0, 1, 2, -1, 0 };// gain:2
            double[][] X = { X1, X2 };
            List<int> indicesToignore = new List<int>();
            indicesToignore.Add(4);
            double[] b = Vec.Regress(Y, X, indicesToignore.ToArray(), out _, out double[] yMod, out double Rsq);
            Assert.Less(Math.Abs(1 - b[0]), 0.001);
            Assert.Less(Math.Abs(2 - b[1]), 0.001);
            Assert.Less(Math.Abs(4 - yMod[4]), 0.0001);

            //   Assert.Greater(Rsq, 99);
        }

        [Test]
        public void ContainsM9999()
        {
            double[] Y1 = { 1, 0, -9999, 0 };
            bool contM9999 = Vec.ContainsM9999(Y1);
            Assert.IsTrue(contM9999);

            double[] Y2 = { 1, 0, 0, -9999 };
            contM9999 = Vec.ContainsM9999(Y2);
            Assert.IsTrue(contM9999);

            double[] Y3 = { 1, 0, 0, 9999 };
            contM9999 = Vec.ContainsM9999(Y3);
            Assert.IsFalse(contM9999);
        }


        [Test]
        public void FindValues_BiggerThan()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            List<int> vecResult = Vec.FindValues(vec, 6, FindValues.BiggerThan);
            List<int> vecExpt = new List<int>() { 7, 8, 9, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void FindValues_SmallerThan()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            List<int> vecResult = Vec.FindValues(vec, 6, FindValues.SmallerThan);
            List<int> vecExpt = new List<int> { 0, 1, 2, 3, 4, 5 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [TestCase(new int[] { 1, 2, 3 }, new int[] { 1,2,3,4})]
        [TestCase(new int[] { 1, 2, 3, 5}, new int[] { 1, 2, 3, 4, 5 ,6 })]
        [TestCase(new int[] { 3, 6, 9 }, new int[] { 3, 4, 6, 7, 9, 10 })]



        public void AppendTrailingIndices(int[] ind_in, int[] exp )
        {
            List<int> outArray = Vec.AppendTrailingIndices(new List<int>(ind_in));
            Assert.AreEqual(exp, outArray);
        }


        [Test]
        public void FindValues_Equal()
        {
            double[] vec = { 0, 1, 2, 3, 4, -9999, 6, 7, 8, -9999, 10 };
            List<int> vecResult = Vec.FindValues(vec, -9999, FindValues.Equal);
            List<int> vecExpt = new List<int> {5,9 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void FindValues_NotNan()
        {
            double[] vec = { 0, 1, 2, 3, 4, -9999, 6, 7, 8, -9999, 10 };
            List<int> vecResult = Vec.FindValues(vec, -9999, FindValues.NotNaN);
            List<int> vecExpt = new List<int> { 0, 1,2,3,4,6,7,8,10 };
            Assert.AreEqual(vecExpt, vecResult);
        }


        [Test]
        public void ReplaceIndWithValuesPrior()
        {
            double[] vec = { 0, 1, 2, 3, 4, -9999, -9999, -9999, 8, 9, 10 };
            List<int> vecInd = new List<int> { 5,6,7 };
            double[] vecResult = Vec.ReplaceIndWithValuesPrior(vec, vecInd);
            double[] vecExpt = { 0, 1, 2, 3, 4, 4, 4, 4, 8, 9, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void ReplaceIndWithValuesPrior2()
        {
            double[] vec = { 0, 1, 2, 3, 4, -9999, -9999, 7, -9999, -9999, 10 };
            List<int> vecInd = new List<int> { 5, 6, 8, 9 };
            double[] vecResult = Vec.ReplaceIndWithValuesPrior(vec, vecInd);
            double[] vecExpt = { 0, 1, 2, 3, 4, 4, 4, 7, 7, 7, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }
        public void ReplaceIndWithValue()
        {
            double[] vec = { 0, 1,2,3,4 };
            List<int> vecInd = new List<int> { 1,3 };
            double[] vecResult = Vec.ReplaceIndWithValue(vec, vecInd, Double.NaN);
            double[] vecExpt = { 0, Double.NaN,2,Double.NaN, 4};
            Assert.AreEqual(vecExpt, vecResult);
        }


        [Test]
        public void Intersect()
        {
            List<int> vecInd = new List<int> { 0, 1,2, 3, 4, 6, 7,8,10};
            List<int> vecInd2 = new List<int> { 5, 6, 8, 9 };
            List<int> vecResult = Vec.Intersect(vecInd, vecInd2);
            List<int> vecExpt = new List<int> { 6,8};
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void Union()
        {
            List<int> vecInd = new List<int> { 0, 1, 2, 3, 4, 6, 7, 8, 10 };
            List<int> vecInd2 = new List<int> { 5, 6, 8, 9 };
            List<int> vecResult = Vec.Union(vecInd, vecInd2);
            List<int> vecExpt = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void SumOfAbsErrors()
        {
            double[] vec1 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            double[] vec2 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            vec1 = Vec.Add(vec1, 1);

            double sumAbsErr = Vec.SumOfAbsErr(vec1,vec2);
            Assert.AreEqual(1, sumAbsErr);
        }

        [Test]
        public void SumOfAbsErrors_IgnoresNaN()
        {
            double[] vec1 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            double[] vec2 = { 0, 1, 2, 3, Double.NaN, 5, 6, 7, 8, 9, 10 };
            vec1 = Vec.Add(vec1, 1);

            double sumAbsErr = Vec.SumOfAbsErr(vec1, vec2);
            Assert.AreEqual(1, sumAbsErr);
        }

        [Test]
        public void VecMax()
        {
            double[] vec1 = { 0, 1, 2, 3,};
            double[] vec2 = { 2, 2, 2, 2 };
            double[] vecres = Vec.Max(vec1,vec2);

            Assert.AreEqual( new double[]{2,2,2,3 }, vecres);
        }

        [Test]
        public void VecMin()
        {
            double[] vec1 = { 0, 1, 2, 3, };
            double[] vec2 = { 2, 2, 2, 2 };
            double[] vecres = Vec.Min(vec1, vec2);

            Assert.AreEqual(new double[] { 0, 1, 2, 2 }, vecres);
        }



        [Test]
        public void SumOfSquareErrors()
        {
            double[] vec1 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            double[] vec2 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            vec1 = Vec.Add(vec1, 2);

            double sumAbsErr = Vec.SumOfSquareErr(vec1, vec2);
            Assert.AreEqual(4, sumAbsErr);
        }

        [Test]
        public void SumOfSquareErrors_IgnoresNaN()
        {
            double[] vec1 = { 0, 1, 2, 3, 4, Double.NaN, 6, 7, 8, 9, 10 };
            double[] vec2 = { 0, 1, 2, Double.NaN, 4, 5, 6, 7, 8, 9, 10 };
            vec1 = Vec.Add(vec1, 2);

            double sumAbsErr = Vec.SumOfSquareErr(vec1, vec2);
            Assert.AreEqual(4, sumAbsErr);
        }

        [Test]
        public void SerializeAndDeserialize_works()
        {
            string fileName = @"C:\Appl\source\TimeSeriesAnalysis\unittest.txt";
            double[] vec1 = { 0.0001,1.00002,-0.02,-1.002, 200000, Double.NaN };
            Vec.Serialize(vec1, fileName);
            double[] vec2 = Vec.Deserialize(fileName);

            Assert.AreEqual(vec1,vec2);


        }



    }
}
