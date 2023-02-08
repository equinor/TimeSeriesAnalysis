using Accord.Math;
using Accord.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis
{
    public class CorrelationObject
    {
        public string signalName;
        public double correlationFactor;
        public double? timeConstant_s;

        public CorrelationObject(string name,double value, double? timeConstant_s = null)
        {
            signalName = name;
            correlationFactor = value;
            this.timeConstant_s = timeConstant_s;
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
        /// <param name="mainSignalName"></param>
        /// <param name="dataSet">the dataset, which must have a correctly set timestamps in order to estimate time constants</param>
        /// <returns>a</returns>
        public static List<CorrelationObject> CalculateAndOrder(string mainSignalName, TimeSeriesDataSet dataSet)
        {
            const double minimumCorrCoeffToDoTimeshiftCalc = 0.4;
            double? EstiamteTimeShift(double[] signalIn, double[] signalOut)
            {
                const double minimumRsqAbs = 10;


                var dataSetUnit = new UnitDataSet();
                dataSetUnit.Y_meas = signalOut;
                dataSetUnit.U = Array2D<double>.CreateFromList(new List<double[]> { signalIn });
                dataSetUnit.CreateTimeStamps(dataSet.GetTimeBase());
                UnitIdentifier ident = new UnitIdentifier();
                var identModel = ident.Identify(ref dataSetUnit);
                if (identModel.modelParameters.Fitting.WasAbleToIdentify && identModel.modelParameters.Fitting.RsqAbs > minimumRsqAbs)
                {
                    return Math.Round(identModel.modelParameters.TimeConstant_s +
                        identModel.modelParameters.TimeDelay_s);
                }
                else
                    return null;
            }

            List<CorrelationObject> ret = new List<CorrelationObject>();

            (double[,] matrix, string[] signalNames) = dataSet.GetAsMatrix();
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

                double timeConstant_s = Double.NaN;
                if (curSignalName != mainSignalName && dataSet.GetTimeBase() != 0
                    && curCorrCoef > minimumCorrCoeffToDoTimeshiftCalc)
                {
                    double[] curSignalValues = dataSet.GetValues(curSignalName);
                    if (curSignalValues != null)
                    {
                        double? timeShift = EstiamteTimeShift(curSignalValues,mainSignalValues);
                        if (timeShift.HasValue)
                            timeConstant_s = timeShift.Value;
                        else
                        { // check if it is possible to identify model if we reverse the 
                            var timeShiftReversed = EstiamteTimeShift(mainSignalValues,curSignalValues );
                            if (timeShiftReversed.HasValue)
                                timeConstant_s = -1* timeShiftReversed.Value;
                        }
                    }
                }
                ret.Add(new CorrelationObject(curSignalName, curCorrCoef,timeConstant_s)); 
            }
            return ret;
        }




    }
}
