TimeSeriesAnalysis : 

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

Originally this code has been written with control-engineering in mind, as control engineers typically 
prefer compiled languages such as c# for code that is to be integrated in the control system. 


Install instructions to make in-browser time-series plotting work
=========================================

The plotting in this package is based on the javacsript plotting library plot.ly., a copy of which is included.

The repo includes a folder "www\plotly" and for plotting to work you need to run an http-server such as Internet Information Services that
in your broser serves up a folder on your computer as "localhost". 

Copy the folder "plotly" into the folder that for instance IIS is serveing(where it can live along side other pages you may be serving up),
such as "c:\inetpub\plotly" if  "c:\inetpub" is the folder that IIS is serving.

Plotting works by launching a browser(chrome) and directing it to "http://localhost/plotly/index.html", but all the low-level details are handled by the Plot class for you.



