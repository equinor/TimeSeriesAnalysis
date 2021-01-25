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
    /*

    TODO: there is a danger that you forget to disable plotting and that plots are active in 
    the release code with the way this is currently coded. 

    There is a need to create some mechanism to disable plots "on-the fly" 
    for instance by a "Plot.Disable()"

    */
    public class Plot
    {
        const string plotDataPath = @"C:\Appl\ProcessDataFramework\www\plotly\Data\";
        const string firefoxPath = @"C:\Program Files\Mozilla Firefox\firefox.exe";
        const string chromePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
        const string plotlyPath = @"localhost\plotly\index.html";

        const bool isEnabled = true;//disable to do "run all unit tests"

        /* public void Disable()
         {
             isEnabled = false;
         }

         public void Enable()
         {
             isEnabled = true;
         }*/

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



        static public void One(double[] X, int dT_s, string tagName = "Var1", string comment=null, DateTime t0 = new DateTime())
        {
            if (!isEnabled)
                return;

            var time = InitTimeList(t0,dT_s, X.Count());
            // 

            WriteSingleDataToCSV(time.ToArray(), X, tagName);

            Start(chromePath,
                @"-r "+ plotlyPath + "#"+tagName + CreateCommentStr(comment), out bool returnVal);
        }

        static public void Two(double[] V1, double[] V2, int dT_s, 
            string tagNameV1 = "Var1",string tagNameV2 = "Var2",
            bool plotAllVarsOnLeftYaxis=true, bool useSubplots= false, string comment = null, DateTime t0 = new DateTime())
        {
            if (!isEnabled)
                return;

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

        static public void Three(double[] V1, double[] V2, double[] V3, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            if (!isEnabled)
                return;

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

        static public void Four(double[] V1, double[] V2, double[] V3,double[] V4, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3", string tagNameV4 = "Var4",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            if (!isEnabled) 
                return;
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

        static public void Five(double[] V1, double[] V2, double[] V3, double[] V4, double[] V5, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3", string tagNameV4 = "Var4", string tagNameV5 = "Var5",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0= new DateTime())
        {
            if (!isEnabled)
                return;

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

        static public void Six(double[] V1, double[] V2, double[] V3, double[] V4, double[] V5, double[] V6, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3", string tagNameV4 = "Var4", string tagNameV5 = "Var5",
            string tagNameV6 = "Var6",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            if (!isEnabled)
                return;

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
