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

namespace TimeSeriesAnalysis
{


    /// <summary>
    /// A class that holds time-series data for any number of tags 
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
        int? N;

        [JsonConstructor]
        public TimeSeriesDataSet()
        {
            dataset = new Dictionary<string, double[]>();
            dataset_constants = new Dictionary<string, double>();
            //didSimulationReturnOk = false;
        }
        /// <summary>
        /// Constructor
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
            N = inputDataSet.N;
            timeStamps = inputDataSet.timeStamps;
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
                if (values.Length > 1)
                {
                    N = values.Length;
                }
            }
            dataset.Add(signalName, values);
            return true;
        }

        /// <summary>
        /// Add a constant value to the time-series to the dataset
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="values"></param>
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

        private void Fill(DateTime[] dateTimes, Dictionary<string, double[]> variableDict)
        {
            if (variableDict.ContainsKey("Time"))
            {
                variableDict.Remove("Time");
            }
            if (variableDict.ContainsKey("time"))
            {
                variableDict.Remove("time");
            }
            dataset = variableDict;
            N = dataset[dataset.Keys.First()].Length;
            if (dateTimes.Length > 1)
            {
                timeStamps = dateTimes.ToList();
            }
        }


        public double GetTimeBase()
        {
            if (timeStamps.Count > 2)
            {
                return timeStamps[2].Subtract(timeStamps[1]).TotalSeconds;
            }
            else
                return 0;
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
                Fill(dateTimes, variableDict);
            }
            return isOK;
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
            TimeSeriesDataSet dataSet = new TimeSeriesDataSet(this);
            foreach (string signalName in inputDataSet.GetSignalNames())
            {
                double[] values = inputDataSet.GetValues(signalName);
                N = values.Length;// todo:check that all are equal length

                bool isOk = dataSet.Add(signalName, values);
            }
            if (inputDataSet.GetTimeStamps() != null)
            {
                dataSet.SetTimeStamps(inputDataSet.GetTimeStamps().ToList());
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
        /// <param name="signalName"></param>
        /// <param name="noiseAmplitude"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        public bool AddNoiseToSignal(string signalName, double noiseAmplitude, int? seed = null)
        {
            if (!dataset.ContainsKey(signalName))
                return false;

            if (seed == null)
            {
                seed = 0;
            }
            dataset[signalName] = new Vec().Add(dataset[signalName], Vec.Rand(N.Value, -noiseAmplitude, noiseAmplitude, seed));
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
            return dataset.ContainsKey(signalID);
        }


        /// <summary>
        /// Returns a copy of the dataset that is downsampled by the given factor
        /// </summary>
        /// <param name="downsampleFactor">value greater than 1 indicating that every nth value of the orignal data will be transferred</param>
        /// <returns></returns>
        public TimeSeriesDataSet CreateDownsampledCopy(int downsampleFactor)
        {
            TimeSeriesDataSet ret = new TimeSeriesDataSet();

            ret.timeStamps = Vec<DateTime>.Downsample(timeStamps.ToArray(), downsampleFactor).ToList();
            ret.N = ret.timeStamps.Count();
            ret.dataset_constants = dataset_constants;
            foreach (var item in dataset)
            {
                ret.dataset[item.Key] = Vec<double>.Downsample(item.Value, downsampleFactor);
            }
            return ret;
        }

        /// <summary>
        /// Creates internal timestamps from a given start time and timebase, must be called after filling the values 
        /// </summary>
        /// <param name="timeBase_s">the time between samples in the dataset, in total seconds</param>
        /// <param name="t0">start time, can be null, which can be usedful for testing</param>
        public void CreateTimestamps(double timeBase_s, DateTime? t0 = null)
        {
            if (t0 == null)
            {
                t0 = new DateTime(2010, 1, 1);//intended for testing
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
        /// Get all signals in the dataset as a matrix
        /// </summary>
        /// <returns>the signals as a 2d-matrix, and the an array of strings with corresponding signal names</returns>
        public (double[,], string[]) GetAsMatrix()
        {
            List<double[]> listOfVectors = (List<double[]>)dataset.Values.ToList();
            double[][] jagged = Array2D<double>.CreateJaggedFromList(listOfVectors);
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
                    if (timeIdx > dataset[signalName].Count())
                    {
                        return null;
                    }
                    retData[valueIdx] = dataset[signalName][timeIdx];
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
        /// </summary>
        /// <returns></returns>
        public int? GetLength()
        {
            return N;
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
        /// <returns></returns>
        public double[] GetValues(string signalName)
        {
            if (signalName == null)
                return null;
            if (dataset.ContainsKey(signalName))
                return dataset[signalName];
            else
            {
                Shared.GetParserObj().AddError("TimeSeriesData.GetValues() did not find signal:" + signalName);
                return null;
            }
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
                Fill(dateTimes, variableDict);
            }
            return isOk;
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
        /// Set the timestamp of the start of the dataset,from which other time stamps can be found using timebase_s
        /// </summary>
        /// <param name="t0"></param>
       /* public void SetT0(DateTime t0)
        {
            this.t0 = t0;
        }
        */

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
