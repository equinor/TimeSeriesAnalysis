# Glossary

### *Time base* 

``Time-base`` in this context is another expression for ``sampling time`` which is the inverse of the ``sampling rate``. 
So a sampling frequency of ``10 Hz``, would mean a sampling time/time base of ``0.1s``.

### *Linear* versus *non-linear* systems

A ``linear`` system refers to a system that can be described by a linear differential equation, i.e. its states are
linear in the dependent variables ``u``. 

A nonlinear is system is any system that is not linear.

Example: If ``x`` is the system state, then 
``dx/dt = b * u`` is *linear* while ``x = b * u + c * u^2`` is *non-linear* 

### *State* of a system

So the state of a system is the values of a set of variables that together define the current condition of the system 
uniequly. Often in system identfication the system state is referred to as ``x``. Note that the output ``y`` is not neccesarily  the same as the state. If the system is described by the differential equation, then the state ``x`` 
is the solution of this equation set.

### *Steady-state* versus *transient*

``Steady-state`` in a differential equation is the condition that the differential terms are zero, so that the system remains at rest. 

### *Static* versus *dynamic*

A model is termed ``static`` if any changes in inputs fully propagete immediately the outputs. 
So a static model is a model with no ``transients``, and thus has no time-constant or time-delay terms. 

### *Working point* and *local* models

Any nonlinear system can be approximated as linear around a *working point*. So a *local* model in this context
is a model that is intended to mainly approximate the actual system close to a given value ``u0`` of the manipulated variables. Depending on the degree of non-linearity, the working range aroundt the working point that the model is useful for will vary. 


### *Tuning dataset* versus *validation dataset*

The ``tuning dataset``, also referred to as the ``fitted dataset`` is the actual set of data that the model was fitted against. It is common practice in system identifiation to evaluate models *not on the fit to the tuning set but on a fresh set of data*, and this fresh data set used for evaluation is referred to as the *validation dataset*. 
