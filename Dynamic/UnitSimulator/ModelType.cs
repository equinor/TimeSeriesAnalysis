using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Enum of recognized types of unit-models.
    /// <para>
    /// This type is used to set the unit model unique identifier and is also used 
    /// in internal logic in the large-scale dynamic simulations</para>
    /// </summary>
    public enum ModelType
    {
        /// <summary>
        /// Type is not set
        /// </summary>
        UnTyped = 0,

        /// <summary>
        /// PID-controller model
        /// </summary>
        PID= 1,

        /// <summary>
        /// SubProcess model (a "normal" unit process)
        /// </summary>
        SubProcess = 2,

        /// <summary>
        /// Disturbance model
        /// </summary>
        Disturbance = 3,

        /// <summary>
        /// Select-block
        /// </summary>
        Select = 4,

        /// <summary>
        /// Divide block
        /// </summary>
        Divide = 5,

        /// <summary>
        /// Divide block
        /// </summary>
        GainSchedModel = 6
    }
}
