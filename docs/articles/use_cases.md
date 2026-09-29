# Use-cases

Actual use can be diverse and it is therefore not possible to give a complete list of use-cases. 

Below is a categorized list of suggested possible use-cases of the library.

*Feedback-loop analysis*:
- disturbance-driven modeling (single or multiple loops) : PID-controller monitoring, what-if and  screening 
- disturbance correlation analysis (multiple loops)
- disturbance root cause analysis 

*Integration with first-order simulators*:
- generating proxies/surrogates from simulator topology (four flavors)
    1. fitting only to measured data
    2. fitting only to simulated data
    3. hybrid modeling (some sub-models fitted to measured data, others to simulated data)
    4. physics-constrained modeling (physical gains, but estimate dynamic parameters (time-constants and delays) from data)
- using proxies (as described above) 
    - as stand-in models to accelerate optimization
    - as part of quality control of physical simulators 
    - as a structured way to estimate gains or other parameters that are uncertain in physical simulators
    - as ``hybrid soft-sensors''
    - as a way to add feedback control loops and dynamic simulation capability to to otherwise steady-state simulators
    - as a part of closed-loop model-based optimization and/or control.


> [!Note]
> **Nonlinear gains** there are at least two supported ways to express nonlinear gains with this library. 
> Either a *UnitModel* can be given a *Curvature* parameter, or a *GainSchedModel* can be set up to interpolate   
> gains from a given table. *UnitIdentfier* and *GainSchedIdentifer* both support estimating these gain parameters 
> from time-series, which could be either measured or simulated time-series. 
>
> The challenge with estimating nonlinear gains from measured data is that it requires significant excitation which is often not seen unless a planned excitation campaign is done, but a simulated dataset can be excited at will.
 



 











