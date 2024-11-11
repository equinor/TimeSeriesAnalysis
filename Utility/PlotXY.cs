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

        private static string ReplaceUrlIllegalStrings(string comment)
        {
            if (comment == null)
                return null;
            return comment.Replace(" ", "_").Replace(";", "");
        }


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
            var fileName = dataPath + caseName +"__"+ ReplaceUrlIllegalStrings(table.GetName()) + ".csv";

            table.ToCSV(fileName);

            var chromePath = Plot.GetChromePath();
            var plotlyURLinternal = Plot.GetPlotlyURL();

            string command = @"-r " + plotlyURLinternal + "#";
            string plotURL = GetCodeFromType(table.GetLineType())+"="+ ReplaceUrlIllegalStrings(table.GetName());
            if (caseName != null)
            {
                plotURL += ";casename:" + ReplaceUrlIllegalStrings(caseName);
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
        /// <param name="tables">List of XYtables containing the data to be plotted</param>
        /// <param name="caseName">a unique string that is used to keep the data separate from all other simultanously created plots</param>
        /// <param name="comment">a string that is added to the top of the plot (often, a user-friendly name of the output)</param>
        /// <param name="doStartChrome">if true then a chrome window will be opened (by default this is true)</param>
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
                var fileName = dataPath + ReplaceUrlIllegalStrings(caseName) + "__" + ReplaceUrlIllegalStrings(table.GetName()) + ".csv";
                table.ToCSV(fileName);
                plotURL += GetCodeFromType(table.GetLineType())+"=" + ReplaceUrlIllegalStrings(table.GetName()) +";";
            }
            if (caseName != null)
            {
                plotURL += "casename:" + ReplaceUrlIllegalStrings(caseName);
            }
            if (comment != null)
            {
                if (!plotURL.EndsWith(";"))
                    plotURL += ";";

                plotURL += "comment:" + ReplaceUrlIllegalStrings(comment);
            }
            if (doStartChrome)
            {
                Plot.Start(chromePath, command + plotURL, out bool returnVal);
            }
            return plotURL;
        }










    }
}
