using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;

using Newtonsoft.Json;

using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Dynamic;
using Newtonsoft.Json.Linq;
using System.ComponentModel.Design;

namespace TimeSeriesAnalysis
{


    /// <summary>
    /// A class that holds time-series data for any number of tags, and with built in support for serlalizing/deserializing to a csv-file.
    /// <para>
    /// Time is either treated by giving a timeBase in seconds and a starting time, or by  
    /// specifying a vector of timestamps.
    /// </para>
    /// </summary>
    public class TimeSeriesDataSet
    {
        List<DateTime> timeStamps = new List<DateTime>();
        Dictionary<string, double[]> dataset;
        Dictionary<string, double> dataset_constants;
        List<int> indicesToIgnore;
        string csvFileName = string.Empty;
        /// <summary>
        /// Some systems for storing data do not support "NaN", but instead some other magic 
        /// value is reserved for indicating that a value is bad or missing. 
        /// </summary>
        public double BadDataID { get; set; } = -9999;

        /// <summary>
        /// Default constructor
        /// </summary>
        [JsonConstructor]
        public TimeSeriesDataSet()
        {
            dataset = new Dictionary<string, double[]>();
            dataset_constants = new Dictionary<string, double>();
            indicesToIgnore = null;
        }
        /// <summary>
        /// Constructor that copies another dataset into the returned object
        /// </summary>
        /// <param name="inputDataSet"></param>

        public TimeSeriesDataSet(TimeSeriesDataSet inputDataSet)
        {
            dataset = new Dictionary<string, double[]>();
            if (inputDataSet != null)
            {
                AddSet(inputDataSet);
            }
            dataset_constants = inputDataSet.dataset_constants;
            //N = inputDataSet.N;
            timeStamps = inputDataSet.timeStamps;
            indicesToIgnore = inputDataSet.indicesToIgnore;
        }

        /// <summary>
        /// Constructor that builds a dataset object from a csv-file, by loading LoadFromCsv()
        /// This version of the constructor is useful when receiving the csv-data across an API.
        /// </summary>
        /// <param name="csvContent">the csv data loaded into a CsvContent object</param>
        /// <param name="separator">the separator in the csv-file</param>
        /// <param name="dateTimeFormat">the format of date-time strings in the csv-file</param>
        public TimeSeriesDataSet(CsvContent csvContent, char separator = ';', string dateTimeFormat = "yyyy-MM-dd HH:mm:ss")
        {
            dataset = new Dictionary<string, double[]>();
            dataset_constants = new Dictionary<string, double>();
            LoadFromCsv(csvContent, separator, dateTimeFormat);
        }

        /// <summary>
        /// Constructor that builds a dataset object from a csv-file, by loading LoadFromCsv().
        /// If file does not exist, it is created
        /// </summary>
        /// <param name="csvFileName">name of csv-file</param>
        /// <param name="separator">the separator in the csv-file</param>
        /// <param name="dateTimeFormat">the format of date-time strings in the csv-file</param>
        public TimeSeriesDataSet(string csvFileName, char separator = ';', string dateTimeFormat = "yyyy-MM-dd HH:mm:ss")
        {
            dataset = new Dictionary<string, double[]>();
            dataset_constants = new Dictionary<string, double>();
            this.csvFileName = csvFileName;
            if (File.Exists(csvFileName))
                LoadFromCsv(csvFileName, separator, dateTimeFormat);
        //    else
         //       File.Create(csvFileName);
        }

        /// <summary>
        /// Add an entire time-series to the dataset
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool Add(string signalName, double[] values)
        {
            if (values == null)
            {
                return false;
            }
            if (signalName == null)
            {
                return false;
            }
            if (dataset.ContainsKey(signalName))
            {
                return false;
            }
            if (GetLength()>0 && GetLength().HasValue)
            {
                if (GetLength() != values.Length)
                {
                    dataset.Add(signalName, values);
                    return false;//incorrect size of signal
                }
            }
            /*else
            {
                if (values.Length > 1)
                {
                    N = values.Length;
                }
            }*/
            dataset.Add(signalName, values);
            return true;
        }

