using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Utility
{
    /// <summary>
    /// Programatically calls plotly to create an in-browser xy-plot of given input data. A utility class mainly intended for debugging. 
    /// </summary>
    public class PlotXY
    {
        /// <summary>
        /// Make an xy-plot from an XYtable entry
        /// </summary>
        /// <param name="table"></param>
        /// <param name="caseName"></param>
        /// <param name="comment"></param>
        /// <param name="doStartChrome"></param>
        /// <returns></returns>
        public static string FromTable(XYTable table,  string caseName, string comment = null,
            bool doStartChrome = true)
        {

            var dataPath = Plot.GetPlotlyDataPath();
            var fileName = dataPath + caseName +"__"+ table.GetName() + ".csv";

            table.ToCSV(fileName);

            var chromePath = Plot.GetChromePath();
            var plotlyURLinternal = Plot.GetPlotlyURL();

            string command = @"-r " + plotlyURLinternal + "#";
            string plotURL = GetCodeFromType(table.GetLineType())+"="+ table.GetName();
            if (caseName != null)
            {
                plotURL += ";casename:" + caseName;
            }
            if (doStartChrome)
            {
                Plot.Start(chromePath, command + plotURL, out bool returnVal);
            }
            return plotURL;
        }

        private static string GetCodeFromType(XYlineType type)
        {
            if (type == XYlineType.withMarkers)
                return "xym";
            if (type == XYlineType.noMarkers)
                return "xy";
            if (type == XYlineType.line)
                return "xyl";
            return "xym";
        }


        /// <summary>
        /// Make an xy-plot from a list of XYTable entries
        /// </summary>
        /// <param name="tables"></param>
        /// <param name="caseName"></param>
        /// <param name="comment"></param>
        /// <param name="doStartChrome"></param>
        /// <returns></returns>
        public static string FromTables(List<XYTable> tables, string caseName, string comment = null,
            bool doStartChrome = true)
        {
            var dataPath = Plot.GetPlotlyDataPath();
            var chromePath = Plot.GetChromePath();
            var plotlyURLinternal = Plot.GetPlotlyURL();

            string command = @"-r " + plotlyURLinternal + "#";
            string plotURL = "";
            foreach (var table in tables)
            {
                var fileName = dataPath + caseName + "__" + table.GetName() + ".csv";
                table.ToCSV(fileName);
                plotURL += GetCodeFromType(table.GetLineType())+"=" +table.GetName() +";";
            }
            if (caseName != null)
            {
                plotURL += "casename:" + caseName;
            }
            if (doStartChrome)
            {
                Plot.Start(chromePath, command + plotURL, out bool returnVal);
            }

            return plotURL;
        }










    }
}
