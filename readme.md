
![Build Status](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/build.yml/badge.svg)
![Test Status](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/tests.yml/badge.svg)


# TimeSeriesAnalysis
*TimeSeriesAnalysis* is a toolbox that allows you to work .NET efficiently with time-series that may include *transients* or *dynamics*.

## Why?
Because the final product of many enterprise time-series applications is *required* to be implemented in .NET, why not try to make development as streamlined as possible?
Time-series analysis it typically done in languages like *Python*,*R* or *Matlab*, because high-level **toolboxes** support time-series analysis in these languages.
Tasks like loading data from a file, fitting a model to data or plotting time-series can be accomplished in a single line of code in .NET with the tools in this class library, just as you would do with one of those scripted languages.

That may even mean that early research/prototyping/data analysis can *start* in .NET, which removes the step re-writing prototype code into .NET.

## But honestly, you know you can just do time-series analysis with standard statistical langauges *xyz*, right? 

Yes and no. Most of the time each data point in a time-series is considered *stationary*, in which case a time-series can be considered a table of independent data points, and standards statisical methods can be applied to time-series just as for any other statisical data. 

But in some-applications the *transients* or *dynamics* of the process are visible in trends, in which case the data points are no longer independent, and in that case many standard statistical methods become invalid. This is often the case in time-series of for instance measured temperatures, pressures or flows, and analyzing such systems often requires filtering, or fitting models that include transient/dynamic terms. Models that include dynamic terms are key in dynamic simulation, and controlling such systems by means of automatic controllers such as PID-controllers is the domain of *control engineering*. 

Fitting dynamic models to dynamic time-series is the domain of *system identification*, running models that include terms for transients and their interactions is the field of *dynamic simulation*, designing algorithms that influence the dynamics of such systems is the domain of *control systems* and designing algorithms which calculate unmeasured variables based on the available measurements is the topic of *estimation theory*.

Dynamic simulators, control systems and estimators are almost exclusively implemented in the compiled languages like C/C++/C# in industry, which is what this toolbox attempts to address. 

## Documentation: Getting started, code examples and api reference documentation

<a href="https://equinor.github.io/TimeSeriesAnalysis">TimeSeriesAnalysis reference documentation</a>

## Contributing
This project welcomes contributions and suggestions. 
Please read [CONTRIBUTING.md](contributing.md) for details on our code of conduct, and the process for submitting pull requests. 

## Discussion forum
Questions related to TimeSeriesAnalysis can be posted in the [github discussion pages](https://github.com/equinor/TimeSeriesAnalysis/discussions).

## Authors and contact persons
Steinar Elgsæter

## Licence
TimeSeriesAnalysis is distributed under the [MIT licence](LICENSE).
