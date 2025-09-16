# Loading and saving time series to and from csv-files with TimeSeriesDataSet

The library is intended to be able to read and write data to file as csv(comma-separated variable)-files.

Internally the code is stored in objects of the ``TimeSeriesDataSet`` type. 

This class has a method ``LoadFromCSV()`` that can read a dataset from a csv-file, or a string with the
contents of a csv-file. Using the method ``ToCsv()`` the time series data in a this object can also be peristed
to a csv-file. 

The format of the csv file is as follows. Note that ``,`` is used to delinate the columns, so that 
``.`` should always be used to describe doubles.

If no absolute timestamp is given, then the time column will be in seconds. 
```
Time,PID.PID_U,PID.Setpoint_Yset,Process1.Output_Y,Disturbance1.Output_Y,Disturbance1.External_U
0,31.8181,60,60,25,25
1,31.8181,60,60,25,25
2,31.8181,60,60,25,25
3,31.8181,60,60,25,25
```
Absolute timestamps are also supported, in which case they are on the format "YYYY-MM-DD HH:mm:ss" such as:

```
Time, Variable1
2024-11-26 12:29:33,1
2024-11-26 12:30:33,2
2024-11-26 12:31:33,3
2024-11-26 12:32:33,4
2024-11-26 12:33:33,5
2024-11-26 12:34:33,6
2024-11-26 12:35:33,7
````
