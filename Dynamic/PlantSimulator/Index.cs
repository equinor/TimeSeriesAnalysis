using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Enum to provide more readable code, for the input instance <c>(int)INDEX.FIRST</c> may be more readable than "0"
    /// </summary>
    public enum INDEX // this is just here to improve readability
    {
        /// <summary>
        /// First index(0)
        /// </summary>
        FIRST = 0,
        /// <summary>
        ///  Second index(1)
        /// </summary>
        SECOND = 1,
        /// <summary>
        ///  Third index(2)
        /// </summary>
        THIRD = 2,
        /// <summary>
        ///  Fourth index(3)
        /// </summary>
        FOURTH = 3,
        /// <summary>
        ///  Fifth index(4)
        /// </summary>
        FIFTH = 4
    }

}
