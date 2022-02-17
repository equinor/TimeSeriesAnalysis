using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;

using Newtonsoft.Json;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{


    /// <summary>
    /// A class that holds time-series data for any number of tags 
    /// <para>
    /// Time is either treated by giving a timeBase in seconds and a starting time, or by  
    /// specifying a vector of timestamps.
    /// </para>
    /// <remark>
    /// This is the return data class of <seealso cref="PlantSimulator"/>
    /// </remark>
    /// </summary>
    public class TimeSeriesDataSet
    {

        public double timeBase_s { get; set; }
        DateTime t0;

        List<DateTime> timeStamps = new List<DateTime>();

        Dictionary<string, double[]> dataset;
        int? N;
        bool didSimulationReturnOk = false;

        /// <summary>
        /// Constructor: Reads data form  a csv-file (such as that created by ToCSV()) 
        /// </summary>
        /// <param name="fileName">csv file name</param>
        /// <param name="separator">default separator</param>
        /// <param name="dateTimeFormat">format string of the time-series vector to be read</param>
        /// <returns></returns>
        public TimeSeriesDataSet(string fileName, char separator=';',string dateTimeFormat = "yyyy-MM-dd HH:mm:ss")
        {
            CSV.LoadDataFromCsvAsTimeSeries(fileName, separator, out DateTime[] dateTimes, 
                out Dictionary<string, double[]> variableDict, dateTimeFormat);

            CommonInit(dateTimes, variableDict);
        }

        public TimeSeriesDataSet(CsvContent csvContent, char separator = ';', string dateTimeFormat = "yyyy-MM-dd HH:mm:ss")
        {
            CSV.LoadDataFromCsvContentAsTimeSeries(csvContent, separator, out DateTime[] dateTimes, 
                out Dictionary<string, double[]> variableDict, dateTimeFormat);

            CommonInit(dateTimes, variableDict);
        }


        private void CommonInit(DateTime[] dateTimes, Dictionary<string, double[]> variableDict)
        {
            didSimulationReturnOk = true;
            if (variableDict.ContainsKey("Time"))
            {
                variableDict.Remove("Time");
            }
            if (variableDict.ContainsKey("time"))
            {
                variableDict.Remove("time");
            }
            dataset = variableDict;
            N = (int?)dataset[dataset.Keys.First()].Length;
            if (dateTimes.Length > 1)
            {
                timeBase_s = (int)dateTimes[1].Subtract(dateTimes[0]).TotalSeconds;
                timeStamps = dateTimes.ToList();
            }
            else
            {
                timeBase_s = 0;
            }
        }


        static public TimeSeriesDataSet FromCsvText(string csvText)
        {
            var ret = new TimeSeriesDataSet();

//            LoadDataFromCSVstring


            return ret;
        }


        public TimeSeriesDataSet()
        { 
        
        }


        [JsonConstructor]
        public TimeSeriesDataSet(double timeBase_s)
        {
            Init(timeBase_s);
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inputDataSet"></param>

        public TimeSeriesDataSet(TimeSeriesDataSet inputDataSet)
        {
            timeBase_s = inputDataSet.timeBase_s;
            Init(timeBase_s);
            if (inputDataSet != null)
            {
                AddSet(inputDataSet);
            }
        }


        /// <summary>
        /// Add an entire time-series to the dataset
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool Add(string signalName, double[] values)
        {
            if (signalName == null)
            {
                return false;
            }
            if (dataset.ContainsKey(signalName))
            {
                return false;
            }
            if (N.HasValue)
               {
                   if (N != values.Length)
                   {
                       dataset.Add(signalName, values);
                       return false;//incorrect size of signal
                   }
               }
            else
            {
                N = values.Length;
            }
            dataset.Add(signalName, values);
            SetTimeStamps();
            return true;
        }


        /// <summary>
        /// Add an entire time-series to the dataset, without specifying the signalID explicitly
        /// </summary>
        /// <param name="processID"></param>
        /// <param name="type"></param>
        /// <param name="values"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public string AddTimeSeries(string processID, SignalType type, double[] values, int index = 0)
        {
            string signalName = SignalNamer.GetSignalName(processID, type, index);

            bool isOk = Add(signalName, values);
            if (isOk)
                return signalName;
            else
                return null;
        }

        /// <summary>
        /// Adds all signals in a given set to this set
        /// </summary>
        /// <param name="inputDataSet"></param>
        /// <returns></returns>
        public bool AddSet(TimeSeriesDataSet inputDataSet)
        {
            foreach (string signalName in inputDataSet.GetSignalNames())
            {
                double[] values = inputDataSet.GetValues(signalName);
                N = values.Length;// todo:check that all are equal length

                bool isOk = Add(signalName, values);
                if (!isOk)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Combine this data set with the inputDataset into a new set
        /// </summary>
        /// <param name="inputDataSet"></param>
        /// <returns>the newly created dataset</returns>
        public TimeSeriesDataSet Combine(TimeSeriesDataSet inputDataSet)
        {
            TimeSeriesDataSet dataSet = new TimeSeriesDataSet(this) ;
            foreach (string signalName in inputDataSet.GetSignalNames())
            {
                double[] values = inputDataSet.GetValues(signalName);
                N = values.Length;// todo:check that all are equal length

                bool isOk = dataSet.Add(signalName, values);
            }
            return dataSet;
        }



        /// <summary>
        /// Add a single data point
        /// </summary>
        /// <param name="signalID"></param>
        /// <param name="idx"></param>
        /// <param name="value"></param>
        /// <returns>returns false if signal does not already exist or if index is beyond dataset size</returns>
        public bool AddDataPoint(string signalID, int idx, double value)
        {
            if (ContainsSignal(signalID))
            {
                if (dataset[signalID].Length >idx)
                {
                    dataset[signalID][idx] = value;
                    return true;
                }
                else
                    return false;

            }
            else
                return false;
        
        }

        /// <summary>
        /// Determine if a specific signal is in the dataset
        /// </summary>
        /// <param name="signalID"></param>
        /// <returns></returns>
        public bool ContainsSignal(string signalID)
        {
            if (signalID == null)
                return false;
            return dataset.ContainsKey(signalID);
        }



        /// <summary>
        /// Returns the simualtion status
        /// </summary>
        /// <returns></returns>
        public bool GetSimStatus()
        {
            return didSimulationReturnOk;
        }


        /// <summary>
        /// Get Data for multiple signals at a specific time index
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="timeIdx"></param>
        /// <returns>May return null if an error occured</returns>
        public double? GetValue(string signalName, int timeIdx)
        {
            if (signalName == null)
            {
                return Double.NaN;
            }
            if (!dataset.ContainsKey(signalName))
            {
                return null;
            }
            else if (timeIdx > dataset[signalName].Count())
            {
                return null;
            }
            return dataset[signalName][timeIdx];
        }


        /// <summary>
        /// Get Data for multiple signals at a specific time index
        /// </summary>
        /// <param name="signalNames"></param>
        /// <param name="timeIdx"></param>
        /// <returns>May return null if an error occured</returns>
        public double[] GetData(string[] signalNames, int timeIdx)
        {
            double[] retData = new double[signalNames.Length];
            int valueIdx = 0;
            foreach (string signalName in signalNames)
            {
                if (signalName == null)
                {
                    retData[valueIdx] = Double.NaN;
                    valueIdx++;
                    continue;
                }
                if (!dataset.ContainsKey(signalName))
                {
                    return null;
                }
                else if (timeIdx > dataset[signalName].Count())
                {
                    return null;
                }
                retData[valueIdx] = dataset[signalName][timeIdx];
                valueIdx++;
            }
            return retData;
        }

        /// <summary>
        /// Get the length in samples of the data set
        /// </summary>
        /// <returns></returns>
        public int? GetLength()
        {
            return N;
        }
        /// <summary>
        /// Get the names of all the singals
        /// </summary>
        /// <returns></returns>
        public string[] GetSignalNames()
        {
            return dataset.Keys.ToArray();
        }

        /// <summary>
        /// Get a vector of the timestamps of the data-set
        /// </summary>
        /// <returns></returns>
        public DateTime[] GetTimeStamps()
        {
            if (timeStamps != null)
            {
                if (timeStamps.Count() > 0)
                {
                    return timeStamps.ToArray();
                }
            }
            // create default timestamps based on timeBase and t0
            SetTimeStamps();

            return timeStamps.ToArray();
        }


        /// <summary>
        /// Get the values of a specific signal
        /// </summary>
        /// <param name="processID"></param>
        /// <param name="signalType"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public double[] GetValues(string processID, SignalType signalType, int index = 0)
        {
            string signalName = SignalNamer.GetSignalName(processID, signalType, index);
            return GetValues(signalName);
        }

        /// <summary>
        /// Get the values of a specific signal
        /// </summary>
        /// <param name="signalName"></param>
        /// <returns></returns>
        public double[] GetValues(string signalName)
        {
            if (dataset.ContainsKey(signalName))
                return dataset[signalName];
            else
            {
                Shared.GetParserObj().AddError("TimeSeriesData.GetValues() did not find signal:" + signalName);
                return null;
            }
        }

        private void Init(double timeBase_s,DateTime? t0=null)
        {
            this.timeBase_s = timeBase_s;
            if (t0.HasValue)
                this.t0 = t0.Value;
            else
            {
                this.t0 = DateTime.Now;
            }
            dataset = new Dictionary<string, double[]>();
            didSimulationReturnOk = false;
        }

        /// <summary>
        /// Set the termination status of the simualtion
        /// </summary>
        /// <param name="didSimulationTerminateOk"></param>
        /// <returns>returns the same status given in</returns>
        public bool SetSimStatus(bool didSimulationTerminateOk)
        {
            didSimulationReturnOk = didSimulationTerminateOk;
            return didSimulationReturnOk;
        }

        /// <summary>
        /// Explicitly sets the timestamps of the time-series (possibly overriding any timeBase_s that was given during init)
        /// If times is null, then the method creates timestamps based on timeBase_s and t0.
        /// </summary>
        /// <param name="times"></param>
        public void SetTimeStamps(List<DateTime> times=null)
        {
            void CreateTimestamps()
            {
                times = new List<DateTime>();
                times.Add(t0);
                DateTime time = t0;
                for (int i = 1; i < N; i++)
                {
                    times.Add(time);
                    time = time.AddSeconds(timeBase_s);
                }
                this.timeStamps = times;
                return;
            }

            if (times == null)
            {
                if (this.timeStamps == null)
                {
                    CreateTimestamps();
                } else if (this.timeStamps.Count() == 0)
                {
                    CreateTimestamps();
                }
                else
                {
                    return;
                }
               
            }
            this.timeStamps = times;
            return;

        }


        /// <summary>
        /// Set the timestamp of the start of the dataset,from which other time stamps can be found using timebase_s
        /// </summary>
        /// <param name="t0"></param>
        public void SetT0(DateTime t0)
        {
            this.t0 = t0;
        }


        /// <summary>
        /// Create a comma-separated-variable(CSV) string of the dataset
        /// </summary>
        /// <param name="csvSeparator">symbol used to separate columns in the string</param>
        /// <param name="nSignificantDigits">number of significant digits per value</param>
        /// <returns>The CSV-string </returns>
        public string ToCsvText(string csvSeparator = ";", int nSignificantDigits = 5)
        {
            StringBuilder sb = new StringBuilder();
            var signalNames = GetSignalNames();
            // make header
            sb.Append("Time" + csvSeparator);
            sb.Append(string.Join(csvSeparator, signalNames));
            sb.Append("\r\n");

            DateTime curDate = t0;
            for (int curTimeIdx = 0; curTimeIdx < GetLength(); curTimeIdx++)
            {
                var dataAtTime = GetData(signalNames, curTimeIdx);
                //sb.Append(UnixTime.ConvertToUnixTimestamp(curDate));
                sb.Append(curDate.ToString("yyyy-MM-dd HH:mm:ss"));

                for (int curColIdx = 0; curColIdx < dataAtTime.Length; curColIdx++)
                {
                    sb.Append(csvSeparator + SignificantDigits.Format(dataAtTime[curColIdx], nSignificantDigits).ToString(CultureInfo.InvariantCulture));
                }
                sb.Append("\r\n");
                curDate = curDate.AddSeconds(timeBase_s);
            }
            return sb.ToString();

        }


        /// <summary>
        /// Exports the time-series data set to a csv-file
        /// <para>
        /// Times are encoded as "yyyy-MM-dd HH:mm:ss" and be loaded with CSV.LoadFromFile() afterwards
        /// </para>
        /// </summary>
        /// <param name="fileName">The CSV-file name</param>
        /// <param name="csvSeparator">the separator to use in the csv-file(despite the name, the most common is perhaps ";" which Excel will recognize automatically)</param>
        /// <param name="nSignificantDigits">the number of singificant digits to include for each variable</param>
        /// <returns></returns>
        public bool ToCsv(string fileName, string csvSeparator = ";", int nSignificantDigits = 5)
        {
            string csvTxt = ToCsvText(csvSeparator, nSignificantDigits);

            if (!fileName.ToLower().EndsWith(".csv"))
            {
                fileName += ".csv";
            }

            using (StringToFileWriter writer = new StringToFileWriter(fileName))
            {
                try
                {
                    writer.Write(csvTxt);
                    writer.Close();
                }
                catch (Exception)
                {
                    Shared.GetParserObj().AddError("Exception writing file:" + fileName);
                }
            }
            return true;
        }
    }
}
