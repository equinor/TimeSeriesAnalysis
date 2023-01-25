using Accord.Math;
using Accord.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace TimeSeriesAnalysis
{
    public class CorrelationObject
    {
        public string signalName;
        public double correlationFactor;

        public CorrelationObject(string name,double value)
        {
            signalName = name;
            correlationFactor = value;
        }
    }


    public class CorrelationCalculator
    {
        /// <summary>
        /// Calculates correlation factors [-1,1] for a signal against all other signals in the dataset
        /// (this corresponds to one row or one column of the covariance matrix)
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public static Dictionary<string,double> Calculate(string signalName, TimeSeriesDataSet dataSet)
        {
            Dictionary<string, double> returnCorrs= new Dictionary<string, double>();
            var vec = new Vec();
            var mainSignal = dataSet.GetValues(signalName);

            (double[,] matrix, string[] signalNames) = dataSet.GetAsMatrix();
            double[,] corrMatrix = Measures.Correlation(matrix);
            double[] corr = corrMatrix.GetColumn(0);
           
            for (int i=0; i< signalNames.Length; i++)
            {
                
                var otherSignalName = signalNames[i];
                if (otherSignalName == signalName)
                    continue;
                returnCorrs.Add(otherSignalName, corr[i]);
            }
            return returnCorrs;
        }

        /// <summary>
        /// Calculates correlation factors [-1,1] for a signal against all other signals in the dataset
        /// returning the results in a list from highest to lowest score _absolute_ correlation factor
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="dataSet"></param>
        /// <returns>a</returns>
        public static List<CorrelationObject> CalculateAndOrder(string signalName, TimeSeriesDataSet dataSet)
        {
            var mainSignal = dataSet.GetValues(signalName);

            (double[,] matrix, string[] signalNames) = dataSet.GetAsMatrix();
            double[,] corrMatrix = Measures.Correlation(matrix);
            double[] corr = corrMatrix.GetColumn(0);

            double[] sortedValues = Vec<double>.Sort((new Vec()).Abs(corr), VectorSortType.Descending,out int[] sortIdx);

            List<CorrelationObject> ret = new List<CorrelationObject>();

            for (int i=0; i < sortedValues.Length; i++)
            {
                string name = signalNames[sortIdx[i]];
                double value = corr[sortIdx[i]];

                ret.Add(new CorrelationObject(name, value)); 
            
            }
            return ret;
        }




    }
}
