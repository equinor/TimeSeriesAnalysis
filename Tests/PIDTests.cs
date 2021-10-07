using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;


namespace TimeSeriesAnalysis.Dynamic.PIDTests
{



    [TestFixture]
    class PIDTests
    {
        static bool doPlotting = false;
        static Plot4Test plot = new Plot4Test(doPlotting);
        double timeBase_s = 1;



        void PlotPIDsim(PIDsim sim, double[] yset, double[] ymeas, double[] u)
        {
            plot.FromList(new List<double[]> { yset, ymeas, u }, new List<string> { "y1=yset", "y1=ymeas", "y3=u" },
                (int)sim.GetTimeBase());
        }

        void PlotOutputSelectDualPIDsim(PIDsim sim, double[] yset1, double[] yset2, double[] ymeas, double[] u1, double[] u2, 
            PidStatus[] pidStatus1, PidStatus[] pidStatus2)
        {
            plot.FromList(new List<double[]> { yset1, yset2, ymeas, u1, u2, ConvertToDouble(pidStatus1), ConvertToDouble(pidStatus2)},
                new List<string> { "y1=yset1", "y1=yset2", "y1=ymeas", "y3=u1", "y3=u2","y4=status1","y4=status2" }, (int)sim.GetTimeBase());
        }

        private double[] ConvertToDouble(PidStatus[] pidStatus1)
        {
            double[] ret = new double[pidStatus1.Length];
            for (int i = 0; i < pidStatus1.Length; i++)
            {
                ret[i] = (double)pidStatus1[i];
            }
            return ret;
        }

        /*
        [TestCase(30,2)]
        [TestCase(40,2)]
        [TestCase(20,2)]
        [TestCase(30,-2)]
        [TestCase(40,-2)]


        public void DoesSimulationStartInStableState(double y0, double processGain_YchangeWhenIncrUonePercent)
        {
            double TimeBase_s = 10;
            int processTimeDelay_samples = 20;
            double processTimeConstant_s = 0;

            double Kp = processGain_YchangeWhenIncrUonePercent * 0.1;
            double Ti_s = 60;
            double yNoiseAmp_abs = 0;

            int Nsamples = (int)Math.Ceiling(Ti_s / TimeBase_s * 40); 
            double[] yset = Vec<double>.Fill(y0,Nsamples);

            double yOffset = 30, uOffset = 10;
            var processSimParams = new PIDsimProcessParams(yNoiseAmp_abs,
                processGain_YchangeWhenIncrUonePercent,
                processTimeConstant_s, processTimeDelay_samples);
            processSimParams.SetY0(yOffset);
            processSimParams.SetU0(uOffset);

            PIDcontroller pid = new PIDcontroller(TimeBase_s);
            pid.SetKp(Kp);
            pid.SetTi(Ti_s);

            PIDsim simObj = new PIDsim(processSimParams, null);
            simObj.SimulateSystem(pid, yset, out double[] u, out double[] ymeas);

            PlotPIDsim(simObj, yset, ymeas, u);

            Assert.AreEqual(yset,ymeas,"no disturbance-> warmup should cause PID-controller to be completely stable in equilibirum at its startup value");
        }
        */


        [TestCase(30,29)]
        [TestCase(30,31)]

        public void StepChange_DoesConverge(double y0, double y1, double TimeBase_s =10)
        {
            double processGain_YchangeWhenIncrUonePercent = 2;
            int processTimeDelay_samples = 0;
            double processTimeConstant_s = 0;

            double Kp = 0.1;
            double Ti_s = 60;
            double yNoiseAmp_abs = 0;
            double yDistAmp_prc = 0;

            PIDcontroller pid = new PIDcontroller(TimeBase_s);
            pid.SetKp(Kp);
            pid.SetTi(Ti_s);

            //  a set
            int Nsamples = (int)Math.Ceiling(Ti_s / TimeBase_s * 40); // *2 because step occurs halfway through dataset, *5 because five timeconstants should be enough
            double[] yset = new double[Nsamples];
            
            yset[0] = y0;

            for (int i = 1; i < yset.Length; i++)
            {
                if (i > 10)
                {
                    yset[i] = y1;
                }
                else
                {
                    yset[i] = y0;
                }
            }

            double yOffset = 30, uOffset = 10;

            var processSimParams = new PIDsimProcessParams(yNoiseAmp_abs,
                processGain_YchangeWhenIncrUonePercent,
                processTimeConstant_s, processTimeDelay_samples);
            processSimParams.SetY0(yOffset);
            processSimParams.SetU0(uOffset);

            PIDsimDisturbance disturbanceObj = new PIDsimDisturbance(yDistAmp_prc);
            PIDsim simObj = new PIDsim(processSimParams, disturbanceObj);
            
            simObj.SimulateSystem(pid, yset, out double[] u, out double[] ymeas);

            PlotPIDsim(simObj,yset,ymeas,u);

            Assert.IsTrue(Math.Abs(ymeas.Last() - yset.Last())<Math.Abs(y0-y1)/100);
        }

