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

The first plot shows the dataset, showing inputs``u1``, ``u2`` and output ``y``:

![Example 4:dataset](./images/ex4_dataset.png)

**Notice of the time-delay and time-constant are clearly visible in this dataset**. 
The resulting fit between model and dataset is shown below. *The two time-series are virtually identical*.

![Example 4:output](./images/ex4_results.png)

The resulting console output gives more detail on the parameters found:

```
DefaultProcessModel
-------------------------
ABLE to identify
TimeConstant_s : 15
TimeDelay_s : 5
ProcessGains : [1;2.01]
ProcessCurvatures : null
Bias : 8,85
u0 : [0.821;1.52]
-------------------------
fitting objective : 1,273E-29
fitting R2: 100
fitting : no error or warnings
solver:v1.Dynamic	
```

> [!Note]
> Notice how the R-squared is ``100``, indicating a perfect match, and that the fitting objective function is 
> **extremly low**, 
> which also indicate a very good match. Of course, on real-world datasets these two metrics will not be quite as good.
 
	