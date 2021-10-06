using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;


namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Simulate any process model that has implemented the IProcessModel interface. 
    /// This class relies on depencency injection and interfaces, so that the 
    /// the specifics of how models outputs are calculated should be encapsulated in the passed model objects.
    /// </summary>
    static public class ProcessSimulator<T1,T2> where T1:IProcessModel<T2> where T2:IProcessModelParameters
    {
        /// <summary>
        /// Simulation is written to ymeas instead of ysim. This is useful when creating generic datasets for  
        /// testing/test driven development.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="processDataSet"></param>
        /// <param name="noiseAmplitude">optionally adds noise to the "measured" y (for testing purposes)</param>
        static public void EmulateYmeas(T1 model,
            ref ProcessDataSet processDataSet, double noiseAmplitude=0)
        {
            Simulate(model,ref processDataSet,true);

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
        /// Version of Simulate that outputs the simulated Y directly
        /// </summary>
        /// <param name="model">model object to simulate</param>
        /// <param name="processDataSet">dataset to simulate over(inputs U and optionally a disturbance)</param>
        /// <returns>returns the simulated <c>y_sim</c> that internally is the states + <c>Disturbance</c> if set in
        /// <c>processDataSet</c></returns>
        static public double[] Simulate(T1 model, ProcessDataSet processDataSet)
        {
            int N  = processDataSet.NumDataPoints;
            double[] y_sim  = Vec<double>.Fill(0,N);

            if (processDataSet.Disturbance != null)
            {
                for (int rowIdx = 0; rowIdx < N; rowIdx++)
                {
                    y_sim[rowIdx] += processDataSet.Disturbance[rowIdx];
                }
            }

            if (processDataSet.U != null)
            {
                for (int rowIdx = 0; rowIdx < N; rowIdx++)
                {

                    y_sim[rowIdx] += model.Iterate(processDataSet.U.GetRow(rowIdx), 
                        processDataSet.BadValueIndicatingValue);
                }
            }
            else
            {
                for (int rowIdx = 0; rowIdx < N; rowIdx++)
                {

                    y_sim[rowIdx] += model.Iterate(null, processDataSet.BadValueIndicatingValue);
                }
            }
            return y_sim;
        }

        /// <summary>
        /// Simulates the output of the model based on the processDataSet provided, by default the output is
        /// written back to <c>processDataSet.Y_sim</c> or <c>processDataSet.Y_meas</c>
        /// By default this method adds to <c>Y_sim</c> o <c>Y_meas</c> if they already contain values.
        /// </summary>
        /// <param name="model">model paramters</param>
        /// <param name="processDataSet">dataset containing the inputs <c>U</c> to be simulated</param>
        /// <param name="writeResultToYmeasInsteadOfYsim">if <c>true</c>, output is written to <c>processDataSet.ymeas</c> instead of <c>processDataSet.ysim</c></param>
        /// <param name="doOverwriteY">(default is false)if <c>true</c>, output overwrites any datay in <c>processDataSet.ymeas</c> or <c>processDataSet.ysim</c></param>
        /// <returns>Returns  <c>true</c> if able to simulate, <c>false</c> otherwise.</returns> 
        static public bool Simulate(T1 model,
            ref ProcessDataSet processDataSet,
            bool writeResultToYmeasInsteadOfYsim = false, bool doOverwriteY=false)
        {
            var output = Simulate(model, processDataSet);

            var vec = new Vec(processDataSet.BadValueIndicatingValue);

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
                return false;
            else 
                return true;
        }
    }
}
