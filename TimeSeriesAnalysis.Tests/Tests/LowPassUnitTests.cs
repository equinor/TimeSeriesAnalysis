using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis.Test
{
    [TestFixture]
    class FilterUnitTests
    {

        // nb only use even numbers
        [TestCase(1,6)]
        [TestCase(2,12)]
        [TestCase(4,24)]
        [TestCase(4,12)]

        public void LowPass_StepChange_CorrectValue(double TimeBase_s, double FilterTc_s)
        {
            double[] input = Vec<double>.Concat(Vec<double>.Fill(0, 10), Vec<double>.Fill(1, 30));
            LowPass lp = new LowPass(TimeBase_s);
            double[] output = lp.Filter(input, FilterTc_s);

            int timestepsToDo67prc = (int)Math.Round(FilterTc_s / TimeBase_s);
            double actualChange = output[10 + timestepsToDo67prc];

            Assert.IsTrue(Math.Abs(0.67- actualChange) <0.02,"should go 66.5% of value in one timeconstant");
        }

        [Test]
        public void LowPass_IgnoreIndices()
        {
            double TimeBase_s = 1;
            double FilterTc_s = 6;
            double[] input = Vec<double>.Concat(Vec<double>.Fill(0, 10), Vec<double>.Fill(1, 30));
            LowPass lp2 = new LowPass(TimeBase_s);
            double[] output_unfiltered = lp2.Filter(input, FilterTc_s, 1);

            ///
            List<int> indToIgnore = new List<int>() { 0, 11, 16 };
            input[0] = Double.NaN;
            input[11] = Double.NaN;
            input[16] = Double.NaN;
            LowPass lp = new LowPass(TimeBase_s);
            double[] output_filtered = lp.Filter(input, FilterTc_s,1,indToIgnore);
            for (int i = 0; i < input.Length; i++)
            {
                    Assert.AreEqual(output_filtered[i], output_unfiltered[i]);
            }
        }



    }
 }
