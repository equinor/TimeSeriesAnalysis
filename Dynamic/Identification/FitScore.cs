using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic.Identification
{
    public class FitScore
    {
        private static double Deviation(double[] reference, double[] model)
        {
            if (reference == null)
                return double.NaN;
            if (model == null)
                return double.NaN;
            if (reference.Length == 0)
                return double.NaN;
            if (model.Length == 0)
                return double.NaN;
            double ret = 0;
            double N = 0;
            for (var i = 0; i < Math.Min(reference.Length, model.Length); i++)
            {
                ret += Math.Abs(reference[i] - model[i]);
                N++;
            }
            if (N == 0)
                return 0;
            ret = ret / N;
            return ret;
        }

        private static double DeviationFromAvg(double[] signal)
        { 
            if (signal == null) return double.NaN;
            if (signal.Length == 0) return double.NaN;

            var vec = new Vec();
            var avg = vec.Mean(signal);

            if (!avg.HasValue)
                return double.NaN;

            var N = signal.Length;
            double ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret += Math.Abs(signal[i] - avg.Value);
            }
            return ret/N;
        }

        public static double Calc(double[] meas, double[] sim)
        {
            if (meas == null)
                return double.NaN;
            if (sim == null)
                return double.NaN;

            double dev = Deviation(meas, sim);
            double devFromSelf = DeviationFromAvg(meas);

            double fitScore = 0;
            if ( !Double.IsNaN(dev))
            {
                fitScore = (double)(1 - dev / devFromSelf) * 100;
            }
            else
            {
                fitScore = double.NaN;
            }
            return fitScore;
        }


    }
}
