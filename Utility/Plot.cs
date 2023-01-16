using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using System.Configuration;

namespace TimeSeriesAnalysis.Utility
{

    ///<summary>
    /// Static methods for plotting one or more time-series across one or more y-axes and one or more subplots by opening up Chrome 
    /// calling a set of JavaScript plotting methods based on plotly.js
    /// <para>
    /// Time-series are written as CSV-files to a specific folder on disk <c>plotDataPath</c>, before starting a chrome-window
    /// that loads a specific "localhost" <c>plotlyURL</c> that reads said CSV-files and initates an in-browser plot using plotly.js.
    /// </para>
    /// <para>
    /// For this method to work you need to be running a web server such as IIS that serves up a "localhost" <c>plotlyURL</c> as described
    /// in the documentation.
    /// </para>
    /// <para>
    /// If you sometimes need to disable plots (for instance if plotting code is included in unit tests) see Plot4Test
    /// </para>
    ///</summary>

    public class Plot
    {
        const string plotDataPath = @"C:\inetpub\wwwroot\plotly\Data";
        const string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";//@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
        const string plotlyURL = @"localhost\plotly\index.html";

        private static bool ShouldPlottingBeDone()
        {
            if (!Shared.IsPlottingEnabled())
                return false;
            var isPlotEnabled = ConfigurationManager.AppSettings.GetValues("PlotsAreEnabled");
            if (isPlotEnabled != null)
            {
                var str = isPlotEnabled[0].ToLower().Trim();
                if (str == "false" || str == "0")
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Plot data from a list of value-date tuples (each time-series can have unique time-vector with unique sampling)
        /// </summary>
        /// <param name="dataDateTupleList"></param>
        /// <param name="plotNames"></param>
        /// <param name="comment"></param>
        /// <param name="caseName"></param>
        /// <param name="doStartChrome"></param>
        /// <returns></returns>
        public static string FromList(List<(double[],DateTime[])> dataDateTupleList, List<string> plotNames,
            string comment = null, string caseName = "", bool doStartChrome = true)
        {
            if (dataDateTupleList == null)
                return "";
            if (plotNames == null)
                return "";

            if(!ShouldPlottingBeDone())
                return "";


            var plotlyURLinternal = plotlyURL;
            var plotlyULRAppConfig = ConfigurationManager.AppSettings.GetValues("PlotlyURL");
            if (plotlyULRAppConfig != null)
            {
                plotlyURLinternal = plotlyULRAppConfig[0];
            }

            string command = @"-r " + plotlyURLinternal + "#";
            string plotURL = ""; ;

            if ((caseName == null || caseName == "") && comment != null)
            {
                caseName = comment;
            }
            caseName = caseName.Replace("(", "").Replace(")", "").Replace(" ", "");

            int j = 0;
            foreach (var dataDateTuple in dataDateTupleList)
            {
                string plotName = plotNames.ElementAt(j);
                string name = plotName;
                
                string ppTagName = PreprocessTagName(caseName +"__"+ plotName);
                string shortPPtagName = PreprocessTagName(plotName);
                plotURL += shortPPtagName;
                if (j < plotNames.Count - 1)//dont add semicolon after last variable
                    plotURL += ";";
                WriteSingleDataToCSV(dataDateTuple.Item2, dataDateTuple.Item1, ppTagName, null);
                j++;
            }

            plotURL += CreateCommentStr(comment);
            if (caseName.Length > 0)
                plotURL += ";casename:" + caseName;

            var chromePathAppConfig = ConfigurationManager.AppSettings.GetValues("ChromePath");
            var chromePathInternal = chromePath;
            if (chromePathAppConfig != null)
            {
                chromePathInternal = chromePathAppConfig[0];
            }


            if (doStartChrome)
            {
                Start(chromePathInternal, command + plotURL, out bool returnVal);
            }
            return plotURL;

        }


        /// <summary>
        /// Plot all variables in a list of doubles, that all have the same timestamps given by <c>dataTimes</c>
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="plotNames"></param>
        /// <param name="dataTimes">common timestamps</param>
        /// <param name="comment"></param>
        /// <param name="caseName"></param>
        /// <param name="doStartChrome"></param>
        /// <returns></returns>
        public static string FromList(List<double[]> dataList, List<string> plotNames,
            DateTime[] dataTimes, string comment = null,
            string caseName = "", bool doStartChrome = true)
        {

            if (dataList == null)
                return "";
            if (plotNames == null)
                return "";
            if (dataList.Count() == 0)
                return "";
            if (plotNames.Count() == 0)
                return "";
            if (dataList.ElementAt(0).Count() == 0)
                return "";

            if (!ShouldPlottingBeDone())
                return "";

            var plotlyURLinternal = GetPlotlyURL();


            string command = @"-r " + plotlyURLinternal + "#";
            string plotURL = ""; ;

            if ((caseName == null || caseName == "") && comment != null)
            {
                caseName = comment;
            }
            caseName = caseName.Replace("(", "").Replace(")", "").Replace(" ", "");

            int j = 0;
            foreach (string plotName in plotNames)
            {
                string csvName = plotName;
                if (caseName != null)
                {
                    csvName = caseName + "__" + plotName;
                }
                string ppTagName = PreprocessTagName(csvName);
                string shortPPtagName = PreprocessTagName(plotName);
                plotURL += shortPPtagName;
                if (j < plotNames.Count - 1)//dont add semicolon after last variable
                    plotURL += ";";
                WriteSingleDataToCSV(dataTimes.ToArray(), dataList.ElementAt(j), ppTagName, null);
                j++;
            }
            plotURL += CreateCommentStr(comment);
            if (caseName.Length > 0)
                plotURL += ";casename:" + caseName;

            var chromePathInternal = GetChromePath();

            if (doStartChrome)
            {
                Start(chromePathInternal, command + plotURL, out bool returnVal);
            }
            return plotURL;
        }

        internal static string GetPlotlyURL()
        {
            var plotlyURLinternal = plotlyURL;
            var plotlyULRAppConfig = ConfigurationManager.AppSettings.GetValues("PlotlyURL");
            if (plotlyULRAppConfig != null)
            {
                plotlyURLinternal = plotlyULRAppConfig[0];
            }
            return plotlyURLinternal;
        }


        internal static string GetChromePath()
        {
            var chromePathAppConfig = ConfigurationManager.AppSettings.GetValues("ChromePath");
            var chromePathInternal = chromePath;
            if (chromePathAppConfig != null)
            {
                chromePathInternal = chromePathAppConfig[0];
            }
            return chromePathInternal;
        }


    /// <summary>
    /// Plot values in a list of vectors <c>dataList</c>, when all vectors start at <c>t0</c> and have a stedy sampling rate
    /// </summary>
    /// <param name="dataList">List of doubles, one entry for each time-series to be plotted</param>
    /// <param name="plotNames">List of string  of unique names to describe each plot, prefixed by either "y1="(top left),"y2="(top right),"y3="(bottom left) 
    /// or "y4="(bottom right) to denote what y-axis to plot the variable on</param>
    /// <param name="dT_s">the time between data samples in seconds</param>
    /// <param name="comment">a comment that is shown in the plot</param>
    /// <param name="t0">the DateTime of the first data point </param>
    /// <param name="caseName">give each plot a casename if creating multiple plots with the re-occurring variable names</param>
    /// <param name="doStartChrome">By setting doStartChrome to false, you can skip opening up chrome, the link to figure
    ///   will instead be returned </param>
    /// <returns>The url of the resulting plot is returned</returns>
    public static string FromList(List<double[]> dataList, List<string> plotNames,
            double dT_s, string comment = null, DateTime t0 = new DateTime(),
            string caseName = "", bool doStartChrome = true)
        {
            if (dataList == null)
                return "";
            if (plotNames == null)
                return "";
            if (dataList.Count() == 0)
                return "";
            if (plotNames.Count() == 0)
                return "";
            if (dataList.ElementAt(0) == null)
                return "";
            if (dataList.ElementAt(0).Count()==0)
                return "";

            if (!ShouldPlottingBeDone())
                return "";

            var time = InitTimeList(t0, dT_s, dataList.ElementAt(0).Count());

            return FromList(dataList, plotNames, time.ToArray(), comment,caseName,doStartChrome);
        }

        ///<summary>
        /// Remove illegal characters from tagName.
        ///</summary>

        static private string PreprocessTagName(string tagName)
        {
            return tagName.Replace(" ", "_");
        }

        ///<summary>
        /// Creates a monotonically increasing list of DataTimes starting at t0, with length N and step time dT_s
        ///</summary>
        static private List<DateTime> InitTimeList(DateTime t0, double dT_s, int N)
        {
            DateTime defaultT0 = new DateTime(2000, 1, 1, 0, 0, 0);

            List<DateTime> time = new List<DateTime>();
            if (t0 < defaultT0)
            {
                time.Add(defaultT0);
            }
            else
            {
                time.Add(t0);
            }

            for (int i = 0; i < N; i++)
            {
                time.Add(time.Last().AddSeconds(dT_s));
            }

            return time;
        }

        static private string CreateCommentStr(string comment)
        {
            if (comment == null)
                return "";
            else
                return ";comment=" + comment.Replace(" ", "_");
        }


        /// <summary>
        /// Starts a process from an executable
        /// </summary>
        /// <param name="procname">path of executable</param>
        /// <param name="arguments">arguemnts to be passed to executable via the command line</param>
        /// <param name="returnValue"></param>
        /// <returns>returns a the process object</returns>
        static internal Process Start(string procname, string arguments, out bool returnValue)
        {
            Process proc = new Process();
            try
            {
                proc.StartInfo.FileName = procname;
                proc.StartInfo.Arguments = arguments;
                returnValue = proc.Start();
            }
            catch (Exception e)
            {
                returnValue = false;
                Shared.GetParserObj().AddError("Exception occurred staring "+ procname+" "+ e.ToString());
            }
            return proc;
        }

        internal static string GetPlotlyDataPath()
        {
            var plotDataPathInternal = plotDataPath;
            var plotDataPathAppConfig = ConfigurationManager.AppSettings.GetValues("PlotDataPath");
            if (plotDataPathAppConfig != null)
            {
                plotDataPathInternal = plotDataPathAppConfig[0];
            }
            if (!plotDataPathInternal.EndsWith("\\"))
                plotDataPathInternal += "\\";

            return  plotDataPathInternal;
        }



        static private void WriteSingleDataToCSV(DateTime[] time, double[] data, string tagName, string comment = null, 
            string customPlotDataPath=null)
        {
            tagName = PreprocessTagName(tagName);

            tagName = tagName.Replace("y1=", "");
            tagName = tagName.Replace("y2=", "");
            tagName = tagName.Replace("y3=", "");
            tagName = tagName.Replace("y4=", "");

            string CSVseparator = ",";
            StringBuilder sb = new StringBuilder();
            // make header
            //     sb.Append("Time" + CSVseparator);
            //       sb.Append(string.Join(CSVseparator, tagNames));

            sb.Append("time,"+ tagName);
            sb.Append("\r\n");


            var writeCulture = new CultureInfo("en-US");// System.Globalization.CultureInfo.InstalledUICulture;
            var numberFormat = (System.Globalization.NumberFormatInfo)writeCulture.NumberFormat.Clone();
            numberFormat.NumberDecimalSeparator = ".";

            // add data. 
            if (data != null)
            {
                for (int row = 0; row < data.Count(); row++)
                {
                    if (time != null)
                    {
                        if (time.Length > row)
                        {
                            sb.Append(UnixTime.ConvertToUnixTimestamp(time.ElementAt(row)).ToString(writeCulture));
                        }
                    }
                    sb.Append(CSVseparator);
                    // this works for .net framework, but gives a "," separator in .net standard 2.0:
                    string value = data[row].ToString(writeCulture);

                    sb.Append(value);
                    // sb.Append(CSVseparator);
                    sb.Append("\r\n");
                }
            }

            /*var plotDataPathInternal = plotDataPath;
            var plotDataPathAppConfig = ConfigurationManager.AppSettings.GetValues("PlotDataPath");
            if (plotDataPathAppConfig != null)
            {
                plotDataPathInternal = plotDataPathAppConfig[0];
            }
            if (!plotDataPathInternal.EndsWith("\\"))
                plotDataPathInternal += "\\";
            */
            var plotDataPathInternal = GetPlotlyDataPath();

            string fileName = plotDataPathInternal + tagName + ".csv";


            if (customPlotDataPath!=null)
            {
                fileName = customPlotDataPath;
                if (!customPlotDataPath.EndsWith("\\"))
                    fileName += "\\";
                fileName+= tagName + ".csv";
            }
            using (StringToFileWriter writer = new StringToFileWriter(fileName))
            {
                try
                {
                    writer.Write(sb.ToString());
                    writer.Close();
                }
                catch(Exception)
                {
                    Shared.GetParserObj().AddError("Exception writing file:"+ fileName);
                }
            }

        }


    }
}        
