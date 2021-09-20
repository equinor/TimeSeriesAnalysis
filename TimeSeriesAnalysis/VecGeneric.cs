﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{

    /// <summary>
    /// Class for generic methods on any type T that treat arrays as vectors(sorting,slicing,concatenating). 
    /// For mathematical methods on vectors of doubles and integers, look into non-generic sister class "Vec".
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Vec<T>
    {
        ///<summary>
        /// sort the vector vec acording to the sortType.
        ///</summary>
        public static T[] Sort(T[] vec, VectorSortType sortType)
        {
            return Vec<T>.Sort(vec, sortType, out _);
        }

        ///<summary>
        /// sort the vector vec acording to the sortType. The indices corresponding tot he sorted values are given out in the idx array.
        ///</summary>
        public static T[] Sort(T[] vec, VectorSortType sortType, out int[] idx)
        {

            if (vec == null)
            {
                idx = new int[0];
                return new T[0];
            }
            T[] sortedAsc = new T[vec.Length];
            idx = new int[vec.Length];
            for (int i = 0; i < vec.Length; i++)
                idx[i] = i;

            Array.Copy(vec, sortedAsc, vec.Length);
            Array.Sort(sortedAsc, idx);  // Sort array in ascending order. 
            if (sortType == VectorSortType.Descending)
            {
                Array.Reverse(sortedAsc);
                Array.Reverse(idx);
            }
            return sortedAsc;
        }

        ///<summary>
        /// returns the portion of array1 starting and indStart, and ending at indEnd(or at the end if third paramter is omitted)
        ///</summary>
        public static T[] SubArray(T[] array1, int indStart, int indEnd = -9999)
        {
            if (array1 == null)
                return null;

            if (indEnd > array1.Length - 1 || indEnd == -9999)
                indEnd = array1.Length - 1;
            else if (indEnd < 0)
            {
                indEnd = 0;
                return new T[0];
            }
            if (indStart < 0)
                indStart = 0;
            int length = indEnd - indStart + 1;
            T[] retArray = new T[length];
            int outInd = 0;
            for (int i = indStart; i <= indEnd; i++)
            {
                retArray[outInd] = array1[i];
                outInd++;
            }
            return retArray;
        }

        ///<summary>
        /// creates an array of size N where every element has value value
        ///</summary>
        public static T[] Fill(T value, int N)
        {
            T[] ret = new T[N];

            for (int i = 0; i < N; i++)
                ret[i] = value;
            return ret;
        }

        ///<summary>
        /// concatenates arrays x and y into a new larger array
        ///</summary>
        public static T[] Concat(T[] x, T[] y)
        {
            var z = new T[x.Length + y.Length];
            x.CopyTo(z, 0);
            y.CopyTo(z, x.Length);
            return z;
        }

        ///<summary>
        /// concatenates the value y to the end of array x
        ///</summary>
        public static T[] Concat(T[] x, T y)
        {
            var z = new T[x.Length + 1];
            x.CopyTo(z, 0);
            z[z.Length - 1] = y;
            return z;
        }


        ///<summary>
        /// replaces all the vaules in array with indices in indList with the last good value
        /// prior to that index.
        ///</summary>
        public static double[] ReplaceIndWithValuesPrior(double[] array, List<int> indList)
        {
            int[] vecInd = indList.ToArray();

            int lastVecInd = -1;
            double lastReplacementValue = -1;
            for (int curIndInd = 0; curIndInd < vecInd.Length; curIndInd++)
            {
                int curVecInd = vecInd[curIndInd];
                if (curVecInd > 0)
                {
                    if (lastVecInd == curVecInd - 1)
                    {
                        array[curVecInd] = lastReplacementValue;
                    }
                    else
                    {
                        array[curVecInd] = array[curVecInd - 1];
                        lastReplacementValue = array[curVecInd];
                    }
                }
                lastVecInd = curVecInd;
            }
            return array;
        }


        ///<summary>
        /// returns an array of the values that are in array at the indeices given by indices list
        ///</summary>

        public static T[] GetValuesAtIndices(T[] array, List<int> indices)
        {
            T[] ret = new T[indices.Count()];

            for (int i = 0; i < indices.Count(); i++)
            {
                ret[i] = array[indices.ElementAt(i)];
            }
            return ret;
        }



    }



   
}