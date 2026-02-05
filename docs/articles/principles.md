# Design choices 


## Motivation

- **human-configurable** and **human-readable** dynamic process models (all parameters have an **intuitive interpretation**) motivated by system identification methodology
	- with intuitive parameters, humans can better understand automatically generated models and it becomes 
	possible for humans to alter such models (semi-autonomous model generation).
	- this excludes neural networks
- create large-scale dynamic simulator by connecting sub-systems, creating "digital twin" models
	- this kind of sub-model is also **intuitive** to humans and **visual**
- **start by modeling the process control system** (PID-control loops), as this is the handle through which process changes are made.
	- to model the process control system, the library must be able to describe **dynamics**, **feedbacks** and different kinds of 
	**PID-controllers**.
- try to **recreate disturbance signals** $D$ of each PID-control loop **explicitly**, as this is the *"excitation"* that causes 
variation in an industrial process, yet these signals are not measured or observed directly. How do disturbances move through a process with multiple PID-control stages? Can this be simulated? An explicit estimate of disturbances allows "playback" in simulations, which means that the response of the control system with other tunings or configurations can potentially be assessed in simulation.
	- such a representation would negate the need for "tuning rules" for PID-controllers, they could instead be tuned **by simulation**
	- it would become possible to consider the **joint effect** on re-tuning **multiple** PID-controllers 
	- it would become possible to consider **changes in control structure**, not just changes in **tuning**.
- if it were possible to find models with the above qualities **from data** it would enable:
	- **advanced analytics** : autonomous or semi-autonomous analysis of process control which generates recommended actions
	- **data-driven digital twins** : rapid development of "fitted" dynamic models for specific cases-studies or advanced control 


## System identification

*"The goal of the identification procedure is, in loose terms, to obtain a good and reliable model with a reasonable amount of work."*(Lennart Ljung, Theory for the user 2ed, p.399)
This quote displays the trade-off of the three different and sometimes opposing goals of identification:
1. a **good**(accurate, descriptive) model,
2. a **reliable** model, and 
3. a model developed with **a reasonable amount of work**.

This library intends to focus on methods that give deliver a good balance of *all three goals*. 
Conversely this means that models that are either
- not *good enough* 
- not *reliable enough*, or
- *cannot be developed with a reasonable amount of work* 
will not be considered.

Further the choice of the model should ideally be based on 
*"posing a criterion for what is a good model and to list the constraints that are imposed on the design by limited time and cost"* (p.406)

Thus system identification is a practical field that acknowledges that better models take more time and cost more money, and that these factors need to be taken into consideration.

This class library is built on the following principles/assertions:
- most time-series are *not* designed for identification, and may have less than ideal amount of excitation, hence 
**handling parameter uncertainty** and **avoiding over-parametrization** are important,
- almost all systems are actually **nonlinear**, many but not all can be considered **locally linear**,
- real-world data will contain bad data points that need to be **filtered** out , and you may need to manually remove further non-representative data. 
Especially for recursive models, a single spurious value can destroy an entire model run, thus the tooling need to support cleaning data to avoid garbage getting into models.
- expect *parameter uncertainty*, treat it explicitly.


## Numerical solvers

- the models need to simulate *without requiring any human intervention*, and this has impacts on design choices
for how models should be initialized and solved:
	- rather than using numerical solvers on large matrices to find steady-state, a conscious choice is 
made to rather *require* each model to include an *explicit* steady-state calculation. 
	- rather than use numerical solvers on large matrices to simulate the model, a conscious choice is made
to rather use *logic* to *traverse the connected models as graphs* and simulate them one-by-one in an order that
is feasible. That means that this library must provide a solver that includes 
logic to determine a feasible solution order. 

## Code design

- use *dependency injection*, *generics* and *interfaces* to make the process model easily replaceable - if you provide a new process model that implements the correct interfaces, it should immediately be compatible with re-usable functionality such as PID-control or simulation. 
- do not use inheritance - except for abstract base classes - deep inheritance is hard to understand for others.

