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
    public class ProcessSimulator<T1,T2> where T1:IProcessModel<T2> where T2:IProcessModelParameters
    {
        /// <summary>
        /// Simulation is written to ymeas instead of ysim. This is useful when creating generic datasets for  
        /// testing and validation.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="processDataSet"></param>
        static public void EmulateYmeas(T1 model,
            ref ProcessDataSet processDataSet)
        {
            Simulate(model,ref processDataSet,true);
        }

        /// <summary>
        /// Simulates the output of the model based on the processDataSet provided, by default the output is
        /// written back to <c>processDataSet.Y_sim</c> or <c>processDataSet.Y_meas</c>
        /// </summary>
        /// <param name="model">model paramters</param>
        /// <param name="processDataSet">dataset containing the inputs <c>U</c> to be simulated</param>
        /// <param name="writeResultToYmeasInsteadOfYsim">if <c>true</c>, output is written to <c>processDataSet.ymeas</c> instead of <c>processDataSet.ysim</c></param>
        /// <returns>Returns  <c>true</c> if able to simulate, <c>false</c> otherwise.</returns> 
        static public bool Simulate(T1 model,
            ref ProcessDataSet processDataSet,
            bool writeResultToYmeasInsteadOfYsim = false)
        {
            int N = processDataSet.U.GetNRows();
            double[] output = new double[N];
            for (int rowIdx = 0; rowIdx < N; rowIdx++)
            {
                output[rowIdx] = model.Iterate(processDataSet.U.GetRow(rowIdx));
            }
            if (writeResultToYmeasInsteadOfYsim)
            {
                processDataSet.Y_meas = output;
            }
            else
            {
                processDataSet.Y_sim = output;
            }
            if (Vec.ContainsBadData(output))
                return false;
            else 
                return true;
        }
    }
}
