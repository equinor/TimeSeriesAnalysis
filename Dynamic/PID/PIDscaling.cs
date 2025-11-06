using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Parameters describing PID-controller parameters for scaling 
    /// <seealso cref="PidModel"/>
    /// <seealso cref="PidController"/>
    /// </summary>

    public class PidScaling
    { 

        /// <summary>
        /// Determines if the SAS scales Kp according to the scaling information in this object or not.
        /// Set this to match the reality in the SAS you are studying.
        /// For example: 
        ///  AIM "PIDcon": does _not_ scale KP and TI
        ///  AIM "PIDx" : _DOES_ scale KP and TI
        /// </summary>
        public bool doesSasPidScaleKp = false;

        /// <summary>
        /// the minimum pid input(process-) value
        /// </summary>
        public double y_min;
        /// <summary>
        /// The maximum pid input(process-) value
        /// </summary>
        public double y_max;
        /// <summary>
        /// The minimum pid output(manipulated-) value
        /// </summary>
        public double u_min;
        /// <summary>
        /// The maximum pid output(mainipulatd-) value
        /// </summary>
        public double u_max;
        /// <summary>
        /// If true, then all min/max are at their default value
        /// </summary>
        public bool isDefault;
        /// <summary>
        /// If true then these parameters have been guessed automatically from data
        /// </summary>
        public bool isEstimated; 

        /// <summary>
        /// Constructor
        /// </summary>
        public PidScaling()
        {
            SetDefault();
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="y_min"></param>
        /// <param name="y_max"></param>
        /// <param name="u_min"></param>
        /// <param name="u_max"></param>
        /// <param name="isKpScalingKpOn"></param>
        public PidScaling(double y_min, double y_max, double u_min, double u_max,
            bool isKpScalingKpOn)
        {
            Set(y_min,  y_max,  u_min,  u_max, isKpScalingKpOn);
        }


        /// <summary>
        /// Set scaling to default
        /// </summary>
        public void SetDefault()
        {
            isEstimated = false;
            isDefault = true;
            doesSasPidScaleKp = false;
            y_min = 0;
            y_max = 100;
            u_min = 0;
            u_max = 100;
        }


        /// <summary>
        /// Set scaling
        /// </summary>
        /// <param name="y_min"></param>
        /// <param name="y_max"></param>
        /// <param name="u_min"></param>
        /// <param name="u_max"></param>
        /// <param name="isKpScalingKpOn"></param>
        public void Set(double y_min, double y_max, double u_min, double u_max,
            bool isKpScalingKpOn)
        {
            isDefault = false;
            this.y_min = y_min;
            this.y_max = y_max;
            this.u_min = u_min;
            this.u_max = u_max;
            this.doesSasPidScaleKp = isKpScalingKpOn;
        }

        /// <summary>
        /// Set the estimated u_min and u_max (if it is not known, but guessed from data)
        /// </summary>
        /// <param name="u_min"></param>
        /// <param name="u_max"></param>
        public void SetEstimatedUminUmax(double u_min, double u_max)
        {
            isDefault = false;
            isEstimated = true;
            this.u_min = u_min;
            this.u_max = u_max;
        }

        /// <summary>
        /// Ask if the scaling is estimated from data or given a priori
        /// </summary>
        /// <returns></returns>
        public bool IsEstimated()
        {
            return isEstimated;
        }


        /// <summary>
        /// Get the scaling factor for Y
        /// </summary>
        /// <returns></returns>
        public double GetYScaleFactor()
        {
            if (doesSasPidScaleKp)
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

        /// <summary>
        /// Get the scaled version of an absolute y
        /// </summary>
        /// <param name="y_abs"></param>
        /// <returns></returns>
        public double ScaleYValue(double y_abs)
        {
            return (y_abs - y_min) / (y_max - y_min);
        }


        /// <summary>
        /// Get the scaling factor of U
        /// </summary>
        /// <returns></returns>
        public double GetUScaleFactor()
        {
            if (doesSasPidScaleKp)
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

        /// <summary>
        /// Get a scaling factor to convert and unscaled Kp
        /// 
        /// By defintion KpUnscaled = Kp / KpScalingFactor
        /// 
        /// </summary>
        /// <returns></returns>
        public double GetKpScalingFactor()
        {
            return GetUScaleFactor() / GetYScaleFactor(); // verified as correct
          //   return GetYScaleFactor() / GetUScaleFactor(); // does not match AIM behaviour
        }


        /// <summary>
        /// Turn scaling on or off
        /// </summary>
        /// <param name="isKpScalingKpOn"></param>
        public void  SetKpScalingOn(bool isKpScalingKpOn)
        {
            doesSasPidScaleKp = isKpScalingKpOn;
        }

        /// <summary>
        /// Ask if scaling of Kp is active
        /// </summary>
        /// <returns></returns>
        public bool IsKpScalingOn()
        {
            return doesSasPidScaleKp;
        }

        /// <summary>
        /// Ask if scaling is at defautl values
        /// </summary>
        /// <returns></returns>
        public bool IsDefault() { return isDefault; }


        /// <summary>
        /// Get the minumum Y
        /// </summary>
        /// <returns></returns>
        public double GetYmin() { return y_min; }
        /// <summary>
        /// Get the maximum Y
        /// </summary>
        /// <returns></returns>
        public double GetYmax() { return y_max; }

        /// <summary>
        /// Get the minimum U
        /// </summary>
        /// <returns></returns>
        public double GetUmin() { return u_min; }
        /// <summary>
        /// Get the maximum U
        /// </summary>
        /// <returns></returns>
        public double GetUmax() { return u_max; }
    }

}
