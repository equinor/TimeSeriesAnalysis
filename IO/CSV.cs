using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace TimeSeriesAnalysis
{

    ///<summary>
    /// Utility class for loading time-series data from a plain text comma-separated variable(CSV) file
    ///</summary>

    public class CSV
    {

        ///<summary>
        /// Load time-series data from a CSV-file into variables for further processing
        ///</summary>
        /// <param name="filename"> path of file to be loaded</param>
        /// <param name="doubleData">(output) the returned 2D array where each column is the data for one variable</param>
        /// <param name="variableNames">(output) an array of the variable names in <c>doubleData</c></param>
        /// <param name="stringData">(output)the raw data of the entire csv-file in a 2D array, only needed if parsing of other two variables has failed, and useful for retireving timestamps </param>
        /// <returns></returns>
        public static bool LoadDataFromCSV(string filename, out double[,] doubleData, out string[] variableNames, out string[,] stringData)
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader(filename))
            {

                doubleData = null;
                stringData = null;
                variableNames = null;

                var linesDouble = new List<double[]>();
                var linesStr = new List<string[]>();
                // readheader
                variableNames = sr.ReadLine().Split(';');
                while (!sr.EndOfStream)
                {
                    double[] LineDouble = new double[variableNames.Length];
                    string currentLine = sr.ReadLine();
                    string[] LineStr = currentLine.Split(';');
                    for (int k = 0; k < LineStr.Length; k++)
                    {
                        if (LineStr[k].Length > 0)
                            RobustParseDouble(LineStr[k], out LineDouble[k]);
                        //   LineD[k] = double.Parse(Line[k], System.Globalization.CultureInfo.InvariantCulture);
                    }
                    linesDouble.Add(LineDouble);
                    linesStr.Add(LineStr);
                }
                if (linesDouble.Count() > 0)
                {
                    doubleData = new double[linesDouble.Count, linesDouble[0].Count()];
                }
                if (linesStr.Count() > 0)
                {
                    stringData = new string[linesDouble.Count, linesStr[0].Count()];
                }
                for (int k = 0; k < linesDouble.Count; k++)
                    for (int l = 0; l < linesDouble[k].Count(); l++)
                    {
                        doubleData[k, l] = linesDouble[k][l];
                    }
                for (int k = 0; k < linesStr.Count; k++)
                    for (int l = 0; l < linesStr[k].Count(); l++)
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
