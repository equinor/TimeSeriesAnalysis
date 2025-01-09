using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.Fundamentals
{
    [TestFixture]
    internal class SignificantDigitsTests
    {

        [TestCase(2.43333,3,2.43)]
        [TestCase(2.43000000000001, 3, 2.43)]
        [TestCase(2.430000000000001, 2, 2.4)]
        [TestCase(24.30000000000001, 3, 24.3)]
        [TestCase(243.123400000000001, 4, 243.1)]
        [TestCase(-243.123400000000001, 4, -243.1)]
        [TestCase(2.0199999999999996, 3, 2.02)]
        [TestCase(-2.0199999999999996, 3, -2.02)]
        public void Format_CorrectDigits(double number,int digits,double expAnswer )
        {
            double result = SignificantDigits.Format(number, digits);
            string resultString = result.ToString();
            Assert.IsTrue(Math.Abs(result - expAnswer) <0.0001, "result string:"+resultString);


            int expCharacters = digits + 1;//+1 for comma
            if (number < 0)
                expCharacters++;//minus sign

            Assert.AreEqual(expCharacters ,resultString.Length );//+1 is because of comma
        
        }

    }
}
