# Data prepropocessing

Data preprocessing is important for any identification, and even more so for dynamic identification.
For static identification it is important to remove outliers that can skew the steady-state estimates, but for dynamic estimation one must 
also remove any data artifacts that may skew dynamic estimates even if they are not outliers in terms of ther absolut values.

The intention of this library is for it to be used in automatic analysis, and so it is important that the data prepreocssing can run autonomously, and that it can 
recognize all the typical kinds of spurious data that it may receive. 


## CommonDataPreprocessor

Several different of the major components of the library need pre-processing. 
In order for results to be comparable, between different solvers and across time, it is a huge advantage if all code that requires
it uses a common code base for data preprocessing. 

The intention is that all identification and simulation should use a common class for determining which indices of a given dataset to ignore. 
By convention, this is the ``CommonDataPreprocessor`` class.

This library is intended to be connected to systen that feeds it with stream of data, and re-run periodically. 
The data may originate in the Saftey and Automation System(SAS), before being stored in an Information Management System, 
which may again stream or batch upoad its data into a cloud service before being batch downloaded over an API into a system to 
run and update models. 

> [!Note]
> As the dynamic models in this library are recusive, a single bad data point will also often require ignoring the next data point.
> To make it easier to compare static vesus dynamic models, the approach to choosing indices to ignore does not commonly rely on 

### Removing NaN and bad data points
By default, the  ``CommonDataPreprocessor`` will remove any bad data points. Bad data points can be ``NaN`` values.
The libray can also treat a chosen double value as NaN. For example, in the Equinor software Sigma, ``-9999`` is treated 
as not-a-number, while in WITSML, ``-99.125`` is treated as NaN.

### Frozen data points and re-starting the PlantSimulator

Because the data that this models receives in real-time is passed through several systems either as treams or in batches, there is 
always the possiblity of buffer overflow, network outages or a myriad of other reason that can cause the data stream to "freeze"
intermittantly. This kind of bad data typically results in all time-series retaining their last values for a period of time. 

For a dynamic model with PID-feedback loops this is especially bad, as it may casue "wind-up" of the PID-controller, it may keep either increasing
or reducing its manipulated variable over time to try to bring the process value back to setpoint. 

After a prolonged period of frozen data, the internal state of dynamic model may thus have drifted off from the value it had initially, and
depending on the time-constans of the models, it may take a long period for the ``PlantSimulator`` to re-aquire tracking of the true plant.
This kind of offset created by frozen data destroys "fit scores" or other KPIs of plant/model mismatch, and can lead to wrong conclusions unless
this situation is recognized.

The ``PlantSimulator`` by default has a feature that it will reset and restart at a value that matched the measured data after prolonged
period of bad data, be they because of "frozen data" or other reasons, but it requires ``CommonDataPreprocessor`` to have set these indices
to be ignored. 


### Oversampled data: using the  DatasetDownsampler

As described above, industrial data passes through many different systems on its way to the library, and in some cases these system
may down-sample data to save storage space. Or it may simply be that data is requested at a higher time base than the data was saved at.
In either case, what can result is *oversampled data*: data that when plotted appears like a series of "steps", as some of the data processing
systems return data that is padded with the value of the most recent data point for multiple samples.

This kind of oversampling creates a form of artificial dynamics in the system that will influence the dynamic parameters found in 
identification. 

Oversampling presents similartly to frozen data data described above, but the difference is that data freezes at regular periods. 

Rather than each identification method and the simulator all having to implementing a strategy to detect and counter this kind 
of data issue, instead the suggested way of dealing with oversampled data is to use the 
``OversampledDataDetector`` class to create a new non-oversampled version of the dataset, and then do identification and simulation on this
new dataset.

> [!Note]
> ``OversampledDataDetector`` is not called by default by the ``CommonDataPreprocessor``. Implementations that require this form of
> pre-rpocessing will need to manually reference ``OversampledDataDetector``. 


## Special considerations and pitfalls

### Unit tests may wrong identify small datasets as frozen

When simulating some smaller systems the ``PlantSimulator`` may wrongly find that the datset has frozen periods. 
For instance if simulating a process where the manipulated variable moves in steps, and where this is the only input data given. 

To counter this, the built-in detection of indices to ignore can be tured off in a flag when calling ``PlantSimulator.Simulate()``.
Another strategy is to ensure that the ``PlantSimulator`` is given an ``inputData`` object that also includes the measured output,
as this give additional information that makes it easier for the ``CommonDataPreprocessor``\ ``FrozenDataDetector`` to determine which 
if any indices need to be ignored.

### Fitting constraints: FittingSpecs 

Note that some identification methods such as UnitIdenfier allow specifying a ``FittingSpecs`` object which includes and optional 
``Y_min_fit``, ``Y_max_fit`` and `` U_min_fit`` and ``U_max_fit``. This allows determining local models that only attempt to describe
a certain opertaing region of the dataset, and this is an important functionality int the ``GainSchedIdentifier``.

For this reason, there is some custom code in these identifiers for further selection of ``indicesToIgnore`` that goes beyond that 
of the ``CommonDataPreprocessor``.

The chosen ``indicesToIgnore`` is returned by the identification methods, and can be speficied when calling ``PlantSimualtor`` if needed

### Physical constraints and saturation:  Ymin, Ymax, Umin, Umax

A consideration similar to the above, but slightly different are *phyiscal* constraints.

A valve may for instance only be able to move between 0% and 100%, or a the level in a tank can only be between 0% and 100% or between 
for intance ``0 mm`` and ``2000 mm``.

For ``PidIdentifier``, indices where either u or y are at or above/below either max/min respectivly are removed when identifiying.


### Re-starting the simulator

If more than a certain number of samples are bad consequtivly, then the simualtor will do a "warm-start" and force a match to measured 
data on the first good sample. The number of simulation restarts is saved in the returned dataset object of ``PlantSimulator``









### Time delays

In the case of th models with non-zero time delays, a bad input may require a data point to be ignored a certain number of samples
later. This functionality is as of time of writing not fully implemented. 




