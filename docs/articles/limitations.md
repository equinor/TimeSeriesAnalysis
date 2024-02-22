# List of known limitations

This is a working list of currenlty known limitations of capability. 
The aim is generally to address these in further work, if possible. 

## Plant Simulator

- ``PlantSimulator`` initalizes systems only to steady-state, and all PID-loops 
are currently initalized to the value of the setpoint. This has implication when simulating system that have input
additive disturbances on the output.
- simulations will currently likely fail if more than one select controller is present
- PlantSimulator currently always consider ``e[i-1]`` when simulating PID-controllers
(unlike UnitSimulator), this should be configurable, but requires a change to the 
``DetermineCalculationOrderOfModels()``
 
### PID-Controller 

- the tracking functionality of the included PID-controller needs work(reformulation?),
 the example of tracking is unfinished as a result,
- split range has not been verified in a test,
- the uncertainty of estimated PID-parameters is currently not calculated or stored, but it is 
possible to do so, 
 
### Disturbance (closed-loop) estimaton 

- the disturbance estimation makes assumptions about ``u[0]`` being at steady-state and
formulates model related to this ``u0=u[0]``
- the current closed-loop unit identifier cannot find dynamic models, for dynamic 
models some of the transients thus bleed into the estimated disturbance
- disturbance estimation is only available currently for systems that the ``UnitSimulator`` can 
simulate, so not for advanced control schemes. 