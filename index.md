
# TimeSeriesAnalysis .NET class library

## At a glance

*TimeSeriesAnalysis* is a .NET class library for making it easy to work with time series in .NET language (mostly C#). 
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

The result is a that tasks that you would normally do in for instance Matlab due to the perceived simplicity offered by the language, can now be 
accomplished in about the same amount of code in C#/.NET. So the resulting code is in many was just as simple.

The benefit of doing this in C#/.NET 
- that you get the benefits of a compiled language,prototype code is written in a lanugage suitable for implementation, (unlike *Matlab* scripts). That means you avoid introducing Matlab code generator toolboxes which act as complex black-boxes, and often require expensive licenses. 
- the resulting code does not required a paid license to run, anybody can download a free copy of *VS Code* and re-compile the code, 
without requiring a working license server, a correct number of licenses or enough licenses. 
- you can easily extend your code to for instance run in paralell using the ``paralell.for`` functionaliy freely available in .NET, wheres this functionality
may require a very expensive toolbox in *Matlab* (*Paralell processing toolbox*).

Originally this code has been written with control-engineering in mind, as control engineers typically 
prefer compiled languages such as C# for code that is to be integrated in the control system. 

## Plotting capabilities

Plotting supports
- one or two subplots(stacked vertically)
- one or two y-axes on either subplot
- support for zooming in the plot, 
- subplots x-axes are linked when zooming
- ability to turn trends on/off, which will cause auto-zoom to update
- ability to hover over trends to inspect values
- currently up to six trends can be plotted on a page in total(this can be increased if there is interest)

The plotting leverages the javascript framework plot.ly. Some javascript extensions have been made to this toolbox to allow
time-series to seemlessly be exported from your .NET code to the browser.


## Install instructions: in-browser time-series plotting

The plotting in this package is based on the javacsript plotting library plot.ly., a copy of which is included.

The repo includes a folder "www\plotly" and for plotting to work you need to run an http-server such as Internet Information Services that
in your broser serves up a folder on your computer as "localhost". 

Copy the folder "plotly" into the folder that for instance IIS is serveing where it can live along side other pages you may be serving up),
such as "c:\inetpub\plotly" if  "c:\inetpub" is the folder that IIS is serving.

Note that this repo includes some custom javascript code which wraps some plot.ly timeseries plotting functionality, so that a "Plot(Var1)" in C# can trigger
a browser to open with a time-series plot of "Var1". 


Plotting works by launching a browser(chrome) and directing it to ``http://localhost/plotly/index.html``, but all the low-level details are handled by the Plot class for you.



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








