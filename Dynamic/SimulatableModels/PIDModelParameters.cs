using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Parameters of the PIDModel
    /// <seealso cref="PIDModel"/>
    /// <seealso cref="PIDcontroller"/>
    /// </summary>
    public class PIDModelParameters:ModelParametersBaseClass 
    {
        /// <summary>
        /// Proportional gain of controller
        /// </summary>
        public double Kp { get; set; } = 0;
        /// <summary>
        /// Integral time constant [in seconds], 0 = no integral term
        /// </summary>
        public double Ti_s { get; set; } = 0;

        /// <summary>
        /// Derivative term time constant[in seconds], 0 = no derivative term
        /// </summary>
        public double Td_s { get; set; } = 0;

        /// <summary>
        /// If the PID-controller is to be protected from a specific value that is used to identify bad or missing data, specify here
        /// </summary>
        public double NanValue { get; set; } = -9999;

        /// <summary>
        /// Gain-scheduling object. This is optional, set to null gain-scheduling is not in use.
        /// </summary>
        public PIDgainScheduling GainScheduling { get; set; } = null;

        /// <summary>
        /// Feed-forward parameters object. This is optional, set to null when feedforward is not in use.
        /// </summary>
        public PIDfeedForward FeedForward { get; set; } = null;

        /// <summary>
        /// PID-scaling object. This is optional, set to null to use unscaled PID.
        /// </summary>
        public PIDscaling Scaling { get; set; }

        /// <summary>
        /// PID anti-surge paramters objet. This is optional, set to null if not anti-surge PID
        /// </summary>
        public PIDAntiSurgeParams AntiSugeParams { get; set; } = null;

    }
}
