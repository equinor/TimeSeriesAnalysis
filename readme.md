
![Build Status](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/build.yml/badge.svg?branch=master)
![Test Status](https://github.com/equinor/TimeSeriesAnalysis/actions/workflows/tests.yml/badge.svg?branch=master)


# TimeSeriesAnalysis
*TimeSeriesAnalysis* is a class library that allows you to work .NET efficiently with time-series that may include *transients* or *dynamics*.

## Why?
Because the final product of many enterprise time-series applications(like control algorithms or dynamic simulators) is *required* to be implemented in .NET, why not create a toolbox streamline such development as much possible? With good tooling, it may even be possible to *start* early research/prototyping/data analysis directly in .NET, removing the need to port code later.

## In a rush: getting started

- check out the repository 
- build. (Developed with ``Visual Studio 2019``)
- open ``Test Explorer`` and read/run unit tests too examine capability and API of the class library
- did not work? Please refer to the detailed ``Getting started`` chapter in the documentation below.

## Documentation: Getting started, code examples and API reference documentation

:red_circle: **<a href="https://equinor.github.io/TimeSeriesAnalysis">TimeSeriesAnalysis reference documentation</a>** :red_circle:

## Roadmap

Currently in versions ``1.x``, the development focuses on creating small-scale "building blocks", modeling components such as individual PID-controllers and small-scale process models. Toward version ``2.x`` the plan is to build on top the established building blocks to expand into more *automated* modeling and analysis of *large-scale* systems (connecting sub-systems together), to build **"digital twins"** of larger parts of process plants based on **data mining**. 

## Contributing
This project welcomes contributions and suggestions. 
Please read [CONTRIBUTING.md](contributing.md) for details on our code of conduct, and the process for submitting pull requests. 

## Discussion forum
Questions related to TimeSeriesAnalysis can be posted in the [github discussion pages](https://github.com/equinor/TimeSeriesAnalysis/discussions).

## Authors and contact persons
Steinar Elgsæter

## License
TimeSeriesAnalysis is distributed under the [MIT license](LICENSE).

