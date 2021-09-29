# Example 4 : Fitting a dynamic model to transient data example

```
	public void ex3_sysid()
	{
		int timeBase_s = 1;
		DefaultProcessModelParamters parameters = new DefaultProcessModelParamters
		{
			TimeConstant_s = 10,
			ProcessGains = new double[] { 1, 2},
			Bias = 10
		};
		DefaultProcessModel model = new DefaultProcessModel(parameters);

		double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
				Vec<double>.Fill(1, 50));
		double[] u2 = Vec<double>.Concat(Vec<double>.Fill(2, 31),
				Vec<double>.Fill(1, 30));
		double[,] U = Array2D<double>.InitFromColumnList(new List<double[]>{u1 ,u2});

		ProcessDataSet dataSet = new ProcessDataSet(null, U, timeBase_s);
		ProcessSimulator.EmulateYmeas(model, ref dataSet);

		Plot.FromList(new List<double[]> { dataSet.y_meas, u1, u2 },
			new List<string> { "y1=y_meas", "y3=u1", "y3=u2" }, timeBase_s);

		DefaultProcessModel identifiedModel = DefaultProcessModelIdentifier.Identify(ref dataSet);

		Plot.FromList(new List<double[]> { dataSet.y_meas, dataSet.y_sim },
			new List<string> { "y1=y_meas", "y1=y_sim"}, timeBase_s);
	}
```
		