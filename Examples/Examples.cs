using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.SysId;

namespace TimeSeriesAnalysis.Examples
{
    [TestFixture]
    class Examples
    {
        [Test, Explicit]
        public void ex1_hello_world()
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
        public void ex2_linreg()
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

            double[] estimated_parameters = Vec.Regress(y, U,null, out _, 
                out double[] y_modelled, out double Rsq);

            TestContext.WriteLine(Vec.ToString(estimated_parameters,3));
            TestContext.WriteLine(SignificantDigits.Format(Rsq,3));

            Plot.FromList(new List<double[]>() { y, y_modelled },
                new List<string>() { "y1=y_meas", "y1=y_mod" }, 1);
        }

        [Test, Explicit]
        public void ex3_sysid()
        {
            int dT_s = 1;
            ProcessModelParamters parameters = new ProcessModelParamters
            {
                TimeConstant_s = 10,
                ProcessGain = new double[] { 1, 2},
                Bias = 10
            };
            ProcessModel model = new ProcessModel(parameters);

            double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
                    Vec<double>.Fill(1, 50));
            double[] u2 = Vec<double>.Concat(Vec<double>.Fill(2, 31),
                    Vec<double>.Fill(1, 30));
            double[,] U = Array2D<double>.InitFromColumnList(new List<double[]>{u1 ,u2});
            double[] y_simulated = model.Simulate(U,dT_s);

            Plot.FromList(new List<double[]> { y_simulated, u1, u2 }, new List<string> { "y1=ysim", "y3=u1", "y3=u2" }, dT_s);


        }

        [Test, Explicit]
        public void ex4_pid()
        {
        }














    }
}
