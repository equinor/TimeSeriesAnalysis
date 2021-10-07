using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Class that describes of the pid-controller is to scale the process value y and also the range of u.
    /// </summary>

    public class PIDscaling
    {
        // init with default values

        // Note that different PID-implementation implement scaling differently
        // AIM "PIDcon": does _not_ scale KP and TI
        // AIM "PIDx" : _DOES_ scale KP and TI

        private bool sasPIDimplementationScalesKp = false;   

        private double y_min;
        private double y_max;
        private double u_min;
        private double u_max;
        private bool   isDefault;
        private bool isEstimated; // if no scaling info is given, in some cases we may guess/estimate umin/umax if constraint is active

        public PIDscaling()
        {
            SetDefault();
        }
        public PIDscaling(double y_min, double y_max, double u_min, double u_max,
            bool isKpScalingKpOn)
        {
            Set(y_min,  y_max,  u_min,  u_max, isKpScalingKpOn);
        }


        public void SetDefault()
        {
            isEstimated = false;
            isDefault = true;
            sasPIDimplementationScalesKp = false;
            /* ySP_min = 0;
             ySP_max = 100;
             yMeas_min = 0;
             yMeas_max = 100;*/
            y_min = 0;
            y_max = 100;
            u_min = 0;
            u_max = 100;
        }


        public void Set(double y_min, double y_max, double u_min, double u_max,
            bool isKpScalingKpOn)
        {
            isDefault = false;
            this.y_min = y_min;
            this.y_max = y_max;
            this.u_min = u_min;
            this.u_max = u_max;
            this.sasPIDimplementationScalesKp = isKpScalingKpOn;
        }

        public void SetEstimatedUminUmax(double u_min, double u_max)
        {
            isDefault = false;
            isEstimated = true;
            this.u_min = u_min;
            this.u_max = u_max;
        }

        public bool IsEstimated()
        {
            return isEstimated;
        }


        public double GetYScaleFactor()
        {
            if (sasPIDimplementationScalesKp)
            {
                double yRange = y_max - y_min;
                if (yRange > 0)
                    return 100 / yRange; // so if y_max =100, and y_min=0, then scale factor should be 1 i.e. no scaling
                else
                    return 1;//error
            }
            else
                return 1;
        }

        public double ScaleYValue(double y_abs)
        {
            return (y_abs - y_min) / (y_max - y_min);
        }


        public double GetUScaleFactor()
        {
            if (sasPIDimplementationScalesKp)
            {
                double uRange = u_max - u_min;
                if (uRange > 0)
                    return 100 / uRange; // so if y_max =100, and y_min=0, then scale factor should be 1 i.e. no scaling
                else
                    return 1;//error
            }
            else
                return 1;
        }

        public double GetKpScalingFactor()
        {
            return GetYScaleFactor() / GetUScaleFactor();
        }


        public void  SetKpScalingOn(bool isKpScalingKpOn)
        {
            sasPIDimplementationScalesKp = isKpScalingKpOn;
        }

        public bool IsKpScalingOn()
        {
            return sasPIDimplementationScalesKp;
        }

        public bool IsDefault() { return isDefault; }
        /*
        public double GetYspMin() { return ySP_min; }
        public double GetYspMax() { return ySP_max; }
        public double GetYMeasMin() { return yMeas_min; }
        public double GetYMeasMax() { return yMeas_max; }
        */

        public double GetYmin() { return y_min; }
        public double GetYmax() { return y_max; }
        public double GetUmin() { return u_min; }
        public double GetUmax() { return u_max; }
    }

}
