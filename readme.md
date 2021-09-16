TimeSeriesAnalysis : 

<a href="https://stunning-fortnight-3ee29831.pages.github.io/api/TimeSeriesAnalysis.html" >API reference documentation</a>


What is this?
=========================================
This is a .NET class library for making it easy to work with time series in .NET language such as C#. 
It handles
- loading data from CSV
- manipulating time-series data as arrays and vectors
- filtering
- doing linear regression
- plotting times-series  

The aim of this library is to make the process of working with time series as easy as possible, 
and the resulting workflow should be comparable to working in Matlab, Python or R. 

This means that you can treat time series as vectors and matrices easily, without worrying about the arrays underneath, and perform 
operations like adding, subtracting, multiplying with vectors and matrices, as well as typicall pre-processing tasks like selecting
subsets of a larger dataset, removing spurious values, min/max range limits etc. 

The result is a that tasks that you would normally do in for instance Matlab due to the "simplicity" offered by the language, can now be 
accomplished in about the same amount of code in C#/.NET. So the resulting code is in many was "just as simple".

The benefit of doing this in C#/.NET 
- that you get the benefits of a compiled language,prototype code is written in a lanugage suitable for implementation, (unlike Matlab scripts). That means you avoid introducing Matlab code generator toolboxes which act as complex black-boxes, and often require expensive licenses. 
- the resulting code does not required a paid license to run, anybody can download a free copy of VS code and re-compile the code, 
without requiring a working license server, a correct number of licenses or enough licenses. 
- you can easily extend your code to for instance run in paralell using the "paralell.for" functionaliy freely available in .NET, wheres this functionality
may require a very expensive toolbox in Matlab(Paralell processing toolbox).

Originally this code has been written with control-engineering in mind, as control engineers typically 
prefer compiled languages such as C# for code that is to be integrated in the control system. 

Install instructions to make in-browser time-series plotting work
=========================================

The plotting in this package is based on the javacsript plotting library plot.ly., a copy of which is included.

The repo includes a folder "www\plotly" and for plotting to work you need to run an http-server such as Internet Information Services that
in your broser serves up a folder on your computer as "localhost". 

Copy the folder "plotly" into the folder that for instance IIS is serveing where it can live along side other pages you may be serving up),
such as "c:\inetpub\plotly" if  "c:\inetpub" is the folder that IIS is serving.

Note that this repo includes some custom javascript code which wraps some plot.ly timeseries plotting functionality, so that a "Plot(Var1)" in C# can trigger
a browser to open with a time-series plot of "Var1". 


Plotting works by launching a browser(chrome) and directing it to "http://localhost/plotly/index.html", but all the low-level details are handled by the Plot class for you.

Plotting
=========================================

Plotting supports
- one or two subplots(stacked vertically)
- one or two y-axes on either subplot
- support for zooming in the plot, 
- subplots x-axes are linked when zooming
- ability to turn trends on/off, which will cause auto-zoom to update
- ability to hover over trends to inspect values
- currently up to six trends can be plotted on a page in total(this can be increased if there is interest)


Usage
=========================================

* You may like to load data from a file into a 2D array for doubles and strings and an array of table headers using CSV.loadDataFromCSV.  
* the dates in the array can be parsed using GetColumnParsedAsDateTime (useful to find data sample time, and data time span, and selecting a subset timespan for analysis)
* use Array.IndexOf() on the header array to find the indices of variables in the CSV-file, 
* use GetColumn() to or GetColumns to load data into 1D vectors or 2D arrays
* use GetRowsAfterIndex to cut off a chunk of data the data 
* use Vec.FindValues to find values which are nan or out-of-range which are to be ignored in regression 
* use Transpose() to transpose matrix before regression as needed
* use Vec.ReplaceIndWithValuesPrior to replace bad values with the prior value, this makes for nicer plots. 
* if you want to multiply, divide, add or subtract or use min, max on the arrays retreived from the csv file, use Vec.Add, Vec.Sub, Vec.Mult or Vec.Div
* you can do the above on rows of a 2d-matrix by using Matrix.ReplaceRow
* scaling of entire 2d-matrices can be done by Matrix.Mult
* Low-pass filtering on specific time-series can be done by LowPass().
* to do regression, give the regressors to Vec.Regress, along with the indices to ignore based on pre-processing, which returns the model output along with paramters and uncertainty
* finally, you can plot the resulting regression using different versions of Plot. To plot a single variable, use Plot.One, to plot two variables Plot.Two() etc. 

