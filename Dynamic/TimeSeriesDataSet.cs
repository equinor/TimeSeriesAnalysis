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

        public double[] GetValues(string processID, SignalType signalType)
        {
            string signalName = GetSignalName(processID, signalType);
            return GetValues(signalName);
        }

        public double[] GetValues(string signalName)
        {
            if (dataset.ContainsKey(signalName))
                return dataset[signalName];
            else
                return null;
        }

        public static string GetSignalName(string modelID, SignalType signalType)
        {
            return modelID + "_" + signalType.ToString();
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


        public string AddTimeSeries(string processID, SignalType type, double[] values )
        {
            string signalName = GetSignalName(processID, type);
            
            bool isOk =  AddTimeSeries(signalName,values);
            if (isOk)
                return signalName;
            else
                return null;
        }

        public double[] GetData(string[] signalNames, int timeIdx)
        {
            double[] retData = new double[signalNames.Length];
            int valueIdx = 0;
            foreach (string signalName in signalNames)
            {
                if (!dataset.ContainsKey(signalName))
                {
                    return null;
                }
                if (timeIdx > dataset[signalName].Count())
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
