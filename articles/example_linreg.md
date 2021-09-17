# Example : realistic linear regression  

This example uses a ficticious csv-file example.csv, that has column headers
 "Time","Var1","Var2","Var3","Var4,"Var5",Var6","Var7".
 
"Var1" is to be modelled by ``Var2``-``Var6`` as regressors, while Var7 is to be multiplied to ``Var2``-``Var6``. 
The data contains some instances of ``-9999`` which indicates bad data, and this is removed in preprocessing.
 
A low-pass filter is used to imitate a time-constant in the system by smoothing the model inputs.
 
Only the data starting after a specific ``t0`` is to be used in the regression, so a subset of the raw data in the csv-file is 
 given to regression.
 
This example illustrates that by using the ``TimeSeriesAnalysis`` package, the complexity of the code required to do practical exploratory time-series
analysis is comparable to what is normally accomplished by parsed languages such as Matlab, R or Python. 
 
```
using System;
using System.Collections.Generic;
using System.Linq;
using TimeSeriesAnalysis;

namespace SubseaPALL
{
    class run
    { 
        public static void Main()
        {
            CSV.loadDataFromCSV(@"C:\Appl\ex1\Data\example.csv", out double[,] data, out string[] variableNames,out string[,] stringData);
            
            int tInd = Array.IndexOf(variableNames, "Time");
            DateTime[] dateTimes = stringData.GetColumnParsedAsDateTime(tInd, "yyyy-MM-dd HH:mm:ss");
            TimeSpan span = dateTimes[1].Subtract(dateTimes[0]);
            int dT_s = (int)span.TotalSeconds;

            int t0Ind = 9476;// first instance

            DateTime t0 = dateTimes.ElementAt(t0Ind);

            int yInd  = Array.IndexOf(variableNames, "var1");
            //V1: use choke openings as inputs
            int u1Ind, u2Ind, u3Ind, u4Ind, u5Ind;

			u1Ind = Array.IndexOf(variableNames, "var2");
			u2Ind = Array.IndexOf(variableNames, "var3");
			u3Ind = Array.IndexOf(variableNames, "var4");
			u4Ind = Array.IndexOf(variableNames, "var5");
			u5Ind = Array.IndexOf(variableNames, "var6");

            int u6ind = Array.IndexOf(variableNames, "var7");

            int[] uIndArray = new int[] { u1Ind, u2Ind, u3Ind, u4Ind, u5Ind };

            double[]  y_raw = data.GetColumn(yInd);
            double[,] u_raw = data.GetColumns(uIndArray) ;
            double[] u6_raw = data.GetColumn(u6ind);

            // clip out desired chunk of data
            double[] y = y_raw.GetRowsAfterIndex(t0Ind);
            double[,] u = u_raw.GetRowsAfterIndex(t0Ind);
            double[] z_topside = u6_raw.GetRowsAfterIndex(t0Ind);

            // preprocessing - remove bad values
            List<int> yIndToIgnoreRaw = new List<int>();
            for (int colInd = 0; colInd < u.GetNColumns(); colInd++)
            {
                List<int> badValInd = Vec.FindValues(u.GetColumn(colInd), -9999, FindValues.NaN);
                yIndToIgnoreRaw.AddRange(badValInd);
            }
            yIndToIgnoreRaw.AddRange(Vec.FindValues(y, -9999, FindValues.NaN));
            yIndToIgnoreRaw.AddRange(Vec.FindValues(z_topside, -9999, FindValues.NaN));
            List<int> yIndToIgnore =(List<int>)yIndToIgnoreRaw.Distinct().ToList();

            // do scaling, input trickery and then regress
           
            u = u.Transpose();

            double[] y_plot = Vec.ReplaceIndWithValuesPrior(y, yIndToIgnore);// -9999 destroys plot
            double[] u1_plot = Vec.ReplaceIndWithValuesPrior(u.GetRow(0), yIndToIgnore);// -9999 destroys plot
            double[] u2_plot = Vec.ReplaceIndWithValuesPrior(u.GetRow(1), yIndToIgnore);// -9999 destroys plot
            double[] u3_plot = Vec.ReplaceIndWithValuesPrior(u.GetRow(2), yIndToIgnore);// -9999 destroys plot
            double[] u4_plot = Vec.ReplaceIndWithValuesPrior(u.GetRow(3), yIndToIgnore);// -9999 destroys plot
            double[] u5_plot = Vec.ReplaceIndWithValuesPrior(u.GetRow(4), yIndToIgnore);// -9999 destroys plot
            double[] u6_plot = Vec.ReplaceIndWithValuesPrior(z_topside, yIndToIgnore);// -9999 destroys plot

            // temperature does not change much based on changes in the upper half of the range valve-opening rate, as flow rates also
            // do not change that much (valve flow vs. choke opening is nonlinear)


			double z_Max = 60;
			double z_MaxTopside = 80;
			u = Matrix.ReplaceRow(u,0, Vec.Min(u.GetRow(0), z_Max));
			u = Matrix.ReplaceRow(u,1, Vec.Min(u.GetRow(1), z_Max));
			u = Matrix.ReplaceRow(u,2, Vec.Min(u.GetRow(2), z_Max));
			u = Matrix.ReplaceRow(u,3, Vec.Min(u.GetRow(3), z_Max));
			u = Matrix.ReplaceRow(u,4, Vec.Min(u.GetRow(4), z_Max));
			u = Matrix.Mult(u, 0.01);

			z_topside = Vec.Mult(Vec.Min(z_topside, z_MaxTopside), 0.01);
			z_topside = Vec.Mult(z_topside, 0.01);
			u = Matrix.Mult(u, z_topside);

			// lowpass filtering of inputs
			double TimeConstant_s = 1800;//73.24

			LowPass filter = new LowPass(TimeConstant_s);
			u = Matrix.ReplaceRow(u, 0, filter.Filter(u.GetRow(0), TimeConstant_s));
			u = Matrix.ReplaceRow(u, 1, filter.Filter(u.GetRow(1), TimeConstant_s));
			u = Matrix.ReplaceRow(u, 2, filter.Filter(u.GetRow(2), TimeConstant_s));
			u = Matrix.ReplaceRow(u, 3, filter.Filter(u.GetRow(3), TimeConstant_s));
			u = Matrix.ReplaceRow(u, 4, filter.Filter(u.GetRow(4), TimeConstant_s));

            if (u == null)
            {
                Console.WriteLine("u is null, something went wrong");
                return;
            }

            var uJaggedArray = u.Convert2DtoJagged();
            double[] parameters = Vec.Regress(y, uJaggedArray, yIndToIgnore.ToArray(), out _, out double[] y_mod,out double Rsq);

            double[] e = Vec.Sub(y_plot, y_mod);

            //present results
            if (y_mod == null)
            {
                Console.WriteLine("something went wrong, regress returned null");
            }
            else
            {
                Plot.Six(u1_plot, u2_plot, u3_plot, u4_plot, u5_plot,u6_plot, dT_s,"z_D1", "z_D2", "z_D3", "z_D4","z_D5","z_topside",true,false,null,t0);
                Plot.Two(y_plot, y_mod, dT_s, "T_Dtopside(meas)", "T_Dtopside(mod)",true,false,"Rsq"+Rsq.ToString("#.##"),t0);
                Plot.One(e,dT_s,"avvik",null, t0);
            }
        }
    }
}
```