        /// <summary>
        /// Add a constant value to the time-series to the dataset
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddConstant(string signalName, double value)
        {
            if (signalName == null)
            {
                return false;
            }
            if (dataset_constants.ContainsKey(signalName))
            {
                return false;
            }
            dataset_constants.Add(signalName, value);
            return true;
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
                //N = values.Length;// todo:check that all are equal length

                bool isOk = Add(signalName, values);
                if (!isOk)
                    return false;
            }
            return true;
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
                if (dataset[signalID].Length > idx)
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
        /// Adds noise to a given signal in the datset. 
        /// (This is mainly intended for testing identification algorithms against simulated data.)
        /// </summary>
        /// <param name="signalName">name of signal to have noise added to it</param>
        /// <param name="noiseAmplitude">the amplutide of noise, the noise will be [-noiseAmplitude, noiseAmplitude] </param>
        /// <param name="seed">a integer seed number is </param>
        /// <returns></returns>
        public bool AddNoiseToSignal(string signalName, double noiseAmplitude, int seed)
        {
            if (!dataset.ContainsKey(signalName))
                return false;

            int N = GetLength().Value;
            dataset[signalName] = new Vec().Add(dataset[signalName], Vec.Rand(N, -noiseAmplitude, noiseAmplitude, seed));
            return true;
        }

        /// <summary>
        /// Append a single value to a given signal.The signal is added if the signal name does not exist.
        /// This method is useful for logging values in a for-loop. Combine with AppendTimeStamp to ensure there is a time-stamp.
        /// If this variable does not exist, then previous values are padded. 
        /// </summary>
        /// <param name="signalName">name of signal</param>
        /// <param name="value">value of signal</param>
        /// <returns></returns>
        public void AppendDataPoint(string signalName, double value)
        {
            if (!dataset.ContainsKey(signalName))
            {
                int? N = GetLength();
                if (N == 0 || !N.HasValue)
                    dataset.Add(signalName, new double[] { value });
                else
                {
                    var valArray = Vec<double>.Fill(Double.NaN, N.Value+1);
                    valArray[N.Value] = value;
                    dataset.Add(signalName, valArray);
                }
            }
            else
            {
                var tempList = new List<double>(dataset[signalName]);
                tempList.Add(value);
                dataset[signalName] = tempList.ToArray();
            }
        }


        /// <summary>
        /// Used in conjunction with AppendDataPoint when appending data to the data set.
        /// Appends a time stamp, and updates the number of data points N, if the timestamp
        /// does not already exist and is newer than the newest time stampe in the curent dataset.
        /// 
        /// </summary>
        /// <returns>true if able to append, otherwise false</returns>
        /// <param name="timestamp"></param>
        public bool AppendTimeStamp(DateTime timestamp)
        {
            if (timeStamps == null)
                timeStamps = new List<DateTime>();
            else if (timeStamps.Count()>0)
            {
                var lastTime = timeStamps.Last();
                if (timestamp <= lastTime)
                    return false;
            }
            timeStamps.Add(timestamp);
            //N = GetLength() + 1;
            return true;
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
            if (dataset.ContainsKey(signalID))
                return true;
            else if (dataset_constants != null)
            {
                if (dataset_constants.ContainsKey(signalID))
                    return true;
                else
                    return false;

            }
            else
                return false;
        }

        /// <summary>
        /// Combine this data set with the inputDataset into a new set
        /// </summary>
        /// <param name="inputDataSet"></param>
        /// <returns>the newly created dataset</returns>
        public TimeSeriesDataSet Combine(TimeSeriesDataSet inputDataSet)
        {
            TimeSeriesDataSet dataSet = new TimeSeriesDataSet(this);
            foreach (string signalName in inputDataSet.GetSignalNames())
            {
                double[] values = inputDataSet.GetValues(signalName);
                //N = values.Length;// todo:check that all are equal length

                bool isOk = dataSet.Add(signalName, values);
            }
            if (inputDataSet.GetTimeStamps() != null)
            {
                dataSet.SetTimeStamps(inputDataSet.GetTimeStamps().ToList());
            }
            return dataSet;
        }




