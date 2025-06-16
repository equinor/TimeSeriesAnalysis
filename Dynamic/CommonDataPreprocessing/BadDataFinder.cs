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
    /// destory identification of dynamic terms.
    /// </summary>
    static class BadDataFinder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inData"></param>
        /// <param name="badValueIndicatingValue"></param>
        /// <returns></returns>
        static public  List<int> GetAllBadIndices(double[] inData, double badValueIndicatingValue)
        {
            List<int> badValueIndices = GetBadValueIndices(inData, badValueIndicatingValue);
            //   List<int> interpolatedIndices = GetIndicesWhereDataSeemsInterpolatedByIMS(inData);

            List<int> badIndices = badValueIndices;
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
        static public List<int> GetAllBadIndicesPlussNext(double[] inData, double badValueIndicatingValue)
        {
            return Index.AppendTrailingIndices(GetAllBadIndices(inData, badValueIndicatingValue));
        }

        static private List<int> GetBadValueIndices(double[] inData, double badValueIndicatingValue)
        {
            List<int> badIndices = (new Vec(badValueIndicatingValue)).FindValues(inData, badValueIndicatingValue, VectorFindValueType.NaN);
           // List<int> interpolatedIndices = GetIndicesWhereDataSeemsInterpolatedByIMS(inData);
            return badIndices;
        }

        // in some cases if the IMS is sampled too frequntly, the ims will return interpolated 
        // data to "fill in" the data request, this interpolated data needs to be identified and removed,
        // otherwise it will fill in. In general you cannot expect this interpolated data to follow a
        // steady pattern, in some cases you may have four good data points and one bad, followed by eight good data points, 
        // for instance. 

        static public List<int> GetIndicesWhereDataSeemsInterpolatedByIMS(double[] inData)
        {
            List<int> interpolatedDataInd = new List<int>();
            for (int i = 1; i < inData.Count(); i++)
            {
                if (inData[i] == inData[i - 1])
                {
                    interpolatedDataInd.Add(i);
                }
            }
            return interpolatedDataInd;
        }

    }
}
