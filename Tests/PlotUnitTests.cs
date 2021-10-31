using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using TimeSeriesAnalysis;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test
{
    [TestFixture]
    class PlotUnitTests
    {
        // nb only use even numbers
        [TestCase(Explicit =true, Reason= "opens Chrome windows")]

        public void MaximumNumberOfPlotsIsObeyed()
        {
            double[] input1 = Vec<double>.Concat(Vec<double>.Fill(0, 10), Vec<double>.Fill(1, 30));

            Plot4Test plot = new Plot4Test(true, 2);
            plot.FromList(new List<double[]> { input1 }, new List<string> {"plot1"}, 10, "first plot");
            plot.FromList(new List<double[]> { input1.Mult(2) }, new List<string> { "plot2" }, 10, "second plot"); 
            plot.FromList(new List<double[]> { input1.Mult(3) }, new List<string> { "plot3" }, 10, "third plot");//should not be shown, maximum plots is two.
        }
        [TestCase(Explicit = true, Reason = "opens Chrome window")]
        public void SubplotPositionWorksOk()
        {
           double[] input1 = Vec<double>.Concat(Vec<double>.Fill(0, 10), Vec<double>.Fill(1, 30));
           double[] input2 = Vec<double>.Concat(Vec<double>.Fill(0, 20), Vec<double>.Fill(2, 20));
           double[] input3 = Vec<double>.Concat(Vec<double>.Fill(0, 30), Vec<double>.Fill(1, 10));
           double[] input4 = Vec<double>.Concat(Vec<double>.Fill(0, 35), Vec<double>.Fill(1, 5));

           string plotURL = Plot.FromList(new List<double[]>{ input1,input2,input3,input4},
                new List<string>{ "y1=input1","y2=input2","y3=input3","y4=input4"},1,"unit test", 
                new DateTime(2020,1,1, 0,0,0), "Test_SubplotPositionWorksOk");
            Console.WriteLine(plotURL);
        }

        [TestCase(Explicit = true, Reason = "opens Chrome window")]
        public void Plot_CorrectlyPlotsTimes()
        {

            List<double> input1  = new List<double>();
            List<DateTime> dates = new List<DateTime>();
            dates.Add(new DateTime(2000,1,1));
            int k = 1;
            for (int l=0;l<10;l++)
            {
                k++;
                input1.Add(k);
                dates.Add(dates.Last().AddDays(k));
            }
            string plotURL = Plot.FromList(new List<double[]> { input1.ToArray()},
            new List<string> { "y1=input1" }, dates.ToArray(), "unit test"
                , "Test_PlotsTimes");
        }

    }
 }
