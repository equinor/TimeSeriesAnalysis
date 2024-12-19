
# PlantSimulator

Simulating multiple processes together is orchestrated by the class ``PlantSimulator``.

## External interface

The intention is that the ``PlantSimulator`` class should be able to simulate any well-formulated
combination of models that implement the ``ISimulateableModel`` (such as ``PIDModel``,``Select`` and ``UnitModel``.)

Connections can be done using
- ``ConnectSignal`` connects a signal to a model
- ``ConnectModels``: connects an output of one model to an input of another model
- ``ConnectModelToOutput``: connects an output of model model to the *output*(additive) of another model

External signals are defined using 
 - ``AddSignal``: defines a new external signal that is to be included in the simulation

Once all connections and signals are defined, the model combination 
is then simulated by ``PlantSimulator.Simulate()``.

The entire simulated dataset is presented after simulation as an object of the ``TimeSeriesDataSet`` class. 

## Internal workings

Internally each signal in the returned ``TimeSeriesDataSet``
 is named by a naming convention that is handled by ``SignalNamer``, which combines
information about modelID and signal type to create a unique ID for each signal in the simulation. 


### Determining calculation order, parsing connections

By default, ``PlantSimulator`` traverses the combination of models and signals to create a feasible run-order in 
which to run the model, orchestrated by ``ConnectionParser.DetermineCalculationOrderOfModels``. 
Models are then 
run in that given order for each iteration in ``PlantSimulator.Simulate()``. 
This means that process simulation does not rely on simultaneously solving large sets of dynamic equations, 
which an approach that may be easier to comprehend and debug, but it is reliant on the quite complex logic required
to determine the calculation order.

### Initialization of the dynamic model

*The model can be started without supplying a initial state.* In that case the model attempts to start in the steady
state that results from the first value (at ``t=t0``) in each supplied ``PlantSimulator.AddSignal()``.

Determining the initial state is handled by the private method ``PlantSimulator.InitToSteadyState()``, which is 
handled by parsing the model set and connections by logic.


### ModelBaseClass: Two types of inputs 

All models should inherit from ``ModelBaseClass``. 

Each model has a number of inputs that travel through the model, each with an id these are referred to as *model input IDs*.
In addition, models support adding signals directly to the output. This is a feature intended for modeling disturbances. 
The IDs of such signals are referred to as *additive input IDs*.

### Estimating disturbances and simulating their effects 

If the ``inputData`` given to the PlantSimulator includes measured pid-outputs ``u`` and process outputs ``y`` of feedback loops,
then PlantSimulator uses that 
``y_meas[k] = y_proc[k-1]+d[k]`` 
calculate the disturbance vector ``d`` and this disturbance is then simulated as the ``driving force`` of closed loop dynamics. 

Thus when the process model is known and inputs to the model are given, the method ``PlantSimulator.SimulateSingle()`` is used to 
simulate the disturbance vector as part of the initialization of ``PlantSimulator.Simulate()``

### Closed loops : simulation order and disturbance

PID-loops are a special case of a computational loop.

In a closed loop the simulation order will be

- *PidModel* reads ``y_meas[k]`` and ``y_setpoint[k]`` and calculates ``u_pid[k]``
- the process model (usally a *UnitModel*) gives ``u_pid[k]`` and any other inputs ``u[k]`` and outputs an internal state ``y_proc[k]``
- In the next iteration y_meas[k+1] = y_proc[k] + d[k+1]

This is by convention, and is how the simulator avoids both reading to ymeas[k] and writing to ymeas[k] in the same iteration.

This above means that implicitly all closed-loop processes in a manner of speaking have a time delay of 1. 

It is somewhat challenging to define this behavior so that it is consistent when a model is for instance open-loop tested, then a pid-control is applied. 
A static process by definition has ``y[k] = f(u[k])`` but in a closed loop that could mean that the measurement y[k] that is used to determine u_pid[k]
is again overwritten by the output of astatic process. 

It coudl also be possible to deal with this by stating that PID-controllers by convention deal with the signals of the previous step, but so that
u_pid[k] = f(y_set[k], y_meas[k-1]), but this does not match real-world industrial data, it introduces a lag. It may be that in some cases pid-controllers
are implemented like this, but often the analysis is done on down-sampled data, and in that case ``u_pid[k]`` appears simultanous to changes in ``y_meas[k]``
 
**Implicitly the above also defines how to interpret the disturbance d[k].** To be extremely precise with how this is defined is important, as the PlantSimulator is
used internally to back-calculte disturbances as is described in the above section, and how the distrubance is calcualted will again be important as both single simulations and co-simulations 
are used by ClosedLoopUnitIdentifier to identify the process model including possibly time constants and time-delays. 





### Computational loops other than PID-feedback loops

The PlantSimulator can deal with computational loops other than PID-feedback loops. These are initalized to steady-state by co-simulating the loop for a number of iterations until the outputs hopefully settle on a steady value. 