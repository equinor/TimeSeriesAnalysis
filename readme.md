
![Build Status](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/build.yml/badge.svg?branch=master)
![Test Status](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/tests.yml/badge.svg?branch=master)


# TimeSeriesAnalysis
*TimeSeriesAnalysis* is a class library that allows you to work efficiently with time-series 
in .NET that may include *transients* or *dynamics*, including identification of dynamic models,
dynamic simulation, filtering and PID-control. The aim of the library is to support mining of time-series data for advanced analytics. 

## Why?
Because the final product of many enterprise time-series applications(like control algorithms or dynamic simulators) is *required* to be implemented in .NET, why not create a toolbox streamline such development as much possible? With good tooling, it may even be possible to *start* early research/prototyping/data analysis directly in .NET, removing the need to port code later.

## In a rush: getting started

- check out the repository 
- build. (Developed with ``Visual Studio 2019``)
- open ``Test Explorer`` and read/run unit tests too examine capability and API of the class library
- did not work? Please refer to the detailed ``Getting started`` chapter in the documentation below.

## Documentation: Getting started, code examples and API reference documentation

:red_circle: **<a href="https://equinor.github.io/TimeSeriesAnalysis">TimeSeriesAnalysis reference documentation</a>** :red_circle:

## But I prefer working with time-series in MatLab or Python

Not to fear, ``MatLab`` supports importing ``.NET`` assemblies(*.dlls) through the built-in [NET.addAssembly()](https://se.mathworks.com/help/matlab/ref/net.addassembly.html) command.
There is a package for  ``Python`` called [Python.Net](http://pythonnet.github.io/)
that allows you to access the methods in this library.

## Roadmap

Currently in versions ``1.x``, the development focuses on creating small-scale "building blocks", 
modeling components such as individual PID-controllers and small-scale process models.
 Toward version ``2.x`` the plan is to build on top the established building blocks to expand 
 into more *automated* modeling and analysis of *large-scale* systems (connecting sub-systems together), 
 to build **"digital twins"** of larger parts of process plants based on **data mining**. 
Models should be *human-configurable* and *human-readable* so that "mined" models can be adjusted with human insight ("grey-box" rather than "black-box").

## Contributing
This project welcomes contributions and suggestions. 
Please read [CONTRIBUTING.md](contributing.md) for details on our code of conduct, and the process for submitting pull requests. 

## Discussion forum and contact person
The contact person for this repository is Steinar Elgsæter, please post any question you may have related to TimeSeriesAnalysis 
in the [github discussion pages](https://github.com/equinor/TimeSeriesAnalysis/discussions).

## License
TimeSeriesAnalysis is distributed under the [MIT license](LICENSE).

