
# TimeSeriesAnalysis .NET class library

## At a glance

*TimeSeriesAnalysis* is a .NET class library for making it easy to work with time series in *.NET* framework (written in *C#*). 
It handles typical tasks related to time-series analysis such as
- loading time-series data from CSV-files,
- manipulating time-series data as arrays and vectors,
- filtering out values by range, detecting and removing bad values
- fitting models to sets on time-series data(by linear regression), and
- plotting times-series (in a browser window).

**The aim of this library is to make the process of working with time series as easy as possible, 
and the resulting workflow should be comparable to working in *Matlab*, *Python* or *R*.**

This means that you can treat time series as vectors and matrices easily, without worrying about the arrays underneath, and perform 
operations like adding, subtracting, multiplying with vectors and matrices, as well as typicall pre-processing tasks like selecting
subsets of a larger dataset, removing spurious values, min/max range limits etc. 

The result is a that tasks that you would normally do in for instance *Matlab* due to the perceived simplicity offered by the language, can now be 
accomplished in about the same amount of code in *C#/.NET*. So the resulting code is in many was just as simple.

The benefit of doing this in *C#/.NET* 
- that you get the benefits of a compiled language,prototype code is written in a lanugage suitable for implementation, (unlike *Matlab* scripts). That means you avoid introducing Matlab code generator toolboxes which act as complex black-boxes, and often require expensive licenses. 
- the resulting code does not required a paid license to run, anybody can download a free copy of *VS Code* and re-compile the code, 
without requiring a working license server, a correct number of licenses or enough licenses. 
- you can easily extend your code to for instance run in paralell using the ``paralell.for`` functionaliy freely available in .NET, wheres this functionality
may require a very expensive toolbox in *Matlab* (*Paralell processing toolbox*).


> [!Note]
> Originally this code has been written with control-engineering in mind, as control engineers typically 
> prefer compiled languages such as C# for code that is to be *integrated in the control system*. 
> Control systems are usually written in compiled languages in the C/C++/C# family, and the same is also true of other
> enterprise commercial software that deals heavily with time-series, such as dynamic simulators or condition-based monitoring systems. 

## Plotting capabilities

Plotting supports
- one or two subplots(stacked vertically)
- one or two y-axes on either subplot
- support for zooming in the plot, 
- subplots x-axes are linked when zooming
- ability to turn trends on/off, which will cause auto-zoom to update
- ability to hover over trends to inspect values
- currently up to six trends can be plotted on a page in total(this can be increased if there is interest)

The plotting leverages the javascript framework [plot.ly](https://plotly.com/javascript/). Some javascript extensions have been made to this toolbox to allow
time-series to seemlessly be exported from your .NET code to the browser.

Consider the unit-test ``PlotUnitTests.SubplotPositionWorksOk()``:

The code below is used to generate four "vectors", arrays of doubles, with a step change in each.
```
public void SubplotPositionWorksOk()
{
   double[] input2 = Vec<double>.Concat(Vec<double>.Fill(0, 20), Vec<double>.Fill(2, 20));
   double[] input1 = Vec<double>.Concat(Vec<double>.Fill(0, 10), Vec<double>.Fill(1, 30));
   double[] input3 = Vec<double>.Concat(Vec<double>.Fill(0, 30), Vec<double>.Fill(1, 10));
   double[] input4 = Vec<double>.Concat(Vec<double>.Fill(0, 35), Vec<double>.Fill(1, 5));

   string plotURL = Plot.FromList(new List<double[]>{ input1,input2,input3,input4},
		new List<string>{ "y1=input1","y2=input2","y3=input3","y4=input4"},1,"unit test", 
		new DateTime(2020,1,1, 0,0,0), "Test_SubplotPositionWorksOk");
}
```
> [!Note]
> Note how the ``Vec.Fill()`` an ``Vec.Concat()`` of ``TimeSeriesAnalysis`` package is used in this example to create two vectors of a given
> length and value and concatenate them in a single line of code.

The above code generates the following interactive plot in a Chrome-window(this window pops up automatically):

![Example plot](articles/images/example_plotting.png)

This plot has two *subplots*(one top, one buttom). Each subplot has both a left and a right axis: 
the top subplot has axes ``y1`` and ``y2`` and the bottom subplot has axes ``y3`` and ``y4``. 

By using the top left menu, it is possible to *zoom* and *drag* the plots, and the two subplots are *linked*,
meaning when you zoom in one of them, the x-axes of the other plot will zoom as well. 
Moving the cursor over each plot allows the values to be browsed by an interactive ``scooter``.

By clicking on the variable names in the *legend* on the top left, it is possible to disable plotting selected variables.

> [!Note]
> Multiple plots will cause Chrome to display them in multiple tabs. A large number of figures can be generated and sorted in this way. 


## Install instructions: in-browser time-series plotting

*For time-series plot to work, you will need to run a http-server locally on your machine.*

The plotting in this package is based on the javacsript plotting library plot.ly., a copy of the bundled minified javascript plot.ly package 
``plotly.min.js`` is included for convenience, but other versions can also be downdloaded from the vendor site.

The repo includes a folder "www\plotly" and for plotting to work you need to run an http-server such as *Internet Information Services(ISS)* that
in your broser serves up a folder on your computer as "localhost". 

Copy the folder ``plotly`` into the folder that for instance IIS is serveing where it can live along side other pages you may be serving up),
such as "c:\inetpub\plotly" if  "c:\inetpub" is the folder that IIS is serving.

Note that this repo includes some custom javascript code which wraps some ``plot.ly`` timeseries plotting functionality, so that a "Plot(Var1)" in C# can trigger
a browser to open with a time-series plot of "Var1". 


Plotting works by launching a browser(chrome) and directing it to ``http://localhost/plotly/index.html``, but all the low-level details are handled by the Plot class for you.



> [!Note]
> You can use any http-server you like to serve up the javascript plotting files, and these files can be served from a 
> http-server that you may already be running. Node.js for instance could be used in stead of IIS, many other alternatives exist.
> If you do not already run a http-server locally on your windows-machine,IIS may be easy to start with as it is bundled with your operating system. 




## A typical use-case

* You may like to load data from a file into a 2D array for doubles and strings and an array of table headers using ``CSV.loadDataFromCSV``.  
* the dates in the array can be parsed using ``Array2D.GetColumnParsedAsDateTime()`` (useful to find data sample time, and data time span, and selecting a subset timespan for analysis)
* use ``Array2D.IndexOf()`` on the header array to find the indices of variables in the CSV-file, 
* use ``Vec.GetColumn()`` or ``Vec.GetColumns()`` to load data into 1D vectors or 2D arrays
* use ``Vec.GetRowsAfterIndex()`` to cut off a chunk of data the data 
* use ``Vec.FindValues()`` to find values which are nan or out-of-range which are to be ignored in regression 
* use ``Matrix.Transpose()`` to transpose matrix before regression as needed
* use ``Vec.ReplaceIndWithValuesPrior()`` to replace bad values with the prior value, this makes for nicer plots. 
* if you want to multiply, divide, add or subtract or use min, max on the arrays retreived from the csv file, use ``Vec.Add()``, ``Vec.Sub``(), ``Vec.Mult()`` or ``Vec.Div()``
* you can do the above on rows of a 2d-matrix by using ``Matrix.ReplaceRow()``
* scaling of entire 2d-matrices can be done by ``Matrix.Mult()``
* Low-pass filtering on specific time-series can be done by ``LowPass.Filter()``.
* to do regression, give the regressors to ``Vec.Regress()``, along with the indices to ignore based on pre-processing, which returns the model output along with paramters and uncertainty
* finally, you can plot the resulting regression using ``Plot.FromList``.  








