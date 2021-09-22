using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Statistics.Models.Regression.Fitting;
using System.Globalization;
using System.IO;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Utility functions and operations for treating arrays as mathetmatical vectors
    /// This class considers doubles, methods that require comparisons cannot be easily ported to generic (Vec<T>)
    /// </summary>
    public static class Vec
    {
        private static readonly double nanValue = -9999;// sometimes a special number is used to denote "NaN", -9999 is used in Sigma

        ///<summary>
        /// All checks for NaN will test both for Double.IsNan and if value== a specific "nan" value (-9999)
        ///</summary>
        static private bool IsNaN(double value)
        {
            if (double.IsNaN(value) || value == nanValue)
                return true;
            else
                return false;
        }

        ///<summary>
        ///  Returns maximum value of two array as new array 
        ///</summary>
        public static double[] Max(double[] array1, double[] array2)
        {
            double[] retVal = new double[array1.Length];

            for (int i = 0; i < retVal.Length; i++)
            {
                if (array1[i] > array2[i])
                    retVal[i] = array1[i];
                else
                    retVal[i] = array2[i];
            }
            return retVal;
        }

        ///<summary>
        ///  Returns maximum value of array between indices startInd and endInd
        ///</summary>
        public static double Max(double[] array, int startInd, int endInd)
        {
            double maxVal = double.MinValue;
            for (int i = startInd; i < endInd; i++)
            {
                double thisNum = array[i];
                if (IsNaN(thisNum))
                    continue;
                if (thisNum > maxVal)
                {
                    maxVal = thisNum;
                }
            }
            return maxVal;
        }

        ///<summary>
        ///  Returns minimum value of two array as new array 
        ///</summary>
        public static double[] Min(double[] array1, double[] array2)
        {
            double[] retVal = new double[array1.Length];

            for (int i = 0; i < retVal.Length; i++)
            {
                if (array1[i] > array2[i])
                    retVal[i] = array2[i];
                else
                    retVal[i] = array1[i];
            }
            return retVal;
        }


        ///<summary>
        ///  Returns maximum value of array and index of maximum value 
        ///</summary>
        public static double Max(double[] array, out int ind)
        {
            ind = 0;
            double maxVal = double.MinValue;
            for (int i = 0; i < array.Length; i++)
            {
                double thisNum = array[i];
                if (IsNaN(thisNum))
                    continue;
                if (thisNum > maxVal)
                {
                    maxVal = thisNum;
                    ind = i;
                }
            }
            return maxVal;
        }

        ///<summary>
        ///  Returns element-wise minimum of array element and value
        ///</summary>
        public static double[] Min(double[] array, double value)
        {
            double[] retArray = new double[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                double thisNum = array[i];
                if (IsNaN(thisNum))
                    continue;
                if (thisNum < value)
                {
                    retArray[i] = thisNum;
                }
                else
                {
                    retArray[i] = value;
                }
            }
            return retArray;
        }

        ///<summary>
        ///  Returns element-wise maximum of array element and value
        ///</summary>
        public static double[] Max(double[] array, double value)
        {
            double[] retArray = new double[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                double thisNum = array[i];
                if (IsNaN(thisNum))
                    continue;
                if (thisNum > value)
                {
                    retArray[i] = thisNum;
                }
                else
                {
                    retArray[i] = value;
                }
            }
            return retArray;
        }



        ///<summary>
        ///  Returns minimum value of array and index of maximum value 
        ///</summary>
        public static double Min(double[] array, out int ind)
        {
            ind = 0;
            double minVal = double.MaxValue;
            for (int i = 0; i < array.Length; i++)
            {
                double thisNum = array[i];
                if (IsNaN(thisNum))
                    continue;
                if (thisNum < minVal)
                {
                    minVal = thisNum;
                    ind = i;
                }
            }
            return minVal;
        }
        ///<summary>
        ///  Returns minimum value of array 
        ///</summary>
        public static double Min(double[] array)
        {
            return Min(array, out _);
        }

        ///<summary>
        ///  Returns maximum value of array 
        ///</summary>
        public static double Max(double[] array)
        {
            return Max(array, out _);
        }
        ///<summary>
        ///  Returns range of an array, the difference between minimum and maximum
        ///</summary>
        public static double Range(double[] array)
        {
            double range = Max(array) - Min(array);

            return range;
        }

        ///<summary>
        ///  creates a monotonically increasing integer (11.12.13...) array starting at startValue and ending at endValue
        ///</summary>
        public static int[] MakeIndexArray(int startValue, int endValue)
        {
            List<int> retList = new List<int>();
            for (int i = startValue; i < endValue; i++)
            {
                retList.Add(i);
            }
            return retList.ToArray();
        }

        ///<summary>
        ///  returns the variance of the array (always apositive number)
        ///</summary>
        public static double Var(double[] array1,bool doNormalize=false)
        {
            double retVal = 0;
            double avg = Mean(array1).Value;
            int N = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    continue;
                N++;
                retVal += Math.Pow(array1[i] - avg, 2);
            }
            if (doNormalize)
            {
#pragma warning disable IDE0054 // Use compound assignment
                retVal = retVal / (N);
#pragma warning restore IDE0054 // Use compound assignment
            }
            return retVal;
        }

        ///<summary>
        ///  returns the co-variance of two arrays(interpreted as "vectors")
        ///</summary>
        public static double Cov(double[] array1, double[] array2, bool doNormalize = false)
        {
            double retVal = 0;
            double avg1 = Mean(array1).Value;
            double avg2 = Mean(array2).Value;
            int N = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]) || IsNaN(array2[i]) )
                    continue;
                N++;
                retVal += (array1[i] - avg1) * (array2[i] - avg2);
            }
            if (doNormalize)
            {
#pragma warning disable IDE0054 // Use compound assignment
                retVal = retVal / (N);
#pragma warning restore IDE0054 // Use compound assignment
            }
            return retVal;
        }
        ///<summary>
        /// returns an array which is the elementwise addition of array1 and array2 
        ///</summary>
        public static double[] Add(double[] array1, double[] array2)
        {

            if (array1 == null || array2 == null)
                return null;
            double[] retVal = new double[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    retVal[i] = nanValue;
                else
                {
                    retVal[i] = array1[i] + array2[i];
                }
            }
            return retVal;
        }

        ///<summary>
        /// elementwise addition of val2 to array1
        ///</summary>

        public static int[] Add(int[] array1, int val2)
        {
            if (array1 == null)
                return null;
            int[] retVal = new int[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    retVal[i] = (int)nanValue;
                else
                    retVal[i] = array1[i] + val2;
            }
            return retVal;
        }

        ///<summary>
        /// elementwise addition of val2 to array1
        ///</summary>
        public static double[] Add(double[] array1, double val2)
        {
            if (array1 == null)
                return null;
            double[] retVal = new double[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    retVal[i] = nanValue;
                else
                    retVal[i] = array1[i] + val2;
            }
            return retVal;
        }

        ///<summary>
        /// elementwise multipliation of val2 to array1
        ///</summary>
        public static double[] Mult(double[] array1, double val2)
        {
            if (array1 == null)
                return null;
            double[] retVal = new double[array1.Length];

            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    retVal[i] = nanValue;
                else
                    retVal[i] = array1[i] * val2;
            }
            return retVal;
        }

        ///<summary>
        /// elementwise  multiplication of array1 and array2, assuming they are same size
        ///</summary>

        public static double[] Mult(double[] array1, double[] array2)
        {
            if (array1 == null)
                return null;
            if (array2 == null)
                return null;

            if (array1.Length != array2.Length)
                return null;

            double[] retVal = new double[array1.Length];

            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]) || IsNaN(array2[i]))
                    retVal[i] = nanValue;
                else
                    retVal[i] = array1[i] * array2[i];
            }
            return retVal;
        }

        ///<summary>
        /// elementwise  subtraction of array1 and array2, assuming they are same size
        ///</summary>

        public static double[] Sub(double[] array1, double[] array2)
        {
            if (array1 == null || array2 == null)
                return null;

            double[] retVal = new double[array1.Length];

            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]) || IsNaN(array2[i]))
                    retVal[i] = nanValue;
                else
                    retVal[i] = array1[i] - array2[i];
            }
            return retVal;
        }
        ///<summary>
        /// elementwise subtraction of val2 from array1
        ///</summary>
        public static double[] Sub(double[] array1, double val2)
        {
            if (array1 == null)
                return null;
            double[] retVal = new double[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    continue;
                retVal[i] = array1[i] - val2;
            }
            return retVal;
        }


        ///<summary>
        /// returns an array of the difference between every neighbhoring item in array
        ///</summary>
        public static double[] Diff(double[] array)
        {
            double[] ucur = Vec<double>.SubArray(array, 1);
            double[] uprev = Vec<double>.SubArray(array, 0, array.Length - 2);
            double[] uDiff = Sub(ucur, uprev);
            return Vec<double>.Concat(new double[] { 0 }, uDiff);
        }

        ///<summary>
        /// subtracts val2 from array2 elements
        ///</summary>
        public static int[] Sub(int[] array1, int val2)
        {
            if (array1 == null)
                return null;
            int[] retVal = new int[array1.Length];

            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    continue;
                retVal[i] = array1[i] - val2;
            }
            return retVal;
        }
        ///<summary>
        /// returns an array where each value is the absolute value of array1
        ///</summary>
        public static double[] Abs(double[] array1)
        {
            if (array1 == null)
                return null;
            double[] retVal = new double[array1.Length];

            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    retVal[i] = nanValue;
                else
                    retVal[i] = Math.Abs(array1[i]);
            }
            return retVal;
        }
        ///<summary>
        /// returns the mean value of array1
        ///</summary>
        public static double? Mean(double[] array1)
        {
            if (array1 == null)
                return null;
            double retVal = 0;
            double N = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                if (!IsNaN(array1[i]))
                {
                    N += 1;
                    retVal = retVal * (N - 1) / N + array1[i] * 1 / N;
                }
            }
            return retVal;
        }
        ///<summary>
        /// returns the sum of array1
        ///</summary>
        public static double? Sum(double[] array1)
        {
            if (array1 == null)
                return null;
            double retVal = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    continue;
                retVal += array1[i];
            }
            return retVal;
        }

        /// <summary>
        /// Create a vector of random numbers
        /// </summary>
        /// <param name="N">the number of samples of the returned array</param>
        /// <param name="minValue">lower end of random number range</param>
        /// <param name="maxValue">higher end of random number range</param>
        /// <param name="seed">optionally, give in a seed number, this makes random sequence repeatable</param>
        /// <returns>an array of size N of random numbers between minValue and maxValue </returns>
        public static double[] Rand(int N, double minValue = 0, double maxValue = 1,int? seed=null)
        {
            Random rand;//= null;
            if (seed.HasValue)
            {
                rand = new Random(seed.Value);
            }
            else
            {
                rand = new Random();
            }

            double[] ret = new double[N];
            for (int i = 0; i < N; i++)
            {
                ret[i] = rand.NextDouble() * (maxValue - minValue) + minValue; ;//NextDouble by itself return a valube btw 0 and 1
            }
            return ret;
        }




        /// <summary>
        /// Linear regression: Fit a linear model to Y based on inputs X
        /// </summary>
        /// <param name="Y">one-dimensional vector/array of output paramters y that is to be modelled</param>
        /// <param name="X">two-dimensonal array of input vectors X that are the inputs </param>
        /// <returns> returns the parameters(one for each column in X and a bias term) which best regress the two-dimensional array X into the vector Y. Returns null if regression fails.</returns>
        public static double[] Regress(double[] Y, double[][] X)
        {
            return Regress(Y, X, null, out _, out _, out _, out _);
        }
        ///<summary>
        /// regression where the rows corresponding to indices yIndToIgnore are ignored (bad data identified in preprocessing)
        ///</summary>
        public static double[] Regress(double[] Y, double[][] X, int[] yIndToIgnore, out double[] param95prcConfInterval, out double[] Y_modelled,
            out double Rsq)
        {
            return Regress(Y, X, yIndToIgnore, out param95prcConfInterval, out _, out Y_modelled, out Rsq);
        }

        ///<summary>
        /// regression where the rows corresponding to indices yIndToIgnore are ignored (bad data identified in preprocessing)
        /// uncertainties in parameters, covariance matrix, modelled output y and R-squared number is also given.
        ///</summary>

        public static double[] Regress(double[] Y, double[][] X, int[] yIndToIgnore,
            out double[] param95prcConfInterval, out double[][] varCovarMatrix,
            out double[] Y_modelled, out double Rsq)
        {
            double[] weights = null;
         //   bool areAllWeightsOne = true;
            Rsq = 0;
            if (yIndToIgnore != null)
            {
                weights = Vec<double>.Fill(1, Y.Length);
                for (int i = 0; i < yIndToIgnore.Length; i++)
                {
           //         areAllWeightsOne = false;
                    int curInd = yIndToIgnore[i];
                    if (curInd >= 0 && curInd < weights.Length)
                        weights[curInd] = 0;
                }
            }

            OrdinaryLeastSquares accordFittingAlgo = new OrdinaryLeastSquares()
            {
                IsRobust = false // to use SVD or not.
            };

            MultipleLinearRegression regression;
            double[][] X_T = Accord.Math.Matrix.Transpose(X);

            int theta_Length = X_T[0].Length + 1;
            param95prcConfInterval = null;
            Y_modelled = null;
            varCovarMatrix = new double[theta_Length][];
            //TODO: try to catch rank deficient or singular X instead of generating exception.
            try
            {
                // note: weights have no effect prior to accord 3.7.0 
                regression = accordFittingAlgo.Learn(X_T, Y, weights);
                // modelled Y
                Y_modelled = regression.Transform(X_T);
                if (yIndToIgnore != null)
                {
                    double lastIgnoredInd = -1;
                    double lastGoodValue = -1;
                    for (int i = 0; i < yIndToIgnore.Length; i++)
                    {
                        int curInd = yIndToIgnore[i];
                        if (curInd == lastIgnoredInd + 1)
                            Y_modelled[yIndToIgnore[i]] = lastGoodValue;
                        else
                        {
                            lastIgnoredInd = curInd;
                            lastGoodValue = Y_modelled[yIndToIgnore[i] - 1];
                            Y_modelled[yIndToIgnore[i]] = lastGoodValue;
                        }
                    }
                }

                if (yIndToIgnore != null)
                {
                    Rsq = RSquared(Y_modelled, Y, yIndToIgnore.ToList()) * 100;
                }
                else
                {
                    Rsq = RSquared(Y_modelled, Y) * 100;
                }


/*
                // uncertainty estimation
                if (false)// unceratinty does not take into account weights now?
                {
                    //start: estimating uncertainty
                    try
                    {
                        double[][] informationMatrix = accordFittingAlgo.GetInformationMatrix();// this should already include weights
                        double mse = 0;
                        if (areAllWeightsOne)
                            mse = regression.GetStandardError(X_T, Y);
                        else
                            mse = regression.GetStandardError(X_T, Mult(weights, Y));
                        double[] SE = regression.GetStandardErrors(mse, informationMatrix);

                        for (int i = 0; i < theta_Length; i++)
                        {
                            varCovarMatrix[i] = new double[theta_Length];
                            for (int j = 0; j < theta_Length; j++)
                            {
                                varCovarMatrix[i][j] = mse * Math.Sqrt(Math.Abs(informationMatrix[i][j]));
                            }
                        }
                        param95prcConfInterval = Mult(SE, 1.96);
                    }

                    catch (Exception e)
                    {
                        param95prcConfInterval = null;
                    }
                }*/
                return Vec<double>.Concat(regression.Weights, regression.Intercept);
            }
            catch 
            {
                return null;
            }
        }
        ///<summary>
        /// Returns true f array contains a "-9999" or NaN indicating missing data
        ///</summary>
        public static bool ContainsBadData(double[] x)
        {
            bool doesVectorContainMinus9999 = false;
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] == nanValue)
                {
                    doesVectorContainMinus9999 = true;
                }
            }
            return doesVectorContainMinus9999;
        }

        ///<summary>
        /// Returns true if all elements in array are "-9999" or Double.NaN
        ///</summary>
        public static bool IsAllNaN(double[] array)
        {
            int count = 0;
            while (array[count] == nanValue && count < array.Length - 1)
            {
                count++;
            }
            if (count >= array.Length - 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///<summary>
        /// Returns true if all elements in array are the specific value
        ///</summary>
        public static bool IsAllValue(double[] array, double value = 0)
        {
            int count = 0;
            while (array[count] == value && count < array.Length - 1)
            {
                count++;
            }
            if (count >= array.Length - 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        ///<summary>
        ///  The sum of absolute errors (|a1-a2|) between array1 and array2
        ///</summary>
        public static double SumOfAbsErr(double[] array1, double[] array2, int indexOffset = -1)
        {
            int nGoodValues = 0;
            if (indexOffset == -1)
            {
                indexOffset = array2.Count() - array1.Count();
            }
            double ret = 0;
            for (int i = indexOffset; i < array2.Count(); i++)
            {
                if (Double.IsNaN(array2[i]) || Double.IsNaN(array1[i]) || array2[i] == nanValue || array1[i] == nanValue)
                    continue;
                nGoodValues++;
                ret += Math.Abs(array2[i] - array1[i - indexOffset]);
            }
            if (nGoodValues > 0)
                return ret / nGoodValues;
            else
                return 0;
        }
        ///<summary>
        ///  The sum of absolute errors (a1-a2)^2 between array1 and array2
        ///</summary>
        public static double SumOfSquareErr(double[] ymod, double[] ymeas, int ymodOffset = -1, bool divByN = true)
        {
            if (ymod.Count() < ymeas.Count())
                return Double.NaN;

            if (ymodOffset == -1)
            {
                ymodOffset = ymeas.Count() - ymod.Count();
            }
            double ret = 0;
            int nGoodValues = 0;
            for (int i = ymodOffset; i < ymeas.Count(); i++)
            {
                if (IsNaN(ymeas[i]) || IsNaN(ymod[i]) )
                {
                    continue;
                }
                nGoodValues++;
                ret += Math.Pow(ymeas[i] - ymod[i - ymodOffset], 2);
            }
            if (divByN && nGoodValues > 0)
                return ret / nGoodValues;
            else
                return ret;
        }


        ///<summary>
        /// sum of square error of the vector compared to a constant. by defautl the return value is normalized by dividing by,
        /// this normalization can be turned off
        ///</summary>

        public static double SumOfSquareErr(double[] vec, double constant, bool doNormalization = true)
        {
            double ret = 0;
            for (int i = 0; i < vec.Count(); i++)
            {
                ret += Math.Pow(vec[i] - constant, 2);
            }
            if (doNormalization)
                return ret / vec.Length;
            else
                return ret;
        }


        ///<summary>
        /// sum of square error of the vector compared to itself
        ///</summary>

        public static double SelfSumOfSquareErr(double[] vec)
        {
            return SumOfSquareErr(Vec<double>.SubArray(vec, 1), Vec<double>.SubArray(vec, 0, vec.Length - 2), 0);
        }

        ///<summary>
        /// sum of absolute error of the vector compared to itself
        ///</summary>

        public static double SelfSumOfAbsErr(double[] vec)
        {
            return SumOfAbsErr(Vec<double>.SubArray(vec, 1), Vec<double>.SubArray(vec, 0, vec.Length - 2), 0);
        }

        ///<summary>
        /// return the indices of elements in the array that have certain relation to value given type (bigger,smaller,equal etc.)
        /// Also capable of finding NaN values
        ///</summary>

        public static List<int> FindValues(double[] vec, double value, VectorFindValueType type)
        {
            List<int> indices = new List<int>();

            if (type == TimeSeriesAnalysis.VectorFindValueType.BiggerThan)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] > value && !IsNaN(vec[i]))
                        indices.Add(i);
                }
            }
            else if (type == TimeSeriesAnalysis.VectorFindValueType.SmallerThan)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] < value && !IsNaN(vec[i]))
                        indices.Add(i);
                }
            }
            else if (type == TimeSeriesAnalysis.VectorFindValueType.BiggerOrEqual)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] >= value && !IsNaN(vec[i]))
                        indices.Add(i);
                }
            }
            else if (type == TimeSeriesAnalysis.VectorFindValueType.SmallerOrEqual)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] <= value && !IsNaN(vec[i]))
                        indices.Add(i);
                }
            }
            else if (type == TimeSeriesAnalysis.VectorFindValueType.Equal)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] == value)
                        indices.Add(i);
                }
            }
            else if (type == TimeSeriesAnalysis.VectorFindValueType.NaN)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (IsNaN(vec[i]))
                        indices.Add(i);
                }
            }
            else if (type == TimeSeriesAnalysis.VectorFindValueType.NotNaN)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (!IsNaN(vec[i]))
                        indices.Add(i);
                }
            }

            return indices;
        }



        ///<summary>
        /// replaces all the vaules in array with indices in indList NaN
        ///</summary>
        public static double[] ReplaceIndWithValue(double[] array, List<int> indList, 
            double valueToReplaceWith)
        {
            int[] vecInd = indList.ToArray();
            for (int curIndInd = 0; curIndInd < vecInd.Length; curIndInd++)
            {
                int curVecInd = vecInd[curIndInd];
                if (curVecInd > 0)
                {
                    array[curVecInd] = valueToReplaceWith;
                }
            }
            return array;
        }





        ///<summary>
        /// returns the intersection of array1 and array2, a list of elements that are in both vectors
        ///</summary>
        public static List<int> Intersect(List<int> vec1, List<int> vec2)
        {
            return vec1.Intersect(vec2).ToList();
        }
        ///<summary>
        /// returns the union of array1 and array2, a list of elements that are in either vector
        ///</summary>
        public static List<int> Union(List<int> vec1, List<int> vec2)
        {
            List<int> c = vec1.Union(vec2).ToList();
            c.Sort();
            return c;
        }



        ///<summary>
        /// given a list of sorted indeces and a desired vector size N, returns the indices that are not in "sortedIndices"
        /// i.e. of the "other vectors
        ///</summary>
        public static List<int> InverseIndices(int N, List<int> sortedIndices)
        {
            List<int> ret = new List<int>();

            int curInd = 0;
            bool lastSortedIndFound = false;
            int nSortedIndices = sortedIndices.Count();
            for (int i = 0; i < N; i++)
            {
                if (curInd < nSortedIndices)
                {
                    if (i < sortedIndices[curInd])
                    {
                        ret.Add(i);
                    }
                    else if (i == sortedIndices[curInd])
                    {
                        if (curInd + 1 < sortedIndices.Count)
                            curInd++;
                        else
                            lastSortedIndFound = true;

                    }
                    else if (lastSortedIndFound)
                    {
                        ret.Add(i);
                    }
                }
            }
            return ret;

        }



        /// <summary>
        /// R-squared 
        /// R-squared (R2) is a statistical measure that represents the proportion of the variance for a dependent 
        /// variable that's explained by an independent variable or variables in a regression model. 
        /// Whereas correlation explains the strength of the relationship between an independent and 
        /// dependent variable, R-squared explains to what extent the variance of one variable explains the
        /// variance of the second variable. So, if the R2 of a model is 0.50, then approximately 
        /// half of the observed variation can be explained by the model's inputs.
        /// </summary>
        /// <param name="vector1">first vector</param>
        /// <param name="vector2">second vector</param>
        /// <param name="indToIgnoreExt">optionally: indices to be ignored(for instance bad values)</param>
        /// <returns>R2 squared, a value between -1 and 1. If an error occured,Double.PositiveInfinity is returned </returns>
        public static double RSquared(double[] vector1, double[] vector2, List<int> indToIgnoreExt=null)
        {
            if (vector1 == null || vector2 == null)
                return Double.PositiveInfinity;

            double[] x_mod_int = new double[vector1.Length];
            double[] x_meas_int = new double[vector2.Length];//
            vector1.CopyTo(x_mod_int, 0);
            vector2.CopyTo(x_meas_int, 0);

            // protect r-squared from -9999 values.
            List<int> minus9999ind = FindValues(vector2, nanValue, TimeSeriesAnalysis.VectorFindValueType.Equal);
            List<int> nanind = FindValues(vector1, Double.NaN, TimeSeriesAnalysis.VectorFindValueType.NaN);
            List<int> indToIgnoreInt = minus9999ind.Union(nanind).ToList();

            List<int> indToIgnore;
            if (indToIgnoreExt != null)
                indToIgnore = indToIgnoreInt.Union(indToIgnoreExt).ToList();
            else
                indToIgnore = indToIgnoreInt;

            foreach (int ind in indToIgnore)
            {
                x_mod_int[ind] = 0;
                x_meas_int[ind] = 0;
            }

            double SSres = SumOfSquareErr(x_mod_int, x_meas_int, -1, false);//explainedVariation
            double meanOfMeas = Mean(x_meas_int).Value;
            double SStot = SumOfSquareErr(x_mod_int, meanOfMeas, false); //totalVariation
            double Rsq = 1 - SSres / SStot;
            return Rsq;
        }

        /// <summary>
        ///  When filtering out bad data before identification, before fitting 
        ///  data to difference equations that depend both y[k] and y[k-1]
        ///  it will some times be neccessary, to append the trailing indices
        ///  for instance on 
        /// 
        /// </summary>

        public static List<int> AppendTrailingIndices(List<int> indiceArray)
        {
            List<int> appendedIndiceArray = new List<int>(indiceArray);
            List<int> indicesToAdd = new List<int>(); 
            for (int i = 0; i < indiceArray.Count; i++)
            {
                int curVal = indiceArray.ElementAt(i);
                if (!indiceArray.Contains(curVal + 1))
                    indicesToAdd.Add(curVal + 1);
            }
            appendedIndiceArray.AddRange(indicesToAdd);

            appendedIndiceArray.Sort();

            return appendedIndiceArray;
        }

        /// <summary>
        ///  serializes a single vector/array to a file for persistent storage to a human-readable text format
        /// </summary>
        static public bool Serialize(double[] vector, string fileName)
        {
            using (StringToFileWriter writer = new StringToFileWriter(fileName))
            {
                foreach (double val in vector)
                {
                    writer.Write(val.ToString("##.###########", CultureInfo.InvariantCulture) +"\r\n");
                }
                writer.Close();
            }
            return true;
        }
        

        /// <summary>
        ///  de-serializes a single vector/array (written by serialize)
        /// </summary>
        static public double[] Deserialize(string fileName)
        {
            List<double> values  = new List<double> ();
            string[] lines = File.ReadAllLines(fileName);

            foreach (string line in lines)
            {
                bool isOk = Double.TryParse(line, NumberStyles.Any,
                    CultureInfo.InvariantCulture,out double result);
                if (isOk)
                {
                    values.Add(result);
                }
                else
                {
                    values.Add(Double.NaN);
                }
            }
            return values.ToArray();
        }

        ///<comment>
        /// Create a compact string of vector with a certain number of significant digits and a chosen divider
        ///</summary>
        public static string ToString(double[] array, int nSignificantDigits, string dividerStr = ";")
        {
            StringBuilder sb = new StringBuilder();
            if (array == null)
            {
                return "null";
            }
            if (array.Length > 0)
            {
                sb.Append("[");
                sb.Append(SignificantDigits.Format(array[0], nSignificantDigits).ToString("", CultureInfo.InvariantCulture));
                for (int i = 1; i < array.Length; i++)
                {
                    sb.Append(dividerStr);
                    sb.Append(SignificantDigits.Format(array[i], nSignificantDigits).ToString("", CultureInfo.InvariantCulture));
                }
                sb.Append("]");
            }
            return sb.ToString();
        }


        
    }
}
