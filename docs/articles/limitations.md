# List of known limitations

This is a working list of currently known limitations of capability. 
The aim is generally to address these in further work, if possible. 

**See also the issue tracker for this repository.**

### Plant Simulator

- ``PlantSimulator`` initializes systems only to steady-state, and all PID-loops 
are currently initialized to the value of the setpoint. This has implication when simulating system that have input
additive disturbances on the output.

### Plant Simulator : only one select controller supported
- simulations will currently likely fail if more than one select controller is present

### UnitIdentifier: Damping not automatically determined
- while a second-order "damping" coefficient has been developed in UnitModel, this parameter is not determined as part of identification.

### UnitIdentifier: Uncertainty estimation
- some questions over the uncertainty of the estimated time-constant, as the time-constant is derived using an equation $Tc= b\(1-a)$ and it is actually $a$
and $b$ that is determined directly in regression. A theoretical relationship between is used, but is hard to verify. 


### PidModel tracking

- the tracking functionality of the included PID-controller needs work(reformulation?),
 the example of tracking is unfinished as a result,

### PidModel parameter uncertainty

- the uncertainty of estimated PID-parameters is currently not calculated or stored, but it is 
possible to do so
 
### ClosedLoopUnitIdenfitifer: hard to gauge uncertainties

- the closed-loop estimation of does not have a reasonable estimate of the uncertainty of the process gain or 
time constants estimated, because these are found using a combination of global search and sequential search, 
the uncertainty estimates associated with linear regression will under-estimate the real uncertainty, and the 
real value is often outside this range in unit tests where the true value is known. 
- in some cases the sequential solver is unable to get past the initial heuristic start-value. This is often an a pragmatic
indication that the dataset may have had little information and that the model is less trustworthy. 

### ClosedLoopUnitIdentifier: no time-delays
- the ``ClosedLoopUnitIdenfitifer`` will always return zero time-delay. There is no fundamental reason why the 
solver could not return these values, but it is not implemented yet.  

### Cascade systems: simulateable but not able to identify multiple disturbance signals 
- in a cascade of two pid-controllers, the ``ClosedLoopUnitIdentifier`` is unable to determine the disturbance signals acting on both pid-loops. The ``PlantSimulator`` is able to simulate cascade systems if two external disturbance signals are specified, but they system is unable to determine both disturbances simultaneously at the moment. 

### A unified "FitScore" is needed that considers sample-over-sample
-  the current implementation of ``FitScore`` looks at absolute difference between two signals. ``PidIdentifier`` requires
the use of additional objectives that compare the sample-over-sample difference of two signals in order to pass many of its acceptance tests. Likely the definition of ``FitScore`` should be revised. The aim should be to have a single objective across the entire solution for evaluating which out of two models is the best fit. 
- sample-over-sample objective functions are less vulnerable to bias errors. It is sometimes observed that for instance a period of bad data, that measurement and estimate track perfectly but with an offset, and this produces a low Fit-Score. 
So the Fit-Score is over-sensitive to bias errors, whereas for dynamic identification we are more interested in the ability of two signals to track changes.

### Logic to automatically determine indices to ignore side-effects
- the logic introduced to "ignore" periods where all data is flat, has had the unintended side-effect of breaking some 
demo-projects, because these have slightly unrealistic data where the process measurement is noise-free, and thus 
large portions of the data set are ignored in demo. The easiest solution is perhaps to introduce a slight noise in all cases.

### Gain-scheduled model estimation : limits of time-constant estimation.

- it is currently not possible to identify more than one time-constant unless the thresholds used for time-constant estimation are identical to those used to estimate the gains. 
- if ignoring sections of the dataset, the gain-scheduled estimator does not seem to find the correct time-constants (see unit test IgnoreIndicesInMiddleOfDataset_ResultShouldStillBeGood)