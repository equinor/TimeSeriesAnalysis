
# TimeSeriesAnalysis 


------------------------------------------------------------------
 Source code:   https://github.com/equinor/timeseriesanalysis     

 Releases      https://github.com/equinor/TimeSeriesAnalysis/releases 

 Documentation  https://equinor.github.io/TimeSeriesAnalysis  

------------------------------------------------------------------

## At a glance

**An open-source library of methods to identify,simulate and control industrial process plants**

The library has four main parts:
1. a *custom dynamic identification algorithm* (built on top of linear regression) that implements many tricks-of-the-trade for finding time-series models 
that may include any combination of time-delay, time-constants, linear- and nonlinear gains to create *grey-box unit models*,
2. a *state-of-the-art advanced industrial PID-controller* implementation,
3. a *dynamic plant simulator* that is able to simulate joined together *combinations of unit models*, each unit-model may be an identified PID-controller or an identified model, and   
4. a library of useful utility methods for dealing with matrices and vectors, loading data from CSV-files and quickly plotting time-series from C#/.NET.

The main value proposition of the library is that
- it collects many years combined industrial and academic experience into one a package of tools that are all integrated (compatible with one another), and 
- because the library can be used to quickly spin-up dynamic simulations with little human interaction, it can serve as a foundation for *advanced analytics* or *data-mining*
efforts within the field of process control or plant supervision.  

## Motivation - "no black boxes" 

To support 
a. development of data-driven dynamic *digital twins* and 
b. "mining" of industrial time-series data for *advanced analytics*.

Specifically, the intention is to be able to easily code **"plant simulators"** by connecting  
**"grey box"** unit models. The intention is to make the process
of identifying unit models, connecting models and simulating as easy as possible, and to automate
wherever possible. 

The aim is to make models that can represent large and complex plants, yet where the meaning of 
each parameter in each unit models is still intuitive, 
representing some intuitive physical property like for instance *"gain"*,*"time delay"* or *"time constant"*.
Grey-box models have two very interesting properties:
- parameters of automatically identified models can be *inspected* by users to *gain insight*, and also
- users can *add insight* by *changing* parameters where needed. 

Unit models are in this library *should not* be designed as "black-boxes", in the sense of being too complex
to interpret, and thus the term "grey-box" is used to differentiate the approach taken.

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

