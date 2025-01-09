using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Enum to classify the status output of PidController.cs
    /// </summary>
    public enum PidStatus 
    { 
        /// <summary>
        /// Controller in in manual mode (u is constant)
        /// </summary>
        MANUAL=0,
        /// <summary>
        /// Controller is in automatic mode(u varies)
        /// </summary>
        AUTO=1, 
        /// <summary>
        /// Controller is in Tracking, i.e. it is in automatic, but its output goes to a select block which has selected another controller
        /// </summary>
        TRACKING=2  
    };

    /// <summary>
    /// Proporitional-Integral-Derivative(PID) controller 
    /// <para>
    /// that supports 
    /// <list type="bullet">
    /// <item><description>first and second order low pass filtering of process variable</description></item>
    /// <item><description> anti-windup</description></item>
    /// <item><description> bumpless transfer between auto and manual mode</description></item>
    /// <item><description> "warmstarting" -bumpless startup</description></item>
    /// <item><description> feedforward </description></item>
    /// <item><description> scaling of input and output values </description></item>
    /// <item><description> gain scheduling of Kp</description></item>
    /// <item><description> gain scheduling of Ti</description></item>
    /// <item><description> "kicking" as is usually applied to compressor recycling controllers/anti-surge</description></item>
    /// <item><description> min select/max select (also referred to as high select or low select: (multiple pid-controllers controlling the same output switch between auto and tracking mode)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// By design decision, this class should be kept relativly simple in terms of coding patterns, so that it is possible to 
    /// hand-port this class to other languages (c++/c/Structured Text/Labview/Matlab etc). 
    /// To simulate PID-control, use the wrapper class PIDModel, as it wraps this class and impelments the
    /// <c>ISimulatableModel</c> interface needed to simulate with <c>ProcessSimulator</c>
    /// </para>
    /// <seealso cref="PidModel"/>
    /// </summary>
    public class PidController
    {
        private double nanValue = -9999; 

        private double TimeBase_s;
        private double Ti, Kp, Td;
        private PidFilterParams pidFilterParams;
        private double u0;
        private PidScaling pidScaling;
        private double u_prev, e_prev_unscaled, e_prev_prev_unscaled;
        private bool isInAuto;

        private double uTrackingOffset = 1;// zero: no tracking range, above zero: min select, below zero: max select -determines how much above or below the tracking signal this controller should place its output.Also determined is tracking is against min-s
        private double uTrackingCutoff;//used for tracking, how much above the active controller that the tracking controllers shoudl be

        private PidStatus controllerStatus;
        private double uIfInAuto;
        private double u_ff_withoutTracking;
        private double uManual;

        private double y_set_prc_prev;
        private PidGainScheduling gsObj = null;
        private PidFilter pidFilter;// LowPass yFilt1,yFilt2;

        private PidFeedForward ffObj;
        private LowPass ffLP; // feed-forward low pass filter object
        private LowPass ffHP; // feed-forward high pass filter object
        private bool isFFActive_prev = false;//previous value of the feed forward active signal
        private double u_ff_prev;


        // double anti-surge related:
        private PidAntiSurgeParams antiSurgeParms=null;
        private double u_ff_antisurge_prev;

        /// <summary>
        /// Constructor
        /// </summary>
        public PidController(double TimeBase_s, double Kp=1, double Ti=50, double Td=0, double nanValue=-9999)
        {
            this.nanValue = nanValue;
            this.pidScaling = new PidScaling();
            this.TimeBase_s = TimeBase_s;

            this.Kp = Kp;
            this.Ti = Ti;
            this.Td = Td;

            this.pidFilterParams = new PidFilterParams();

            this.u_prev = double.NaN;
            this.e_prev_unscaled = double.NaN;
            this.e_prev_prev_unscaled = double.NaN;

            this.y_set_prc_prev = double.NaN;

            this.isInAuto = true;

            this.gsObj = new PidGainScheduling();
            this.ffObj = new PidFeedForward();

            this.TimeBase_s = TimeBase_s;
            //            this.yFilt1 = new LowPass(TimeBase_s);
            //           this.yFilt2 = new LowPass(TimeBase_s);

            this.pidFilter = new PidFilter(pidFilterParams,TimeBase_s);

            this.ffLP = new LowPass(TimeBase_s);
            this.ffHP = new LowPass(TimeBase_s);
            
        }

        /// <summary>
        /// Set the feedforward of the controller 
        /// Re-calling this setter to update
        /// </summary>
        public void SetFeedForward(PidFeedForward feedForwardObj)
        {
            this.ffObj = feedForwardObj;
        }

        /// <summary>
        /// Set the object that defines input filtering for the pid-controller
        /// </summary>
        /// <param name="pidFiltering"></param>
        public void SetPidFiltering(PidFilterParams pidFiltering)
        {
            this.pidFilterParams = pidFiltering;
            this.pidFilter = new PidFilter(pidFilterParams, (int)TimeBase_s);
        }


        /// <summary>
        /// Get the feedforward parameters of the controller 
        /// </summary>
        public PidFeedForward GetFeedForward()
        {
            return this.ffObj ;
        }

        /// <summary>
        /// Set the gain scheduling of the controller (by default controller has no gain-scheduling)
        /// Re-calling this setter to update gain-scheduling
        /// </summary>
        public void SetGainScheduling(PidGainScheduling gainSchedulingObj)
        {
            this.gsObj = gainSchedulingObj;
        }
        /// <summary>
        /// Get the gain scheduling settings of the controller
        /// </summary>
        public PidGainScheduling GetGainScehduling(PidGainScheduling gainSchedulingObj)
        {
            return this.gsObj;
        }

        /// <summary>
        /// Returns a status code, to determine if controller is in manual, auto or in tracking(relevant for split range controllers.)
        /// </summary>

        public PidStatus GetControllerStatus()
        {
            return this.controllerStatus;
        }


        /// <summary>
        /// Sets the length of time in seconds between each iteration of the controller, i.e. the "clock time" or "time base".
        /// This is very important to set correctly for the controller to function properly. Normally the clock time is set only once during intalization.
        /// </summary>
        public double GetTimeBase()
        {
            return this.TimeBase_s;
        }

        /// <summary>
        /// Get the object that contains scaling information 
        /// </summary>

        public PidScaling GetScaling()
        {
            return this.pidScaling;
        }



        /// <summary>
        /// Sets the anti-surge "kick" paramters of the controller
        /// </summary>

        public void SetAntiSurgeParams(PidAntiSurgeParams antiSurgeParams)
        {
            this.antiSurgeParms = antiSurgeParams;
        }

        /// <summary>
        /// Sets the Proportional gain(P) of the controller
        /// </summary>

        public void SetKp(double Kp)
        
        {
            this.Kp = Kp;
        }

        /// <summary>
        /// Sets the Integral(I) time contant of the controller in seconds
        /// </summary>
        public void SetTi(double Ti_seconds)
        {
            this.Ti = Ti_seconds;
        }

        /// <summary>
        /// Sets the differential(D) time contant of the controller in seconds
        /// </summary>

        public void SetTd(double Td_seconds)
        {
            this.Td = Td_seconds;
        }

        /// <summary>
        /// Gives a PIDscaling object that specifies how input and output is to be scaled.
        /// </summary>

        public void SetScaling(PidScaling pidScaling)
        {
            this.pidScaling = pidScaling;
        }

        /// <summary>
        /// For proportional-only controllers (P-controllers), a u0 offset to be added to u is specified by this method.
        /// </summary>

        public void SetU0ForPcontrol(double u0)
        {
            this.u0 = u0;
        }


        /// <summary>
        /// Set the control to manual mode (i.e. constant u). Will cause a bumpless transfer.(use SetAutoMode to switch back)
        /// </summary>

        public void SetManualMode()
        {
            isInAuto = false;
        }

        /// <summary>
        /// Set the manual output of the model (will only be used if set)
        /// </summary>
        public void SetManualOutput(double uManual)
        {
            this.uManual = uManual;
        }


        /// <summary>
        /// Set the control to autoamtic mode (i.e. u varies based on inputs and settings) (use SetManualMode to switch back)
        /// </summary>

        public void SetAutoMode()
        {
            isInAuto = true;
        }

        /// <summary>
        /// Set the offset that is to be added or subtracte to a split range controller that is inactive or tracking.
        /// If this value is above zero, then the controller is MIN SELECT, 
        /// if this value is negative then the controller is assumed MAX SELECT
        /// if this vlaue is zero, then trakcking will not work properly as controller will be unable 
        /// to determine if its output was selected by looking at the tracking signal.
        /// </summary>
        public void SetTrackingOffset(double uTrackingOffset, double uTrackingCutoff =0.5)
        {
            this.uTrackingOffset = uTrackingOffset;
            this.uTrackingCutoff = uTrackingCutoff;
        }

        /// <summary>
        /// Calculates an entire output vector u given vector of processes and setpoints (and optionally a tracking signal for split range)
        /// This method is useful for back-testing against historic data.
        /// </summary>

        public double[] Iterate(double[] y_process_abs, double[] y_set_abs, double[] uTrackSignal=null)
        {
            double[] u = new double[y_process_abs.Length];
            for (int i = 0; i < y_process_abs.Length; i++)
            {
                if (uTrackSignal == null)
                {
                    u[i] = Iterate(y_process_abs[i], y_set_abs[i]);
                }
                else
                {
                    u[i] = Iterate(y_process_abs[i], y_set_abs[i], uTrackSignal[i]);
                }
            }
            return u;
        }

        /// <summary>
        /// Calculates the next u[k] output of the controller given the most recent process value and setpoint value, and optionally also 
        /// including the tracking signal (only applicable if this is a split range controller) and optionally the gainScehduling variable if controller is to gain-schedule
        /// </summary>
        public double Iterate(double y_process_abs, double y_set_abs, double? uTrackSignal=null, double? gainSchedulingVariable=null,
            double? feedForwardVariable=null)
        {
            double u;

            if (isInAuto == false)
                return u_prev;
            if (y_process_abs == nanValue || Double.IsNaN(y_process_abs))
                return u_prev;
            if (y_set_abs == nanValue || Double.IsNaN(y_set_abs))
                return u_prev;

            // gain-scheduling
            if (gsObj != null)
            {
                if (gsObj.GSActive_b && gainSchedulingVariable.HasValue)
                {
                    gsObj.GetKpandTi(gainSchedulingVariable.Value, out double? gsKp, out double? gsTi);
                    if (gsKp.HasValue)
                    {
                        Kp = gsKp.Value;
                    }
                    if (gsTi.HasValue)
                    {
                        Ti = gsTi.Value;
                    }
                }
            }

            // scaling
            double KpScalingFactor = pidScaling.GetKpScalingFactor();
            double KpUnscaled = Kp / KpScalingFactor;

            // it iterate PID-controller one timestep
            // y_process_abs is the absolute value of the process value(scalar)
            if (Double.IsNaN(u_prev))
                u_prev = 0;
            if (Double.IsNaN(e_prev_unscaled))
                e_prev_unscaled = 0;
            if (Double.IsNaN(e_prev_prev_unscaled))
                e_prev_prev_unscaled = 0;
            double e_unscaled = CalcUnscaledE(y_process_abs, y_set_abs);
            //% protoct controller from divide - by - zero error
            if (TimeBase_s < 0.1)
                TimeBase_s = 0.1;
            if (Ti > 0 || Ti< 0)
                u = u_prev + KpUnscaled * (e_unscaled - e_prev_unscaled) + KpUnscaled * TimeBase_s / Ti * e_unscaled 
                    + KpUnscaled * Td / TimeBase_s * (e_unscaled - 2 * e_prev_unscaled + e_prev_prev_unscaled);
            else
                u = u0 + KpUnscaled * e_unscaled;

            ///////////////////////////////////////////////////////////
            // general feed-forward
            if (ffObj != null)
            {
                if (ffObj.isFFActive && feedForwardVariable.HasValue)
                {
                    double u_ff = 0;
                    double ff_signal_1 = ffHP.Filter(feedForwardVariable.Value,
                        ffObj.FF_HP_Tc_s, ffObj.FFHP_filter_order);
                    double ff_signal_2 = ffLP.Filter(feedForwardVariable.Value,
                        ffObj.FF_LP_Tc_s, ffObj.FFLP_filter_order);

                    //*"Band-pass type: feed forward changes between the two timeconstants of the two filters *)	
                    if (ffObj.FF_LP_Tc_s > 0 && ffObj.FF_HP_Tc_s > 0)
                    {
                        u_ff = ffObj.FF_Gain * (ff_signal_1 - ff_signal_2);
                    }
                    else
                    {
                        u_ff = ffObj.FF_Gain * ff_signal_1;
                    }
                    // bumpless transfer when activating feed-forward
                    if (!isFFActive_prev)
                    {
                       u_ff_prev  = u_ff;// moves u to uFF on first step of isFFactive or on startup
                    }
                    else
                    {
                        u = u + (u_ff- u_ff_prev);
                    }
                    u_ff_prev = u_ff;
                    isFFActive_prev = true;
                }
                else
                {
                    isFFActive_prev = false;
                }
            }
            ///////////////////////////////////////////////////////////
            // anti-surge-specific feed-forward
            if (antiSurgeParms != null)
            {
                double u_ff_antisurge = 0;

                // too close to surge line, "kick" open valve
                // nb! since e = yset-ymeas,  a "minus" is needed below!
                if (-e_unscaled < antiSurgeParms.kickBelowThresholdE)
                {
                    if (u_ff_antisurge_prev < pidScaling.GetUmax())
                        u_ff_antisurge = antiSurgeParms.kickPrcPerSec* TimeBase_s + u_ff_antisurge_prev;
                    else
                        u_ff_antisurge = u_ff_antisurge_prev;

                    // anti-surge feedforward anti-windup
                    if ( u_ff_antisurge - u > pidScaling.GetUmax() )
                        u_ff_antisurge = pidScaling.GetUmax() - u;
                }
                // aftermath of a kick, the rate at which valve closes is rate-limited.
                else if (u_ff_antisurge_prev > 0)
                {
                    if (antiSurgeParms.ffRampDownRatePrcPerMin.HasValue)
                    {
                        double u_ff_change = antiSurgeParms.ffRampDownRatePrcPerMin.Value / 60 * TimeBase_s;
                        u_ff_antisurge = Math.Max(0, u_ff_antisurge_prev - u_ff_change);
                    }
                    else
                    {
                        u_ff_antisurge = 0;
                    }
                }
                u_ff_antisurge_prev = u_ff_antisurge;
                u = u + u_ff_antisurge;
            }

            u_ff_withoutTracking = u;
            uIfInAuto = u;
            ////////////////////////////////////////////////
            // tracking:(min-select/max-select)
            if (uTrackSignal.HasValue)
            {
                double y_set_prc = pidScaling.ScaleYValue(y_set_abs);

                // tracking for MIN SELECT, where the minimum of two or more controllers is chosen
                if (this.uTrackingOffset > 0 && u > uTrackSignal.Value + uTrackingOffset + Math.Abs(Kp * (y_set_prc - y_set_prc_prev)))
                {
                    u = uTrackSignal.Value + this.uTrackingOffset;
                }
                // tracking MAX SELECT
                if (this.uTrackingOffset < 0 && u < uTrackSignal.Value + uTrackingOffset + Math.Abs(Kp * (y_set_prc - y_set_prc_prev)))
                {
                    u = uTrackSignal.Value + this.uTrackingOffset;
                }

                if (Math.Abs(u_prev - uTrackSignal.Value) <= uTrackingCutoff)
                {
                    controllerStatus = PidStatus.AUTO;
                }
                else
                {
                    controllerStatus = PidStatus.TRACKING;
                }
                y_set_prc_prev = y_set_prc;
            }
            else
            {
                controllerStatus = PidStatus.AUTO;
            }
            // handle bumpless transfer into manual mode

            if (isInAuto == false)
            {
                u = u_prev;
                // if uManual has changes after changing to manual mode
                if (this.uManual != u_prev)
                {
                    u = this.uManual;
                }
                controllerStatus = PidStatus.MANUAL;
            }

            //   anti - wind up
            if (u > pidScaling.GetUmax())
                u = pidScaling.GetUmax();
            if (u < pidScaling.GetUmin())
                u = pidScaling.GetUmin();
            
            // store values for next iteration
            u_prev = u;
            e_prev_prev_unscaled = e_prev_unscaled;
            e_prev_unscaled = e_unscaled;
            return u;
        }

        /// <summary>
        /// Split range controllers add an offset to the output u for inactive or "tracking" controllers. 
        /// This function returns the "raw" u without tracking, which can be useful for initalizing simulations with split range control.
        /// </summary>

        public double GetUWithoutTracking()
        {
            return uIfInAuto;
        }


        /// <summary>
        /// Returns the value of the output u that the controller would give if it was in auto.
        /// This is useful when considering turning on a controller that is in manual to see that the controller gives a sensible output.
        /// Call "iterate" first to update the internals of the controller.
        /// </summary>
        /// 
        public double GetUIfInAuto()
        {
            return uIfInAuto;
        }

        /// <summary>
        /// Initalizes the controller internal state(integral term) to be steady at the given process value and output value, 
        /// useful to avoid bumps when staring controller
        /// </summary>

        public void  WarmStart(double y_process_abs, double y_set_abs, double u_abs)
        {
            y_set_prc_prev = pidScaling.ScaleYValue(y_set_abs);
            u_prev      = u_abs; 
            double e    = CalcUnscaledE(y_process_abs, y_set_abs);
            e_prev_unscaled      = e;
            e_prev_prev_unscaled = e;
           // u0      = u_abs;
            uIfInAuto = u_abs;
            controllerStatus = PidStatus.AUTO; 
         }
        /*

        /// <summary>
        /// When simualting split range controllers, initalize this contorller to be in a stable state but in tracking, 
        /// so the output u (return value) will be offset from the u_abs by uTrackingOffset 
        /// </summary>

        public double WarmStartOutputSelectControllerMode(double y_process_abs, double y_set_abs, double u_abs)
        {
            u_abs = u_abs;
            WarmStart(y_process_abs, y_set_abs, u_abs);
            controllerStatus = PidStatus.TRACKING;
            return u_abs;
        }
        */

        /// <summary>
        /// Returns the tracking offset of the controller, the offset that a non-active controller will add or subtract from its 
        /// output when inactive in a split range control scheme
        /// </summary>

        public double GetTrackingOffset()
        {
            return uTrackingOffset;
        }

        /// <summary>
        /// Get the tracking cutoff parameter
        /// </summary>
        /// <returns></returns>
        public double GetTrackingCutoff()
        {
            return uTrackingCutoff;
        }


        //
        // private functions below
        //


        /// <summary>
        /// Caculates the error term from the setpoint and process measurement, 
        /// while also accounting for input filtering
        /// </summary>
        /// <param name="y_process_abs"></param>
        /// <param name="y_setpoint_abs"></param>
        /// <returns></returns>
        private double  CalcUnscaledE(double y_process_abs, double y_setpoint_abs)
        {
            double y_setpoint_prc=0, y_process_prc, y_processFilt_prc, e;
            // let E be unscaled, then scale Kp as neccessary instead!
            y_setpoint_prc = y_setpoint_abs ;//
            y_process_prc  = y_process_abs  ;


            y_processFilt_prc = pidFilter.Filter(y_process_prc);
            /*
            if (pidFiltering.IsEnabled)
            {
                if (pidFiltering.FilterOrder == 1 && pidFiltering.TimeConstant_s > 0)
                    y_processFilt_prc = yFilt1.Filter(y_process_prc, pidFiltering.TimeConstant_s, 1, false);
                else if (pidFiltering.FilterOrder == 2 && pidFiltering.TimeConstant_s > 0)
                {
                    double y_processFilt1_prc = yFilt1.Filter(y_process_prc, pidFiltering.TimeConstant_s, 1, false);
                    y_processFilt_prc = yFilt2.Filter(y_processFilt1_prc, pidFiltering.TimeConstant_s, 1, false);
                }
                else
                {
                    y_processFilt_prc = y_process_prc;
                }
            }
            else
            {
                y_processFilt_prc = y_process_prc;
            }*/

            e = y_setpoint_prc - y_processFilt_prc;
            return e;
        }



    }
}
