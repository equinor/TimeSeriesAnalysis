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
    class VecExtensionsUnitTests
    {


        [TestCase(new double[] { 1.234, 5.225, 8.454 })]
        public void ToString_isOK(double[] vec)
        {
            Console.WriteLine(vec.Print(2));
        }
    }
}