        /// <summary>
        /// Returns a copy of the dataset that is downsampled by the given factor.
        /// </summary>
        /// <param name="downsampleFactor">value greater than 1 indicating that every Floor(n*factor) value of the orignal data will be transferred.</param>
        /// <param name="keyIndex">optional index around which to perform the downsampling.</param>
        /// <returns></returns>
        public TimeSeriesDataSet CreateDownsampledCopy(double downsampleFactor, int keyIndex = 0)
        {
            TimeSeriesDataSet ret = new TimeSeriesDataSet();

            ret.timeStamps = Vec<DateTime>.Downsample(timeStamps.ToArray(), downsampleFactor, keyIndex).ToList();
            ret.dataset_constants = dataset_constants;
            foreach (var item in dataset)
            {
                ret.dataset[item.Key] = Vec<double>.Downsample(item.Value, downsampleFactor, keyIndex);
            }
            return ret;
        }

        /// <summary>
        /// Returns a copy of the dataset that is oversampled by the given factor.
        /// </summary>
        /// <param name="oversampleFactor">value greater than 1 indicating that every value will be sampled until (i / factor > 1).</param>
        /// <param name="keyIndex">optional index around which to perform the oversampling.</param>
        /// <returns></returns>
        public TimeSeriesDataSet CreateOversampledCopy(double oversampleFactor, int keyIndex = 0)
        {
            TimeSeriesDataSet ret = new TimeSeriesDataSet();

            ret.CreateTimestamps(timeBase_s: GetTimeBase() / oversampleFactor, N: (int)Math.Ceiling(GetNumDataPoints() * oversampleFactor), t0: timeStamps[0]);
            ret.dataset_constants = dataset_constants;
            foreach (var item in dataset)
            {
                ret.dataset[item.Key] = Vec<double>.Oversample(item.Value, oversampleFactor, keyIndex);
            }
            return ret;
        }

        /// <summary>
        /// Creates internal timestamps from a given start time and timebase, must be called after filling the values 
        /// </summary>
        /// <param name="timeBase_s">the time between samples in the dataset, in total seconds</param>
        /// <param name="N">number of datapoints</param>
        /// <param name="t0">start time, can be null, which can be usedful for testing</param>
        public void CreateTimestamps(double timeBase_s, int N=0, DateTime? t0 = null)
        {
            if (t0 == null)
            {
                t0 = new DateTime(2010, 1, 1);//intended for testing
            }

            if (N == 0)
            {
                N = dataset.First().Value.Count();
            }
            var times = new List<DateTime>();
            DateTime time = t0.Value;
            for (int i = 0; i < N; i++)
            {
                times.Add(time);
                time = time.AddSeconds(timeBase_s);
            }
            timeStamps = times;
        }


