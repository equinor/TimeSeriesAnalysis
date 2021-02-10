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
    class PlotUnitTests
    {

        // nb only use even numbers
        [TestCase]


        public void MaximumNumberOfPlotsIsObeyed()
        {
            double[] input1 = Vec.Concat(Vec.Fill(0, 10), Vec.Fill(1, 30));

            Plot4Test plot = new Plot4Test(true, 2);
            plot.One(input1, 10, "first plot");
            plot.One(input1.Mult(2), 10, "second plot"); 
            plot.One(input1.Mult(3), 10, "third plot");//should not be shown, maximum plots is two.
        }
    }
 }
