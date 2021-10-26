using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Class that contains special pid-controller parameters for gain-scheduling controllers
    /// </summary>
    public class PIDgainScheduling
    {
        public PIDgainScheduling()
        {
            GSActive_b = false;
            GSActiveTi_b = false;
        }

        public PIDgainScheduling(double TimeStep_s, double GSVariableLP_Tc_s)
        {
            this.GSVariableLP_Tc_s = GSVariableLP_Tc_s;
            this.TimeStep_s = TimeStep_s;

            gsFilter = new LowPass(TimeStep_s); 

        }
        LowPass gsFilter;

        double TimeStep_s;

        /// <summary>
        /// if TRUE then the gainScheduling variable and gain-scheduling inputs are used
        /// </summary>
        public bool GSActive_b =  false;

        /// <summary>
        /// time constant of low-pass filtering of the gain-scheduling variable
        /// </summary>
        double GSVariableLP_Tc_s ;

        /// <summary>
        /// Gain-sheduling(x) variable minimum 
        /// </summary>
        public double GS_x_Min;

        /// <summary>
        /// Gain-sheduling(x) variable x1,x=GsVariable
        /// </summary>
        public double GS_x_1;

        /// <summary>
        /// Gain-sheduling(x) variable  x2,x=GsVariable
        /// </summary>
        public double GS_x_2;

        /// <summary>
        /// Gain-sheduling(x) variable maximum 
        /// </summary>
        public double GS_x_Max;

        /// <summary>
        /// KP(proportional term)  @ GsVariable=GS_x_Min
        /// </summary>
        public double GS_Kp_Min;

        /// <summary>
        ///  KP(proportional term)  @ GsVariable=GS_x_1
        /// </summary>
        public double GS_Kp_1;

        /// <summary>
        /// KP(proportional term)  @ GsVariable=GS_x_2
        /// </summary>
        public double GS_Kp_2;

        /// <summary>
        /// KP(proportional term) @ GsVariable=GS_x_Max
        /// </summary>
        public double GS_Kp_Max;

        /// <summary>
        /// if TRUE then the gainScheduling is also done on Ti
        /// </summary>
        public bool GSActiveTi_b = false;

        /// <summary>
        /// Ti(integral effect) @ GsVariable=GS_x_Min
        /// </summary>
        public double GS_Ti_Min;

        /// <summary>
        /// Ti(integral effect) @ GsVariable=GS_x_1
        /// </summary>
        public double GS_Ti_1;

        /// <summary>
        /// Ti(integral effect) @ GsVariable=GS_x_2
        /// </summary>
        public double GS_Ti_2;

        /// <summary>
        /// Ti(integral effect) @ GsVariable=GS_x_Max
        /// </summary>
        public double GS_Ti_Max;

        /// <summary>
        /// Get the linear interpolated Kp and Ti for a given value of the gainSchedulingVariable
        /// </summary>
        /// <param name="gainSchedulingVariable"></param>
        /// <param name="Kp"></param>
        /// <param name="Ti"></param>
        internal void GetKpandTi(double gainSchedulingVariable, out double? Kp, out double? Ti)
        {
            double gsVaribleFiltered = gainSchedulingVariable;
            if (gsFilter != null)
            {
                gsVaribleFiltered = gsFilter.Filter(gainSchedulingVariable, GSVariableLP_Tc_s);
            }
            Kp = null;
            Ti = null;

            double x = gsVaribleFiltered;
            // (*interpolate Kp based on the value of PID.GSVariable(x) compared to GS_x_Min1,GS_x_Min2,GS_x_Max1,GS_x_Min2 *)

            if (GSActive_b)
            {
                double x1, x2, y1, y2;
                if (x < GS_x_1)
                {
                    x1 = GS_x_Min;
                    x2 = GS_x_1;
                    y1 = GS_Kp_Min;
                    y2 = GS_Kp_1;
                }
                else
                {
                    if (x > GS_x_2)
                    {
                        x1 = GS_x_2;
                        x2 = GS_x_Max;
                        y1 = GS_Kp_2;
                        y2 = GS_Kp_Max;
                    }
                    else//(*GS_x_1 < x < GS_x_2 *)
                    {
                        x1 = GS_x_1;
                        x2 = GS_x_2;
                        y1 = GS_Kp_1;
                        y2 = GS_Kp_2;
                    }
                }
                double y = (y2 - y1) / (x2 - x1) * (x - x1) + y1; //*Linear interpolation between two points *)
                Kp = y;
            }

            if (GSActiveTi_b)
            {
                double x1, x2, y1, y2;
                double y;
                if (x < GS_x_1)
                {
                    x1 = GS_x_Min;
                    x2 = GS_x_1;
                    y1 = GS_Ti_Min;
                    y2 = GS_Ti_1;
                }
                else
                {
                    if (x > GS_x_2)
                    {
                        x1 = GS_x_2;
                        x2 = GS_x_Max;
                        y1 = GS_Ti_2;
                        y2 = GS_Ti_Max;
                    }
                    else //(*GS_x_1 < x < GS_x_2 *)
                    {
                        x1 = GS_x_1;
                        x2 = GS_x_2;
                        y1 = GS_Ti_1;
                        y2 = GS_Ti_2;
                    }

                }
                y= (y2 - y1) / (x2 - x1) * (x - x1) + y1; //(*Linear interpolation between two points *)
	            Ti= y;
            }
        }
    }
}
