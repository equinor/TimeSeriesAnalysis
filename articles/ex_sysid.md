# Example 4 : Fitting a dynamic model to transient data example

```
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
```
		