using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Recursive average
    /// </summary>
    public class RecursiveAverage

    {

        private double retVal ;
        private double N ;
        private double nanValue = -9999;
        private Vec vec;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nanValue">valeue to be treated as NaN and ignored if applicable</param>
        public RecursiveAverage(double nanValue = -9999)
        {
            Reset();
            vec = new Vec(nanValue);
            this.nanValue = nanValue;
        }

        private bool IsNaN(double value)
        {
            if (double.IsNaN(value) || value == nanValue)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Adds one data point to the recursive average, ignoring NaN values
        /// </summary>
        public void AddDataPoint(double value)
        {
            if (!IsNaN(value))
            {
                N += 1;
                retVal = retVal * (N - 1) / N + value * 1 / N;
            }
        }
        /// <summary>
        /// Gets average over all data points added since last reset
        /// </summary>
        public double GetAverage()
        {
            return retVal;
        }

        /// <summary>
        /// Resets filter
        /// </summary>
        public void  Reset()
        {
            retVal = 0;
            N = 0;
        }

    }
}
