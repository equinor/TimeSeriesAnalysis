using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using System.Diagnostics;



namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Stand-alone simulation of any ISimulatableModel model. 
    /// </summary>
    /// 
    public class UnitSimulator
    {
        UnitModel model;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="model"></param>
        public UnitSimulator(UnitModel model)
        {
            this.model = model;
        }

        /// <summary>
        /// Simulation is written to ymeas instead of ysim. This is useful when creating generic datasets for  
        /// testing/test driven development.
        /// </summary>
        /// <param name="processDataSet"></param>
        /// <param name="noiseAmplitude">optionally adds noise to the "measured" y (for testing purposes)</param>
        public void SimulateYmeas(ref UnitDataSet processDataSet, double noiseAmplitude=0)
        {
            Simulate(ref processDataSet,true);
            if (noiseAmplitude > 0)
            {
                // use a specific seed here, to avoid potential issues with "random unit tests" and not-repeatable
                // errors.
                Random rand = new Random(1232);

                for (int k = 0; k < processDataSet.GetNumDataPoints(); k++)
                {
                    processDataSet.Y_meas[k] += (rand.NextDouble()-0.5)*2* noiseAmplitude;
                }
            }
        }


        /// <summary>
        /// Co-simulate a process model and pid-controller(both Y_sim and U_sim)
        /// </summary>
        /// <param name="pid">the </param>
        /// <param name="processDataSet">the process will read the <c>.Y_set</c> and <c>.Times</c> and 
        /// possibly <c>.D</c>c>, 
        /// and write simulated inputs to <c>U_sim</c> and <c>Y_sim</c></param>
        /// <param name="pidActsOnPastStep">pid-controller looks at e[k-1] if true, otherwise e[k]</c> 
        /// <param name="writeResultToYmeasInsteadOfYsim">write data to <c>processDataSet.Y_meas</c> 
        /// instead of <c>processDataSet.Y_sim</c></param>
        /// <returns>Returns true if able to simulate, otherwise false (simulation is written into processDataSet )</returns>
        public bool CoSimulate
            ( PidModel pid, ref UnitDataSet processDataSet, bool pidActsOnPastStep=true, bool writeResultToYmeasInsteadOfYsim = false)
        {
            processDataSet.Y_sim = null;
            processDataSet.U_sim = null;

            if (processDataSet.Y_setpoint == null)
            {
                return false;
            }
            if (processDataSet.Y_setpoint.Length == 0)
            {
                return false;
            }
            if (pid.GetModelParameters().Fitting != null)
            {
                if (pid.GetModelParameters().Fitting.WasAbleToIdentify == false)
                {
                    return false;
                }
            }
            if (model.GetModelParameters().Fitting != null)
            {
                if (model.GetModelParameters().Fitting.WasAbleToIdentify == false)
                {
                    return false;
                }
            }

            int N = processDataSet.GetNumDataPoints();
            double[] Y = Vec<double>.Fill(0, N);
            double[] U = Vec<double>.Fill(0, N);
            double y0,u0,y, u;

            // initalize PID/model together
            if (processDataSet.Y_meas != null)
            {
                y0 = processDataSet.Y_meas[0];
            }
            else
            {
                y0 = processDataSet.Y_setpoint[0];
            }
            double x0 = y0;
            if (processDataSet.D != null)
            {
                x0 -= processDataSet.D[0];
            }

            // this assumes that the disturbance is zero?
            u0 = model.GetSteadyStateInput(x0).Value;
            double umax = pid.GetModelParameters().Scaling.GetUmax();
            double umin = pid.GetModelParameters().Scaling.GetUmin();

            if (u0 >umin && u0<umax)
            {
                pid.WarmStart(y0, processDataSet.Y_setpoint[0], u0);
                // main loop
                u = u0;
            }
            else
            {
                if (processDataSet.Warnings == null)
                {
                    processDataSet.Warnings = new List<UnitWarnings>();
                }
                processDataSet.Warnings.Add(UnitWarnings.FailedToInitializePIDcontroller);
                Debug.WriteLine("Failed to initalize PID-contoller.");
                u = umin + (umax-umin)/2;
            }

            double timeBase_s = processDataSet.GetTimeBase();
            double y_prev = y0;

            for (int rowIdx = 0; rowIdx < N; rowIdx++)
            {
                if (Double.IsNaN(u))
                {
                    return false;
                }

                if (pidActsOnPastStep)
                {
                    double[] pidInputs = new double[] { y_prev, processDataSet.Y_setpoint[Math.Max(0,rowIdx-1)] };
                    u = pid.Iterate(pidInputs, timeBase_s, processDataSet.BadDataID)[0];
                    double x = model.Iterate(new double[] { u }, timeBase_s, processDataSet.BadDataID)[0];
                    y = x;
                    if (processDataSet.D != null)
                    {
                        y += processDataSet.D[rowIdx];
                    }
                }
                else
                { // pid acts on current step
                    double x = model.Iterate(new double[] { u }, timeBase_s, processDataSet.BadDataID)[0];
                    y = x;
                    if (processDataSet.D != null)
                    {
                        y += processDataSet.D[rowIdx];
                    }
                    double[] pidInputs = new double[] { y, processDataSet.Y_setpoint[rowIdx] };
                    u = pid.Iterate(pidInputs, timeBase_s, processDataSet.BadDataID)[0];
                }
                
                if (Double.IsNaN(u))
                {
                    Debug.WriteLine("pid.iterate returned NaN!");
                }
                y_prev = y;
                Y[rowIdx] = y;
                U[rowIdx] = u;
            }
            processDataSet.U_sim = Array2D<double>.CreateFromList(new List<double[]> { U });
            if (writeResultToYmeasInsteadOfYsim)
            {
                processDataSet.Y_meas = Y;
            }
            else
            {
                processDataSet.Y_sim = Y;
            }
            return true ;
        }

        /// <summary>
        /// Simulates the output of the model based on the processDataSet.U provided, by default the output is
        /// written back to <c>processDataSet.Y_sim</c> or <c>processDataSet.Y_meas</c>
        /// By default this method adds to <c>Y_sim</c> o <c>Y_meas</c> if they already contain values.
        /// </summary>
        /// <param name="processDataSet">dataset containing the inputs <c>U</c> to be simulated</param>
        /// <param name="writeResultToYmeasInsteadOfYsim">if <c>true</c>, output is written to <c>processDataSet.ymeas</c> instead of <c>processDataSet.ysim</c></param>
        /// <param name="doOverwriteY">(default is false)if <c>true</c>, output overwrites any data in <c>processDataSet.ymeas</c> or <c>processDataSet.ysim</c></param>
        /// <returns>Returns  the simulate y if able to simulate,otherwise null</returns> 
        public double[] Simulate(ref UnitDataSet processDataSet,
            bool writeResultToYmeasInsteadOfYsim = false, bool doOverwriteY=false)
        {
            int N = processDataSet.GetNumDataPoints(); ;
            double[] output = Vec<double>.Fill(0, N);
            double timeBase_s = processDataSet.GetTimeBase();

            if (processDataSet.D != null)
            {
                for (int rowIdx = 0; rowIdx < N; rowIdx++)
                {
                    output[rowIdx] += processDataSet.D[rowIdx];
                }
            }
            if (processDataSet.U != null)
            {
                for (int rowIdx = 0; rowIdx < N; rowIdx++)
                {
                    output[rowIdx] += model.Iterate(processDataSet.U.GetRow(rowIdx),
                        timeBase_s,
                        processDataSet.BadDataID)[0];
                }
            }
            else
            {
                for (int rowIdx = 0; rowIdx < N; rowIdx++)
                {
                    output[rowIdx] += model.Iterate(null, timeBase_s,processDataSet.BadDataID)[0];
                }
            }
            var vec = new Vec(processDataSet.BadDataID);
            if (writeResultToYmeasInsteadOfYsim)
            {
                if (processDataSet.Y_meas == null|| doOverwriteY)
                {
                    processDataSet.Y_meas = output;
                }
                else
                {
                    processDataSet.Y_meas = vec.Add(processDataSet.Y_meas, output);
                }
            }
            else
            {
                if (processDataSet.Y_sim == null || doOverwriteY)
                {
                    processDataSet.Y_sim = output;
                }
                else
                {
                    processDataSet.Y_sim = vec.Add(processDataSet.Y_sim, output);
                }
            }
            if (vec.ContainsBadData(output))
                return null;
            else 
                return output;
        }
    }
}
