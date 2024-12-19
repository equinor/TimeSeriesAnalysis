# List of known limitations

This is a working list of currently known limitations of capability. 
The aim is generally to address these in further work, if possible. 

### Plant Simulator

- ``PlantSimulator`` initalizes systems only to steady-state, and all PID-loops 
are currently initalized to the value of the setpoint. This has implication when simulating system that have input
additive disturbances on the output.
- simulations will currently likely fail if more than one select controller is present


### UnitIdentifier
- while a second-order "damping" coefficient has been developed in UnitModel, this parameter is not determined as part of identification.
- some questionmarks over the uncertainty of the estimated time-constant, as the timeconstant is derived using an equation Tc= b\(1-a) and it is actually ``a``
and ``b`` that is determined directly in regression. A theoretical relationship between is used, but is hard to verify. 
- many of the paramters returend in the Fitting object are not in use and are candidates to be removed 

### PidModel 

- the tracking functionality of the included PID-controller needs work(reformulation?),
 the example of tracking is unfinished as a result,
- split range has not been verified in a test,
- the uncertainty of estimated PID-parameters is currently not calculated or stored, but it is 
possible to do so
 
### Disturbance (closed-loop) estimaton 

- the closed-loop estimation of does not have a reasonable estimate of the uncertainty of the process gain or 
time constants estimated, because these are found using a combination of global search and sequential search, 
the uncertainty estimates associated with linear regression will under-estimate the real uncertainty, and the 
real value is often outside this range in unit tests where the true value is known. 
- the unit-tests related to disturbance estimation of process that have multiple inputs need to be revisted after refactoring the PlantSimulator
- there are questions over the time-constant estimate of the process model. Often the objective function used to determine this is quite 
flat, and in some cases it has been observed that the time-constant will for whatever reason resolve at the maximum allowed value on real-world data, 
but this has not been seen in unit tests. 
- currently the Fitscore retuned by ClosedLoopUnitIdentfier will always be zero. 
- it should be possible to extend the "step3" of the ClosedLoopIdentifier to also estimate the time-constant. 
- consider more closely the number of passes that the ClosedLoopIdentifier should do in it sequential global search.

### Gain-scheduled model estimation

- it is currently not possible to identify more than one time-constant unless the tresholds used for time-constant estimation are 
identifical to those used to estimte the gains. 
- if ignoring sections of the dataset, the gain-scheduled estimator does not seem to find the correct time-constants (see unit test IgnoreIndicesInMiddleOfDataset_ResultShouldStillBeGood)