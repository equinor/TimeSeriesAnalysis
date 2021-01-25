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
    class FilterUnitTests
    {

        // nb only use even numbers
        [TestCase(1,6)]
        [TestCase(2,12)]
        [TestCase(4,24)]
        [TestCase(4,12)]

        public void StepChange(double TimeBase_s, double FilterTc_s)
        {
            double[] input = Vec.Concat(Vec.Fill(0, 10), Vec.Fill(1, 30));
            LowPass lp = new LowPass(TimeBase_s);
            double[] output = lp.Filter(input, FilterTc_s);

            int timestepsToDo67prc = (int)Math.Round(FilterTc_s / TimeBase_s);
            double actualChange = output[10 + timestepsToDo67prc];

            Assert.IsTrue(Math.Abs(0.67- actualChange) <0.02,"should go 66.5% of value in one timeconstant");
        }

        /*[TestCase(1, 6)]
        [TestCase(2, 12)]
        [TestCase(4, 24)]
        [TestCase(4, 12)]

        public void StepChange_2order(double TimeBase_s, double FilterTc_s)
        {
            double[] input = Vec.Concat(Vec.Fill(0, 10), Vec.Fill(1, 30));
            LowPass lp = new LowPass(TimeBase_s);
            double[] output = lp.Filter(input, FilterTc_s,2);

            int timestepsToDo67prc = (int)Math.Round(FilterTc_s / TimeBase_s);
            double actualChange = output[10 + timestepsToDo67prc];

            Assert.IsTrue(Math.Abs(0.67 - actualChange) < 0.02, "should go 66.5% of value in one timeconstant");
        }*/


    }
 }