Currently plotting supports up to two subplots where each can have two y-axes. Remember that for plotting to work, you need to be running a web-server on your computer
and add the "www\plotly" folder to the folder that the web server serves up. 

Example usage: 
=========================================

This example uses a ficticious csv-file example.csv, that has column headers
 "Time","Var1","Var2","Var3","Var4,"Var5",Var6","Var7".
 
 "Var1" is to be modelled by Var2-Var6 as regressors, while Var7 is to be multiplied to Var2-Var6. 
 The data contains some instances of -9999 which indicates bad data, and this is removed in preprocessing.
 
 A low-pass filter is used to imitate a time-constant in the system by smoothing the model inputs.
 
 Only the data starting after a specific t0 is to be used in the regression, so a subset of the raw data in the csv-file is 
 given to regression.
 
This example illustrates that by using the TimeSeriesAnalysis package, the complexity of the code required to do practical exploratory time-series
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



Nuget package upload how-to
=========================================

This is  .NET framework 4.6.1 class library published to github packages, by means of the following tutorial:
https://github.community/t/publish-net-framework-nuget-package/3077/2
and here:
https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild

Note that the steps here are somewhat different to most online tutorials which target .NET Core and use the "dotnet" CLI instead of "nuget" CLI.

For future reference, this is the steps followed:

* make sure the classes you want to give access to are public.

* the description that will be shown in nuget when downloading is pulled from <description></description> in the .csproj file beneath <propertygroup>. Consider adding it.
Also add the the url to the repository and some other info such as shown below:
	
```
    <RepositoryUrl>https://github.com/equinor/TimeSeriesAnalysis</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageOutputPath>bin\debug\</PackageOutputPath>
    <Version>1.0.0</Version>
    <Authors>Steinar Elgsæter</Authors>
	<description>todo</description> 
```	

* reccomend adding in information to the AssemblyInfo.cs before starting.
* a personal acceess token xxxxxxx needs to be generated for your user in github,and needs access to "read:packages", 
"write:packages" and "repo".organization access (authorize with SSO ) and then press "authorize"
* create a nuget.config file that defines "github" as a nuget destination:

```	<?xml version="1.0" encoding="utf-8"?>
	<configuration>
		<packageSources>
			<clear />
			<add key="github" value="https://nuget.pkg.github.com/equinor/index.json" />
			<add key="nuget" value="https://api.nuget.org/v3/index.json" />
		</packageSources>
	 <packageSourceCredentials>
			<github>
				<add key="Username" value="yourgithubuser" />
				<add key="ClearTextPassword" value=xxxxxxx" />
			</github>
		</packageSourceCredentials>
</configuration>
```

* pacakges.nuget needs to be moved into project file *.csproj as <packagereference> items instead
* NuGet.Build.Tasks.Pack need to be added as a pacakage to project
* need to download nuget.exe and use it to push generated .nupkg file

* then to publish put the following two commands in a publish.bat file:
```	
	nuget setapikey xxxxxxxx -source "github"
	nuget push bin\Debug\*.nupkg -source "github" -SkipDuplicate
	pause
```

* check that the script concludes with "Your package was pushed" and no error messages in yellow or red.

* notice that you need to iterate the version number in your .csproj file every time you push a new version of the package.


Nuget package download how-to
=========================================

To use this nuget pacakge, you need to incldue the nuget.config file above in the solution that intends to pull down the package. You can then select github as
the package source in Visual Studio "Manage Nuget packages for Solution". 
	
