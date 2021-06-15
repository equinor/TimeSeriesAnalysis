using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TimeSeriesAnalysis
{

    ///<summary>
    /// Static methods for plotting one or more time-series across one or more y-axes and one or more subplots. 
    /// If you sometimes need to disable plots (for instance if plotting code is included in unit tests) see Plot4Test
    ///</summary>


    public class Plot
    {
        const string plotDataPath = @"C:\Appl\ProcessDataFramework\www\plotly\Data\";
        const string firefoxPath = @"C:\Program Files\Mozilla Firefox\firefox.exe";
        const string chromePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
        const string plotlyPath = @"localhost\plotly\index.html";

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
        static private List<DateTime> InitTimeList(DateTime t0, int dT_s, int N)
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


        ///<summary>
        /// (deprecated)Plot one vector X, where the sampling time interval is dT_s. 
        ///</summary>

        static public void One(double[] X, int dT_s, string tagName = "Var1", string comment=null, DateTime t0 = new DateTime())
        {
            tagName = PreprocessTagName(tagName);
            var time = InitTimeList(t0,dT_s, X.Count());
            // 

            WriteSingleDataToCSV(time.ToArray(), X, tagName);

            Start(chromePath,
                @"-r "+ plotlyPath + "#"+tagName + CreateCommentStr(comment), out bool returnVal);
        }

        ///<summary>
        ///(deprecated) Plot two vectors V1 and V2, where the sampling time interval is dT_s. 
        ///</summary>

        static public void Two(double[] V1, double[] V2, int dT_s, 
            string tagNameV1 = "Var1",string tagNameV2 = "Var2",
            bool plotAllVarsOnLeftYaxis=true, bool useSubplots= false, string comment = null, DateTime t0 = new DateTime())
        {
            tagNameV1 = PreprocessTagName(tagNameV1);
            tagNameV2 = PreprocessTagName(tagNameV2);

            var time = InitTimeList(t0, dT_s,V1.Count());
            WriteSingleDataToCSV(time.ToArray(), V1, tagNameV1);
            WriteSingleDataToCSV(time.ToArray(), V2, tagNameV2);

            if (useSubplots)
            {
                Start(chromePath,
                  @"-r " + plotlyPath + "#" + tagNameV1 + ";y3=" + tagNameV2 + CreateCommentStr(comment), out bool returnVal);
            }
            else if (plotAllVarsOnLeftYaxis)
            {
                Start(chromePath,
                        @"-r " + plotlyPath + "#" + tagNameV1 + ";" + tagNameV2 + CreateCommentStr(comment), out bool returnVal);
            }
            else
            {
                Start(chromePath,
                     @"-r " + plotlyPath + "#" + tagNameV1 + ";y2=" + tagNameV2 + CreateCommentStr(comment), out bool returnVal);
            }
        }

        ///<summary>
        /// (deprecated)Plot three vectors V1,V2,V3 where the sampling time interval is dT_s. 
        ///</summary>
        static public void Three(double[] V1, double[] V2, double[] V3, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            var time = InitTimeList(t0, dT_s, V1.Count());
            
            WriteSingleDataToCSV(time.ToArray(), V1, tagNameV1);
            WriteSingleDataToCSV(time.ToArray(), V2, tagNameV2);
            WriteSingleDataToCSV(time.ToArray(), V3, tagNameV3);

            if (useSubplots)
            {
                if (plotAllOnLeftYaxis)
                {
                    Start(chromePath,
                        @"-r " + plotlyPath + "#" + tagNameV1 + ";" + tagNameV2 + ";y3=" + tagNameV3 + CreateCommentStr(comment), out bool returnVal);
                }
                else
                {
                    Start(chromePath,
                        @"-r " + plotlyPath + "#" + tagNameV1 + ";y2=" + tagNameV2 + ";y3=" + tagNameV3 + CreateCommentStr(comment), out bool returnVal);
                }
            }
            else
            {

                if (plotAllOnLeftYaxis)
                {
                    Start(chromePath,
                        @"-r " + plotlyPath + "#" + tagNameV1 + ";" + tagNameV2 + ";" + tagNameV3 + CreateCommentStr(comment), out bool returnVal);
                }
                else
                {
                    Start(chromePath,
                         @"-r " + plotlyPath + "#" + tagNameV1 + ";" + tagNameV2 + ";y2=" + tagNameV3 + CreateCommentStr(comment), out bool returnVal);
                }
            }
        }

        static private string CreateCommentStr(string comment)
        {
            if (comment == null)
                return "";
            else
                return ";comment=" + comment.Replace(" ","_"); 
        }

        ///<summary>
        /// (deprecated)Plot four vectors V1,V2,V3,V4 where the sampling time interval is dT_s. 
        ///</summary>

        static public void Four(double[] V1, double[] V2, double[] V3,double[] V4, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3", string tagNameV4 = "Var4",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            tagNameV1 = PreprocessTagName(tagNameV1);
            tagNameV2 = PreprocessTagName(tagNameV2);
            tagNameV3 = PreprocessTagName(tagNameV3);
            tagNameV4 = PreprocessTagName(tagNameV4);

            var time = InitTimeList(t0,dT_s, V1.Count());
            // 
            if (V1 == null)
                return;
            for (int i = 0; i < V1.Length; i++)
            {
                time.Add(time.Last().AddSeconds(dT_s));
            }
            WriteSingleDataToCSV(time.ToArray(), V1, tagNameV1);
            WriteSingleDataToCSV(time.ToArray(), V2, tagNameV2);
            WriteSingleDataToCSV(time.ToArray(), V3, tagNameV3);
            WriteSingleDataToCSV(time.ToArray(), V4, tagNameV4);


            if (useSubplots)
            {
                if (plotAllOnLeftYaxis)
                {
                    Start(chromePath,
                        @"-r " + plotlyPath + "#" + tagNameV1 + ";" + tagNameV2 + ";"+tagNameV3 
                        +";y3=" + tagNameV4 + CreateCommentStr(comment), out bool returnVal);
                }
                else
                {
                    Start(chromePath,
                        @"-r " + plotlyPath + "#" + tagNameV1 + ";y2=" + tagNameV2 + ";y3=" + tagNameV3 
                        + ";y4=" + tagNameV3+";" + CreateCommentStr(comment), out bool returnVal);
                }
            }
            else
            {

                if (plotAllOnLeftYaxis)
                {
                    Start(chromePath,
                        @"-r " + plotlyPath + "#" + tagNameV1 + ";" + tagNameV2 + ";" + tagNameV3 +";" + tagNameV4 + CreateCommentStr(comment), out bool returnVal);
                }
                else
                {
                    Start(chromePath,
                         @"-r " + plotlyPath + "#" + tagNameV1 + ";" + tagNameV2 + ";y2=" + tagNameV3 + ";" + tagNameV4 + CreateCommentStr(comment), out bool returnVal);
                }
            }
        }

        ///<summary>
        /// (deprecated)Plot five vectors V1,V2,V3,V4,V5 where the sampling time interval is dT_s. 
        ///</summary>
        static public void Five(double[] V1, double[] V2, double[] V3, double[] V4, double[] V5, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3", string tagNameV4 = "Var4", string tagNameV5 = "Var5",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0= new DateTime())
        {
            tagNameV1 = PreprocessTagName(tagNameV1);
            tagNameV2 = PreprocessTagName(tagNameV2);
            tagNameV3 = PreprocessTagName(tagNameV3);
            tagNameV4 = PreprocessTagName(tagNameV4);
            tagNameV5 = PreprocessTagName(tagNameV5);

            var time = InitTimeList(t0, dT_s, V1.Count());
            // 
            if (V1 == null)
                return;
            for (int i = 0; i < V1.Length; i++)
            {
                time.Add(time.Last().AddSeconds(dT_s));
            }
            WriteSingleDataToCSV(time.ToArray(), V1, tagNameV1);
            WriteSingleDataToCSV(time.ToArray(), V2, tagNameV2);
            WriteSingleDataToCSV(time.ToArray(), V3, tagNameV3);
            WriteSingleDataToCSV(time.ToArray(), V4, tagNameV4);
            WriteSingleDataToCSV(time.ToArray(), V5, tagNameV5);

            if (useSubplots)
            {
            }
            else
            {

                if (plotAllOnLeftYaxis)
                {
                    Start(chromePath,
                        @"-r " + plotlyPath + "#" + tagNameV1 + ";" + tagNameV2 + ";" + tagNameV3 + ";" + tagNameV4 +";"+tagNameV5+ CreateCommentStr(comment), out bool returnVal);
                }
            }
        }

        ///<summary>
        /// (deprecated)Plot six vectors V1,V2,V3,V4 where the sampling time interval is dT_s. 
        ///</summary>
        static public void Six(double[] V1, double[] V2, double[] V3, double[] V4, double[] V5, double[] V6, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3", string tagNameV4 = "Var4", string tagNameV5 = "Var5",
            string tagNameV6 = "Var6",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            tagNameV1 = PreprocessTagName(tagNameV1);
            tagNameV2 = PreprocessTagName(tagNameV2);
            tagNameV3 = PreprocessTagName(tagNameV3);
            tagNameV4 = PreprocessTagName(tagNameV4);
            tagNameV5 = PreprocessTagName(tagNameV5);
            tagNameV6 = PreprocessTagName(tagNameV6);

            var time = InitTimeList(t0, dT_s, V1.Count());
            // 
            if (V1 == null)
                return;
            for (int i = 0; i < V1.Length; i++)
            {
                time.Add(time.Last().AddSeconds(dT_s));
            }
            WriteSingleDataToCSV(time.ToArray(), V1, tagNameV1);
            WriteSingleDataToCSV(time.ToArray(), V2, tagNameV2);
            WriteSingleDataToCSV(time.ToArray(), V3, tagNameV3);
            WriteSingleDataToCSV(time.ToArray(), V4, tagNameV4);
            WriteSingleDataToCSV(time.ToArray(), V5, tagNameV5);
            WriteSingleDataToCSV(time.ToArray(), V6, tagNameV6);

            if (useSubplots)
            {
            }
            else
            {

                if (plotAllOnLeftYaxis)
                {
                    Start(chromePath,
                        @"-r " + plotlyPath + "#" + tagNameV1 + ";" + tagNameV2 + ";" + tagNameV3 + ";" + tagNameV4 
                        + ";" + tagNameV5 + ";" + tagNameV6 +  CreateCommentStr(comment), out bool returnVal) ;
                }
            }
        }

        ///<summary>
        ///  Plot any number of variables,by giving values and names by lists (preferred)
        ///  
        ///  If you want to plot mutliple plots with the same variable names, 
        ///  specify a unique casename for each plot.
        ///  
        ///  By setting doStartChrome to false, you can skip opening up chrome, the link to figure
        ///   will instead be returned 
        /// 
        ///</summary>

        public static string FromList(List<double[]> plotValue, List<string> plotNames, 
            int dT_s, string comment = null, DateTime t0 = new DateTime(),
            string caseName="",bool doStartChrome=true)
        {
            if (plotValue == null)
                return "";
            if (plotNames == null)
                return "";
            if (plotValue.Count() == 0)
                return "";
            if (plotNames.Count() == 0)
                return "";

            string command = @"-r " + plotlyPath + "#";
            string plotURL = ""; ;

            var time = InitTimeList(t0, dT_s, plotValue.ElementAt(0).Count());
            /*for (int i = 0; i < plotValue.ElementAt(0).Length; i++)
            {
                time.Add(time.Last().AddSeconds(dT_s));
            }*/

            int j = 0;
            foreach (string plotName in plotNames)
            {
                string ppTagName = PreprocessTagName(caseName + plotName);
                string shortPPtagName = PreprocessTagName(plotName);
                plotURL += shortPPtagName;
                if (j < plotNames.Count-1)//dont add semicolon after last variable
                    plotURL += ";";
                WriteSingleDataToCSV(time.ToArray(), plotValue.ElementAt(j), ppTagName);
                j++;
            }
            plotURL += CreateCommentStr(comment);
            if (caseName.Length > 0)
                plotURL += ";casename:" + caseName;
            if (doStartChrome)
            {
                Start(chromePath, command+plotURL, out bool returnVal);
            }
            return plotURL;

        }


        static private Process Start(string procname, string arguments, out bool returnValue, string comment = null)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = procname;
            proc.StartInfo.Arguments = arguments;
            returnValue = proc.Start();
            return proc;
        }


        // plotly interface used to plot csv expects 
        // - first row to be time in unix time
        // - ";" column separator
        static private void WriteSingleDataToCSV(DateTime[] time, double[] data, string tagName, string comment = null)
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

            // add data. 
            if (data != null)
            {
                for (int row = 0; row < data.Count(); row++)
                {
                    if (time != null)
                    {
                        if (time.Length > row)
                        {
                            sb.Append(UnixTime.ConvertToUnixTimestamp(time.ElementAt(row)));
                        }
                    }
                    sb.Append(CSVseparator);
                    sb.Append(data[row].ToString(CultureInfo.InvariantCulture));
                    // sb.Append(CSVseparator);
                    sb.Append("\r\n");
                }
            }
            string fileName = plotDataPath + tagName + ".csv";

            using (StringToFileWriter writer = new StringToFileWriter(fileName))
            {
                writer.Write(sb.ToString());
                writer.Close();
            }

        }


    }
}        
