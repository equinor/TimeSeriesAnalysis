using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Utility
{
    public class PlotXY
    {
        public static void FromTable(XYTable table,  string caseName, string comment = null,
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


        public static void FromTables(List<XYTable> tables, string caseName, string comment = null,
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
        }










    }
}
