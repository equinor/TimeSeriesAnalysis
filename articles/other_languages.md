# Loading this library from MatLab or Python

## Python

There is a package for ``Python`` called [Python.Net](http://pythonnet.github.io/)
that allows you to access the methods in this library.

## MatLab

``MatLab`` supports importing ``.NET`` assemblies(*.dlls) through the built-in [NET.addAssembly()](https://se.mathworks.com/help/matlab/ref/net.addassembly.html) command.
> [!Note]
> Once an assembly is loaded in ``MatLab``, it cannot be unloaded except by restarting the program.

> [!Note]
> Not all .NET features are supported in ``MatLab`` a list of limitiations is given in 
> https://se.mathworks.com/help/matlab/matlab_external/limitations-to-net-support.html


Step-by-step:

- Take a complete build of ``TimeSeriesAnalyis.dll`` and accompanying assemblies such as ``Accord.NET`` ``*.dlls`` and copy into your ``MatLab`` working directory such as ``C:\Appl\OneDrive\Documents\MATLAB`` (either build yourself or copy pre-built zip-file from https://github.com/equinor/TimeSeriesAnalysis/releases/ and unzip.)
- load the assembly with ``NET.addAssembly()``:
- import the methods of the .NET assembly using the ``MatLab`` command ``import``
- you can now directly call methods in the "TimeSeriesAnalysis" namespace (consult the API reference)

*A simple "hello-world" example:*
```
NET.addAssembly('C:\Appl\OneDrive\Documents\MATLAB\TimeSeriesAnalysis.dll')
import TimeSeriesAnalysis.*
results = Vec.Add([1 2],[3 4])
```
will give as a result a ``Double[]``
where
```
>> result(1)

ans =

     4
>> result(2)

ans =

     6
```
