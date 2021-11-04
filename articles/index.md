
# TimeSeriesAnalysis .NET class library

## At a glance

**TimeSeriesAnalysis is a library intended to support efficient test-driven development of 
time-series-based algorithms. 
Studying data with transients/dynamics, through dynamic model identification, 
dynamic simulation, filtering and PID-control is supported. 
The library aims to support mining of time-series data for advanced analytics.**

*TimeSeriesAnalysis*  handles typical tasks related to time-series analysis such as
- *loading* time-series data from CSV-files,
- *manipulating* time-series data as arrays and vectors,
- *filtering* out values by range, detecting and removing bad values, smoothing
- *fitting static models* to time-series data by linear regression(based on ``Accord.NET``), 
- *fitting dynamic models* to time-series by custom methods that build on linear regression, and
- *plotting* times-series (in a browser window with ``plot.ly``).
- *dynamic simulation* of systems that may include interactions with *PID-controllers* (the library includes a reference PID-controller implementation).

**The aim of this library is to make the process of working with time-series as easy as possible, 
and the resulting work flow should be comparable to working in *Matlab*, *Python* or *R*.**

## Motivation

The aim in creating this library is to support 
a) development of data-driven dynamic *digital twins* and 
b) "mining" of industrial time-series data for *advanced analytics*.

Advanced analytics requires **automated** or **semi-automated** analysis to generate reccomendations, 
thus it needs to be "data-driven", yet be reliable and require little manual configuration and assitance.

Assuming that a sucess criterion for creating high-quality, dependable and adaptable software is to 
have a software whose source-code is available for debugging to end-users (i.e. **is open**) and
 free of "black-box" external dependencies. This kind of software is also easier to integrate into other 
 solutions, share and to deploy in different ways (on-premise, on your local computer, integrated into
vendor software, or running in the cloud).
 
Another success criterion is beleived to be **collaboration** across the industry, something creating 
this library and sharing hopefully will foster.

