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
    class CsvTest
    {
        [TestCase, Explicit]
        public void CSV1_containsExtraValue_IsIgnored()
        {
            CSV.LoadDataFromCSV(@"C:\Appl\source\TimeSeriesAnalysis\Tests\TestCSVs\test1.csv", ',',out double[,] doubleData,
                out string[] variableNames, out string[,] stringData);

            Assert.AreEqual(new string[] {"variable","value1","value2"}, variableNames);

            Assert.AreEqual(new string[,] { { "var1", "1", "2" }, { "var2", "3", "4" }, { "var3","5", "6"} }, stringData);
            Assert.AreEqual(new double[,] { { Double.NaN, 1, 2 }, { Double.NaN, 3, 4 }, { Double.NaN, 5, 6 } }, doubleData);

        }
        [TestCase, Explicit]
        public void CSV2()
        {
        }

    }
}