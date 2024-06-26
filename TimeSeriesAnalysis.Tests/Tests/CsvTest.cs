﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;
using NUnit.Framework;

namespace TimeSeriesAnalysis.Test
{
    [TestFixture]
    class Xml
    {
       // [TestCase, Explicit]
        public void CSV1_containsExtraValue_IsIgnored()
        {
            CSV.LoadDataFromCSV(@"C:\Appl\source\TimeSeriesAnalysis\Tests\TestCSVs\test1.csv", ',',out double[,] doubleData,
                out string[] variableNames, out string[,] stringData);

            Assert.AreEqual(new string[] {"variable","value1","value2"}, variableNames);

            Assert.AreEqual(new string[,] { { "var1", "1", "2" }, { "var2", "3", "4" }, { "var3","5", "6"} }, stringData);
            Assert.AreEqual(new double[,] { { Double.NaN, 1, 2 }, { Double.NaN, 3, 4 }, { Double.NaN, 5, 6 } }, doubleData);

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
