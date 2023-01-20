using Accord.Math;
using Accord.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace TimeSeriesAnalysis
{
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
    }
}
