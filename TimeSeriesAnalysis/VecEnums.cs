using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{
    public enum VectorFindValueType
    {
        BiggerThan = 1,
        SmallerThan = 2,
        BiggerOrEqual = 3,
        SmallerOrEqual = 4,
        Equal = 5,
        NaN = 6,
        NotNaN = 7
    }

    public enum VectorSortType
    {
        Ascending = 1,
        Descending = 2
    }

}