        /// <summary>
        /// Min select is often when two controllers share the same actuator, but have different setpoints either different or the same process variables.
        /// For instance if controlling a level or pressure, one controller may handle "normal conditons" while a second more agressive 
        /// controller may "kick in" only if levels get too high or pressure too high, for instance opening the valve to releive pressure.
        /// 
        /// In this test for simplicity, both controllers act on a the same process, 
        /// </summary>

        [TestCase(10)]
        public void MinSelect_TrackingModeOnOffIsOK(double y0)
        {

            double TimeBase_s = 10;

            double processGain_YchangeWhenIncrUonePercent = 0.1  ;
            int processTimeDelay_samples = 0;
            double processTimeConstant_s = 0;

            // assume two level or pressure controllers which consider the same process meaurement, 
            // but where pid2 is intended to kick in pressure or level becomes too high. 
            double Kp1 = processGain_YchangeWhenIncrUonePercent * 0.1;
            double Ti1_s = 60;
            double Kp2 = processGain_YchangeWhenIncrUonePercent * 0.2;
            double Ti2_s = 50;

            double yNoiseAmp_abs = 0;
            double yDistAmp_prc = 0;

            double trackingCutoff = 0.1;

            // slow controller
            PIDcontroller pid1 = new PIDcontroller(TimeBase_s);
            pid1.SetKp(Kp1);
            pid1.SetTi(Ti1_s);
            pid1.SetTrackingOffset(0.5, trackingCutoff);// min select

            // fast controller, 
            PIDcontroller pid2 = new PIDcontroller(TimeBase_s);
            pid2.SetKp(Kp2);
            pid2.SetTi(Ti2_s);
            pid2.SetTrackingOffset(pid1.GetTrackingOffset(), pid1.GetTrackingCutoff());// min select

            int Nsamples = 2000; 
            double[] yset1 = Vec<double>.Fill(y0,Nsamples);
            double[] yset2 = Vec<double>.Fill(y0,Nsamples);

            PIDsimDisturbance disturbanceObj = new PIDsimDisturbance(yDistAmp_prc);
            disturbanceObj.AddStepDisturbance(1000, 1);
            PIDsim simObj = new PIDsim(new PIDsimProcessParams(yNoiseAmp_abs,
                processGain_YchangeWhenIncrUonePercent,
                processTimeConstant_s, processTimeDelay_samples,0,true,10,50), disturbanceObj);

            simObj.SimulateOutputSelectSystem(pid1,pid2, yset1,yset2, out double[] u1, out double[] u2, out double[] ymeas, out PidStatus[] pid1Status, out PidStatus[] pid2Status);

            PlotOutputSelectDualPIDsim(simObj, yset1,yset2, ymeas, u1,u2, pid1Status,pid2Status);
         //   Assert.IsTrue(Vec.Max(Vec.Add(pid1Status as int[], pid2Status as int[])) ==1, "only one controller of the two should be active at any given time");
        //    Assert.IsTrue(Vec.Min(Vec.Add(pid1Status as int[], pid2Status as int[])) == 1, "only one controller of the two should be active at any given time");



            //    Assert.IsTrue(Math.Abs(ymeas.Last() - yset.Last()) < Math.Abs(y0 - y1) / 100);
        }









    }
}

