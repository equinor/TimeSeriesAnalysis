using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

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
        public static void LoadFromFile(string xmlFileName)
        {

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
                /*
                if (objProcessDataDictionaryCollection.ContainsKey(remoteApplicationName) == false)
                {
                    objProcessDataDictionaryCollection.Add(remoteApplicationName, new Dictionary<string, double[]>());
                }*/
       
                
                foreach (XElement element in xConfigOrg.Root.Elements())
                {
                    string key = element.Elements().ElementAt(0).Value;
                    string value = element.Elements().ElementAt(1).Value;

                    if ((key != "Time") && (key != "Dmmy")) // SIGMA uses UnixTime
                    {
                        try
                        {
                    /*        double[] results = element.Elements().ElementAt(1).Elements().Select(row => Convert.ToDouble(row.Value)).ToArray();
                            if (objProcessDataDictionaryCollection[remoteApplicationName].ContainsKey(key) == false)
                            {
                                objProcessDataDictionaryCollection[remoteApplicationName].Add(key, results);
                            }
                    */
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }


            }
        }
    }

}
