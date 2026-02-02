using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{


    /// <summary>
    /// For dynamic model identification in system identification, bad data points will create "spurious dynamics"
    /// that it is especially important to filter out, otherwise it may 
    /// destroy identification of dynamic terms.
    /// </summary>
    public static class BadDataFinder
    {
        /// <summary>
        /// Get all the values which are NaN or equal to the badValueIndicatingValue for a single vector/array
        /// </summary>
        /// <param name="inData"></param>
        /// <param name="badValueIndicatingValue"></param>
        /// <returns></returns>
        static public  List<int> GetAllBadIndices(double[] inData, double badValueIndicatingValue)
        {
            List<int> badValueIndices = GetBadValueIndices(inData, badValueIndicatingValue);

            List<int> badIndices = badValueIndices;
            return badIndices;
        }

        /// <summary>
        /// Get all the values which are NaN or equal to the badValueIndicatingValue for any and all datapoints in an entire
        /// TimeSeriesDataSet
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="badValueIndicatingValue"></param>
        /// <returns></returns>
        static public List<int> GetAllBadIndices(TimeSeriesDataSet inputData, double badValueIndicatingValue)
        {
            List<int> badIndices = new List<int>();

            foreach (var signalName in inputData.GetSignalNames())
            {
                var curData = inputData.GetValues(signalName);
                var curBadindices = GetBadValueIndices(curData, badValueIndicatingValue);
                badIndices = badIndices.Union(curBadindices).ToList();  
            }
            badIndices.Sort();
            return badIndices;
        }

        /// <summary>
        /// Get the bad value indices AND the indices trailing them. 
        /// This is useful when considering difference equations that require the values both at index <c>k</c> and <c>k-1</c> to 
        /// perform identification.
        /// </summary>
        /// <param name="inData"></param>
        /// <param name="badValueIndicatingValue"></param>
        /// <returns></returns>
        static public List<int> GetAllBadIndicesPlusNext(double[] inData, double badValueIndicatingValue)
        {
            return Index.AppendTrailingIndices(GetAllBadIndices(inData, badValueIndicatingValue));
        }

        static private List<int> GetBadValueIndices(double[] inData, double badValueIndicatingValue)
        {
            List<int> badIndices = (new Vec(badValueIndicatingValue)).FindValues(inData, badValueIndicatingValue, VectorFindValueType.NaN);
            return badIndices;
        }






    }
}
