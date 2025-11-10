using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis._Examples;
using System.IO;
using System.Data;

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
        public void CsvAppendDataPoint()
        {
            var path = @"C:\Appl\source\TimeSeriesAnalysis\test.csv";
            var signalNames = new List<string>() { "signal_1", "signal_2", "signal_3", "signal_4" };
            var signalNames2 = new List<string>() { "signal_1", "signal_2", "signal_3", "signal_4","signal_5" };
            if (File.Exists(path))
               File.Delete(path);
            // first data point should create dataset if it does not exist
            {
                TimeSeriesDataSet dataset = new TimeSeriesDataSet(path);
                int value = 1;
                foreach (var signal in signalNames)
                {
                    dataset.AppendDataPoint(signal, value);
                    value++;
                }
                dataset.AppendTimeStamp(new DateTime(2025, 1, 1, 12, 00, 00));
                var isOk = dataset.UpdateCsv();
                Assert.IsTrue(isOk);
            }
            // second data point 
            {
                TimeSeriesDataSet dataset2 = new TimeSeriesDataSet(path);
                int value = 11;
                foreach (var signal in signalNames)
                {
                    dataset2.AppendDataPoint(signal, value);
                    value++;
                }
                var isOK2 = dataset2.AppendTimeStamp(new DateTime(2025, 1, 1, 12, 01, 00));
                Assert.IsTrue(isOK2);
                dataset2.UpdateCsv();
            }
            // third data point : re-doing the sampe data point, should not be added
            {
                TimeSeriesDataSet dataset3 = new TimeSeriesDataSet(path);
                int value = 11;
                foreach (var signal in signalNames)
                {
                    dataset3.AppendDataPoint(signal, value);
                    value++;
                }
                var isOK3 = dataset3.AppendTimeStamp(new DateTime(2025, 1, 1, 12, 01, 00));//same as prev
                Assert.IsFalse(isOK3);
            }
     
            // fourth  data point : add a new signal , should not be added
            {
                TimeSeriesDataSet dataset4 = new TimeSeriesDataSet(path);
                      int value = 41;
                foreach (var signal in signalNames2) // NB! signalnames2
                {
                    dataset4.AppendDataPoint(signal, value);
                    value++;
                }
                var isOK4 = dataset4.AppendTimeStamp(new DateTime(2025, 1, 1, 12, 02, 00));
                Assert.IsTrue(isOK4);
                dataset4.UpdateCsv();
            }
            // fifth  data point : try add just four of the five signals
            {
                TimeSeriesDataSet dataset5 = new TimeSeriesDataSet(path);
                int value = 51;
                foreach (var signal in signalNames) // NB! signalnames
                {
                    dataset5.AppendDataPoint(signal, value);
                    value++;
                }
                var isOK5 = dataset5.AppendTimeStamp(new DateTime(2025, 1, 1, 12, 03, 00));
                Assert.IsTrue(isOK5);
                dataset5.UpdateCsv();
            }
            // sixth  data point : add all signals again
            {
                TimeSeriesDataSet dataset6 = new TimeSeriesDataSet(path);
                int value = 61;
                foreach (var signal in signalNames) // NB! signalnames
                {
                    dataset6.AppendDataPoint(signal, value);
                    value++;
                }
                var isOK6 = dataset6.AppendTimeStamp(new DateTime(2025, 1, 1, 12, 04, 00));
                Assert.IsTrue(isOK6);
                dataset6.UpdateCsv();
            }
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

            var copy = data.SubSetPrc(startPrc, endPrc);

            Assert.IsTrue(copy.ContainsSignal("test1"));
            Assert.IsTrue(copy.ContainsSignal("test2"));
            Assert.IsTrue(copy.ContainsSignal("const1"));
          
            Assert.IsTrue(copy.GetValues("test1").Length == endPrc-startPrc+1);
            Assert.IsTrue(copy.GetValues("test2").Length == endPrc - startPrc+1);
            Assert.IsTrue(copy.GetTimeStamps().Length == endPrc - startPrc+1);

        }

        [TestCase(2, new int[] { 5, 10, 15, 80, 90 }, new int[] { }) ]
        [TestCase(2, new int[] { 0,1,2,3,4,5,6,7,8,9,10 }, new int[] {0,1,2,3,4 })  ]
        [TestCase(4, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, new int[] { 0, 1 })]
        [TestCase(4, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,11 }, new int[] { 0, 1,2})]
        [TestCase(10, new int[] { 91,92,93,94,95,96,97,98,99,100}, new int[] {10})]

        public void Downsample(int downsampleFactor, int[] indToIgnoreArray, int[] expIndToIgnore)
        {
            int N = 101;
            int timeBase_s = 1;

            var data = new TimeSeriesDataSet();
            data.Add("test1", TimeSeriesCreator.Sinus(1, 20, timeBase_s, N));
            data.Add("test2", TimeSeriesCreator.Step(N / 2, N, 1, 2));
            data.AddConstant("const1", 5);
            data.CreateTimestamps(timeBase_s);
            data.SetIndicesToIgnore(new List<int>(indToIgnoreArray) );

            var downsampled = data.CreateDownsampledCopy(downsampleFactor);

            if (downsampled.GetIndicesToIgnore().Count > 0)
            {
                Assert.IsTrue(downsampled.GetIndicesToIgnore().Max() <= downsampled.GetLength() - 1);
            }
            Assert.AreEqual(new List<int>(expIndToIgnore), downsampled.GetIndicesToIgnore());

            if (false)
            {
                Shared.EnablePlots();
                Plot.FromList(
                    new List<(double[], DateTime[])>
                    {
                    (data.GetValues("test1"), data.GetTimeStamps()),
                    (downsampled.GetValues("test1"), downsampled.GetTimeStamps()),
                    },
                    new List<string> { "y1=orig", "y1=downsample" }, "DownsampleTest"
                    );
                Shared.DisablePlots();
            }
        }




    }


}
