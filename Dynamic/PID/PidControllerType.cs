using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// The type of PID-controller.
    /// </summary>
    public enum PidControllerType
    {
        /// <summary>
        /// Usnet
        /// </summary>
        Unset = 0,
        /// <summary>
        /// Flow-controller
        /// </summary>
        Flow = 1,
        /// <summary>
        /// Pressure-controller
        /// </summary>
        Pressure = 2,
        /// <summary>
        /// Level-controller
        /// </summary>
        Level = 3,
        /// <summary>
        /// Temperature-controller
        /// </summary>
        Temperature = 4,
        /// <summary>
        /// Duty-controller
        /// </summary>
        Duty = 5,
        /// <summary>
        /// Anti-surge controller
        /// </summary>
        AntiSurge = 6
    }
}
