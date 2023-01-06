using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis.Utility
{
    public class SigmaXml
    {
        /// <summary>
        /// Load the sigma "cache" file format, often named "cache.js". This file is json file, but
        /// is converted to a xml-file by replacing the header, and is read from javascript in json format and 
        /// is also read from c# in the xml-converted format. The data has both a "cache" and an "eventdata" portion,
        /// but this driver reads only the cache format where the time-series data is usually stored.
        /// </summary>
        /// <param name="xmlFileName"></param>
        public static (TimeSeriesDataSet,int) LoadFromFile(string xmlFileName)
        {
            TimeSeriesDataSet dataset = new TimeSeriesDataSet();
            int nErrors = 0;

            if (System.IO.File.Exists(xmlFileName) == true)
            {
                string fingerPrintFileContent = System.IO.File.ReadAllText(xmlFileName, Encoding.UTF8);
                // Remove eventData
                int indexEventData = fingerPrintFileContent.IndexOf("eventData");
                string cacheContent = fingerPrintFileContent.Substring(0, indexEventData);

                cacheContent = Regex.Replace(cacheContent, @"^.*,,.*$", delegate (Match match)
                {
                    string v = match.ToString();
                    string result = "";
                    return result;
                }, RegexOptions.Multiline);

                cacheContent = Regex.Replace(cacheContent, @"^.*\[,.*$", delegate (Match match)
                {
                    string result = "";
                    return result;
                }, RegexOptions.Multiline);

                cacheContent = Regex.Replace(cacheContent, @"^.*,\].*$", delegate (Match match)
                {
                    string result = "";
                    return result;
                }, RegexOptions.Multiline);

                string fingerPrintFileContentMod = cacheContent.Replace("cacheContent = [", "{\"?xml\": {\"@version\": \"1.0\",\"@standalone\": \"no\"},  \"root\": {\"textContent\" : [") + "}";

                XmlDocument xmlConfig = (XmlDocument)JsonConvert.DeserializeXmlNode(fingerPrintFileContentMod);

                XDocument xConfigOrg = XDocument.Load(xmlConfig.CreateNavigator().ReadSubtree());


                double Ts = 1;


                foreach (XElement element in xConfigOrg.Root.Elements())
                {
                    string key = element.Elements().ElementAt(0).Value;
               //     string value = element.Elements().ElementAt(1).Value;

                    if ((key != "Time") && (key != "Dmmy") && (key != "UnixTime")) 
                    {
                        try
                        {
                            List<double> results = new List<double>();

                            foreach (var val in element.Elements().ElementAt(1).Elements())
                            {
                                results.Add(Convert.ToDouble(val.Value,CultureInfo.InvariantCulture));
                            }
                           // double[] results = element.Elements().ElementAt(1).Elements().Select(row => Convert.ToDouble(row.Value)).ToArray();
                            if (dataset.ContainsSignal(key) == false)
                            {
                                // some "special" variables are scalars containting meta-data, do not add these
                                if (results.Count > 1)
                                {
                                    dataset.Add(key, results.ToArray());
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            nErrors++;
                        }
                    }
                    else if (key == "Time")
                    {
                        DateTime[] results = element.Elements().ElementAt(1).Elements().Select(row => Convert.ToDateTime(row.Value)).ToArray();
                        dataset.SetTimeStamps(results.ToList());
                    }
                }
            }
            return (dataset, nErrors);
        }
    }

}
