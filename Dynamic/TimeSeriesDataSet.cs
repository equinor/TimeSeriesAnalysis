using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// A  class that holds time-series data for a number of tags
    /// </summary>
    public class TimeSeriesDataSet
    {
        int timeBase_s;
        Dictionary<string, double[]> dataset;
        int? N;
        bool didSimulationReturnOk = false;

        private void Init(int timeBase_s)
        {
            this.timeBase_s = timeBase_s;
            dataset = new Dictionary<string, double[]>();
            didSimulationReturnOk = false;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeBase_s"></param>
        public TimeSeriesDataSet(int timeBase_s)
        {
            Init(timeBase_s);
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeBase_s"></param>
        /// <param name="inputDataSet"></param>
        public TimeSeriesDataSet(int timeBase_s, TimeSeriesDataSet inputDataSet)
        {
            Init(timeBase_s);
            AddSet(inputDataSet); 
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

                bool isOk = AddTimeSeries(signalName, values);
                if (!isOk)
                    return false;
            }
            return true;
        }


        /// <summary>
        /// Add an entire time-series to the dataset
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool AddTimeSeries(string signalName, double[] values)
        {
            if (signalName == null)
            {
                return false;
            }
            if (dataset.ContainsKey(signalName))
            {
                return false;
            }
            N = values.Length;
            dataset.Add(signalName, values);
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

            bool isOk = AddTimeSeries(signalName, values);
            if (isOk)
                return signalName;
            else
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


    }
}
