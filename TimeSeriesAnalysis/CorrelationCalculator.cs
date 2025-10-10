using Accord.Math;
using Accord.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Holds the result of a correlation calcultion between two vectors
    /// </summary>
    public class CorrelationObject
    {
        /// <summary>
        /// Name of the signal
        /// </summary>
        public string signalName;
        /// <summary>
        /// a correlation factor betweeen -1 and 1 indicating the degree of correlation
        /// </summary>
        public double correlationFactor;
        /// <summary>
        /// Time constant (1. order) between the two vectors
        /// </summary>
        public double? timeConstant_s;
        /// <summary>
        /// Time delay in seconds between the two vectors
        /// </summary>
        public double? timeDelay_s;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="timeConstant_s"></param>
        /// <param name="timeDelay_s"></param>
        public CorrelationObject(string name,double value, double? timeConstant_s = null, double? timeDelay_s=null)
        {
            signalName = name;
            correlationFactor = value;
            this.timeConstant_s = timeConstant_s;
            this.timeDelay_s = timeDelay_s;
        }
    }

    /// <summary>
    /// Class that performns correlations between vectors
    /// </summary>
    public class CorrelationCalculator
    {
        /// <summary>
        /// Determine the correlation betweeen vectors v1 and v2
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="indicesToIgnore"></param>
        /// <returns></returns>
        public static double CorrelateTwoVectors(double[] v1, double[] v2, List<int> indicesToIgnore)
        {

            var array2d = Array2D<double>.CreateJaggedFromList(new List<double[]> { v1, v2 }, indicesToIgnore);
            double[,] matrix = Array2D<double>.Created2DFromJagged(array2d).Transpose();

            double[,] corrMatrix = Measures.Correlation(matrix);
            double[] corr = corrMatrix.GetColumn(0);

            return corr[1];
        }



        /// <summary>
        /// Calculates correlation factors [-1,1] for a signal against all other signals in the dataset
        /// (this corresponds to one row or one column of the covariance matrix)
        /// </summary>
        /// <param name="signalName"></param>
        /// <param name="dataSet"></param>
        /// <param name="indicesToIgnore"> these indices are ignored in the calculation (pre-filtered bad or out of range values)</param>
        /// <returns></returns>
        public static Dictionary<string,double> Calculate(string signalName, TimeSeriesDataSet dataSet, List<int> indicesToIgnore= null)
        {
            Dictionary<string, double> returnCorrs= new Dictionary<string, double>();
            var vec = new Vec();
            var mainSignal = dataSet.GetValues(signalName);

            (double[,] matrix, string[] signalNames) = dataSet.GetAsMatrix(indicesToIgnore);
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
        /// Calculate the correlation factor between signal1 and signal 2.
        /// </summary>
        /// <param name="signal1"></param>
        /// <param name="signal2"></param>
        /// <param name="indicesToIgnore">Optionally ignore indices in this list from signal1 and signal2 </param>
        /// <returns></returns>
        public static double Calculate(double[] signal1, double[] signal2, List<int> indicesToIgnore=null)
        {
            var signal1_filt = Vec<double>.GetValuesExcludingIndices(signal1, indicesToIgnore);
            var signal2_filt = Vec<double>.GetValuesExcludingIndices(signal2, indicesToIgnore);

            var matrix = Array2D<double>.CreateFromList(new List<double[]> { signal1,signal2});
            double[,] corrMatrix = Measures.Correlation(matrix);
            return corrMatrix.GetColumn(0)[1];
        }



        /// <summary>
        /// Calculates correlation factors [-1,1] for a signal against all other signals in the dataset
        /// returning the results in a list from highest to lowest score _absolute_ correlation factor
        /// </summary>
        /// <param name="mainSignalName"></param>
        /// <param name="dataSet">the dataset, which must have a correctly set timestamps in order to estimate time constants(also consider indicesToIgnore!)</param>
        /// <param name="minimumCorrCoeffToDoTimeshiftCalc">calculate time-shift for every corr coeff with absolute value that is above this threshold(0.0-1.0)</param>
        /// <param name="minimumFitScore">for a time-shift to be valid, the resulting model nees to have Rsq over this threshold</param>
        /// <returns>a</returns>
        public static List<CorrelationObject> CalculateAndOrder(string mainSignalName, TimeSeriesDataSet dataSet,
             double minimumCorrCoeffToDoTimeshiftCalc=0.4,double minimumFitScore = 10)
        {
            (double?,double?) EstimateTimeShift(double[] signalIn, double[] signalOut,List<int> indToIgnore )
            {
                var dataSetUnit = new UnitDataSet();
                dataSetUnit.Y_meas = signalOut;
                dataSetUnit.U = Array2D<double>.CreateFromList(new List<double[]> { signalIn });
                dataSetUnit.CreateTimeStamps(dataSet.GetTimeBase());
                dataSetUnit.IndicesToIgnore = indToIgnore;
                var identModel = UnitIdentifier.Identify(ref dataSetUnit);
                if (identModel.modelParameters.Fitting.WasAbleToIdentify && identModel.modelParameters.Fitting.FitScorePrc > minimumFitScore)
                {
                    return (Math.Round(identModel.modelParameters.TimeConstant_s),
                        Math.Round(identModel.modelParameters.TimeDelay_s));
                }
                else
                    return (null,null);
            }

            List<CorrelationObject> ret = new List<CorrelationObject>();

            (double[,] matrix, string[] signalNames) = dataSet.GetAsMatrix(dataSet.GetIndicesToIgnore());
            // not found in datset.
            if (!signalNames.Contains<string>(mainSignalName))
                return ret;

            var indice = Vec<string>.GetIndicesOfValue(mainSignalName,signalNames.ToList()).First();

            var mainSignalValues = dataSet.GetValues(mainSignalName);
            // signal was not found in the dataset.
            if (mainSignalValues == null)
                return ret;

            double[,] corrMatrix = Measures.Correlation(matrix);
            double[] corr = corrMatrix.GetColumn(indice);
            double[] sortedValues = Vec<double>.Sort((new Vec()).Abs(corr), VectorSortType.Descending,out int[] sortIdx);

            for (int i=0; i < sortedValues.Length; i++)
            {
                string curSignalName = signalNames[sortIdx[i]];
                double curCorrCoef = corr[sortIdx[i]];

                double? timeConstant_s = null;
                double? timeDelay_s = null;

                if (curSignalName != mainSignalName && dataSet.GetTimeBase() != 0
                    && Math.Abs(curCorrCoef) > minimumCorrCoeffToDoTimeshiftCalc)
                {
                    double[] curSignalValues = dataSet.GetValues(curSignalName);
                    if (curSignalValues != null)
                    {
                        (timeConstant_s,timeDelay_s) = EstimateTimeShift(curSignalValues,mainSignalValues,dataSet.GetIndicesToIgnore());
                    }
                }
                ret.Add(new CorrelationObject(curSignalName, curCorrCoef,timeConstant_s, timeDelay_s)); 
            }
            return ret;
        }




    }
}
