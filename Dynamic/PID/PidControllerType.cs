using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic
{
    public enum ControllerType
    {
        Unset = 0,
        Flow = 1,
        Pressure = 2,
        Level = 3,
        Temperature = 4,
        Duty = 5,
        AntiSurge = 6
    }
}
