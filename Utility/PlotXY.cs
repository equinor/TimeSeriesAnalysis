using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Utility
{
    public class PlotXY
    {
        public static void FromTable(Table table, string caseName, string comment = null,
            bool doStartChrome = true)
        {

            var dataPath = Plot.GetPlotlyDataPath();
            var fileName = dataPath + caseName + ".csv";

            table.ToCSV(fileName);

            var chromePath = Plot.GetChromePath();
            var plotlyURLinternal = Plot.GetPlotlyURL();

            string command = @"-r " + plotlyURLinternal + "#";
            string plotURL = "xym="+ caseName ;



            if (doStartChrome)
            {
                Plot.Start(chromePath, command + plotURL, out bool returnVal);
            }
        }
    }
}