        /// <summary>
        /// Fills a dataset with variables, values and dates, removes "time" or "Time" from variableDict if present, and stores timestamps in internal dateTimes
        /// </summary>
        /// <param name="dateTimes"></param>
        /// <param name="variableDict"></param>
        private bool Fill(DateTime[] dateTimes, Dictionary<string, double[]> variableDict)
        {
            if (dateTimes == null)
                return false;
            if (variableDict == null)
                return false;
            if (variableDict.Keys == null)
                return false;
            if (variableDict.Keys.Count() == 0)
                return false;

            if (variableDict.ContainsKey("Time"))
            {
                variableDict.Remove("Time");
            }
            if (variableDict.ContainsKey("time"))
            {
                variableDict.Remove("time");
            }
            // if the file only included the "time" but no values
            if (variableDict.Keys.Count() == 0)
                return false;

            var N = variableDict[variableDict.Keys.First()].Length;
            if (N == 0)
            {
                return false;
            }
            // load actual dataset
            dataset = variableDict;
            if (dateTimes.Length >= 1)
            {
                timeStamps = dateTimes.ToList();
            }
            else
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Returns the number of data points in the dataset
        /// </summary>
        /// <returns></returns>
        public int GetNumDataPoints()
        {
            if (timeStamps == null)
                return 0;
            return timeStamps.Count;
        }

        /// <summary>
        /// Get the timebase, the average time between two samples in the dataset
        /// </summary>
        /// <returns>The timebase in seconds</returns>
        public double GetTimeBase()
        {
            if (timeStamps.Count > 2)
            {
                int N = GetNumDataPoints();
                return timeStamps[N-1].Subtract(timeStamps[0]).TotalSeconds / (N-1);
            }
            else
                return 0;
        }

        /// <summary>
        /// Get all signals in the dataset as a matrix
        /// </summary>
        /// <returns>the signals as a 2d-matrix, and the an array of strings with corresponding signal names</returns>
        public (double[,], string[]) GetAsMatrix(List<int> indicesToIgnore = null)
        {
            List<double[]> listOfVectors = (List<double[]>)dataset.Values.ToList();
            double[][] jagged = Array2D<double>.CreateJaggedFromList(listOfVectors, indicesToIgnore);
            double[,] ret2D = Array2D<double>.Created2DFromJagged(jagged);
            return (ret2D.Transpose(), dataset.Keys.ToArray());
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
                return double.NaN;
            }
            if (dataset.ContainsKey(signalName))
            {
                if (timeIdx > dataset[signalName].Count() - 1)
                {
                    return null;
                }
                else
                    return dataset[signalName][timeIdx];
            }
            if (dataset_constants.ContainsKey(signalName))
            {
                return dataset_constants[signalName];
            }
            return null;
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
                    retData[valueIdx] = double.NaN;
                    valueIdx++;
                    continue;
                }
                if (!dataset.ContainsKey(signalName) && !dataset_constants.ContainsKey(signalName))
                {
                    return null;
                }
                else if (dataset.ContainsKey(signalName))
                {
                    if (timeIdx >= dataset[signalName].Count())
                    {
                        retData[valueIdx] = double.NaN;
//                        return null;
                    }
                    else
                    {
                        retData[valueIdx] = dataset[signalName][timeIdx];
                    }
                }
                else if (dataset_constants.ContainsKey(signalName))
                {
                    retData[valueIdx] = dataset_constants[signalName];
                }
                valueIdx++;
            }
            return retData;
        }

        /// <summary>
        /// Get the length in samples of the data set
        /// 
        /// The length of the dataset is determined by the number of timestamps.
        /// 
        /// </summary>
        /// <returns>an integer indicating the length of the dataset</returns>
        public int? GetLength()
        {
            return timeStamps.Count();

        }
        /// <summary>
        /// Get the names of all the singals, wheter constant or varying
        /// </summary>
        /// <returns></returns>
        public string[] GetSignalNames()
        {
            var ret = dataset.Keys.ToList();
            if (dataset_constants != null)
            {
                ret.AddRange(dataset_constants.Keys.ToList());
            }
            return ret.ToArray();
        }

        /// <summary>
        /// Get a vector of the timestamps of the data-set, or null if no timestamps
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
            return null;
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
        /// Get one or more signals from the dataset at a given time
        /// </summary>
        /// <param name="signalIds"></param>
        /// <param name="timeIndex"></param>
        /// <returns></returns>
        public double[] GetValuesAtTime(string[] signalIds, int timeIndex)
        {
            double[] retVals = new double[signalIds.Length];

            int index = 0;
            foreach (var inputId in signalIds)
            {
                double? retVal = null;
                retVal = GetValue(inputId, timeIndex);
                if (!retVal.HasValue)
                {
                    retVals[index] = double.NaN;
                }
                else
                {
                    retVals[index] = retVal.Value;
                }
                index++;
            }
            return retVals;
        }


        /// <summary>
        /// Get the values of a specific signal
        /// </summary>
        /// <param name="signalName"></param>
        /// <returns>null if signal not found</returns>
        public double[] GetValues(string signalName)
        {
            if (signalName == null)
                return null;
            if (dataset.ContainsKey(signalName))
                return dataset[signalName];
            else if (dataset_constants.ContainsKey(signalName))
            {
                var N = GetLength();
                return Vec<double>.Fill(dataset_constants[signalName], N.Value);
            }
            else
            {
                Shared.GetParserObj().AddError("TimeSeriesDataSet.GetValues() did not find signal:" + signalName);
                return null;
            }
        }

        /// <summary>
        /// Get a list of the indices in the dataset that are flagged to be ignored in identification
        /// </summary>
        /// <returns>empty list if no indices, otherwise a list of indices (never returns null)</returns>
        public List<int> GetIndicesToIgnore()
        {
            if (indicesToIgnore == null)
                return new List<int>();
            return indicesToIgnore;
        }

        /// <summary>
        /// Define a new signal, specifying only its inital value
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="initalValue">the value of time zero</param>
        /// <param name="N">number of time stamps</param>
        /// <param name="nonYetSimulatedValue">what value to fill in for future undefined times, default:nan</param>
        public void InitNewSignal(string signalName, double initalValue, int N, double nonYetSimulatedValue = double.NaN)
        {
            Add(signalName, Vec<double>.Concat(new double[] { initalValue },
                Vec<double>.Fill(nonYetSimulatedValue, N - 1)));
        }

        /// <summary>
        /// Determine if internal IndicesToIgnore is "null" i.e. has not been speified
        /// </summary>
        /// <returns>true if IndicesToIgnore is not null, or false it is null</returns>
        public bool IsIndicesToIgnoreSet()
        {
            if (indicesToIgnore == null)
                return false;
            else
                return true;
        }


        /// <summary>
        /// Loads the CsvContent(which can be read from a file) into a TimeSeriesDataSet object
        /// </summary>
        /// <param name="csvContent"></param>
        /// <param name="separator"></param>
        /// <param name="dateTimeFormat"></param>
        /// <returns></returns>
        public bool LoadFromCsv(CsvContent csvContent, char separator = ';', string dateTimeFormat = "yyyy-MM-dd HH:mm:ss")
        {
            bool isOK = CSV.LoadDataFromCsvContentAsTimeSeries(csvContent, separator, out DateTime[] dateTimes,
                out Dictionary<string, double[]> variableDict, dateTimeFormat);
            if (isOK)
            {
                return Fill(dateTimes, variableDict);
            }
            return false;
        }




        /// <summary>
        ///  Reads data form  a csv-file (such as that created by ToCSV()) 
        /// </summary>
        /// <param name="csvFileName">csv file name</param>
        /// <param name="separator">default separator</param>
        /// <param name="dateTimeFormat">format string of the time-series vector to be read</param>
        /// <returns></returns>
        public bool LoadFromCsv(string csvFileName, char separator = ';', string dateTimeFormat = "yyyy-MM-dd HH:mm:ss")
        {
            bool isOk = CSV.LoadDataFromCsvAsTimeSeries(csvFileName, separator, out DateTime[] dateTimes,
                out Dictionary<string, double[]> variableDict, dateTimeFormat);
            if (isOk)
            {
                return Fill(dateTimes, variableDict);
            }
            return false;
        }

        /// <summary>
        /// Removes a signal from the dataset
        /// </summary>
        /// <param name="signalName"></param>
        /// <returns></returns>
        public bool Remove(string signalName)
        {
            if (dataset.ContainsKey(signalName))
            {
                dataset.Remove(signalName);
                return true;
            }
            else
            {
                return false;
            }
        }



        /// <summary>
        /// The given indices will be skipped in any subsequent simulation of the dataset
        /// </summary>
        /// <param name="indicesToIgnore"></param>
        public void SetIndicesToIgnore(List<int> indicesToIgnore)
        {
            this.indicesToIgnore = indicesToIgnore;
        }

        /// <summary>
        /// Explicitly sets the timestamps of the time-series (possibly overriding any timeBase_s that was given during init)
        /// If times is null, then the method creates timestamps based on timeBase_s and t0.
        /// </summary>
        /// <param name="times"></param>
        public void SetTimeStamps(List<DateTime> times)
        {
            timeStamps = times;
        }
        /// <summary>
        /// Create a copy of the data set that is a "subset", given using start
        /// and end percentages of the original data span.
        /// </summary>
        /// <param name="startPrc"></param>
        /// <param name="endPrc"></param>
        /// <returns></returns>
        public TimeSeriesDataSet SubSetPrc(double startPrc, double endPrc)
        {
            if (startPrc > 100)
                startPrc = 100;
            if (endPrc > 100)
                endPrc = 100;
            if (startPrc < 0)
                startPrc = 0;
            if (endPrc < 0)
                endPrc = 0;

            var N = GetLength();

            int startInd = (int)Math.Floor(startPrc / 100 * N.Value);
            int endInd = (int)Math.Floor(endPrc / 100 * N.Value);
            return SubsetInd(startInd, endInd);
        }

        /// <summary>
        /// Create a copy of the data set that is a "subset", given using start
        /// and end indices of the original data span.
        /// </summary>
        /// <param name="startInd"></param>
        /// <param name="endInd"></param>
        /// <returns></returns>
        public TimeSeriesDataSet SubsetInd(int startInd, int endInd)
        {
            var N = GetLength();

            if (!N.HasValue)
                return null;
            if (endInd > N-1)
                endInd = N.Value - 1;
            if (startInd < 0)
                startInd = 0;

            TimeSeriesDataSet retDataSet = new TimeSeriesDataSet();
            foreach (var constant in dataset_constants)
            {
                retDataSet.AddConstant(constant.Key, constant.Value);
            }
            foreach (var signalName in GetSignalNames())
            {
                // do not add constants as "regular signals"
                if (retDataSet.ContainsSignal(signalName))
                    continue;
                var values = GetValues(signalName);
                if (values != null)
                {
                    var copy = values.SubArray(startInd, endInd);
                    retDataSet.Add(signalName, copy);
                }
            }

            if (timeStamps != null)
            {
                DateTime[] timeStampsArray = (DateTime[])timeStamps.ToArray<DateTime>();
                retDataSet.SetTimeStamps((Vec<DateTime>.SubArray(timeStampsArray, startInd, endInd)).ToList());
            }
            return retDataSet;
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

            for (int curTimeIdx = 0; curTimeIdx < GetLength(); curTimeIdx++)
            {
                DateTime curDate = timeStamps.ElementAt(curTimeIdx); ;
                var dataAtTime = GetData(signalNames, curTimeIdx);
                //sb.Append(UnixTime.ConvertToUnixTimestamp(curDate));
                sb.Append(curDate.ToString("yyyy-MM-dd HH:mm:ss"));

                for (int curColIdx = 0; curColIdx < dataAtTime.Length; curColIdx++)
                {
                    sb.Append(csvSeparator + SignificantDigits.Format(dataAtTime[curColIdx], nSignificantDigits).ToString(CultureInfo.InvariantCulture));
                }
                sb.Append("\r\n");
                //curDate = curDate.AddSeconds(timeBase_s);
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
        /// <returns>true if successful, otherwise false</returns>
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
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Create a dictionary of all dataset values. Constants are padded out to be of N length.
        /// </summary>
        /// <returns>Returns the dataset as a dictionary </returns>
        public Dictionary<string, double[]> ToDict()
        {
            var ret = new Dictionary<string, double[]>(dataset);
            var vec = new Vec();
            var N = GetLength();
            foreach (var constant in dataset_constants)
            {
                if (N.HasValue)
                { 
                    ret.Add(constant.Key, Vec<double>.Fill(constant.Value, N.Value));
                }
            }
            return ret;
        }



        /// <summary>
        ///  When a dataset has been loaded from csv-file and changed, this method writes said changes back to the csv-file. 
        /// </summary>
        /// <returns>true if succesful, otherwise false</returns>
        public bool UpdateCsv()
        {
            return ToCsv(csvFileName);
        }


    }
}
