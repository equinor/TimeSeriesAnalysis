using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeSeriesAnalysis.Test
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
            double[] sorted = Vec<double>.Sort(vec, VectorSortType.Ascending, out int[] idx);
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
            double[] sorted = Vec<double>.Sort(vec, VectorSortType.Descending, out int[] idx);
            Assert.AreEqual(sorted, vecExp);
            Assert.AreEqual(idx, idxExp);
        }

        [Test]
        public void Mean_isOk()
        {
            double[] vec = Vec<double>.Fill(10, 1);
            double mean = (new Vec()).Mean(vec).Value;
            Assert.AreEqual(10, mean);
        }

        [Test]
        public void Var_ZeroForConstantVector()
        {
            double[] vec = Vec<double>.Fill(10, 1);
            double var = (new Vec()).Var(vec);
            Assert.AreEqual(0, var);
        }

        [Test]
        public void SubArray_GivesCorrectSubArray()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            double[] vecResult = Vec<double>.SubArray(vec, 1, 2);
            double[] vecExpt = { 1, 2 };
            Assert.AreEqual(vecExpt, vecResult);
        }
        [Test]
        public void SubArray_GivesCorrectSubArray2()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            double[] vecResult = Vec<double>.SubArray(vec, 9);
            double[] vecExpt = { 9, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void SubArray_GivesCorrectSubArray3()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            double[] vecResult = Vec<double>.SubArray(vec, -1, 2);
            double[] vecExpt = { 0, 1, 2 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void Cov_ZeroForConstantVectors()
        {
            double[] vec1 = Vec<double>.Fill(2,10);
            double[] vec2 = Vec<double>.Fill(1,10);
            double cov = (new Vec()).Cov(vec1, vec2);
            Assert.AreEqual(0, cov);
        }

        [Test]
        public void Cov_OrderIrrelevant()
        {
            double[] vec1 = Vec.Rand(10);
            double[] vec2 = Vec.Rand(10);
            double cov1 = (new Vec()).Cov(vec1, vec2);
            double cov2 = (new Vec()).Cov(vec2, vec1);
            Assert.AreEqual(cov1, cov2);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]

        public void Regress_givesCorrectValue(double bias)
        {
            double[] Y = (new Vec()).Add(new double[]{ 1, 0, 3, 4, 2 },bias);
            double[] X1 = { 1, 0, 1, 0,2 }; // gain:1
            double[] X2 = { 0, 0, 1, 2, 0 };// gain:2
            double[][] X = { X1, X2 };
            var results = (new Vec()).Regress(Y, X);
            Assert.IsNotNull(results);
            Assert.IsTrue(results.ableToIdentify);
            Assert.Less(Math.Abs(1 - results.param[0]), 0.001,"gain paramter should be correct");
            Assert.Less(Math.Abs(2 - results.param[1]), 0.001, "gain paramter should be correct");
            Assert.Less(Math.Abs(results.param[2] - bias), 0.001, "bias paramter should be correct");
            Assert.Less(Math.Abs(results.Bias- bias), 0.001, "bias should be close to true");
            Assert.Less(results.objectiveFunctionValue, 0.001,"obj function value should be close to zero");
            Assert.Greater(results.Rsq, 99, "Rsqured should be close to 100");
        }

        /*
        [Test]
        public void Regress_SingularMatrixCaught()
        {
            double[] Y = { 1, 0, 1, 0 };
            double[] X1 = { 1, 0, 1, 0 };
            double[] X2 = { 1, 0, 1, 0 };
            double[][] X = { X1, X2 };
            var results = Vec.Regress(Y, X);
        }
        */


        [Test]
        public void Regress_IgnoreIndices()
        {
            double[] Y = { 1, 0, 3, 4, -9999, 1 };
            double[] X1 = { 1, 0, 1, 0, -9999, 1, }; // gain:1
            double[] X2 = { 0, 0, 1, 2, -9999, 0 };// gain:2
            double[][] X = { X1, X2 };
            List<int> indicesToignore = new List<int>
            {
                4
            };
            var results= (new Vec()).Regress(Y, X, indicesToignore.ToArray());
            Assert.Less(Math.Abs(1 - results.param[0]), 0.001);
            Assert.Less(Math.Abs(2 - results.param[1]), 0.001);
            Assert.Less(Math.Abs(4 - results.Y_modelled[4]), 0.0001);

             Assert.Greater(results.Rsq, 99);
        }

        [Test]
        public void ContainsM9999()
        {
            double[] Y1 = { 1, 0, -9999, 0 };
            bool contM9999 = (new Vec()).ContainsBadData(Y1);
            Assert.IsTrue(contM9999);

            double[] Y2 = { 1, 0, 0, -9999 };
            contM9999 = (new Vec()).ContainsBadData(Y2);
            Assert.IsTrue(contM9999);

            double[] Y3 = { 1, 0, 0, 9999 };
            contM9999 = (new Vec()).ContainsBadData(Y3);
            Assert.IsFalse(contM9999);
        }


        [Test]
        public void FindValues_BiggerThan()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            List<int> vecResult = (new Vec()).FindValues(vec, 6, VectorFindValueType.BiggerThan);
            List<int> vecExpt = new List<int>() { 7, 8, 9, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void FindValues_SmallerThan()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            List<int> vecResult = (new Vec()).FindValues(vec, 6, VectorFindValueType.SmallerThan);
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
            List<int> vecResult = (new Vec()).FindValues(vec, -9999, VectorFindValueType.Equal);
            List<int> vecExpt = new List<int> {5,9 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void FindValues_NotNan()
        {
            double[] vec = { 0, 1, 2, 3, 4, -9999, 6, 7, 8, -9999, 10 };
            List<int> vecResult = (new Vec()).FindValues(vec, -9999, VectorFindValueType.NotNaN);
            List<int> vecExpt = new List<int> { 0, 1,2,3,4,6,7,8,10 };
            Assert.AreEqual(vecExpt, vecResult);
        }


        [Test]
        public void ReplaceIndWithValuesPrior()
        {
            double[] vec = { 0, 1, 2, 3, 4, -9999, -9999, -9999, 8, 9, 10 };
            List<int> vecInd = new List<int> { 5,6,7 };
            double[] vecResult = Vec<double>.ReplaceIndWithValuesPrior(vec, vecInd);
            double[] vecExpt = { 0, 1, 2, 3, 4, 4, 4, 4, 8, 9, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void ReplaceIndWithValuesPrior2()
        {
            double[] vec = { 0, 1, 2, 3, 4, -9999, -9999, 7, -9999, -9999, 10 };
            List<int> vecInd = new List<int> { 5, 6, 8, 9 };
            double[] vecResult = Vec<double>.ReplaceIndWithValuesPrior(vec, vecInd);
            double[] vecExpt = { 0, 1, 2, 3, 4, 4, 4, 7, 7, 7, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }
        [Test]
        public void ReplaceIndWithValue()
        {
            double[] vec = { 0, 1,2,3,4 };
            List<int> vecInd = new List<int> { 1,3 };
            double[] vecResult = Vec.ReplaceIndWithValue(vec, vecInd, Double.NaN);
            double[] vecExpt = { 0, Double.NaN,2,Double.NaN, 4};
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void ReplaceValuesAbove()
        {
            double[] vec = { 0, 6, 2, 3, 4 };
            double[] vecResult = Vec.ReplaceValuesAbove(vec, 3,Double.NaN);
            double[] vecExpt = { 0, Double.NaN, 2, 3,Double.NaN};
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void ReplaceValuesBelow()
        {
            double[] vec = { 0, 6, 2, 3, 4 };
            double[] vecResult = Vec.ReplaceValuesBelow(vec, 3, Double.NaN);
            double[] vecExpt = { Double.NaN, 6, Double.NaN, 3, 4 };
            Assert.AreEqual(vecExpt, vecResult);
        }


        [Test]
        public void Intersect()
        {
            List<int> vecInd = new List<int> { 0, 1,2, 3, 4, 6, 7,8,10};
            List<int> vecInd2 = new List<int> { 5, 6, 8, 9 };
            List<int> vecResult = Vec<int>.Intersect(vecInd, vecInd2);
            List<int> vecExpt = new List<int> { 6,8};
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void IntersectMultiple ()
        {
            List<int> vecInd = new List<int> { 0, 1, 2, 3, 4, 6, 7, 8, 10 };
            List<int> vecInd2 = new List<int> { 5, 6, 8, 9 };
            List<int> vecInd3 = new List<int> { 2, 3, 6, 8 };
            List<int> vecInd4 = new List<int> { 6, 8, 8,10 };

            List<int> vecResult = Vec<int>.Intersect(new List<List<int>> { vecInd,vecInd2,vecInd3,vecInd4});
            List<int> vecExpt = new List<int> { 6, 8 };
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
            vec1 = (new Vec()).Add(vec1, 1);

            double sumAbsErr = (new Vec()).SumOfAbsErr(vec1,vec2);
            Assert.AreEqual(1, sumAbsErr);
        }

        [Test]
        public void SumOfAbsErrors_IgnoresNaN()
        {
            double[] vec1 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            double[] vec2 = { 0, 1, 2, 3, Double.NaN, 5, 6, 7, 8, 9, 10 };
            vec1 = (new Vec()).Add(vec1, 1);

            double sumAbsErr = (new Vec()).SumOfAbsErr(vec1, vec2);
            Assert.AreEqual(1, sumAbsErr);
        }

        [Test]
        public void VecMax()
        {
            double[] vec1 = { 0, 1, 2, 3,};
            double[] vec2 = { 2, 2, 2, 2 };
            double[] vecres = (new Vec()).Max(vec1,vec2);

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


        [TestCase(null,ExpectedResult = 4)]
        [TestCase(new int[] { 1 },ExpectedResult = 4 )]
        //[TestCase(null, -1, ExpectedResult = 2)]
        //[TestCase(new int[] { 1 },-1, ExpectedResult = 2)]

        public double SumOfSquareErrors(int[] indToIgnoreArray)
        {
            double[] vec1 = { 0, Double.NaN, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            double[] vec2 = (new Vec()).Add(vec1, 2); ;

            List<int> indToIgnoreList=null;
            if (indToIgnoreArray != null)
            {
                indToIgnoreList = indToIgnoreArray.ToList();
            }
            double sumAbsErr = (new Vec()).SumOfSquareErr(vec1, vec2, 0, true, indToIgnoreList);
            return sumAbsErr;
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
