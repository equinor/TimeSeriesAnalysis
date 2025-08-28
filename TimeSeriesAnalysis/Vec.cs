using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Statistics.Models.Regression.Fitting;
using System.Globalization;
using System.IO;

using TimeSeriesAnalysis.Utility;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Utility functions and operations for treating arrays as mathematical vectors.
    /// <para>
    /// This class considers doubles, methods that require comparisons cannot be easily ported to generic "Vec"/>
    /// </para>
    /// </summary>
    public class Vec
    {
        private  double valuteToReturnElementIsNaN;// so fi an element is either NaN or "-9999", what value shoudl a calculation return?
        private double nanValue;// an input value that is to be considrered "NaN" 

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nanValue">inputs values matching this value are treated as "NaN" 
        /// and are excluded from all calculations</param>
        /// <param name="valuteToReturnElementIsNaN">value to return in elementwise calculations to indiate NaN output</param>
        public Vec(double nanValue = -9999, double valuteToReturnElementIsNaN = Double.NaN)
        {
            this.nanValue = nanValue;
            this.valuteToReturnElementIsNaN = valuteToReturnElementIsNaN;
        }

        //  Methods should be sorted alphabetically


        /// <summary>
        /// Returns an array where each value is the absolute value of array1.
        /// </summary>
        public double[] Abs(double[] array1)
        {
            if (array1 == null)
                return null;
            double[] retVal = new double[array1.Length];

            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    retVal[i] = valuteToReturnElementIsNaN;
                else
                    retVal[i] = Math.Abs(array1[i]);
            }
            return retVal;
        }

        /// <summary>
        /// Returns an array which is the elementwise addition of array1 and array2.
        /// </summary>
        public double[] Add(double[] array1, double[] array2)
        {

            if (array1 == null || array2 == null)
                return null;
            double[] retVal = new double[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    retVal[i] = valuteToReturnElementIsNaN;
                else
                {
                    retVal[i] = array1[i] + array2[i];
                }
            }
            return retVal;
        }


        /// <summary>
        /// Elementwise addition of val2 to array1.
        /// </summary>
        public double[] Add(double[] array1, double val2)
        {
            if (array1 == null)
                return null;
            double[] retVal = new double[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    retVal[i] = valuteToReturnElementIsNaN;
                else
                    retVal[i] = array1[i] + val2;
            }
            return retVal;
        }



        /// <summary>
        /// Returns true if array contains a "-9999" or NaN indicating missing data.
        /// </summary>
        public bool ContainsBadData(double[] x)
        {
            bool doesContainBadData = false;
            for (int i = 0; i < x.Length; i++)
            {
                if (IsNaN(x[i]))
                {
                    doesContainBadData = true;
                }
            }
            return doesContainBadData;
        }

        /// <summary>
        /// Returns the co-variance of two arrays (interpreted as "vectors").
        /// </summary>
        public double Cov(double[] array1, double[] array2, bool doNormalize = false)
        {
            double retVal = 0;
            double avg1 = Mean(array1).Value;
            double avg2 = Mean(array2).Value;
            double absMax1 = Max(Abs(array1));
            double absMax2 = Max(Abs(array2));

            int N = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]) || IsNaN(array2[i]))
                    continue;
                N++;
                if (doNormalize)
                {
                    retVal += (array1[i] - avg1)/absMax1 * (array2[i] - avg2) / absMax2;
                }
                else
                {
                    retVal += (array1[i] - avg1) * (array2[i] - avg2);
                }
            }
            if (doNormalize)
            {
                retVal /= N;
            }
            return retVal;
        }

        /// <summary>
        /// De-serializes a single vector/array (written by serialize).
        /// </summary>
        static public double[] Deserialize(string fileName)
        {
            List<double> values = new List<double>();
            string[] lines = File.ReadAllLines(fileName);

            foreach (string line in lines)
            {
                bool isOk = Double.TryParse(line, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double result);
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




        /// <summary>
        /// Return an array of the differences between the neighboring items in array
        /// but ignores indices in the array that are in "indicesToIgnore".
        /// </summary>
        /// <param name="vec"></param>
        /// <param name="indicesToIgnore"></param>
        /// <returns></returns>
        public double[] Diff(double[] vec,List<int> indicesToIgnore=null)
        {
            double[] vec_cur, vec_prev;
            if (indicesToIgnore == null)
            {
                vec_cur = Vec<double>.SubArray(vec, 1);
                vec_prev = Vec<double>.SubArray(vec, 0, vec.Length - 2);
            }
            else
            {
                var vec_reduced = GetValues(vec,indicesToIgnore);
                vec_cur = Vec<double>.SubArray(vec_reduced, 1);
                vec_prev = Vec<double>.SubArray(vec_reduced, 0, vec_reduced.Length - 2);
            }
            double[] vec_Diff = Subtract(vec_cur, vec_prev);
            return Vec<double>.Concat(new double[] { 0 }, vec_Diff);
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="scalar"></param>
        /// <returns>a vector of values representing the array divided by a scalar. 
        /// In case of NaN inputs or divide-by-zero, NaN elements are returned.</returns>
        public double[] Div(double[] vector, double scalar)
        {
            double[] outArray = new double[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                if (IsNaN(vector[i]) || scalar == 0)
                {
                    outArray[i] = valuteToReturnElementIsNaN;
                }
                else
                {
                    outArray[i] = vector[i] / scalar;
                }
            }
            return outArray;
        }

        /// <summary>
        /// Divides two vectors of equal length.
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns>a vector of values representing the array divided by a scalar. 
        /// In case of NaN inputs or divide-by-zero, NaN elements are returned.</returns>
        public double[] Div(double[] vector1, double[] vector2)
        {
            int N = Math.Min(vector1.Length, vector2.Length);
            double[] outArray = new double[N];
            for (int i = 0; i < N; i++)
            {
                if (IsNaN(vector1[i]) || IsNaN(vector2[i])|| vector2[i]==0)
                {
                    outArray[i] = valuteToReturnElementIsNaN;
                }
                else
                {
                    outArray[i] = vector1[i] / vector2[i];
                }
            }
            return outArray;
        }



        /// <summary>
        /// Returns true if the two vectors are equal
        /// </summary>
        /// <param name="arr1"></param>
        /// <param name="arr2"></param>
        /// <returns></returns>
        public static bool Equal(double[] arr1, double[] arr2)
        {
            if (arr1.Length != arr2.Length)
                return false;

            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i])
                    return false;
            }
            return true;
        }


        /// <summary>
        /// Returns all the values of vec, except for those corresponding with indices in "indicesToIngore".
        /// </summary>
        /// <param name="vec"></param>
        /// <param name="indicesToIgnore"></param>
        /// <returns></returns>
        public double[] GetValues(double[] vec, List<int> indicesToIgnore)
        {
            if (indicesToIgnore == null)
                return vec;
            if (indicesToIgnore.Count == 0)
                return vec;
            var goodIndices = Index.InverseIndices(vec.Length, indicesToIgnore);
            List<double> values = new List<double>();
            for (int i = 0; i <goodIndices.Count(); i++)
            {
                values.Add(vec[goodIndices.ElementAt(i)]);
            }
            return values.ToArray();
        }

        /// <summary>
        /// Return the indices of elements in the array that have a certain relation to value given type (bigger, smaller, equal, etc.).
        /// Also capable of finding NaN values.
        /// </summary>
        /// <param name="vec"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="indicesToIgnore"> if given, these indices are ignored when doing the operation</param>
        /// <returns></returns>
        public List<int> FindValues(double[] vec, double value, VectorFindValueType type, List<int>indicesToIgnore = null)
        {
            List<int> indices = new List<int>();
            if (vec == null)
                return indices;

            void Add(int index)
            {
                if (indicesToIgnore == null)
                    indices.Add(index);
                else if (!indicesToIgnore.Contains(index))
                    indices.Add(index);
            }

            if (type == VectorFindValueType.BiggerThan)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] > value && !IsNaN(vec[i]))
                        Add(i);
                }
            }
            else if (type == VectorFindValueType.SmallerThan)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] < value && !IsNaN(vec[i]))
                        Add(i);
                }
            }
            else if (type == VectorFindValueType.BiggerOrEqual)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] >= value && !IsNaN(vec[i]))
                        Add(i);
                }
            }
            else if (type == VectorFindValueType.SmallerOrEqual)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] <= value && !IsNaN(vec[i]))
                        Add(i);
                }
            }
            else if (type == VectorFindValueType.Equal)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] == value)
                        Add(i);
                }
            }
            else if (type == VectorFindValueType.NaN)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (IsNaN(vec[i]))
                        Add(i);
                }
            }
            else if (type == VectorFindValueType.NotNaN)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (!IsNaN(vec[i]))
                        Add(i);
                }
            }
            else if (type == VectorFindValueType.NotEqual)
            {
                for (int i = 0; i < vec.Length; i++)
                {
                    if (vec[i] != value)
                        Add(i);//indices.Add(i);
                }
            }
            else if (type == VectorFindValueType.SameAsPrevious)
            {
                for (int i = 1; i < vec.Length; i++)
                {
                    if (vec[i] == vec[i-1])
                        Add(i);
                }
            }
            else if (type == VectorFindValueType.DifferentFromPrevious)
            {
                for (int i = 1; i < vec.Length; i++)
                {
                    if (vec[i] != vec[i-1])
                        Add(i);
                }
            }
            return indices;
        }


        /// <summary>
        /// Gets the gradient of a time-series.
        /// <para>
        /// Works by running a regression with time as the "X" variable
        /// </para>
        /// </summary>
        /// <param name="values">values for which the gradient is sought</param>
        /// <param name="dates">dates corresponding to the values</param>
        /// <param name="sampleTime_sec">in what unit of time (given in seconds)the gradient shall be presented</param>
        /// <param name="indicesToIgnore">optional array of indices that are to be ignored during regression</param>
        /// <returns>the gradient will be the "Gain" of the returned object (in units per second by default).</returns>
        public static RegressionResults GetGradient(double[] values, DateTime[] dates, int sampleTime_sec = 1,int[] indicesToIgnore=null)
        {
            var vec = new Vec();
            var datesAsNumbers = new List<double>();
            foreach (var date in dates)
            {
                datesAsNumbers.Add( UnixTime.ConvertToUnixTimestamp(date)/ sampleTime_sec);
            }
            var X = Array2D<double>.Create(datesAsNumbers.ToArray());
            var results = vec.Regress(values,X, indicesToIgnore);

            return results;
        }




        /// <summary>
        /// Returns true if all elements in the array are the specific value.
        /// </summary>
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

        /// <summary>
        /// Returns true if all elements in the array are "-9999" or Double.NaN, or is null.
        /// </summary>
        public bool IsAllNaN(double[] array)
        {
            if (array == null)
            {
                return true;
            }
            int count = 0;
            while (IsNaN(array[count]) && count < array.Length - 1)
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


        /// <summary>
        /// All checks for NaN will test both for Double.IsNan and if value == {a specific "NaN" value (like "-9999")}.
        /// </summary>
        private bool IsNaN(double value)
        {
            if (double.IsNaN(value) || value == nanValue)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Returns the maximum value of two arrays as a new array.
        /// </summary>
        public double[] Max(double[] array1, double[] array2)
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

        /// <summary>
        /// Returns the maximum value of the array and the index of the maximum value.
        /// </summary>
        public double Max(double[] array, out int ind)
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

        /// <summary>
        /// Returns the maximum value of the array, ignoring the given indices.
        /// </summary>
        public double Max(double[] array, List<int> indicesToIgnore)
        {
            double maxVal = double.MinValue;
            for (int i = 0; i < array.Length; i++)
            {
                if (indicesToIgnore != null)
                    if (indicesToIgnore.Contains(i))
                        continue;
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
    
        /// <summary>
        /// Returns the element-wise maximum of the array element and value.
        /// </summary>
        public double[] Max(double[] array, double value)
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

        /// <summary>
        /// Returns the maximum value of the array.
        /// </summary>
        public double Max(double[] array)
        {
            return Max(array, out _);
        }

        /// <summary>
        /// Returns the minimum value of the array.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public double Min(double[] array)
        {
            return Min(array, out _);
        }
        /// <summary>
        /// Returns the minimum value of the array, ignoring certain indices.
        /// </summary>
        public double Min(double[] array, List<int> indicesToIgnore)
        {

            double minVal = double.MaxValue;
            for (int i = 0; i < array.Length; i++)
            {
                if (indicesToIgnore != null)
                   if (indicesToIgnore.Contains(i))
                        continue;
                double thisNum = array[i];
                if (IsNaN(thisNum))
                    continue;
                if (thisNum < minVal)
                {
                    minVal = thisNum;
                }
            }
            return minVal;
        }

        /// <summary>
        /// Returns the minimum value of the array and the index of the maximum value.
        /// </summary>
        public double Min(double[] array, out int ind)
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


        /// <summary>
        /// Returns the minimum value of the two arrays as new array.
        /// </summary>
        static public double[] Min(double[] array1, double[] array2)
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

        /// <summary>
        /// Returns the element-wise minimum of the array elements and value.
        /// </summary>
        public double[] Min(double[] array, double value)
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


        /// <summary>
        /// Elementwise multiplication of val2 to array1.
        /// </summary>
        public double[] Multiply(double[] array1, double val2)
        {
            if (array1 == null)
                return null;
            double[] retVal = new double[array1.Length];

            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]))
                    retVal[i] = valuteToReturnElementIsNaN;
                else
                    retVal[i] = array1[i] * val2;
            }
            return retVal;
        }

        /// <summary>
        /// Elementwise multiplication of array1 and array2, assuming they are same size.
        /// </summary>

        public double[] Multiply(double[] array1, double[] array2)
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
                    retVal[i] = valuteToReturnElementIsNaN;
                else
                    retVal[i] = array1[i] * array2[i];
            }
            return retVal;
        }

        /// <summary>
        /// Returns the mean value of array1.
        /// </summary>
        public double? Mean(double[] array1)
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

        /// <summary>
        /// Returns the mean value of array1. while ignoring given indices
        /// </summary>
        public double? Mean(double[] array1, List<int> indToIgnore = null)
        {
            if (array1 == null)
                return null;
            double retVal = 0;
            double N = 0; 
            var indices =  Index.InverseIndices(array1.Length, indToIgnore);
            foreach  (var ind in indices)
            {
                if (!IsNaN(array1[ind]))
                {
                    N += 1;
                    retVal = retVal * (N - 1) / N + array1[ind] * 1 / N;
                }
            }
            return retVal;
        }

    




        /// <summary>
        /// Calculates the power of the array.
        /// </summary>
        public double[] Pow(double[] array, double factor)
        {
            double[] ret = new double[array.Length];
            for (int i = 0; i < ret.Length; i++)
            {
                if (IsNaN(array[i]))
                    ret[i] = valuteToReturnElementIsNaN;
                else
                    ret[i] = Math.Pow(array[i], factor);
            }
            return ret;
        }

        /// <summary>
        /// Returns the range of an array; the difference between minimum and maximum element values.
        /// </summary>
        public double Range(double[] array)
        {
            double range = Max(array) - Min(array);

            return range;
        }



        /// <summary>
        /// Create a vector of random numbers.
        /// </summary>
        /// <param name="N">the number of samples of the returned array</param>
        /// <param name="minValue">lower end of random number range</param>
        /// <param name="maxValue">higher end of random number range</param>
        /// <param name="seed">optionally, give in a seed number, this makes random sequence repeatable</param>
        /// <returns>an array of size N of random numbers between minValue and maxValue.</returns>
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
                ret[i] = rand.NextDouble() * (maxValue - minValue) + minValue; ;//NextDouble by itself return a value btw 0 and 1
            }
            return ret;
        }

        /// <summary>
        /// Robust linear regression.
        /// </summary>
        /// <param name="Y">vector of response variable values (to be modelled)</param>
        /// <param name="X">2D matrix of manipulated values/independent values/regressors used to explain Y</param>
        /// <param name="yIndToIgnore">(optional) a list of the indices of values in Y to ignore in regression. By default it is <c>null</c></param>
        /// <returns>an object of the <c>RegressionResult</c> class with the parameters, as well as 
        /// some statistics on the fit and uncertainty thereof.</returns>

        public RegressionResults Regress(double[] Y, double[,] X, int[] yIndToIgnore = null)
        {
            return Regress(Y,X.Convert2DtoJagged(),yIndToIgnore);
        }

        /// <summary>
        /// Regression that does not attempt to regualarize inputs toward zero.
        /// </summary>
        /// <param name="Y"></param>
        /// <param name="X"></param>
        /// <param name="yIndToIgnore"></param>
        /// <returns></returns>
        public RegressionResults RegressUnRegularized(double[] Y, double[][] X, int[] yIndToIgnore = null)
        {
            return Regress(Y, X, yIndToIgnore, null, false);
        }

        /// <summary>
        /// Robust linear regression, regularized.
        /// To avoid parameters taking on extremely high values in the case of a little excitation in the inputs, 
        /// two mitigating actions are implemented by the solver to be "robust":
        /// - a "robust" Singular Value Decomposition (SVD) -based solver is used.
        /// - a regularization term is added to the objective function that will bring parameters to zero if (Y,X) does not contain
        ///  any information to force the parameter away from zero.
        /// </summary>
        /// <param name="Y">vector of output variable values to be modelled</param>
        /// <param name="X">jagged 2D matrix of manipulated values/independent values/regressors used to explain Y</param>
        /// <param name="yIndToIgnore">(optional) a list of the indices of values in Y to ignore in regression. By default it is <c>null</c></param>
        /// <param name="XindicesToRegularize">(optional) only the indices in this list are to be regularized to zero</param>
        /// <returns>an object of the <c>RegressionResult</c> class with the parameters, as well as 
        /// some statistics on the fit and uncertainty thereof.</returns>
        public RegressionResults RegressRegularized(double[] Y, double[][] X, int[] yIndToIgnore = null, List<int> XindicesToRegularize = null)
        {
            return Regress(Y, X, yIndToIgnore, XindicesToRegularize, true);
        }


        /// <summary>
        /// Robust linear regression.
        /// To avoid parameters taking on exteremly high values in the case of a little excitation in the inputs, 
        /// two mitigating actions are implemented by the solver to be "robust":
        /// - a "robust" Singular Value Decomposition (SVD) -based solver is used.
        /// - a regularization term is added to the objective function that will bring parameters to zero if (Y,X) does not contain
        ///  any information to force the parameter away from zero.
        /// </summary>
        /// <param name="Y">vector of output variable values to be modelled</param>
        /// <param name="X">jagged 2D matrix of manipulated values/independent values/regressors used to explain Y</param>
        /// <param name="yIndToIgnore">(optional) a list of the indices of values in Y to ignore in regression. By default it is <c>null</c></param>
        /// <param name="XindicesToRegularize">(optional) only the indices in this list are to be regularized to zero</param>
        /// <param name="doNormalizationToZero">(optional) adds a minor "regularization" term to objective that will pull values toward zero if corresponding input is not excited</param>
        /// <returns>an object of the <c>RegressionResult</c> class with the parameters, as well as 
        /// some statistics on the fit and uncertainty thereof.</returns>
        private RegressionResults Regress(double[] Y, double[][] X, int[] yIndToIgnore=null, List<int> XindicesToRegularize=null, bool doNormalizationToZero = true)
        {
            RegressionResults results = new RegressionResults();
            var vec = new Vec();

            double[][] X_T;
            var X_withBias = Array2D<double>.Append(X, Vec<double>.Fill(1, X.GetNColumns()));
            if (X_withBias.GetNColumns() > X_withBias.GetNRows())
            {
                X_T = Accord.Math.Matrix.Transpose(X_withBias);
            }
            else
            {
                X_T = X_withBias;
            }

            var X_rank =  Accord.Math.Matrix.Rank(Array2D<double>.Created2DFromJagged(X_T));
            var X_columnsToDisable = new List<int>();
            if (X_rank < X_T.GetNColumns())
            {
                for (int colIdx = 0; colIdx < X_T.GetNColumns()-1; colIdx++)
                {
                    var colMax = vec.Max(Array2D<double>.GetColumn(X_T, colIdx));
                    var colMin = vec.Min(Array2D<double>.GetColumn(X_T, colIdx));

                    if (colMax - colMin < 0.001)
                    {
                        X_columnsToDisable.Add(colIdx);
                    }
                    // TODO: you could possibly check if two inputs are scaled versions of each other here.
                }
                results.RegressionWarnings.Add(RegressionWarnings.InputMatrixIsRankDeficient);
            }

  
            //if any columns in X are constant, they trigger a numerical instability where
            // gain of constant X column is a very big positive number and bias is a very large negative number.

            bool doInterpolateYforBadIndices = true;
            MultipleLinearRegression regression;

            // weight-to-zero all indices which are to be ignored!
            double[] weights = Vec<double>.Fill(1, Y.Length); //null;
            if (yIndToIgnore != null)
            {
                for (int i = 0; i < yIndToIgnore.Length; i++)
                {
                    int curInd = yIndToIgnore[i];
                    // NB! "weights" dont actully do anything, it is the below code that ensures bad data is removed.
                    if (curInd >= 0 && curInd < weights.Length)
                    {
                        weights[curInd] = 0;
                    }
                    // set Y and X_T to zero for values that are bad
                    // the weight do not always appear to work, sometimes the accord
                    // solver just returns "null" and hard to know why, and this is a
                    // workaround
                    if (curInd < Y.Length && curInd>=0)
                    {
                        Y[curInd] = 0;
                        for (int curX = 0; curX < X_T[curInd].Count(); curX++)
                        {
                            X_T[curInd][curX] = 0;
                        }
                    }
                }
            }

            bool doDebug = false;
            if (doDebug)
            {
                Plot.FromList(new List<double[]> {Y, Array2D<double>.GetColumn(X_T,0),
                    Array2D<double>.GetColumn(X_T,1) },
                    new List<string> { "y1=Y","y3=u1", "y3=u2" },1,null,default,"regresstest");
            }

            OrdinaryLeastSquares accordFittingAlgo = new OrdinaryLeastSquares()
            {
                IsRobust = true, // to use SVD or not, has benefits if columns in the X-matrix are constant, otherwise the gains might run up to big values.
                UseIntercept = false // default is "true", uses a default bias term, but this does not paly well with regularization.
            };

            //TODO: try to catch rank deficient or singular X instead of generating exception.
            try
            {
                if (doNormalizationToZero)
                {
                    //var X_T_reg = X_T;
                    List<double[]> regX = new List<double[]>();

                    int nGains = X_T[0].Length-1;//minus one: bias should not be normalized!!!
                    // if no indices are specified, then apply to all..
                    if (XindicesToRegularize == null)
                    {
                        for (int inputIdx = 0; inputIdx < nGains; inputIdx++)
                        {
                            var newRow = Vec<double>.Fill(0, nGains + 1);//+1 for bias
                            newRow[inputIdx] = 1;
                            regX.Add(newRow);
                        }
                    }
                    else
                    {
                        for (int inputIdx = 0; inputIdx < XindicesToRegularize.Count ; inputIdx++)
                        {
                            var idx = XindicesToRegularize[inputIdx];
                            if (idx < nGains)
                            {
                                var newRow = Vec<double>.Fill(0, nGains + 1);//+1 for bias
                                newRow[idx] = 1;
                                regX.Add(newRow);
                            }
                        }
                    }
                    var X_T_reg = Array2D<double>.Combine(X_T, Array2D<double>.CreateJaggedFromList(regX));
                    var Y_reg = Vec<double>.Concat(Y, Vec<double>.Fill(0, regX.Count()));
                    double? Y_mean = vec.Mean(Y, indToIgnore: yIndToIgnore?.ToList());
                    double regressionWeight = (double)Y.Length / 1000;
                    var weights_reg = Vec<double>.Concat(weights, Vec<double>.Fill(regressionWeight, regX.Count())) ;

                    // note: weights have no effect prior to accord 3.7.0 
                    regression = accordFittingAlgo.Learn(X_T_reg, Y_reg, weights_reg);
                }
                else
                {
                    // note: weights have no effect prior to accord 3.7.0 
                    regression = accordFittingAlgo.Learn(X_T, Y, weights);
                }
                if (yIndToIgnore == null)
                {
                    results.NfittingBadDataPoints = 0;
                }
                else
                {
                    results.NfittingBadDataPoints = yIndToIgnore.Length;
                }
                results.NfittingTotalDataPoints = Y.Length;
                // modelled Y
                results.Y_modelled = regression.Transform(X_T);
                if (yIndToIgnore != null)
                {
                    if (doInterpolateYforBadIndices)
                    {
                        // write interpolated values to y_modelled. 
                        // these should 
                        double lastIgnoredInd = -1;
                        double lastGoodValue = -1;
                        for (int i = 0; i < yIndToIgnore.Length; i++)
                        {
                            int curInd = yIndToIgnore[i];
                            if (yIndToIgnore[i] >= results.Y_modelled.Length)
                                continue;
                            if (curInd == lastIgnoredInd + 1)
                            {
                                results.Y_modelled[yIndToIgnore[i]] = lastGoodValue;
                            }
                            else
                            {
                                lastIgnoredInd = curInd;
                                lastGoodValue = results.Y_modelled[yIndToIgnore[i] - 1];
                                results.Y_modelled[yIndToIgnore[i]] = lastGoodValue;
                            }
                        }
                    }
                }

                if (yIndToIgnore != null)
                {
                    results.Rsq = RSquared(results.Y_modelled, Y, yIndToIgnore.ToList()) * 100;
                }
                else
                {
                    results.Rsq = RSquared(results.Y_modelled, Y) * 100;
                }
                List<int> yIndToIgnoreList=null;
                if (yIndToIgnore != null)
                {
                    yIndToIgnoreList = yIndToIgnore.ToList();
                }
                results.ObjectiveFunctionValue = vec.SumOfSquareErr(results.Y_modelled, Y, 0, false, yIndToIgnoreList);
                // NB! note that Accord here uses the term "weigths" to mean "parameters.
                results.Bias = regression.Weights.Last();
                results.Gains = Vec<double>.SubArray(regression.Weights,0, regression.Weights.Length-2);
                results.Param = regression.Weights;

                // uncertainty estimation
                try
                {
                    //re-run regression without regularization, use that to get information matrix.
                    var regression_noreg = accordFittingAlgo.Learn(X_T, Y, weights);
                    double[][] informationMatrix = accordFittingAlgo.GetInformationMatrix();// this should already include weights

                    double mse = regression.GetStandardError(X_T, vec.Multiply(weights, Y));
                    double[] SE = regression.GetStandardErrors(mse, informationMatrix);

                    results.Param95prcConfidence = vec.Multiply(SE, 1.96);

                    int thetaLength = regression.Weights.Length;
                    results.VarCovarMatrix = Accord.Math.Matrix.PseudoInverse(informationMatrix);
                }
                catch
                {
                    results.Param95prcConfidence = null;
                }
                if (Vec.IsAllValue(results.Param, 0))
                {
                    results.AbleToIdentify = false;
                }
                else
                {
                    results.AbleToIdentify = true;
                }
                return results;
            }
            catch 
            {
                results.AbleToIdentify = false;
                return results;
            }
        }


        /// <summary>
        /// Replace certain values in an array with a new value. 
        /// </summary>
        /// <param name="array">the array to be replaced</param>
        /// <param name="indList">list of all the indices of all data points in the array to be replaced</param>
        /// <param name="valueToReplaceWith">the new value to use in place of old values</param>
        /// <returns>a copy of the original array with the values replaced as specified.</returns>
        public static double[] ReplaceIndWithValue(double[] array, List<int> indList,
            double valueToReplaceWith)
        {
            if (indList == null)
                return array;

            int[] vecInd = indList.ToArray();
            double[] outArray = new double[array.Length] ;
            array.CopyTo(outArray,0);
            for (int curIndInd = 0; curIndInd < vecInd.Length; curIndInd++)
            {
                int curVecInd = vecInd[curIndInd];
                if (curVecInd >= 0)
                {
                    outArray[curVecInd] = valueToReplaceWith;
                }
            }
            return outArray;
        }

        /// <summary>
        /// Replace values below a certain threshold in an array with a new value.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="threshold"></param>
        /// <param name="valueToReplaceWith"></param>
        /// <returns></returns>
        public static double[] ReplaceValuesAbove(double[] array, double threshold, double valueToReplaceWith)
        {
            var ind = new Vec().FindValues(array, threshold,VectorFindValueType.BiggerThan);
            return ReplaceIndWithValue(array,ind, valueToReplaceWith);
        }

        /// <summary>
        /// Replace all values above a certain threshold in array with a new value.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="threshold"></param>
        /// <param name="valueToReplaceWith"></param>
        /// <returns></returns>
        public static double[] ReplaceValuesBelow(double[] array, double threshold, double valueToReplaceWith)
        {
            var ind = new Vec().FindValues(array, threshold, VectorFindValueType.SmallerThan);
            return ReplaceIndWithValue(array, ind, valueToReplaceWith);
        }

        /// <summary>
        /// Replace values above a higher threshold or below a lower threshold with a new value.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="lowerThreshold"></param>
        /// <param name="higherThreshold"></param>
        /// <param name="valueToReplaceWith"></param>
        /// <returns></returns>
        public static double[] ReplaceValuesAboveOrBelow(double[] array, double lowerThreshold, double higherThreshold, double valueToReplaceWith)
        {
            return ReplaceValuesAbove(ReplaceValuesBelow(array,lowerThreshold, valueToReplaceWith),higherThreshold,valueToReplaceWith);
        }

        /// <summary>
        /// R-squared. 
        /// R-squared (R2) is a statistical measure that represents the proportion of the variance for a dependent 
        /// variable that's explained by an independent variable or variables in a regression model. 
        /// Whereas correlation explains the strength of the relationship between an independent and 
        /// dependent variable, R-squared explains to what extent the variance of one variable explains the
        /// variance of the second variable. So, if the R2 of a model is <c>0.50</c>, then approximately 
        /// half of the observed variation can be explained by the model's inputs.
        /// </summary>
        /// <param name="vector1">first vector</param>
        /// <param name="vector2">second vector</param>
        /// <param name="indToIgnoreExt">optionally: indices to be ignored (for instance bad values)</param>
        /// <param name="ymodOffset">set the offset beteen vector1 and vector2 (for difference equations, ymod is offset by -1 from ymeas). -1 is default.</param>
        /// <returns>R2 squared, a value between <c>-1</c> and <c>1</c>. If an error occured, 
        /// <c>Double.PositiveInfinity</c> is returned.</returns>
        /// 
        public double RSquared(double[] vector1, double[] vector2, List<int> indToIgnoreExt = null, int ymodOffset = -1)
        {
            if (vector1 == null || vector2 == null)
                return Double.PositiveInfinity;

            double[] x_mod_int = new double[vector1.Length];
            double[] x_meas_int = new double[vector2.Length];//
            vector1.CopyTo(x_mod_int, 0);
            vector2.CopyTo(x_meas_int, 0);

            // protect r-squared from -9999 values.
            List<int> minus9999ind = FindValues(vector2, nanValue, VectorFindValueType.Equal);
            List<int> nanind = FindValues(vector1, Double.NaN, VectorFindValueType.NaN);
            List<int> indToIgnoreInt = minus9999ind.Union(nanind).ToList();

            List<int> indToIgnore;
            if (indToIgnoreExt != null)
                indToIgnore = indToIgnoreInt.Union(indToIgnoreExt).ToList();
            else
                indToIgnore = indToIgnoreInt;

            foreach (int ind in indToIgnore)
            {
                if (ind > x_mod_int.Length - 1)
                    continue;
                x_mod_int[ind] = 0;
                x_meas_int[ind] = 0;
            }

            // Plot.FromList(new List<double[]> { x_mod_int , x_meas_int },new List<string> {"y1=xmod","y1=xmeas" },
            //      TimeSeriesCreator.CreateDateStampArray(new DateTime(2000,1,1),1, x_mod_int.Length));
            //  double SSres = SumOfSquareErr(x_mod_int, x_meas_int, 0, false);
            double SSres = SumOfSquareErr(x_mod_int, x_meas_int, ymodOffset, false, indToIgnore);//explainedVariation
            double meanOfMeas = Mean(x_meas_int, indToIgnore).Value;
            double SStot = SumOfSquareErr(x_mod_int, meanOfMeas, false, indToIgnore); //totalVariation
            double Rsq = 1 - SSres / SStot;
            return Rsq;
        }


        /// <summary>
        /// Smooths the array without phase-shifting by using both past and future values
        /// (i.e. so called non-causal smoothing).
        /// </summary>
        public double[] NonCausalSmooth(double[] array1,int kernel=1)
        {
            if (array1 == null)
                return null;

            if (kernel <= 0)
                return array1;

            double[] retVal = new double[array1.Length];

            for (int i = 0; i < kernel; i++)
            {
                retVal[i] = array1[i];
            }
            for (int i = kernel; i < array1.Length-kernel; i++)
            {
                int N = 0;
                double curMean = 0;
                for (int k = i - kernel; k <= i + kernel; k++)
                {
                    if (!IsNaN(array1[k]))
                    {
                        N += 1;
                        curMean = curMean * (N - 1) / N + array1[k] * 1 / N;
                    }
                }
                retVal[i] = curMean;
            }
            for (int i = array1.Length - kernel; i < array1.Length; i++)
            {
                retVal[i] = array1[i];
            }
            return retVal;
        }




        /// <summary>
        /// Elementwise subtraction of array1 and array2, assuming they are same size.
        /// </summary>

        public double[] Subtract(double[] array1, double[] array2)
        {
            if (array1 == null || array2 == null)
                return null;

            double[] retVal = new double[array1.Length];

            for (int i = 0; i < array1.Length; i++)
            {
                if (IsNaN(array1[i]) || IsNaN(array2[i]))
                    retVal[i] = valuteToReturnElementIsNaN;
                else
                    retVal[i] = array1[i] - array2[i];
            }
            return retVal;
        }
        /// <summary>
        /// Elementwise subtraction of val2 from array1.
        /// </summary>
        public double[] Subtract(double[] array1, double val2)
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




        /// <summary>
        /// Returns the elementwise sum of array1.
        /// </summary>
        public double? Sum(double[] array1)
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
        /// The sum of absolute errors <c>(|a1-a2|)</c> between <c>array1</c> and <c>array2</c>.
        /// </summary>
        public double SumOfAbsErr(double[] array1, double[] array2, int indexOffset = -1)
        {
            int nGoodValues = 0;
            if (indexOffset == -1)
            {
                indexOffset = array2.Count() - array1.Count();
            }
            double ret = 0;
            for (int i = indexOffset; i < array2.Count(); i++)
            {
                if ( IsNaN(array2[i]) || IsNaN(array1[i]) )
                    continue;
                nGoodValues++;
                ret += Math.Abs(array2[i] - array1[i - indexOffset]);
            }
            if (nGoodValues > 0)
                return ret / nGoodValues;
            else
                return 0;
        }


        /// <summary>
        /// The sum of square errors <c>(a1-a2)^2</c> between <c>array1</c> and <c>array2</c>.
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <param name="ymodOffset"></param>
        /// <param name="divByN">if true, the result is normalized by the number of good values</param>
        /// <param name="indToIgnore">optionally a list of indices of <c>array1</c> to ignore</param>
        /// <returns></returns>
        public double SumOfSquareErr(double[] array1, double[] array2, int ymodOffset = -1, 
            bool divByN = true, List<int> indToIgnore=null)
        {
            if (array1 == null || array2 == null)
                return Double.NaN;

            if (array1.Count() < array2.Count())
                return Double.NaN;

            if (ymodOffset == -1)
            {
                ymodOffset = array2.Count() - array1.Count();
            }
            double ret = 0;
            int nGoodValues = 0;
            for (int i = ymodOffset; i < array2.Count(); i++)
            {
                if (IsNaN(array2[i]) || IsNaN(array1[i - ymodOffset]) )
                {
                    continue;
                }
                if (indToIgnore != null)
                {
                    if (indToIgnore.Contains(i))
                        continue;
                }
                nGoodValues++;
                ret += Math.Pow(array2[i] - array1[i - ymodOffset], 2);
            }
            if (divByN && nGoodValues > 0)
                return ret / nGoodValues;
            else
                return ret;
        }

        /// <summary>
        /// Sum of the square errors of the vector compared to a constant. by default the return value is normalized by dividing by the number of elements;
        /// this normalization can be turned off.
        /// </summary>
        public static double SumOfSquareErr(double[] vec, double constant, bool doNormalization = true, List<int> indToIgnore=null)
        {
            double ret = 0;
            int nGoodValues = 0;
            for (int i = 0; i < vec.Count(); i++)
            {
                if (indToIgnore != null)
                {
                    if (indToIgnore.Contains(i))
                        continue;
                }
                nGoodValues++;
                ret += Math.Pow(vec[i] - constant, 2);
            }
            if (doNormalization)
                return ret / nGoodValues;
            else
                return ret;
        }

        /// <summary>
        /// Sum of the square errors of the vector compared to itself
        /// </summary>
        public double SelfSumOfSquareErr(double[] vec)
        {
            return SumOfSquareErr(Vec<double>.SubArray(vec, 1), Vec<double>.SubArray(vec, 0, vec.Length - 2), 0);
        }

        /// <summary>
        /// Sum of the absolute errors of the vector compared to itself.
        /// </summary>
        public double SelfSumOfAbsErr(double[] vec)
        {
            return SumOfAbsErr(Vec<double>.SubArray(vec, 1), Vec<double>.SubArray(vec, 0, vec.Length - 2), 0);
        }

        /// <summary>
        /// Serializes a single vector/array to a file for persistent storage to a human-readable text format.
        /// Vector data can then be retreived by companion method <c>Deserialize</c>.
        /// </summary>
        /// <param name="vector">vector to be written to a file</param>
        /// <param name="fileName">the file name (or path) of the file to which the vector is to serialized to</param>
        /// <returns></returns>
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
        /// Create a compact string of vector with a certain number of significant digits and a chosen divider.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="nSignificantDigits"></param>
        /// <param name="dividerStr"></param>
        /// <returns></returns>
        public static string ToString(double[] array, int nSignificantDigits, string dividerStr = ";")
        {
            var writeCulture = new CultureInfo("en-US");// System.Globalization.CultureInfo.InstalledUICulture;
            var numberFormat = (System.Globalization.NumberFormatInfo)writeCulture.NumberFormat.Clone();
            numberFormat.NumberDecimalSeparator = ".";

            StringBuilder sb = new StringBuilder();
            if (array == null)
            {
                return "null";
            }
            if (array.Length > 0)
            {
                sb.Append("[");
                sb.Append(SignificantDigits.Format(array[0], nSignificantDigits).ToString("", writeCulture));
                for (int i = 1; i < array.Length; i++)
                {
                    sb.Append(dividerStr);
                    sb.Append(SignificantDigits.Format(array[i], nSignificantDigits).ToString("", writeCulture));
                }
                sb.Append("]");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns the variance of the array (always a positive number).
        /// </summary>
        public double Var(double[] array1, bool doNormalize = false)
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
                retVal = retVal / (N);
            }
            return retVal;
        }




    }
}
