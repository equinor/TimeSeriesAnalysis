using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;
using NUnit.Framework;

namespace TimeSeriesAnalysis.Test.Fundamentals
{
    [TestFixture]
    class IndexTest
    {



        [TestCase(new int[] { 0, 1 }, 3, new int[] { 2 })]
        [TestCase(new int[] { 0, 2 }, 3, new int[] { 1 })]
        [TestCase(new int[] { 1, 2 }, 3, new int[] { 0 })]
        [TestCase(new int[] { 0, 1, 2 }, 3, new int[] { })]
        [TestCase(new int[] { 1, 5, 8 }, 10, new int[] { 0, 2, 3, 4, 6, 7, 9 })]
        public void InverseIndices_isOK(int[] vec, int N, int[] vecExpectedResult)
        {
            List<int> vecResult = Index.InverseIndices(N, vec.ToList());
            Assert.AreEqual(vecExpectedResult, vecResult);
        }


        [Test]
        public void Union()
        {
            List<int> vecInd = new List<int> { 0, 1, 2, 3, 4, 6, 7, 8, 10 };
            List<int> vecInd2 = new List<int> { 5, 6, 8, 9 };
            List<int> vecResult = Index.Union(vecInd, vecInd2);
            List<int> vecExpt = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Assert.AreEqual(vecExpt, vecResult);
        }

    }
}
