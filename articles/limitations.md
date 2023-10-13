# List of known limitations

This is a working list of currenlty known limitations of capability. 
The aim is generally to address these in further work, if possible. 

## Plant Simulator

- ``PlantSimulator`` initalizes systems only to steady-state, and all PID-loops 
are currently initalized to the value of the setpointg. This has implication when simulating system that have input
additive disturbances on the output, it is generally 
- The code is not tested for a wide variety of complex configurations, so it is likely
that the simulator may fail to initialize in some complex scenarios, as of yet undiscovered.
- simulations will currently likely fail if more than one select controller is present
- PlantSimulator currenlty always consider ``e[i-1]`` when simulating PID-controllers
(unlike UnitSimulator), this should be configurable, but requires a change to the 
``DetermineCalculationOrderOfModels()``
- PlantSimulator cannot handle "computational loops", where two or more systems co-depend on each other. 



### Unit Simulator

- ``UnitSimulator`` does not support advanced control involving more than 
one PID-controller like cascade, select control/tracking or split range (this can be simulated
using ``PlantSimulator``, though.) 
- ``PIDmodel`` always at time ``i`` consider ``e[i]`` in the UnitSimulator only(PlantSimualtor is different)
 
### Unit Identifier
 
- ``UnitIdentifier`` has no functionality to simultanously discover multiple time-constants
acting through different inputs ``u`` on the output. 
 
### PID-Controller 

- the tracking functionality of the included PID-controller needs work(reformulation?),
 the example of tracking is unfinished as a result,
- split range has not been verified in a test,
- the uncertainty of estimated PID-parameters is currently not calculated or stored, but it is 
possible to do so, 
- the "warm-start" capability does not appear to be able to reset a controller object that 
has already been run. 
 
### Disturbance (closed-loop) estimaton 

- a disturbance signal is always assumed to be zero at the start of the dataset, and for 
pracitical reasons the bias thus always comes back as ``y=y[0]`` in closed loopd
- the disturbance estimation makes assumptions about ``u[0]`` being at steady-state and
formulates model related to this ``u0=u[0]``
- the current closed-loop unit identifier cannot find dynamic models, for dynamic 
models some of the transients thus bleed into the estimated disturbance
- disturbance estimation is only availabe currently for systems that the ``UnitSimulator`` can 
simulate, so not for advanced control schemes. 