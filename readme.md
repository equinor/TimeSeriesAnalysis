
![Build Status](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/build.yml/badge.svg?branch=master)
![Test Status](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/tests.yml/badge.svg?branch=master)


# TimeSeriesAnalysis
An open-source library for development and acceptance testing of time-series-based algorithms that may incude
- Transients/dynamic model identification, 
- dynamic simulation, 
- filtering, and
- advanced industrial PID-control. 

The library can be used from C#, Matlab or Python.

## Documentation:

:red_circle: **<a href="https://equinor.github.io/TimeSeriesAnalysis">TimeSeriesAnalysis documentation</a>** :red_circle:

:red_circle: **<a href="https://equinor.github.io/TimeSeriesAnalysis/api/TimeSeriesAnalysis.html">TimeSeriesAnalysis API documentation</a>** :red_circle:

## Getting started

Regardless if you call this library from C#, Python or Matlab, the resulting code is very similar. 
A good first step is to read through the [getting started](https://equinor.github.io/TimeSeriesAnalysis/articles/examples.html) examples 
and try copying in that code and getting it to run to get the hang of things

### Calling this library from Python

The library can be conveniently used from ``Python``
- grab the zipped binaries from a [release](https://github.com/equinor/TimeSeriesAnalysis/releases) and unzip 
- set up [Python.Net](http://pythonnet.github.io/),
- start calling the [API-methods](https://equinor.github.io/TimeSeriesAnalysis/api/TimeSeriesAnalysis.html) 
(see help article [Getting started:Python](https://equinor.github.io/TimeSeriesAnalysis/articles/python.html).)

### Calling this library from MatLab

The library be conveniently used from ``Matlab``
- grab the zipped binaries from a [release](https://github.com/equinor/TimeSeriesAnalysis/releases) and unzip 
- load the assembly with `` NET.addAssembly()``
- start calling the [API](https://equinor.github.io/TimeSeriesAnalysis/api/TimeSeriesAnalysis.html) 
(see article [Getting started:Matlab](https://equinor.github.io/TimeSeriesAnalysis/articles/matlab.html).)

### Calling this library from your .NET project

- create a project in Visual Studio, and 
- import the "TimeSeriesAnalysis" library from NuGet
- start calling the [API](https://equinor.github.io/TimeSeriesAnalysis/api/TimeSeriesAnalysis.html) 

### Working with the code of this repository

- check out this repository,
- make sure [NuGet is setup](https://equinor.github.io/TimeSeriesAnalysis/articles/nuget_setup.html) correctly, and
- all examples are implemented as [unit-tests](/articles/unit_tests.html) using NUnit, try running these to 

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

