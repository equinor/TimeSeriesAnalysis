
![Build Status](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/build.yml/badge.svg?branch=master)
[![Tests](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/tests.yml/badge.svg)](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/tests.yml)
# TimeSeriesAnalysis : Data-driven dynamic modeling and simulation 

## Overview
This library deals with developing *time-series models and simulators* from *time-series data*. 

The methods in this library are primarily designed to describe time-series of physical, real-world systems for an industrial setting.

Real-world industrial systems 
- often exhibit repeatable *transient responses* to changes in inputs
- usually include *feeback loops*, either due to recirculation in the process or because the system is controlled with *PID-controllers*(PID-model is included)
- and often consist of a network of interconnected units that interact. 

This library was designed to create *dynamic unit models* that can be chained into interconnected networks that can include PID-control or other feedback- or circulation loops.

Models are not derived from physical first principles, but are inferred from time-series data using the principles of *system identification*, the methods to infer models are built on multiple modfied linear regression steps(identification methods are included).   

The library is written in C# but can be referenced from any language that can reference .NET language, including Matlab or Python. The target framework is <i>.NET Standard 2.0.</i>

### Use cases: Digital twins, anomaly detection, "what-if", PID-tuning, monitoring and screening

The power of this library lies in the ability to automate or semi-automate the steps of identfiying new models or simulating existing models at scale. In effect this libary can create "digital twin" models of sections of a process. 

The methods lend themselves readily to for instance automatically building a large number of similar models for similar sections of a process, for instance to monitor every valve, separation tank or PID-control loop in a similar fashion, in a way that lends itself to process monitoring and -screening. 

The methods in this library are *scalable* and *modular*, i.e. by chaining together unit models, the library can also simulate larger sections of a process plant, so the methodology could in principle be extended to assemble a digital twin of an entire process plant. 

Models can be run alongside the plant, and monitoring the difference between measured and modelled values can provide insight into changes in the plant (anomaly detection.) By manually simulating changes on these kinds of models, "what-if" scenarios can be evaluted either manually or automatically. One very interesting such use-case is to evaluate the possible benefits of re-tuning PID-controllers.

The above use-cases could be put under the umbrella term "advanced analytics", i.e. using algorithms and data to make deep insights about causes and effects to make predictions and reccomendations. 

### Explainable models for low-information datasets

Note that although this library uses regression methods and algorithms to learn parameters to describe data, the methodology is somewhat different from traditional machine learning in that models follow the principles of system identification rather than AI. 
The number of free parameters are kept low by design, as this makes reduces the likelihood of over-fitting. Over-fitting is particularly important when dealing with industrial data, as this kind of data has to be used "as-is" and one often has less information than one would like. 
Another benefit of keepting the number of fitted paramters low is that models remain *explainable* and human-understandable, and it becomes possible to combine fitted models with human pre-knowledge, what is often referred to as "grey-box" modeling.

## Documentation:

:red_circle: **<a href="https://equinor.github.io/TimeSeriesAnalysis">TimeSeriesAnalysis documentation</a>** :red_circle:

:red_circle: **<a href="https://equinor.github.io/TimeSeriesAnalysis/api/TimeSeriesAnalysis.html">TimeSeriesAnalysis API documentation</a>** :red_circle:

## Roadmap

The aim of this repository is to collect building blocks to developing digital twins from datasets in a manner that can be automated. 
This repository development in versions ``1.x``, is focused on  "getting the basics right"
- defining a set of unit models that together can simulate most industrial processes (especially ``PidModel`` and ``UnitModel``),
- establishing unit identification methods for each unit model type, and
- efficent simulation of "plants" of unit models

## Contributing
This project welcomes contributions and suggestions. 
Please read [CONTRIBUTING.md](contributing.md) for details on our code of conduct, and the process for submitting pull requests. 

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


## Discussion forum and contact person
The contact person for this repository is Steinar Elgsæter, please post any question you may have related to TimeSeriesAnalysis 
in the [github discussion pages](https://github.com/equinor/TimeSeriesAnalysis/discussions).

## License
TimeSeriesAnalysis is distributed under the [Apache 2.0 license](LICENSE).
