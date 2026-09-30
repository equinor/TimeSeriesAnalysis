# Integration with physics-based simulators

Integration with first-order simulators deals with creating hybrids of empirical and physical data or with making fitting or simulation easier or faster. 


#### Generating proxies/surrogates from simulator topology 
There are at least four flavors of proxies:
  1. fitting only to measured data
  2. fitting only to simulated data
  3. hybrid modeling (some sub-models fitted to measured data, others to simulated data)
  4. physics-constrained modeling (physical gains, but estimate dynamic parameters (time-constants and delays) from data)
#### Using proxies (as described above)
The following are use-cases for proxies as described above
   - as stand-in models to accelerate optimization
   - as part of quality control of physical simulators 
   - as a structured way to estimate gains or other parameters that are uncertain in physical simulators
   - as ``hybrid soft-sensors''
   - as a way to add feedback control loops and dynamic simulation capability to to otherwise steady-state simulators
   - as a part of closed-loop model-based optimization and/or control.



## Important considerations

> [!Note]
> **Empirical versus physical modeling tradeoffs:** Before a plant is built, a physical model is the only available tool to aid in the process design and is considered a "ground truth". However, when a process exists and time-series of process measurements exists, it is common for there to sometimes be *deviations* between simulated and measured data, and in such cases it is common to consider the measured plant data as the "ground truth".


> [!Note]
> **Nonlinear gains:** There are at least two supported ways to express nonlinear gains with this library. 
> Either a ``UnitModel`` can be given a local ``.Curvature`` parameter, or a ``GainSchedModel`` can be set up to interpolate   
> gains from a given table. ``UnitIdentfier`` and ``GainSchedIdentifer`` both support estimating these gain parameters 
> from time-series, which could be either measured or simulated time-series. 
>
> The challenge with estimating nonlinear gains from measured data is that it requires significant excitation which is often not seen unless a planned excitation campaign is done, but a simulated dataset can be excited at will.
 

> [!Note]
> **Dynamic models for steady-state analysis:** Even when performing steady-state analysis, there is a huge benefit to fitting dynamic models, namely that there is no need to discard transient data. Instead when fitting a dynamic model all data can be used, and there is no need to detect steady-state periods. After fitting, it is possible to extract the steady-state solution of any model for any input as all models that support ``ISimulateableModel`` implement explicitly returning their steady-state solution for a given input through ``.GetSteadyStateOutput()``
 



### Mass flow 

Especially for oil and gas the feed rate is usually not directly measured, but can only be inferred from downstream measurements after separation. 

This represents a challenge for what-if simulation, as many physical quantities in a process plant will depend on the mass rate:
- the pressure drop over pipes
- the pressure drop over chokes
- the heat transfer in heat exchangers
- the pressure rise over a compressor

Introducing mass rates into a plant model also causes a dilemma for the designer, as mass conservation requires adding algebraic equations to a solver, which rules out explicit solvers and results 
in longer computational times. 
