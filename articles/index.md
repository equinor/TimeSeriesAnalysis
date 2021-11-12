
# TimeSeriesAnalysis 


------------------------------------------------------------------
 Source code:   https://github.com/equinor/timeseriesanalysis     

 Releases      https://github.com/equinor/TimeSeriesAnalysis/releases 

 Documentation  https://equinor.github.io/TimeSeriesAnalysis  

------------------------------------------------------------------

## At a glance

**An open-source C# .NET class library, integrable with Matlab and Python, 
intended to support efficient test-driven development of 
time-series-based algorithms: transients/dynamics, dynamic model identification, 
dynamic simulation, filtering and PID-control - 
in support of mining of time-series data for advanced analytics.**

Handles typical tasks related to time-series analysis such as
- *loading* time-series data from CSV-files,
- *manipulating* time-series data as arrays and vectors,
- *filtering* out values by range, detecting and removing bad values, smoothing
- *fitting static models* to time-series data by linear regression(based on ``Accord.NET``), 
- *fitting dynamic models* to time-series by custom methods that build on linear regression, and
- *plotting* times-series (in a browser window with ``plot.ly``).
- *dynamic simulation* of systems that may include interactions with *PID-controllers* (the library includes a reference PID-controller implementation).




## Motivation - "no black boxes" 

To support 
a. development of data-driven dynamic *digital twins* and 
b. "mining" of industrial time-series data for *advanced analytics*.

Specifically, the intention is to be able to easily code **"plant simulators"** by connecting  
**"grey box"** unit models. The intention is to make the process
of identifiying unit models, connecting models and simulating as easy as possible, and to automate
wherever possible. 

The aim is to make models that can represent large and complex plants, yet where the meaning of 
each parameter in each unit models is still intuitive, 
representing some intuitive phyisical property like for instance *"gain"*,*"time delay"* or *"time constant"*.
Grey-box models have two very interesting properties:
- paramters of automatically identifed models can be *inspected* by users to *gain insight*, and also
- users can *add insight* by *changing* paramters where needed. 

Unit models are in this library *should not* be designed as "black-boxes", in the sense of being too complex
to interpret, and thus the term "grey-box" is used to differentiate the approch taken.

### Advanced analytics

The above concepts are related to the term "advanced analytics".

Advanced analytics is [defined](https://www.gartner.com/en/information-technology/glossary/advanced-analytics) as :

*" the autonomous or semi-autonomous examination
 of data or content using sophisticated techniques and tools, typically beyond those of 
 traditional business intelligence (BI), to discover deeper insights, make predictions, 
 or generate recommendations."*

Note that advanced analytics by definition **requires** **automated** or **semi-automated**
 analysis to generate recommendations, 
thus it needs to be *"data-driven"*.

A success criterion for any *data driven* method will be its **reliability**, so that despite
little manual configuration and assistance, methods should consistently provide **dependable** high-quality
output. To create reliable and dependable software, complete control of the entire software stack is 
advantageous as: 
- solutions are easier to integrate and deploy (be it on-premise, on your local computer, integrated into
vendor software, or running in the cloud), and
- the quality, behavior and performance of each component can be inspected and, if need be, improved,i.e. there
are no code "black boxes".

An advantage of an open software stack is that it can create industrial collaboration, and industrial/academic
collaboration that can hopefully raise the quality of software by pooling resources.

