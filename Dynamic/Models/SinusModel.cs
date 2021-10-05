using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// A model of a sinusioid. 
    /// This class is mainly intended for testing.
    /// </summary>
    public class SinusModel : IProcessModel<SinusModelParameters>
    {
        private SinusModelParameters parameters;
        private double timeBase_s;

        private double lastOutput ;
        private int iterationCounter;


        public SinusModel(SinusModelParameters parameters,double timeBase_s)
        {
            this.parameters = parameters;
            this.timeBase_s = timeBase_s;
            this.iterationCounter = 0;
            this.lastOutput = 0;
        }

        public SinusModelParameters GetModelParameters()
        {
            return parameters;
        }

        public double Iterate(double[] inputsU,double badValueIndicator=-9999)
        {
            double amplitude     = parameters.amplitude;
            double sinusPeriod_s = parameters.period_s;
            lastOutput = amplitude *
                Math.Sin((iterationCounter * timeBase_s) / sinusPeriod_s * Math.PI * 2);

            iterationCounter++;
            return lastOutput;
        }
    }
}
