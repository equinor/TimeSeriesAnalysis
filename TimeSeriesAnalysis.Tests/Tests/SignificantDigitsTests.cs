using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Tests
{
    [TestFixture]
    internal class SignificantDigitsTests
    {

        [TestCase(2.43333,3,2.43)]
        [TestCase(2.430000000000001, 3, 2.43)]
        [TestCase(2.430000000000001, 1, 2)]
        public void Format_CorrectDigits(double number,int digits,double expAnswer )
        {
            double result = SignificantDigits.Format(number, digits);
            Assert.IsTrue(Math.Abs(result - expAnswer) <0.0001,expAnswer.ToString());
        

        
        }

    }
}
