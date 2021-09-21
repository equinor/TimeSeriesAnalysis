# Example 3 : Fitting a dynamic model to transient data example

```
	[Test, Explicit]
	public void ex3_sysid()
	{
		double dT_s = 1;
		ProcessModelParamters paramters = new ProcessModelParamters
		{
			TimeConstant_s = 10,
			ProcessGain = new double[] { 1, 2},
			Bias = 10
		};
		ProcessModel model = new ProcessModel(paramters);

		double[] u1 = Vec<double>.Concat(Vec<double>.Fill(0, 11),
				Vec<double>.Fill(1, 50));
		double[] u2 = Vec<double>.Concat(Vec<double>.Fill(2, 31),
				Vec<double>.Fill(1, 30));
		double[,] U = Array2D<double>.InitFromColumnList ( new List<double[]>{u1 ,u2} );
		double[] y_simulated = model.Simulate(U,dT_s);


	}
```
		