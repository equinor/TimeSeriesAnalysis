using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace TimeSeriesAnalysis.Utility
{

    /// <summary>
    /// A class that contains the text content of a csv-file.
    /// This class is introduced as a convenience to avoid confusing filenames and csv-content string
    /// in overloaded methods.
    /// </summary>
    public class CsvContent
    {
        public string CsvTxt { get; }
        public CsvContent(string csvTxt)
        {
            this.CsvTxt = csvTxt;
        }
    }

    ///<summary>
    /// IO Utility class for loading time-series data from a plain text comma-separated variable(CSV) file
    ///</summary>

    public class CSV
    {


        /// <summary>
        /// Return just string data from a CSV(useful if data contains no numerical data)
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="stringData"></param>
        /// <returns></returns>
        public static bool LoadStringsFromCSV(string filename, out string[,] stringData)
        {
            return LoadDataFromCSV(filename, out double[,] _, out string[] _, out stringData);
        }

        ///<summary>
        /// Load time-series data from a CSV-file into variables for further processing (using the default ";" separator)
        ///</summary>
        /// <param name="filename"> path of file to be loaded</param>
        /// <param name="doubleData">(output) the returned 2D array where each column is the data for one variable</param>
        /// <param name="variableNames">(output) an array of the variable names in <c>doubleData</c></param>
        /// <param name="stringData">(output)the raw data of the entire csv-file in a 2D array, only needed if parsing of other two variables has failed, and useful for retireving timestamps </param>
        /// <returns></returns>

        public static bool LoadDataFromCSV(string filename, out double[,] doubleData, out string[] variableNames, out string[,] stringData)
        {
            return LoadDataFromCSV(filename, ';', out doubleData, out variableNames, out stringData);
        }

        /// <summary>
        /// Loads data from a CSV-file, including parsing the times(first column)
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="separator"></param>
        /// <param name="dateTimes"></param>
        /// <param name="variables"></param>
        /// <param name="dateTimeFormat"></param>
        /// <returns></returns>
        public static bool LoadDataFromCsvAsTimeSeries(string filename, char separator, out DateTime[] dateTimes,
            out Dictionary<string,double[]> variables, string dateTimeFormat = "yyyy-MM-dd HH:mm:ss")
        {
            var isOk = LoadDataFromCSV(filename, separator, out double[,] doubleData, out string[] variableNames, out string[,] stringData);

            int indexOfTimeData = 0;
            dateTimes = Array2D.GetColumnParsedAsDateTime(stringData, indexOfTimeData, dateTimeFormat);

            variables = new Dictionary<string, double[]>();
            int colIdx = 0;
            foreach(string variableName in variableNames)
            {
                variables.Add(variableName,doubleData.GetColumn(colIdx));
                colIdx++;
            }
            return isOk;
        }


        ///<summary>
        /// Load time-series data from a CSV-file, low-level version for further processing
        ///</summary>
        /// <param name="filename"> path of file to be loaded</param>
        /// <param name="separator"> separator character used to separate data in file</param>
        /// <param name="doubleData">(output) the returned 2D array where each column is the data for one variable(NaNs if not able to parse as number)</param>
        /// <param name="variableNames">(output) an array of the variable names in <c>doubleData</c></param>
        /// <param name="stringData">(output)the raw data of the entire csv-file in a 2D array, only needed if parsing of other two variables has failed, and useful for retireving timestamps </param>
        /// <returns></returns>
        public static bool LoadDataFromCSV(string filename, char separator,out double[,] doubleData,
            out string[] variableNames, out string[,] stringData)
        {
            var sr = new StreamReader(filename);
            return LoadDataFromCSV(sr,separator,out doubleData, out variableNames, out stringData);
        }



        /// <summary>
        /// Version of  <c>LoadDataFromCSV</c> that accepts a <c>CSVcontent </c> containting the contents of a CSV-file as input
        /// </summary>
        /// <param name="csvString"></param>
        /// <param name="separator"></param>
        /// <param name="doubleData"></param>
        /// <param name="variableNames"></param>
        /// <param name="stringData"></param>
        /// <param name="dateTimeFormat"></param>
        /// <returns></returns>
        public static bool LoadDataFromCsvContentAsTimeSeries(CsvContent csvString, char separator, out DateTime[] dateTimes,
            out Dictionary<string, double[]> variables, string dateTimeFormat = "yyyy-MM-dd HH:mm:ss")
        {
            var sr = new StringReader(csvString.CsvTxt);
            var isOk = LoadDataFromCSV(sr, separator, out double[,] doubleData, out string[] variableNames, out string[,] stringData);

            int indexOfTimeData = 0;
            dateTimes = Array2D.GetColumnParsedAsDateTime(stringData, indexOfTimeData, dateTimeFormat);

            variables = new Dictionary<string, double[]>();
            int colIdx = 0;
            foreach (string variableName in variableNames)
            {
                variables.Add(variableName, doubleData.GetColumn(colIdx));
                colIdx++;
            }
            return isOk;


        }

        private static bool LoadDataFromCSV(TextReader sr, char separator, out double[,] doubleData,
            out string[] variableNames, out string[,] stringData)
        {
            using (sr)
            {
                // bool isOk = true;
                doubleData = null;
                stringData = null;
                variableNames = null;

                var linesDouble = new List<double[]>();
                var linesStr = new List<string[]>();
                // readheader
                variableNames = sr.ReadLine().Split(separator);
                string currentLine = sr.ReadLine();
                while (currentLine != null)
                {
                    double[] LineDouble = new double[variableNames.Length];
                    currentLine = sr.ReadLine();
                    if (currentLine == null)
                        continue;
                    string[] LineStr = currentLine.Split(separator);
                    for (int k = 0; k < Math.Min(variableNames.Length, LineStr.Length); k++)
                    {
                        if (LineStr[k].Length > 0)
                        {
                            bool ableToParse = RobustParseDouble(LineStr[k], out LineDouble[k]);
                            if (!ableToParse)
                                LineDouble[k] = Double.NaN;
                        }
                        else
                        {
                            LineDouble[k] = Double.NaN;
                        }
                    }
                    linesDouble.Add(LineDouble);
                    linesStr.Add(LineStr);
                }
                int nColumns = linesStr[0].Count();

                if (linesDouble.Count() > 0)
                {
                    doubleData = new double[linesDouble.Count, nColumns];
                }
                if (linesStr.Count() > 0)
                {
                    stringData = new string[linesDouble.Count, nColumns];
                }
                for (int k = 0; k < linesDouble.Count; k++)
                    for (int l = 0; l < linesDouble[k].Count(); l++)
                    {
                        doubleData[k, l] = linesDouble[k][l];
                    }
                for (int k = 0; k < linesStr.Count; k++)
                    for (int l = 0; l < Math.Min(linesStr[k].Count(), nColumns); l++)
                    {
                        stringData[k, l] = linesStr[k][l];
                    }
            }
            return true;
        }

        /// <summary>
        ///  Loading string data into a double value.
        /// </summary>
        /// <param name="str">the string to be parsed</param>
        /// <param name="value">(output) is the value of the parsed double(if successfully parsed)</param>
        /// <returns>The method returns true if succesful, otherwise it returns false.</returns>
        static public  bool RobustParseDouble(string str, out double value)
        {
            str = str.Replace(',', '.');
            bool abletoParseVal = false;
            if (Double.TryParse(str, System.Globalization.NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value))
                abletoParseVal = true;
            else if (Double.TryParse(str, System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out value))
                abletoParseVal = true;
            else if (Double.TryParse(str, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture,
                out value))
                abletoParseVal = true;
            return abletoParseVal;
        }

    }

}
