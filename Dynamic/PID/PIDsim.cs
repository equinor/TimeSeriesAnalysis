using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Class that contains the description of the process to be simulated in ProcessPIDsim
    /// </summary>
    public class PIDsim
    {
        private double[] d;
        private double[] u_ff;
        private double ff_gain;
        private bool pidHasFilter;

        private PIDsimProcessParams processParams;
        private PIDsimDisturbance disturbance;

        private double uNoiseAmplitude_abs=0.0005;// remember:u is scaled 0-100
   //     private double u1Offset = 0, u2Offset = 0;
        private double imsSamplingRate_s=0;//0 is now downsampling.
        private double TimeBase_s;


        ///<summary>
        /// Initialization of the process to be simulated, requires setting the process paramters processParams and specifying the disturbance.
        ///</summary>

        public PIDsim(PIDsimProcessParams processParams, PIDsimDisturbance disturbance)
        {
            this.processParams = processParams;
            this.disturbance = disturbance;
            this.u_ff = null;
              
         }


        ///<summary>
        /// Simulates the effect of feeding downdsampled data to the pid-controller (this is mainly useful for identification)
        ///</summary>

        internal void AddSimulatedIMSDownsampling(double imsSampleRate_s)
        {
            this.imsSamplingRate_s = imsSampleRate_s;
        }


        ///<summary>
        /// Defines a feedforward signal u_ff and the gain of said signal, the product of the two will be added to the controller output
        ///</summary>


        public void AddFeedForward(double[] u_ff, double ff_gain)
        {
            this.u_ff = u_ff;
            this.ff_gain = ff_gain;
        }


        ///<summary>
        /// Defines the process u0, the value of u which causes y=0 (optionally also the offset of a second split range controller u2Offset)
        ///</summary>

        /*
        public void SetUOffset(double u1Offset, double u2Offset=0)
        {
            this.u1Offset = u1Offset;
            this.u2Offset = u2Offset;
        }*/

        ///<summary>
        /// Sets the amplitude of white noise that is added to u in absolute value
        ///</summary>
        public void SetUnoiseAmplitude(double amp)
        {
            this.uNoiseAmplitude_abs = amp;
        }

        ///<summary>
        ///Enables pre-filtering of the process variable y before being fed to pid-algorithm.
        ///</summary>
        public void EnablePidFilter()
        {
             pidHasFilter = true;
        }


        ///<summary>
        /// Return the sample time/clock internval/"time base" of the simulations in seconds.
        ///</summary>

        public double GetTimeBase()
        {
            return TimeBase_s;
        }

        ///<summary>
        /// Simulates that the pid-controller pid attempting to follow the setpoint vector yset, retuning the simulated u_withFF and ymeas
        ///</summary>

        public void SimulateSystem(PIDcontroller pid, double[] yset, out double[] u_withFF, out double[] ymeas)
        {
            // call split range code, but with only one controller, it should then revert to acting as a "normal" single controller system
            SimulateOutputSelectSystem(pid, null, yset, null, out u_withFF, out _, out ymeas, out _, out _);
        }

        ///<summary>
        /// Simulates a min select or max select of two pid controllers pid1 and pid2 that attempt ng to follow the setpoint vector yset, retuning the simulated u_withFF and ymeas.
        /// To understand the importance of uTrackingOffset, see the descriptions in PIDcontroller class.
        ///</summary>

        public void SimulateOutputSelectSystem(PIDcontroller pid1, PIDcontroller pid2, double[] yset1, double[] yset2, out double[] u1_withFF, out double[] u2_withFF,out double[] ymeas,
            out PidStatus[] pid1Status, out PidStatus[] pid2Status)
        {
            bool isSplitRange = (pid2 != null);
            TimeBase_s = pid1.GetTimeBase();
            ymeas = new double[yset1.Length];
            u1_withFF = new double[yset1.Length];
            pid1Status = new PidStatus[yset1.Length];
            pid2Status = new PidStatus[yset1.Length];
            if (pid2 == null)
            {
                u2_withFF = null;
            }
            else
            {
                u2_withFF = new double[yset2.Length];
            }
               
            Random randNoise = new Random(12364);//important to seed random so tests are repeatable

            int PID_timedelay_samples = 1;
            if (processParams.IsPIDinputDelayedOneSample())
                PID_timedelay_samples = 1;// should be either zero or one
            else
                PID_timedelay_samples = 0;// should be either zero or one

            double uTrackingOffset = 0;
            double x_constant = 0;

            double x, x_prev;
            double a = 1 / (1 + TimeBase_s / processParams.GetTimeConstant_s());
            double b = processParams.GetProcessGain() * (1 - a); // ss-gain = b/(1-a)
            /*  if (pidHasFilter)
            {
                int filterOrder = 2;
                pid.SetYFilter(processTimeConstant_s / 7, filterOrder);
            }*/

            ////////////////////////////////
            // BEGIN starting up system
            // y[t] = x[t]+ y0   where
            // x[t] = a * x[t-1] + b * (u[k] - u0) 
            // where y[t] = y0 when u[t]=u0 by definition (and disturbance is zero)


            double y0 = yset1[0];
            double u_prev = 0;
            // pid1: if not split range
            if (!isSplitRange)
            {
                double u1_ss, x_ss;
                x_ss = yset1[0] - processParams.GetY0();
                u1_ss = x_ss * (1 - a) / b + processParams.GetU0();// set disturbance and noise to zero, solve for u so that ymeas=yset;
                pid1.SetTrackingOffset(0.0);
                pid1.WarmStart(yset1[0], yset1[0], u1_ss);

                u_prev = u1_ss;
                x_prev = x_ss;
                if (PID_timedelay_samples == 1)
                {
                    u1_withFF[0] = u1_ss;
                }
            }
            // initalize split range control
            else // (isSplitRange)
            {
//                pid1.SetTrackingOffset(uTrackingOffset);
 //               pid2.SetTrackingOffset(uTrackingOffset);

                double u_ss, x_ss;

                double y_startpoint = yset1[0]+0.1 ;// cannot start in steady-state, but start close to it(to allow output control to initalize correctly)

                x_ss = y_startpoint - processParams.GetY0();
                u_ss = x_ss*(1-a)/b + processParams.GetU0();// set disturbance and noise to zero, solve for u so that ymeas=yset;

           
                pid1.WarmStart(y_startpoint, yset1[0], u_ss);
                pid2.WarmStart(y_startpoint, yset2[0], u_ss);

                double u1_init = pid1.Iterate(y0, y_startpoint);
                double u2_init = pid2.Iterate(y0, y_startpoint);

                u_prev = u_ss;
                x_prev = x_ss;

                uTrackingOffset = pid1.GetTrackingOffset();

                if (uTrackingOffset > 0) // is min select
                {
                    if (u1_init < u2_init)
                    {
                        u_prev = u1_init;
                    }
                    else 
                    {
                        u_prev = u2_init;
                    }
                }
                else // is max select
                {
                    if (u1_init > u2_init)
                    {
                        u_prev = u1_init;
                    }
                    else
                    {
                        u_prev = u2_init;
                    }
                }
                

                if (PID_timedelay_samples == 1)
                {
                    u1_withFF[0] = u1_init;
                }
                if (PID_timedelay_samples == 1)
                {
                    u2_withFF[0] = u2_init;
                }


            }
            // END starting up system
            ////////////////////////////////
            ///
            // SIMULATE system

            this.d = new double[yset1.Length];

            if (PID_timedelay_samples == 1)
            {
                ymeas[0] = x_prev + processParams.GetY0();
            }

          
            for (int i = PID_timedelay_samples; i < yset1.Length; i++)
            {
                u1_withFF[i] = pid1.Iterate(ymeas[i - PID_timedelay_samples], yset1[i - PID_timedelay_samples],u_prev);
                pid1Status[i] = pid1.GetControllerStatus();
                if (u_ff != null)
                {
                    if (u_ff.Count() >= i)
                    {
                        u1_withFF[i] = u1_withFF[i] + ff_gain * u_ff[i];
                    }
                }

                if (pid2 != null)
                {
                    u2_withFF[i] = pid2.Iterate(ymeas[i - PID_timedelay_samples], yset2[i - PID_timedelay_samples], u_prev);
                    pid2Status[i] = pid2.GetControllerStatus();
                }

                if (disturbance != null)
                {
                    d[i] = disturbance.Iterate();
                }
                double y_noise_abs = (randNoise.NextDouble() - 0.5) * 2 * processParams.GetNoiseAmplitude();
                if (i < ymeas.Length)
                {
                    double u1 = u1_withFF[Math.Max(0, i - processParams.GetTimeDelay_samples())];
                    double curU = u1;
                    if (u2_withFF != null)
                    {
                        double u2 = u2_withFF[Math.Max(0, i - processParams.GetTimeDelay_samples())];
                        if (uTrackingOffset > 0) // is min select
                        {
                            curU = Math.Min(u1, u2);
                        }
                        else // is max select
                        {
                            curU = Math.Max(u1, u2);
                        }
                    }
                    x = a * x_prev + b * (curU - processParams.GetU0()) + x_constant;
                    ymeas[i + (1 - PID_timedelay_samples)] = x + y_noise_abs + processParams.GetMeasurementBias() + processParams.GetY0() + d[i];
                    x_prev = x;
                    u_prev = curU;
                }
            }

            // add output noise
            for (int i = 0; i < u1_withFF.Count(); i++)
            {
                double u1Internal = u1_withFF[i] + (randNoise.NextDouble() - 0.5) * uNoiseAmplitude_abs * 2;
                u1_withFF[i] = Math.Min(Math.Max(u1Internal, 0), 100);
                if (pid2 != null)
                {
                    double u2Internal = u2_withFF[i] + (randNoise.NextDouble() - 0.5) * uNoiseAmplitude_abs * 2;
                    u2_withFF[i] = Math.Min(Math.Max(u2Internal, 0), 100);
                }
            }

            // simulate the "sampling" that ims does to save space/memory and that can produce "jagged" data
            if (imsSamplingRate_s > 0)
            {
                double imsSamplingTimeSinceLastChange_s = 0;
                int lastSampledIterator = 0;
                for (int i = 0; i < u1_withFF.Count(); i++)
                {
                    // change output
                    if (imsSamplingTimeSinceLastChange_s >= imsSamplingRate_s)
                    {
                        imsSamplingTimeSinceLastChange_s = 0;
                        lastSampledIterator = i;
                    }
                    else // otherwise freeze output
                    {
                        imsSamplingTimeSinceLastChange_s += TimeBase_s;
                    }

                    u1_withFF[i] = u1_withFF[lastSampledIterator];
                    ymeas[i] = ymeas[lastSampledIterator];
                }
            }
        }

        ///<summary>
        /// Returns the disturbance vector d that has been applied to the pid-simulation 
        ///</summary>

        public double[] GetTrueDisturbance()
        {
            return d;
        }



    }

}
