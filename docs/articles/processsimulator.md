
# PlantSimulator

## Overview

Simulating graphs or single or multiple connected ``ISimulateable`` models (so called "plants") over a given dataset of boundary conditions 
is done by calling the ``PlantSimulator``.

The ``PlantSimulator`` works as an *explicit* solver, solving the models one-by-one in a feasible order. 
The class will itself determine an order to solve the models in given their connections and the given boundary conditions, by parsing the graph of the plant
as well as the given signals in the boundary condition dataset. 

This parsing to determine the order is done by the class ``ConnectionParser`` automatically. 

Both the ``PlantSimulator`` and the ``ConnectionParser`` are designed to support 
- PID-feedback loops
- non-PID computational loops, and 
- disturbances signals, both estimated and modeled. 

The advantage of this approach is that the resulting solver is extremely computationally efficient. It avoids some of the potential complexity of 
introducing an implicit solver to solve all model equations simultaneously, but trades some of this complexity for more logic required to parse 
and order the graph of the model before solving. 

The ``PlantSimulator`` is able to determine the steady-state solution in the first time step, and will initialize the model to this steady-state condition. 
The advantage of this is that no initial condition needs to be provided to the simulator. (This feature is the motivation behind requiring the ``ISimulateableModel`` 
interface to include methods for a model to return its steady-state solution for a given input.) 

Great care has been done to test certain typical graph configurations in unit tests. In general it cannot be guaranteed that the ``PlantSimulator`` will 
always be able solve any given combination of models, connections and input data. 


## Details of the simulator 

The ``PlantSimulator`` is used extensively to create generic time-series data for unit tests. 

It is also used internally in some of the classes that do system identification, such as ``ClosedLoopUnitIdentifier``, ``DisturbanceCalculator``
and the ``GainSchedIdentifier``. 

**The intention is that the ``PlantSimulator`` class should be able to simulate any well-formulated
combination of models that implement the ``ISimulateableModel`` (such as ``PIDModel``,``Select`` and ``UnitModel``.)**

Furthermore, it is intended that all models in the library should implement the ``ISimulateableModel`` model and that all 
simulations should be done using ``PlantSimulator`` and only using this class. 


### External interface

Connections can be done using
- ``ConnectSignal`` connects a signal to a model
- ``ConnectModels``: connects an output of one model to an input of another model
- ``ConnectModelToOutput``: connects an output of model model to the *output*(additive) of another model

External signals are defined using 
 - ``AddSignal``: defines a new external signal that is to be included in the simulation

Once all connections and signals are defined, the model combination 
is then simulated by ``PlantSimulator.Simulate()``.

The entire simulated dataset is presented after simulation as an object of the ``TimeSeriesDataSet`` class. 


### ModelBaseClass: Two types of inputs 

All models should inherit from ``ModelBaseClass``. 

Each model has a number of inputs that travel through the model, each with an id these are referred to as *model input IDs*.
In addition, models support adding signals directly to the output. This is a feature intended for modeling disturbances. 
The IDs of such signals are referred to as *additive input IDs*.

### PlantSimulatorHelper 

The class ``PlantSimulatorHelper`` gives some convenience methods that make it easier to do common types of simulations:
- ``SimulateSingle()`` is useful for quickly simulating any ``ISimulateableModel``. 
- methods that allow calling the PlantSimulator with data in ``UnitData`` datasets rather than the more general ``TimeSeriesDataSet``.
- methods that return a PlantSimulator object with a standard feedback loop. 

### Internal workings

Internally each signal in the returned ``TimeSeriesDataSet``
 is named by a naming convention that is handled by ``SignalNamer``, which combines
information about modelID and signal type to create a unique ID for each signal in the simulation. 


### Initialization of the dynamic model

*The model can be started without supplying a initial state.* In that case the model attempts to start in the steady
state that results from the first value (at ``t=t0``) in each supplied ``PlantSimulator.AddSignal()``.

Determining the initial state is handled by the private method ``PlantSimulator.InitToSteadyState()``, which is 
handled by parsing the model set and connections by logic.

### Estimating disturbances and simulating their effects 

If the ``inputData`` given to the PlantSimulator includes measured pid-outputs ``u`` and process outputs ``y`` of feedback loops,
then PlantSimulator uses that 
``y_meas[k] = y_proc[k-1]+d[k]`` 
calculate the disturbance vector ``d`` and this disturbance is then simulated as the ``driving force`` of closed loop dynamics. 

Thus when the process model is known and inputs to the model are given, the methods  in ``DisturbanceCalculator`` are used to 
simulate the disturbance vector as part of the initialization of ``PlantSimulator.Simulate()``. 

PID-loops are a special case of a computational loop.

In a closed loop the simulation order will be

- *PidModel* reads $y_{meas}[k]$ and $y_{setpoint}[k]$ and calculates $u_{pid}[k]$
- the process model (usually a *UnitModel*) gives $u_{pid}[k]$ and any other inputs $u[k]$ and outputs an internal state $y_{proc}[k]$
- In the next iteration 
$$y_{meas}[k+1] = y_{proc}[k] + d[k+1]$$

This is *by convention*, and is how the simulator avoids both reading to $y_{meas}[k]$ and writing to $y_{meas}[k]$ in the same iteration.

This above means that implicitly all closed-loop processes in a manner of speaking have a time delay of 1. 

It is somewhat challenging to define this behavior so that it is consistent when a model is for instance open-loop tested, then a pid-control is applied. 
A static process by definition has $y[k] = f(u[k])$ but in a closed loop that could mean that the measurement $y[k]$ that is used to determine $u_{pid}[k]$
is again overwritten by the output of astatic process. 

It could also be possible to deal with this by stating that PID-controllers by convention deal with the signals of the previous step, but so that
$u_{pid}[k] = f(y_{set}[k], y_{meas}[k-1])$, but this does not match real-world industrial data, it introduces a lag. It may be that in some cases pid-controllers
are implemented like this, but often the analysis is done on down-sampled data, and in that case $u_{pid}[k]$ appears simultaneous to changes in $y_{meas}[k]$
 
**Implicitly the above also defines how to interpret the disturbance $d[k]$.** To be extremely precise with how this is defined is important, as the PlantSimulator is
used internally to back-calculate disturbances as is described in the above section, and how the disturbance is calculated will again be important as both single simulations and co-simulations 
are used by ClosedLoopUnitIdentifier to identify the process model including possibly time constants and time-delays. 

### Computational loops other than PID-feedback loops

The PlantSimulator can deal with computational loops other than PID-feedback loops. 
These are initialized to steady-state by co-simulating the loop for a number of iterations until the outputs hopefully settle on a steady value. 





