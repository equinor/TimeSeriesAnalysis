using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Class that expresses the tuning of a PIDcontroller
    /// </summary>
    public class PIDtuning
    {
        private double Kp;
        private double Ti;
        private double Td;
        private bool? isReversed= null;


        /// <summary>
        /// Initalize tuning,Ti and Td are expected to be in seconds, and if Kp sign is to be reversed
        /// </summary>

        public PIDtuning(double Kp, double Ti, double Td=0, bool isReversed=false)
        {
            this.Kp = Kp;
            this.Ti = Ti;
            this.Td = Td;
            this.isReversed = isReversed;
        }

        /// <summary>
        /// Initalize tuning of Kp and Ti and if controller is reversed.
        /// </summary>

        public PIDtuning(double Kp, double Ti, bool isReversed=false)
        {
            this.Kp = Kp;
            this.Ti = Ti;
            this.isReversed = isReversed;
        }

        /// <summary>
        /// Specifies that Kp is to be reversed (By default Kp is not reversed)
        /// </summary>
        public void SetReversed()
        {
            this.isReversed = true;
        }

        /// <summary>
        /// Gets the Kp, including the sign(accounts for pid-controller being set to reverese Kp sign)
        /// </summary>
        public double GetKp()
        {
            if (isReversed.HasValue)
            {
                if (isReversed.Value == true)
                    return -Kp;
                else
                    return Kp;
            }
            return Kp;
        }

        /// <summary>
        /// Gets the Ti in seoncds
        /// </summary>
        public double GetTi()
        {
            return Ti;
        }

        /// <summary>
        /// Gets the Td in seoncds
        /// </summary>
        public double GetTd()
        {
            return Td;
        }

        /// <summary>
        /// Returns true if the K sign is to be reversed
        /// </summary>
        public bool IsReversed()
        {
            if (isReversed.HasValue)
            {
                    return isReversed.Value;
            }
            else
               return false;
        }




    }
}
