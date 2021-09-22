using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;


namespace TimeSeriesAnalysis.SysId
{
    /// <summary>
    /// Simulate any process model that has implemented the IProcessModel interface. 
    /// This class relies on depencency injection and interfaces, so that the 
    /// the specifics of how models outputs are calculated should be encapsulated in the passed model objects.
    /// </summary>
    public class ProcessSimulator
    {
        /// <summary>
        /// Simulation is written to ymeas instead of ysim. This is useful when creating generic datasets for  
        /// testing and validation.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="processDataSet"></param>
        static public void EmulateYmeas(IProcessModel model,
            ref ProcessDataSet processDataSet)
        {
            Simulate(model,ref processDataSet,true);
        }

        /// <summary>
        /// Simulates the output of the model based on the processDataSet provided, by default the output is
        /// written back to processDataSet.ySim
        /// </summary>
        /// <param name="model">model paramters</param>
        /// <param name="processDataSet">dataset containing the inputs U to be simulated</param>
        /// <param name="writeResultToYmeasInsteadOfYsim">if true, output is written to ymeas instead of ysim</param>
        static public void Simulate(IProcessModel model,
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
            return;
        }
    }
}
