using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.Fundamentals
{
    [TestFixture]
    class VecTests
    {

        [TestCase(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3, 4 })]
        [TestCase(new int[] { 1, 2, 3, 5 }, new int[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(new int[] { 3, 6, 9 }, new int[] { 3, 4, 6, 7, 9, 10 })]
        public void AppendTrailingIndices(int[] ind_in, int[] exp)
        {
            List<int> outArray = Index.AppendTrailingIndices(new List<int>(ind_in));
            Assert.AreEqual(exp, outArray);
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
        public void Cov_ZeroForConstantVectors()
        {
            double[] vec1 = Vec<double>.Fill(2, 10);
            double[] vec2 = Vec<double>.Fill(1, 10);
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

        [Test]
        public void Downsample()
        {
            double[] vec1 = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            double[] expResult = new double[] { 1, 5, 9, 13 };
            var result = Vec<double>.Downsample(vec1, 4);
            Assert.AreEqual(expResult, result);
        }


        [TestCase(4, new int[] { 0, 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14 }, new double[] { 3, 7, 11, 15 }, new int[]{}) ]
        [TestCase(4, new int[] { 0, 1, 2, 3 }, new double[] { 3, 7, 11, 15 }, new int[] {0 })]
        [TestCase(4, new int[] { 1,2,3 }, new double[] {0, 4,8, 12}, new int[] {  })]
        [TestCase(4, new int[] { 1, 3, 5, 7, 9, 11, 13, 15}, new double[] { 0, 4, 8, 12 }, new int[] { })]
        [TestCase(4, new int[] { 1, 2, 3, 5,6, 7, 9,10, 11, 13,14, 15 }, new double[] { 0, 4, 8, 12 }, new int[] { })]
        [TestCase(4, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, new double[] { 3, 7, 11, 15 }, new int[] {0,1,2,3 })]

        public void Downsample_IndToIgnore_corretIgnoreVals(int factor, int[] indToIgnoreArray,
            double[] expVec1, int[] expIndToIgnore)
        {
            double[] vec1 = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            var indToIgnore = new List<int>(indToIgnoreArray);
            var result = Vec<double>.Downsample(vec1, factor, indToIgnore);
            Assert.AreEqual(expVec1, result.Item1);
            Assert.AreEqual(expIndToIgnore, result.Item2.ToArray());
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


        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]

        public void Regress_givesCorrectValue(double bias)
        {
            double[] Y = (new Vec()).Add(new double[]{ 1, 0, 3, 4, 2 },bias);
            double[] X1 = { 1, 0, 1, 0,2 }; // gain:1
            double[] X2 = { 0, 0, 1, 2, 0 };// gain:2
            double[][] X = { X1, X2 };
            var results = (new Vec()).RegressUnRegularized(Y, X);
            Assert.IsNotNull(results);
            Assert.IsTrue(results.AbleToIdentify);
            Assert.Less(Math.Abs(1 - results.Param[0]), 0.005,"gain paramter should be correct");
            Assert.Less(Math.Abs(2 - results.Param[1]), 0.005, "gain paramter should be correct");
            Assert.Less(Math.Abs(results.Param[2] - bias), 0.01, "bias paramter should be correct");
            Assert.Less(Math.Abs(results.Bias- bias), 0.01, "bias should be close to true");
            Assert.Less(results.ObjectiveFunctionValue, 0.01,"obj function value should be close to zero");
            Assert.Greater(results.Rsq, 99, "Rsqured should be close to 100");
            Assert.IsTrue(results.VarCovarMatrix != null);
            Assert.IsTrue(results.Param95prcConfidence != null);
            Assert.IsTrue(results.Param95prcConfidence[0] < 0.2, "confidence interval too large?");
            Assert.IsTrue(results.Param95prcConfidence[1] < 0.2, "confidence interval too large?");
        

        }


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
            var results= (new Vec()).RegressUnRegularized(Y, X, indicesToignore.ToArray());
            Assert.IsTrue(results.AbleToIdentify);
            Assert.Less(Math.Abs(1 - results.Param[0]), 0.05);
            Assert.Less(Math.Abs(2 - results.Param[1]), 0.05);
            Assert.Less(Math.Abs(4 - results.Y_modelled[4]), 0.005);
            Assert.Greater(results.Rsq, 99);

            Assert.IsTrue(results.Param95prcConfidence[0] < 0.2, "confidence interval too large?");
            Assert.IsTrue(results.Param95prcConfidence[1] < 0.2, "confidence interval too large?");

        }

        [Test/*,Explicit(reason:"needs regularization turned on to work")*/]
        public void Regress_UnobservableInputsCauseLowGain()
        {
            double bias = 10000;
            double[] common = new double[] { 1, 0, 3, 4, -2, 1, 3, 5, 7, 8, 9, 10, 0, -5, 7, 8, -2 };
            double[] Y = (new Vec()).Add(common,bias);
            double[] X1 = common; // gain:1
            double[] X2 = Vec<double>.Fill(0, common.Length);// gain:0
            double[][] X = { X1, X2 };
            var results = (new Vec()).RegressUnRegularized(Y, X);
            Assert.IsTrue(results.AbleToIdentify);
            Assert.Less(Math.Abs(1 - results.Param[0]), 0.01);
            Assert.Less(Math.Abs(0 - results.Param[1]), 0.1);
            Assert.Greater(results.Rsq, 99);

            // uncertainty of second paratmer should be at least ten times larger than the first
      //      Assert.IsTrue(results.Param95prcConfidence[1]/results.Param95prcConfidence[0] > 10);
            Assert.IsTrue(results.Param95prcConfidence[0] < 0.2, "confidence interval too large?");
            Assert.IsTrue(results.Param95prcConfidence[1] < 0.2, "confidence interval too large?");

        }

        [Test]
        public void Regress_RegularizeJustSpecificInputs()
        {
            double bias = 10000;
            double[] common = new double[] { 1, 0, 3, 4, -2, 1, 3, 5, 7, 8, 9, 10, 0, -5, 7, 8, -2 };
            double[] Y = (new Vec()).Add(common, bias);
            double[] X1 = common; // gain:1
            double[] X2 = Vec<double>.Fill(0, common.Length);// gain:0
            double[][] X = { X1, X2 };
            var results = (new Vec()).RegressRegularized(Y, X,null, new List<int> {1});
            Assert.IsTrue(results.AbleToIdentify);
            Assert.Less(Math.Abs(1 - results.Param[0]), 0.01);
            Assert.Less(Math.Abs(0 - results.Param[1]), 0.1);
            Assert.Greater(results.Rsq, 99);

            Assert.IsTrue(results.Param95prcConfidence[0] < 0.2, "confidence interval too large?");
            Assert.IsTrue(results.Param95prcConfidence[1] < 0.2, "confidence interval too large?");
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
        public void FindValues_IgnoreIndices()
        {
            double[] vec = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var indToIgnore = new List<int> { 7, 10 };
            List<int> vecResult = (new Vec()).FindValues(vec, 6, VectorFindValueType.BiggerThan, indToIgnore);
            List<int> vecExpt = new List<int>() {8, 9 };
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
        public void FindValues_DifferentFromPrevious()
        {
            double[] vec = { 2, 2, 2, 5, 5, 5, 5, 5, 5, 5, 9 };
            List<int> vecResult = (new Vec()).FindValues(vec, -9999, VectorFindValueType.DifferentFromPrevious);
            List<int> vecExpt = new List<int> { 3, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void FindValues_Inf ()
        {
            double[] vec = { 0, 1, 2, 3, 4, double.PositiveInfinity, 6, 7, 8, double.NegativeInfinity, 10 };
            List<int> vecResult = (new Vec()).FindValues(vec, 0, VectorFindValueType.Inf);
            List<int> vecExpt = new List<int> {5,9 };
            Assert.AreEqual(vecExpt, vecResult);
        }


        [Test]
        public void FindValues_NotInf()
        {
            double[] vec = { 0, 1, 2, 3, 4, double.PositiveInfinity, 6, 7, 8, double.NegativeInfinity, 10 };
            List<int> vecResult = (new Vec()).FindValues(vec, 0, VectorFindValueType.NotInf);
            List<int> vecExpt = new List<int> { 0,1,2,3,4,6,7,8,10 };
            Assert.AreEqual(vecExpt, vecResult);
        }


        [Test]
        public void GetIndicesOfValues()
        {
            var vec2 = new List<int> { 10, 30, 50, 70 };
            var vec1 = new List<int> { 10, 20,30,40, 50,60,70 };
            var resutlExp = new List<int> { 0,2,4,6 };
            var result = Vec<int>.GetIndicesOfValues(vec1,vec2);

            Assert.AreEqual(resutlExp, result);
        }

        [Test]
        public void GetValuesExcludingIndices()
        {

            var vec1 = new double[] {0, 10, 20, 30, 40, 50, 60, 70 };
            var indToIgnore = new List<int> { 0, 3, 7 };
            var resutlExp = new List<int> { 10,20,40,50,60 };
            var result = Vec<double>.GetValuesExcludingIndices(vec1, indToIgnore);

            Assert.AreEqual(resutlExp, result);
        }


        /*   [Test]
           public void GetGradient()
           {
               var vec1 = new List<double> { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
               var dates = TimeSeriesCreator.CreateDateStampArray(new DateTime(2000, 1, 1), 3600, 10);
               var results = Vec.GetGradient(vec1.ToArray(),dates,3600);
               Assert.IsTrue(Math.Abs(results.Gains.First() -10 )<0.01);
           }*/

        [Test]
        public void Intersect()
        {
            List<int> vecInd = new List<int> { 0, 1, 2, 3, 4, 6, 7, 8, 10 };
            List<int> vecInd2 = new List<int> { 5, 6, 8, 9 };
            List<int> vecResult = Vec<int>.Intersect(vecInd, vecInd2);
            List<int> vecExpt = new List<int> { 6, 8 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void IntersectMultiple()
        {
            List<int> vecInd = new List<int> { 0, 1, 2, 3, 4, 6, 7, 8, 10 };
            List<int> vecInd2 = new List<int> { 5, 6, 8, 9 };
            List<int> vecInd3 = new List<int> { 2, 3, 6, 8 };
            List<int> vecInd4 = new List<int> { 6, 8, 8, 10 };

            List<int> vecResult = Vec<int>.Intersect(new List<List<int>> { vecInd, vecInd2, vecInd3, vecInd4 });
            List<int> vecExpt = new List<int> { 6, 8 };
            Assert.AreEqual(vecExpt, vecResult);
        }


        [Test]
        public void ReplaceIndWithValuesPrior()
        {
            double[] vec = { 0, 1, 2, 3, 4, -9999, -9999, -9999, 8, 9, 10 };
            List<int> vecInd = new List<int> { 5, 6, 7 };
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
            double[] vec = { 0, 1, 2, 3, 4 };
            List<int> vecInd = new List<int> { 1, 3 };
            double[] vecResult = Vec.ReplaceIndWithValue(vec, vecInd, Double.NaN);
            double[] vecExpt = { 0, Double.NaN, 2, Double.NaN, 4 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void ReplaceValuesAbove()
        {
            double[] vec = { 0, 6, 2, 3, 4 };
            double[] vecResult = Vec.ReplaceValuesAbove(vec, 3, Double.NaN);
            double[] vecExpt = { 0, Double.NaN, 2, 3, Double.NaN };
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
        public void ReplaceValuesAboveOrBelow()
        {
            double[] vec = { 0, 6, 2, 3, 4 };
            double[] vecResult = Vec.ReplaceValuesAboveOrBelow(vec, 3,5, Double.NaN);
            double[] vecExpt = { Double.NaN, Double.NaN, Double.NaN, 3, 4 };
            Assert.AreEqual(vecExpt, vecResult);
        }

        [Test]
        public void SerializeAndDeserialize_works()
        {
            string fileName = @"C:\Appl\source\TimeSeriesAnalysis\unittest.txt";
            double[] vec1 = { 0.0001, 1.00002, -0.02, -1.002, 200000, Double.NaN };
            Vec.Serialize(vec1, fileName);
            double[] vec2 = Vec.Deserialize(fileName);

            Assert.AreEqual(vec1, vec2);
        }

        [TestCase(null, ExpectedResult = 4)]
        [TestCase(new int[] { 1 }, ExpectedResult = 4)]
        //[TestCase(null, -1, ExpectedResult = 2)]
        //[TestCase(new int[] { 1 },-1, ExpectedResult = 2)]

        public double SumOfSquareErrors(int[] indToIgnoreArray)
        {
            double[] vec1 = { 0, Double.NaN, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            double[] vec2 = (new Vec()).Add(vec1, 2); ;

            List<int> indToIgnoreList = null;
            if (indToIgnoreArray != null)
            {
                indToIgnoreList = indToIgnoreArray.ToList();
            }
            double sumAbsErr = (new Vec()).SumOfSquareErr(vec1, vec2, 0, true, indToIgnoreList);
            return sumAbsErr;
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

        [Test]
        public void VecMin_IgnoresIndices()
        {
            var ind = new List<int> { 0, 3 }; 
            double[] vec1 = { 1, 2, 3, 4 };
            var  vec = new Vec();
            double vecres = vec.Min(vec1, ind);

            Assert.AreEqual(2, vecres);
        }

        [Test]
        public void VecMax_IgnoresIndices()
        {
            var ind = new List<int> { 0, 3 };
            double[] vec1 = { 1, 2, 3, 4 };
            var vec = new Vec();
            double vecres = vec.Max(vec1, ind);

            Assert.AreEqual(3, vecres);
        }



    }
}
