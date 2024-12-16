
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
``y_meas = y_proc+d`` 
calculate the disturbance vector ``d`` and this disturbance is then simulated as the ``driving force`` of closed loop dynamics. 

Thus when the process model is known and inputs to the model are given, the method ``PlantSimulator.SimulateSingle()`` is used to 
simulate the disturbance vector as part of the initialization of ``PlantSimulator.Simulate()``

