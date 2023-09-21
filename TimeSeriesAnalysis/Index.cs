﻿using Accord.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Class with utility-methods forr working with indices (vectors of integers)
    /// 
    /// There are some special features of indices: they should never be negative,
    /// and indice vectors are often monotonically increasing.
    /// </summary>
    public class Index
    {
        private static double valuteToReturnElementIsNaN = double.NaN;// so fi an element is either NaN or "-9999", what value shoudl a calculation return?

        /// <summary>
        ///  When filtering out bad data before identification of
        ///  difference equations that depend both y[k] and y[k-1]
        ///  it will some times be neccessary, to append the trailing indices
        /// </summary>
        static public List<int> AppendTrailingIndices(List<int> indiceArray)
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
                    retVal[i] = (int)valuteToReturnElementIsNaN;
                else
                    retVal[i] = array1[i] + val2;
            }
            return retVal;
        }


        ///<summary>
        /// All checks for NaN will test both for Double.IsNan and if value== a specific "nan" value (-9999)
        ///</summary>
        private static bool IsNaN(double value)
        {
            if (double.IsNaN(value))
                return true;
            else
                return false;
        }


        ///<summary>
        /// given a list of sorted indeces and a desired vector size N, returns the indices that are not in "sortedIndices"
        /// i.e. of the "other vectors
        ///</summary>
        public static List<int> InverseIndices(int N, List<int> sortedIndices)
        {
            List<int> ret = new List<int>();

            if (sortedIndices == null)
            {
                return Index.MakeIndexArray(0, N - 1).ToList();
            }
            if (sortedIndices.Count() == 0)
            {
                return Index.MakeIndexArray(0, N - 1).ToList();
            }
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
        ///  Returns element-wise maximum of array element and value
        ///</summary>
        public static int[] Max(int[] array, int value)
        {
            int[] retArray = new int[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                int thisNum = array[i];
                //      if (IsNaN(thisNum))
                //          continue;
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
        ///  Removes ceratin indices from the array
        ///</summary>
        public static int[] Remove(int[] array, List<int> indicesToRemove)
        {
            if (indicesToRemove == null)
                return array;
            if (indicesToRemove.Count == 0)
                return array;

            List<int> new_array = new List<int>();
            for (int i = 0; i < array.Length; i++)
            {
                if (!indicesToRemove.Contains(array[i]))
                    new_array.Add(array[i]);
            }
            return new_array.ToArray() ;
        }


        ///<summary>
        /// subtracts val2 from array2 elements
        ///</summary>
        public static int[] Subtract(int[] array1, int val2)
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
        /// returns the union of array1 and array2, a list of elements that are in either vector
        ///</summary>
        public static List<int> Union(List<int> vec1, List<int> vec2)
        {
            List<int> c = vec1.Union(vec2).ToList();
            c.Sort();
            return c;
        }


    }
}
