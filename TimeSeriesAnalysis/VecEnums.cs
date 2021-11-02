using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Input to <c>Vec.FindValues</c> which specifies the criteria of the search
    /// </summary>
    public enum VectorFindValueType
    {
        /// <summary>
        /// ">" : Find values which are bigger than
        /// </summary>
        BiggerThan = 1,
        /// <summary>
        ///  Find values which are smaller than
        /// </summary>
        SmallerThan = 2,
        /// <summary>
        /// ">=" : Find values which are bigger than or equal
        /// </summary>
        BiggerOrEqual = 3,
        /// <summary>
        /// Find values which are smaller than or equal
        /// </summary>
        SmallerOrEqual = 4,
        /// <summary>
        /// "==": Find values which are equal
        /// </summary>
        Equal = 5,
        /// <summary>
        /// Find values that are Double.NaN
        /// </summary>
        NaN = 6,
        /// <summary>
        /// Find values that are NOT Double.NaN
        /// </summary>
        NotNaN = 7
    }

    /// <summary>
    /// Input to <c>Vec.Sort</c> that specifies how values are to be sorted
    /// </summary>
    public enum VectorSortType
    {
        /// <summary>
        /// Sort in ascending order (smallest first)
        /// </summary>
        Ascending = 1,

        /// <summary>
        /// Sort in descending order (biggest first)
        /// </summary>
        Descending = 2
    }

}
