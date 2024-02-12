using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Parameters of the PIDModel
    /// <seealso cref="PidModel"/>
    /// <seealso cref="PidController"/>
    /// </summary>
    public class PidParameters:ModelParametersBaseClass 
    {
        private List<PidIdentWarning> warnings;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PidParameters()
        {
            warnings = new List<PidIdentWarning>();
            Scaling = new PidScaling();
            GainScheduling = new PidGainScheduling();
            FeedForward = new PidFeedForward();
            Filtering = new PidFilterParams();
        }

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
        /// Output value when e=0, used only for P-only controllers, u = Kp*e + u0
        /// </summary>
        public double u0 { get; set; } = 50;

        /// <summary>
        /// If the PID-controller is to be protected from a specific value that is used to identify bad or missing data, specify here
        /// </summary>
        public double NanValue { get; set; } = -9999;

        /// <summary>
        /// Gain-scheduling object. This is optional, set to null gain-scheduling is not in use.
        /// </summary>
        public PidGainScheduling GainScheduling { get; set; } = null;

        /// <summary>
        /// Feed-forward parameters object. This is optional, set to null when feedforward is not in use.
        /// </summary>
        public PidFeedForward FeedForward { get; set; } = null;

        /// <summary>
        /// PID-scaling object. This is optional, set to null to use unscaled PID.
        /// </summary>
        public PidScaling Scaling { get; set; }

        /// <summary>
        /// PID-filtering object. This is optional
        /// </summary>
        public PidFilterParams Filtering { get; set; }

        /// <summary>
        /// PID anti-surge parameters object. This is optional, set to null if not anti-surge PID
        /// </summary>
        public PidAntiSurgeParams AntiSurgeParams { get; set; } = null;

        /// <summary>
        /// Store a warning that arouse during identification
        /// </summary>
        /// <param name="warning"></param>
        public void AddWarning(PidIdentWarning warning)
        {
            warnings.Add(warning);
        }

        /// <summary>
        /// Gives any warnings that occured during identification
        /// </summary>
        /// <returns></returns>
        public PidIdentWarning[] GetWarnings()
        { 
            return warnings.ToArray();
        }

        /// <summary>
        /// Creates a string summarizing the content of the paramters(useful for unit testing and debugging)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            int sDigits = 3;

            const int cutOffForUsingDays_s = 86400;
            const int cutOffForUsingHours_s = 3600;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine("-------------------------");
            if (Fitting != null)
            {
                if (Fitting.WasAbleToIdentify)
                {
                    sb.AppendLine("ABLE to identify");
                }
                else
                {
                    sb.AppendLine("---NOT able to identify---");
                }
            }
            sb.AppendLine("Kp : " + SignificantDigits.Format(Kp,sDigits));

            string timeConstantString = "Ti : ";
            // time constant
            if (Ti_s < cutOffForUsingHours_s)
            {
                timeConstantString +=
                    SignificantDigits.Format(Ti_s, sDigits) + " sec";
            }
            else if (Ti_s < cutOffForUsingDays_s)
            {
                timeConstantString +=
                    SignificantDigits.Format(Ti_s / 3600, sDigits) + " hours";
            }
            else // use days
            {
                timeConstantString +=
                    SignificantDigits.Format(Ti_s / 86400, sDigits) + " days";
            }
            sb.AppendLine(timeConstantString);
            sb.AppendLine("Td : " + SignificantDigits.Format(Td_s, sDigits)+ " sec");

            if (Scaling.IsEstimated())
            {
                sb.AppendLine("Scaling has been estimated by the algorithm:");
            }
            else if (Scaling.IsDefault())
            {
                sb.AppendLine("Scaling not given, default values used:");
            }
            else
            {
                sb.AppendLine("Scaling specified externally:");
            }

            sb.AppendLine("Umin : " +Scaling.GetUmin());
            sb.AppendLine("Umax : " + Scaling.GetUmax());
            sb.AppendLine("Ymin : " + Scaling.GetYmin());
            sb.AppendLine("Ymax : " + Scaling.GetYmax());
            if (GainScheduling == null)
            {
                sb.AppendLine("NO gainsceduling");
            }
            else
            {
                sb.AppendLine("Gainsceduling configured");//todo:give output
            }
            if (FeedForward == null)
            {
                sb.AppendLine("NO feedforward");
            }
            else
            {
                sb.AppendLine("Feedforward configured");//todo:give output
            }

            if (Filtering == null)
            {
                sb.AppendLine("NO filtering");
            }
            else
            {
                sb.AppendLine("Filtering configured");//todo:give output
            }

            return sb.ToString();

        }


    }
}
