using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace TimeSeriesAnalysis
{
    public class CSV
    {
        public static bool loadDataFromCSV(string filePath, out double[,] doubleData, out string[] variableNames, out string[,] stringData)
        {
            System.IO.StreamReader sr;
            doubleData = null;
            stringData = null;
            variableNames = null;
            try
            {
                sr = new System.IO.StreamReader(filePath);
            }
            catch
            {
                return false;
            }
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
            return true;
        }

        static private bool RobustParseDouble(string str, out double value)
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
