using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis._Examples;

namespace TimeSeriesAnalysis.Tests.TimeSeriesData
{

    [TestFixture]
    class CSV_IO
    {

        [Test, Explicit]
        public void CSVWriteAndReadBack_SmallDatatasetToCSV()
        {
            int N = 600;
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.MinSelect(N);
            dataSet.SetT0(new DateTime(2021, 1, 1));
            var isOk = dataSet.ToCsv(@"C:\Appl\source\TimeSeriesAnalysis\minSelect.csv");
            Assert.IsTrue(isOk);
            var dataSet2 = new TimeSeriesDataSet(@"C:\Appl\source\TimeSeriesAnalysis\minSelect.csv");
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
                dataSet.SetT0(new DateTime(2021, 1, 1));
                var isOk = dataSet.ToCsv(@"C:\Appl\source\TimeSeriesAnalysis\minSelect_large.csv");
                Assert.IsTrue(isOk);
                Shared.EnablePlots();
            }
            var dataSet2 = new TimeSeriesDataSet(@"C:\Appl\source\TimeSeriesAnalysis\minSelect_large.csv");
            Assert.AreEqual(N, dataSet2.GetLength());
            Assert.AreEqual(7,dataSet2.GetSignalNames().Count());




        }

    }
}
