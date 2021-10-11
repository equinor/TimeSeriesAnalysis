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
    /// Simulate any subprocess model that has implemented the IProcessModel interface. 
    /// This class relies on depencency injection and interfaces, so that the 
    /// the specifics of how models outputs are calculated should be encapsulated in the passed model objects.
    /// 
    /// This class should not be static, as it is to be used as a type for MultiProcessSimulator
    /// 
    /// </summary>
    public class SubProcessSimulator
    {
        IProcessModelSimulate model;
        public SubProcessSimulator(IProcessModelSimulate model)
        {
            this.model = model;
        }

        /// <summary>
        /// Simulation is written to ymeas instead of ysim. This is useful when creating generic datasets for  
        /// testing/test driven development.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="processDataSet"></param>
        /// <param name="noiseAmplitude">optionally adds noise to the "measured" y (for testing purposes)</param>
        public void EmulateYmeas(ref SubProcessDataSet processDataSet, double noiseAmplitude=0)
        {
            Simulate(ref processDataSet,true);
            if (noiseAmplitude > 0)
            {
                // use a specific seed here, to avoid potential issues with "random unit tests" and not-repeatable
                // errors.
                Random rand = new Random(1232);

                for (int k = 0; k < processDataSet.NumDataPoints; k++)
                {
                    processDataSet.Y_meas[k] += (rand.NextDouble()-0.5)*2* noiseAmplitude;
                }
            }
        }


        /// <summary>
        /// Co-simulate a process model and pid-controller
        /// </summary>
          /// <param name="pid">the </param>
        /// <param name="processDataSet">the process will read the <c>.Disturbance</c> and <c>.TimeBase_s</c>, 
        /// and write simulated inputs to <c>.U</c> and <c>.Y_sim</c></param>
        /// <param name="writeResultToYmeasInsteadOfYsim">write data to <c>processDataSet.Y_meas</c> 
        /// instead of <c>processDataSet.Y_sim</c></param>
        /// <returns>Returns true if able to simulate, otherwise false (simulation is written into processDataSet )</returns>
        public bool CoSimulateProcessAndPID
            ( PIDModel pid, ref SubProcessDataSet processDataSet, bool writeResultToYmeasInsteadOfYsim = false)
        {
            if (processDataSet.Y_setpoint == null)
            {
                return false;
            }
            if (processDataSet.Y_setpoint.Length == 0)
            {
                return false;
            }

            int N = processDataSet.NumDataPoints;
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
            u0 = model.GetSteadyStateInput(y0).Value;
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
                processDataSet.warnings.Add(SubProcessDataSetWarnings.FailedToInitializePIDcontroller);
                Debug.WriteLine("Failed to initalize PID-contoller.");
                u = umin + (umax-umin)/2;
            }

            for (int rowIdx = 0; rowIdx < N; rowIdx++)
            {
                if (Double.IsNaN(u))
                {
                    return false;
                }
                double x= model.Iterate(new double[] { u}, processDataSet.BadDataID);
                y = x;
                if (processDataSet.D != null)
                {
                    y += processDataSet.D[rowIdx];
                }
                double[] pidInputs = new double[] { y, processDataSet.Y_setpoint[rowIdx] };
                u = pid.Iterate(pidInputs, processDataSet.BadDataID);
                if (Double.IsNaN(u))
                {
                    Debug.WriteLine("pid.iterate returned NaN!");
                }
                Y[rowIdx] = y;
                U[rowIdx] = u;
            }
            processDataSet.U_sim = Array2D<double>.InitFromColumnList(new List<double[]> { U });
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
        public double[] Simulate(ref SubProcessDataSet processDataSet,
            bool writeResultToYmeasInsteadOfYsim = false, bool doOverwriteY=false)
        {
            int N = processDataSet.NumDataPoints;
            double[] output = Vec<double>.Fill(0, N);

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
                        processDataSet.BadDataID);
                }
            }
            else
            {
                for (int rowIdx = 0; rowIdx < N; rowIdx++)
                {
                    output[rowIdx] += model.Iterate(null, processDataSet.BadDataID);
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
