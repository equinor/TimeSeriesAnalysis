using Accord.Statistics.Testing;
using System;
using System.Collections.Generic;
using System.Text;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Simulate a PIDmodel PID for a given set of inputs timeseries 
    /// </summary>
    public class PidSimulator
    {
        PidModel model;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="model"></param>
        public PidSimulator(PidModel model)
        {
            this.model = model;
        }

        public double[] Simulate(ref UnitDataSet processDataSet,
            bool writeResultToUmeasInsteadOfUsim = false, bool doOverwriteU = false, int uWriteColumnIdx = 0)
        {
            int N = processDataSet.GetNumDataPoints(); ;
            double[] output = Vec<double>.Fill(0, N);
            double timeBase_s = processDataSet.GetTimeBase();

            if (processDataSet.Y_meas != null)
            {
                for (int rowIdx = 0; rowIdx < N; rowIdx++)
                {
                    //  First value is < c > y_process_abs </ c >,  second value is < c > y_set_abs </ c >, optional third value is
                    /// <c>uTrackSignal</c>, optional fourth value is <c>gainSchedulingVariable</c>
                    List<double> input = new List<double>();

                    input.Add(processDataSet.Y_meas[rowIdx]);
                    input.Add(processDataSet.Y_setpoint[rowIdx]);
                    //todo: track signal 
                    // todo: gain scheduling signal

                    output[rowIdx] += model.Iterate(input.ToArray(),
                        timeBase_s,
                        processDataSet.BadDataID);
                }
            }
            else
            {
                return null;
            }
            var vec = new Vec(processDataSet.BadDataID);
            
            if (writeResultToUmeasInsteadOfUsim)
            {
                if (processDataSet.U == null || doOverwriteU)
                {
                    processDataSet.U.WriteColumn(uWriteColumnIdx,output);
                }
            }
            else
            {
                if (processDataSet.U_sim == null || doOverwriteU)
                {
                    processDataSet.U_sim.WriteColumn(uWriteColumnIdx,output);
                }
            }
            if (vec.ContainsBadData(output))
                return null;
            else
                return output;
        }





    }
}
