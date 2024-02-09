using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis._Examples;

namespace TimeSeriesAnalysis.Tests.TimeSeriesData
{

    [TestFixture]
    class CSV_IO
    {
        [TestCase("29,44")]
        [TestCase("29,440")]
        [TestCase("29.44")]

        public void RobustParseDouble_ParsesCommas(string input)
        {
            //RobustParseDouble is used by CSV read-methods to extract data
            // Norwegian regional settings use commas instead of "." in decimals

            var isOk = CSV.RobustParseDouble(input, out double result);

            Assert.IsTrue(result == 29.44);
        }



        [Test, Explicit]
        public void CSVWriteAndReadBack_SmallDatatasetToCSV()
        {
            int N = 600;
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.MinSelect(N);
           // dataSet.SetT0(new DateTime(2021, 1, 1));
            var isOk = dataSet.ToCsv(@"C:\Appl\source\TimeSeriesAnalysis\minSelect.csv");
            Assert.IsTrue(isOk);
            var dataSet2 = new TimeSeriesDataSet();
            dataSet2.LoadFromCsv(@"C:\Appl\source\TimeSeriesAnalysis\minSelect.csv");
            Shared.EnablePlots();
            Assert.AreEqual(N, dataSet2.GetLength());
            Assert.AreEqual(7, dataSet2.GetSignalNames().Count());
        }


        [TestCase(true),Explicit]
        [TestCase(false)]
        public void CSVWriteAndReadBack_LargeDatatset(bool doRecreateDataset)
        {
            // if file is sufficiently large, there may be an issue with loading it in x86

            int N = 10000000;

            if (doRecreateDataset)
            {
                Shared.DisablePlots();
                ProcessControl pc = new ProcessControl();
                var dataSet = pc.MinSelect(N);
                //dataSet.SetT0(new DateTime(2021, 1, 1));
                var isOk = dataSet.ToCsv(@"C:\Appl\source\TimeSeriesAnalysis\minSelect_large.csv");
                Assert.IsTrue(isOk);
                Shared.EnablePlots();
            }
            var dataSet2 = new TimeSeriesDataSet();
            dataSet2.LoadFromCsv(@"C:\Appl\source\TimeSeriesAnalysis\minSelect_large.csv");
            Assert.AreEqual(N, dataSet2.GetLength());
            Assert.AreEqual(7,dataSet2.GetSignalNames().Count());

        }

    }

    [TestFixture]
    class BasicMethods
    {
        [TestCase(0, 50)]
        [TestCase(50,51)]

        public void Subset_DividesDataCorectly(double startPrc, double endPrc)
        {
            int N = 100;
            int timeBase_s = 1;

            var data = new TimeSeriesDataSet();
            data.Add("test1",TimeSeriesCreator.Sinus(1, 10, timeBase_s,N));
            data.Add("test2", TimeSeriesCreator.Step(N/2, N, 1, 2));
            data.AddConstant("const1",5);
            data.CreateTimestamps(timeBase_s);

            var copy = data.Subset(startPrc, endPrc);

            Assert.IsTrue(copy.ContainsSignal("test1"));
            Assert.IsTrue(copy.ContainsSignal("test2"));
            Assert.IsTrue(copy.ContainsSignal("const1"));
          
            Assert.IsTrue(copy.GetValues("test1").Length == endPrc-startPrc+1);
            Assert.IsTrue(copy.GetValues("test2").Length == endPrc - startPrc+1);
            Assert.IsTrue(copy.GetTimeStamps().Length == endPrc - startPrc+1);

        }


    }


}
