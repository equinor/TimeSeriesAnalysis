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


        private void Init(int timeBase_s)
        {
            this.timeBase_s = timeBase_s;
            dataset = new Dictionary<string, double[]>();
        }

        public TimeSeriesDataSet(int timeBase_s)
        {
            Init(timeBase_s);
        }
        public TimeSeriesDataSet(int timeBase_s, TimeSeriesDataSet inputDataSet)
        {
            Init(timeBase_s);
            AddSet(inputDataSet); 
        }

        /// <summary>
        /// 
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

        public bool ContainsSignal(string signalID)
        {
            if (signalID == null)
                return false;
            return dataset.ContainsKey(signalID);
        }

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

        public string[] GetSignalNames()
        {
            return dataset.Keys.ToArray();
        }

        public double[] GetValues(string processID, SignalType signalType, int index=0)
        {
            string signalName = SignalNamer.GetSignalName(processID, signalType, index);
            return GetValues(signalName);
        }

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


        public string AddTimeSeries(string processID, SignalType type, double[] values, int index=0 )
        {
            string signalName = SignalNamer.GetSignalName(processID, type,index);
            
            bool isOk =  AddTimeSeries(signalName,values);
            if (isOk)
                return signalName;
            else
                return null;
        }

        /// <summary>
        /// Get Data
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

        public int? GetLength()
        {
            return N;
        }


    }
}
