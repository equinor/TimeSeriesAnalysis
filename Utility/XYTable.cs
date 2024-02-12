using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;


namespace TimeSeriesAnalysis.Utility
{
    /// <summary>
    /// When plotting an x/y plot, this enum determines if plot is to use markers or line for data points
    /// </summary>
    public enum XYlineType
    {
        /// <summary>
        /// only makers, no lines, for each x-y point
        /// </summary>
        withMarkers=0,
        /// <summary>
        /// markers or lines should not be drawn,
        /// </summary>
        noMarkers=1,
        /// <summary>
        /// xy-plot should draw line between points
        /// </summary>
        line=2
    }

    /// <summary>
    /// Holds a "table" of x-y value pairs 
    /// </summary>
    public class XYTable
    {
        List<string> names;
        List<double[]> values;
        int nValues = 0;
        List<string> columnNames;
        string tableName;

        XYlineType type;

        const int nSignificantDigits = 5;

        /// <summary>
        /// Constructor for XYTable
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <param name="type"></param>
        public XYTable(string tableName, List<string> columnNames, XYlineType type = XYlineType.withMarkers)
        {
            this.type = type;
            this.tableName = tableName;
            this.nValues = columnNames.Count;
            this.columnNames = columnNames;
            names = new List<string>();
            values = new List<double[]>();
        }

        /// <summary>
        /// Get the name of the table
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return tableName;
        }

        /// <summary>
        /// Returns the type of the line
        /// </summary>
        /// <returns></returns>
        public XYlineType GetLineType()
        {
            return type;
        }

        /// <summary>
        /// Add a row to the table
        /// </summary>
        /// <param name="rowValues"></param>
        /// <param name="rowName"></param>
        public void AddRow(double[] rowValues, string rowName="")
        {
            names.Add(rowName);
            values.Add(rowValues);
        }

        //for this to be parsed by plotly, use comma as csv-separator
        /// <summary>
        /// Write table to a csv-file
        /// </summary>
        /// <param name="fileName">csv file name</param>
        /// <param name="CSVseparator">optioanlly choose the seprator of the csv-file</param>
        public void ToCSV(string fileName, string CSVseparator = ",")
        {
            StringBuilder sb = new StringBuilder();
            // make header
            sb.Append("Name" + CSVseparator);
            sb.Append(string.Join(CSVseparator, columnNames));
            sb.Append("\r\n");

            for (int curRow = 0; curRow < values.Count; curRow++)
            {
                var dataAtTime = values.ElementAt(curRow);
                sb.Append(names.ElementAt(curRow));
                for (int curColIdx = 0; curColIdx < dataAtTime.Length; curColIdx++)
                {
                    // sb.Append(CSVseparator + dataAtTime[curColIdx]);
                    sb.Append(CSVseparator + SignificantDigits.Format(dataAtTime[curColIdx], nSignificantDigits).ToString(CultureInfo.InvariantCulture));
                    //       sb.Append(CSVseparator + SignificantDigits.Format(dataAtTime[curColIdx], nSignificantDigits).ToString());
                }
                sb.Append("\r\n");
            }

            using (StringToFileWriter writer = new StringToFileWriter(fileName))
            {
                try
                {
                    writer.Write(sb.ToString());
                    writer.Close();
                }
                catch (Exception)
                {
                    Console.WriteLine("Exception writing file:" + fileName);
                }
            }
            return;
        }
    }
}
