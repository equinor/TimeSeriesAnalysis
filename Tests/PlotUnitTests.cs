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
        public void XYPlot_single_nonames()
        {
            XYTable table = new XYTable("test",new List<string> { "var1","var2"});
            table.AddRow(new double[] {1, 1 });
            table.AddRow(new double[] { 2, 2 });
            table.AddRow(new double[] { 3, 3 });
            PlotXY.FromTable(table,"XYunitTest1");
       
        }

        [TestCase(Explicit = true, Reason = "opens Chrome window")]
        public void XYPlot_double_nonames()
        {
            XYTable table = new XYTable("test", new List<string> { "var1", "var2" });
            table.AddRow(new double[] { 1, 1 });
            table.AddRow(new double[] { 2, 2 });
            table.AddRow(new double[] { 3, 3 });
            XYTable table2 = new XYTable("test2", new List<string> { "var1", "var2" });
            table2.AddRow(new double[] { 1, 2 });
            table2.AddRow(new double[] { 2, 3 });
            table2.AddRow(new double[] { 3, 4 });

            PlotXY.FromTables(new List<XYTable> {table,table2 }, "XYunitTest1");
        }

        [TestCase(Explicit = true, Reason = "opens Chrome window")]
        public void XYPlot_single_withnames()
        {
            XYTable table = new XYTable("test", new List<string> { "var1", "var2" });
            table.AddRow(new double[] { 1, 1 },"aa");
            table.AddRow(new double[] { 2, 2 },"bb");
            table.AddRow(new double[] { 3, 3 },"cc");
            PlotXY.FromTable(table, "XYunitTest2");
        }

        [TestCase(Explicit = true, Reason = "opens Chrome window")]
        public void XYPlot_double_withames()
        {
            XYTable table = new XYTable("test", new List<string> { "var1", "var2" });
            table.AddRow(new double[] { 1, 1 }, "aa");
            table.AddRow(new double[] { 2, 2 }, "bb");
            table.AddRow(new double[] { 3, 3 }, "cc");
            XYTable table2 = new XYTable("test2", new List<string> { "var1", "var2" },XYlineType.line);
            table2.AddRow(new double[] { 1, 2 }, "aa");
            table2.AddRow(new double[] { 2, 3 }, "bb");
            table2.AddRow(new double[] { 3, 4 }, "cc");

            PlotXY.FromTables(new List<XYTable> { table, table2 }, "XYunitTest3");
        }

        [TestCase(Explicit = true, Reason = "opens Chrome window")]
        public void PlotFromTupleList()
        {
            var values1 = TimeSeriesCreator.Constant(1,100);
            var values2 = TimeSeriesCreator.Constant(2,100);
            var values3 = TimeSeriesCreator.Constant(1.5,100);

            var times1 = TimeSeriesCreator.CreateDateStampArray(new DateTime(2000, 1, 1,0,0,0), 3600, 100);
            var times2 = TimeSeriesCreator.CreateDateStampArray(new DateTime(2000, 1, 1,12,0,0), 3600, 100);
            var times3 = TimeSeriesCreator.CreateDateStampArray(new DateTime(2000, 1, 1,6,0,0), 3600, 100);

            var list = new List<(double[], DateTime[])>()
            {
                (values1,times1),
                (values2,times2),
                (values3,times3)
            };

            string plotURL = Plot.FromList(list,
                new List<string> { "y1=input1", "y1=input2", "y1=input3" }, "unit test" , "Test_PlotsTimes");
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
