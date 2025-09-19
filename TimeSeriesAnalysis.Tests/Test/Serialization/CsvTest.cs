using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace TimeSeriesAnalysis.Test.Serialization
{
    [TestFixture]
    class FileReading
    {
        // [TestCase(@"..\..\..\TestData\test1_extraValue.csv")]

        [TestCase(@"test_twoValuesAndThreeRows.csv",Description ="a flawless file, the base case")]
        [TestCase(@"test_extraValue.csv", Description = "an extra value with no corresponding header should be ignored")]
        [TestCase(@"test_extraCommaFirstRow.csv", Description = "an extra comma in the header should be ignored")]


        public void CSV_read(string filename)
        {
            CSV.LoadDataFromCSV(@"..\..\..\TestData\" + filename, ',',out double[,] doubleData,
                out string[] variableNames, out string[,] stringData);
            Assert.AreEqual(new string[] {"variable","value1","value2"}, variableNames);
            Assert.AreEqual(new string[,] { { "var1", "1", "2" }, { "var2", "3", "4" }, { "var3","5", "6"} }, stringData);
            Assert.AreEqual(new double[,] { { Double.NaN, 1, 2 }, 
                { Double.NaN, 3, 4 }, 
                { Double.NaN, 5, 6 } }, doubleData);
        }

        [TestCase(@"test_emptyrow.csv", Description = "a tag without data is added")]
        public void CSV_read_emptyrows(string filename)
        {
            CSV.LoadDataFromCSV(@"..\..\..\TestData\" + filename, ',', out double[,] doubleData,
                out string[] variableNames, out string[,] stringData);
            Assert.AreEqual(new string[] { "variable", "value1", "value2","value3" }, variableNames);
            Assert.AreEqual(new string[,] { { "var1", "1", "", "2" }, { "var2", "3", "", "4" }, { "var3", "5", "", "6" } }, stringData);
            Assert.AreEqual(new double[,] { { Double.NaN, 1, Double.NaN, 2 }, 
                { Double.NaN, 3, Double.NaN, 4 }, 
                { Double.NaN, 5, Double.NaN, 6 } }, doubleData);
        }

        [TestCase(@"test_edgecase_justdates.csv", Description = "")]
        [TestCase(@"test_edgecase_justdates2.csv", Description = "")]
        public void CsvLoadAsTimeSeries_Edgecases_DoesNotCrash(string filename)
        {
            var ret = CSV.LoadDataFromCsvAsTimeSeries(@"..\..\..\TestData\" + filename, ';', out var dateTimes,
                out var variables);
        }

        [TestCase(@"test_edgecase_justdates.csv", Description = "")]
        [TestCase(@"test_edgecase_justdates2.csv", Description = "")]
        public void TimeSeriesDataSet_LoadFromCsv_Edgecases_DoesNotCrash(string filename)
        {
            var csvTxt = File.ReadAllText(@"..\..\..\TestData\"+filename);
            var dataset = new TimeSeriesDataSet();
            var ret = dataset.LoadFromCsv(new CsvContent(csvTxt),';');
            Assert.IsFalse(ret);
        }





        [TestCase, Explicit]
        public void Xml_load()
        {
            // add xml file name above, but do not check in.
            (var dataset,var nErrors) = SigmaXml.LoadFromFile(@"");

            Assert.IsTrue(dataset != null);
            Assert.IsTrue(dataset.GetLength() > 0);
            Assert.IsTrue(dataset.GetTimeBase() > 0);
            Assert.Greater(dataset.GetTimeStamps().Length,0);
            Assert.AreEqual(0,nErrors);
            Assert.Greater(dataset.GetSignalNames().Length,120);
            var csv = dataset.ToCsvText();
        }
    }
}
