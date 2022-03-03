# List of known limitations

This is a working list of currenlty known limitations of capability. 
The aim is generally to address these in further work, if possible. 

## Plant Simulator

- ``PlantSimulator`` initalizes systems only to steady-state, it is not possible to 
start in a transient state
- The code is not tested for a wide variety of complex configurations, so it is likely
that the simulator may fail to initialize in some complex scenarios, as of yet undiscovered.
- simulations will currently likely fail if more than one select controller is present

### Unit Simulator

- ``UnitSimulator`` does not support advanced control involoving more than 
one PID-controller like cascade, select control/tracking or split range (this can be simulated
using ``PlantSimulator``, though)
 
### Unit Identifier
 
- ``UnitIdentifier`` has no functionality to simultanously discover multiple time-constants
acting through different inputs u on the outputl 
 
### PID-Controller 

- the tracking functionality of the included PID-controller needs work(reformulation?),
 the example of tracking is unfinished as a result 
- split range has not been verified in a test
- the uncertainty of estimated PID-paramters is currently not calculated or stored, but it is 
possible to do so 
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