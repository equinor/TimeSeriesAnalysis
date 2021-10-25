
# ProcessSimulator

Simulating multiple processes together is orchestrated by the class ``ProcessSimulator``.

## External interface

The intention is that the ``ProcessSimualtor`` class should be able to simulate any well-formulated
combination of models that implement the ``ISimulateableModel`` (such as ``PIDModel`` and ``DefaultProcessModel``.)

Before simulation the models must be connected using ``ProcessSimulator.ConnectModels()`` and input signals need to 
be added using ``ProcessSimulator.AddSignal()``. Once all connections and signals are defined, the model combination 
is then simulated by ``ProcessSimulator.Simulate()``.

The entire simulated dataset is presented after simulation as an object of the ``TimeSeriesDataSet`` class. 


## Internal workings

Internally each signal in the returned ``TimeSeriesDataSet``
 is named by a naming convention that is handled by ``SignalNamer``, which combines
information about modelID and signal type to create a unique ID for each signal in the simulation. 


### Determining calculation order, parsing connections

By default, ``ProcessSimulator`` traverses the combination of models and signals to create a feasible run-order in 
which to run the model, orchestrated by ``ConnectionParser.DetermineCalculationOrderOfModels``. 
Models are then 
run in that given order for each iteration in ``ProcessSimulator.Simulate()``. 
This means that process simulation does not rely on simultaneously solving large sets of dynamic equations, 
which an approach that may be easier to comprehend and debug, but it is reliant on the quite complex logic required
to determine the calculation order.

### Initialization of the dynamic model

*The model can be started without supplying a initial state.* In that case the model attempts to start in the steady
state that results from the first value (at ``t=t0``) in each supplied ``ProcessSimulator.AddSignal()``.

Determining the initial state is handled by the private method ``ProcessSimulator.InitToSteadyState()``, which is 
handled by parsing the model set and connections by logic.


### ModelBaseClass: Two types of inputs 

All models should inherit from ``ModelBaseClass``. 

Each model has a number of inputs that travel through the model, each with an id these are referred to as *model input IDs*.
In addition, models support adding signals directly to the output. This is a feature intended for modeling disturbances. 
The IDs of such signals are referred to as *additive input IDs*.
