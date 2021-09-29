using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Parameters belonging to the <c>SinusModel</c> class.
    /// Note that this class implements the <c>IProcessModelParameters</c> as it is not intended to be 
    /// fitted against data, but is useful more as a signal generator for testing.
    /// </summary>
    public class SinusModelParameters : IProcessModelParameters
    {
        public double amplitude;
        public double period_s ;
        // TODO: add support for phase-shift
    }
}
