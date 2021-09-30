using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis.Examples
{
    [TestFixture]
    class Examples
    {
        [Test, Explicit]
        public void Ex1_hello_world()
        {
            int dT_s = 1;
            double filterTc_s = 10;

            double[] input = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                Vec<double>.Fill(1, 50));

            LowPass lp = new LowPass(dT_s);
            var output = lp.Filter(input, filterTc_s);

            Plot.FromList(new List<double[]> { input, output},
                new List<string> { "y1=V1_input","y1=V2_output"}, dT_s, "ex1_hello_world",
                new DateTime(2020, 1, 1, 0, 0, 0));
        }


        [Test, Explicit]
        public void Ex2_linreg()
        {
            double[] true_gains = {1,2,3};
            double true_bias = 5;
            double noiseAmplitude = 0.1;

            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                Vec<double>.Fill(1, 50));
            double[] u2 = Vec<double>.Concat(Vec<double>.Fill(1, 31),
                Vec<double>.Fill(2, 30));
            double[] u3 = Vec<double>.Concat(Vec<double>.Fill(1, 21),
                Vec<double>.Fill(-1, 40));

            double[] y = new double[u1.Length];
            double[] noise = Vec.Mult(Vec.Rand(u1.Length, -1,1,0),noiseAmplitude);
            for (int k = 0; k < u1.Length; k++)
            {
                y[k] = true_gains[0] * u1[k] 
                    + true_gains[1] * u2[k] 
                    + true_gains[2] * u3[k] 
                    + true_bias + noise[k]; 
            }

            Plot.FromList(new List<double[]> { y, u1, u2, u3 }, 
                new List<string> { "y1=y1", "y3=u1", "y3=u2", "y3=u3" }, 1);

            double[][] U = new double[][] { u1, u2, u3 };

            var results = Vec.Regress(y, U);

            TestContext.WriteLine(Vec.ToString(results.param, 3));
            TestContext.WriteLine(SignificantDigits.Format(results.Rsq, 3));

            Plot.FromList(new List<double[]>() { y, results.Y_modelled },
                new List<string>() { "y1=y_meas", "y1=y_mod" }, 1);
        }

        [Test, Explicit]
        public void Ex3_filters()
        {
            double timeBase_s = 1;
            int nStepsDuration = 2000;
            var sinus1 = new SinusModel(new SinusModelParameters 
                { amplitude = 10, period_s = 400 },timeBase_s);
            var sinus2 = new SinusModel(new SinusModelParameters 
                { amplitude = 1, period_s = 25 }, timeBase_s);

            var dataset = new ProcessDataSet(timeBase_s, nStepsDuration);
            ProcessSimulator<SinusModel, SinusModelParameters>.Simulate(sinus1, ref dataset);
            ProcessSimulator<SinusModel, SinusModelParameters>.Simulate(sinus2, ref dataset);

            var lpFilter = new LowPass(timeBase_s);
            var lpFiltered = lpFilter.Filter(dataset.Y_sim,40,1);

            var hpFilter = new HighPass(timeBase_s);
            var hpFiltered = hpFilter.Filter(dataset.Y_sim,3,1);

            Plot.FromList(new List<double[]> { dataset.Y_sim, lpFiltered, hpFiltered },
                new List<string> { "y1=y","y3=y_lowpass","y3=y_highpass" }, (int)timeBase_s);
        }


        [Test, Explicit]
        public void Ex4_sysid()
        {
            int timeBase_s = 1;
            DefaultProcessModelParameters parameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 15,
                ProcessGains = new double[] {1,2},
                TimeDelay_s = 5,
                Bias = 5
            };
            DefaultProcessModel model = new DefaultProcessModel(parameters, timeBase_s);

            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                    Vec<double>.Fill(1, 50));
            double[] u2 = Vec<double>.Concat(Vec<double>.Fill(2, 31),
                    Vec<double>.Fill(1, 30));
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]>{u1 ,u2});

            ProcessDataSet dataSet = new ProcessDataSet(timeBase_s,U);
            ProcessSimulator<DefaultProcessModel,DefaultProcessModelParameters>.
                EmulateYmeas(model, ref dataSet);

            Plot.FromList(new List<double[]> { dataSet.Y_meas, u1, u2 },
                new List<string> { "y1=y_meas", "y3=u1", "y3=u2" }, timeBase_s);

            DefaultProcessModelIdentifier modelId = new DefaultProcessModelIdentifier();
            DefaultProcessModel identifiedModel = modelId.Identify(ref dataSet);
    
            Plot.FromList(new List<double[]> { identifiedModel.FittedDataSet.Y_meas, 
                identifiedModel.FittedDataSet.Y_sim },
                new List<string> { "y1=y_meas", "y1=y_sim"}, timeBase_s);

            Console.WriteLine(identifiedModel.ToString());
        }

        [Test, Explicit]
        public void Ex5_pid()
        {
        }














    }
}
