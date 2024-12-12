# Closed-loop disturbance signal estimation

## What is it?

The *disturbance* is an additive signal that moves the output of the given unit process.
Counter-acting disturbances are the very reason that feedback controllers are used, they 
observe the deviation between setpoint and measurement of the plant output, and change 
one-or more inputs to counter-act the disturbance. 

 ![System definitions](./images/sysid_disturbance_system.png)

### Example: step disturbance

Consider a step disturbance acting on a system without feedback

 ![ex1](./images/sysid_disturbance_ex1.png)

The feedback is directly fed through to the output, while the input is constant. 

Now consider and compare the same step disturbance, but this time a PID-controller counter-acts the disturbance

 ![ex1](./images/sysid_disturbance_ex2.png)

The disturbance initally appears on the system output, then is slowly counter-acted by change of the manipulated 
variable ``u`` by feedback control, thus moving the effect of the disturbance from the output ``y`` to the 
manipulated ``u``.

Observing the offset between setpoint and measurement gives a *"high-frequency"* ``d_HF`` response and is seen 
first, while the change in ``u`` is gradual and *"low-frequency"* ``d_LF`` and the approach
will attempt to combine the two as shown below

![ex3](./images/sysid_disturbance_ex3.png)

*The aim of this section is to develop an algorithm to estimate the the un-measured disturbance ``d``
indirectly based on the measured ``u`` and ``e``*

## Why is distrubance signal estimation important?

Disturbances are the "action" or "excitation" that causes feedback-controlled systems to 
move, if these signals could be estimated, then a disturbance could be "played back" in 
a simulation and different changes to the control system could be assessed and compared.

Describing the disturbance signal is also important for identifying the other components 
of a feedback-controlled system correctly, as disturbances are "non-white" noise that tends
to skew estimates (destroying the regression accuracy) if not accounted for.

## What are the challenges?

The challenge in describing disturbances in feedback-systems is that the feedback aims
 to counter-act the very disturbance which needs to be described by changing the manipulated
 variable.
 
 Thus, the effect of the disturbance is in the short-term seen on the system output ``y``,
 but in the long-term the effects of the disturbance are seen on the feedback-manipulated variable
 ``u``. The PID-controller will act with some time-constant on ``u``,and this change in ``u``
 will again act back on the output ``y`` with a delay or dynamic behavior that is 
 given by the process(described by the process model.) To know what amplitude a disturbance has,
 requires knowledge of how much effect (or "gain") the change in manipulated variable u will 
 have caused on the output ``y``.
 
*Thus the two tasks of estimating the disturbance and estimating the process model are linked*,
and thus they likely need to be solved *jointly*.
 
## Approach 

The chosen approach to solve the linked problem of solving for 
process model and disturbance signal is *sequential*(as opposed to simultaneous),
 meaning that the algorithm first estimates a disturbance signal, then 
what process model best describes the data for the given disturbance signal, then the estimate
of the disturbance is updated using the model, back-and-forth until both estimates hopefully converge.

Let the control deviation ``e`` be defined as
```
e = (y_meas-y_set)
```

Further, the disturbance is divided into a high-frequency part ``d_HF``
and a low-frequency part ``d_LF``,
and it is assumed that 
```
d = d_HF+d_LF = d_HF(e)+ d_LF(u)
```
``d_LF`` will in general also be a function of the process model, especially the process gain.


### Estimating the disturbance vector when a model is assumed

By ``y_meas = y_mod(u) + d``, where ``y_internal(u)`` is the response of the process

it stands to reason that once the a model ``y_mod(u)`` is assumed, the disturbance vector can be calculated
by subtracting the effect of the model from the measured ``y_meas``:

``d_est(t) = y_meas(t) - y_mod(u(t)) ``


### Step 1 : First, model-free estimate of the process gain

The idea of the inital estimate gain is to get an idea of the approximate value of the process gain, which will 
determine the bounds for global search in subsequent steps. 

A model-free estimate of the disturbance is required to initialize
subsequent sequential estimation. 

For the first iteration, all process dynamics and nonlinearities are neglected, 
a linear static model essentially boils down to estimating the process gain. 

 ![init](./images/sysid_disturbance_init.png)

This first estimate of the process gain ``G_0`` in a linear model ``y = G_0 x u``
is found by the approximation 
```
G_0 = max(e)/(max(u)-min(u)) 
```

The PID-controller integral effect time constant meant that a peak in the deviation ``e`` will not coincide with the peak
in ``u``.
The idea of creating an inital estimate withh ``min`` and ``max`` values is that it circumvents the lack 
of knowledge of the dynamics at this early stage of estimaton. 

It has been observed in unit tests that this estimate in some cases is spot on the actual gain, such as when 
the disturbance is a perfect step.

It seems that the accuracy of this initial estimate may depend on how much the process is close to steady-state for different disturbance values, 
as disturbance step produces far better gain estiamtes than if the disturbance is a steady sinus(so that the system never reaches steady-state.)

Given the gain an inital UnitModel is created with a rudimentary bias and operating point ``u0``, so that the model can 
be simulated to give an inital ``y_mod(u)``, so that an estimate ``d_est(t)`` can be found.

Because no process dynamics are assumed yet, ``d_est(t)`` at this stage will include some transient artifacts if the process
has dynamics. ``d_est(t)`` will be attempted improved in subsquent steps.


### Guessing the sign of the process gain

Methods for open-loop estimation when applied naively to closed loop
time-series often estimate process gain with incorrect sign. 

The reason for this is that cause-and-effect relationships are different 
in closed- and open loop. 

It is thus important to use an identification algorithm that is intended
for closed loop signals, and to also include information about the setpoint ``yset``
to the algorithm, so that the algorith can infer about the control error ``e``.

> [!Note]
> As an example, if inputs ``u`` and output ``y`` *increase* in unison,
> you would in the open-loop case assume that the process gain is *positive*.
> In the closed-loop case, the same relation between input and output change
> is often indicative of a *negative process gain*. 
> In the closed loop, a disturbance enteres the output ``y``, is counter-acted
> by the controller output ``u``, so an increasing ``u`` in response to 
> an increasing ``y`` would be because the sign is negative. The inverse
> is also true, what appear to be negative process gains at first sight may in
> closed-loop be positive process gains.

The strategy that is employed by the algorithm is to required the PID-parameters to be identfied
prior to running the closed-loop identifier, and to use the sign of the Kp in these paramters to set the 
sign of the process gain. 

### Step2: Process gain global search (setpoint changes or u_ext changes)

The inital guesss for the process gain and process gain sign above have not considered
 the pid-controller and its dynamcis, including any setpoint changes that the dataset may contain.

#### in the case of setpoint changes 

If there are setpoint changes, it would be wise to try to use these to improve the model, but broadly speaking there
are two kinds of changes that we could envisage:

- setpoint "step" changes due to an operator manually changing a setpoint 
- continous smaller setpoint changes due to the controller being the innner loop in a "cascade" where another PID-controller
or an MPC acts as the outer loop. 

In the second case, the the setpoint changes will be frequent and small, and will probably be correlated with the disturbance, while in the 
first case, the setpoint change will be independent of the disturbance. 

To determine the disturbance in the case of setpoint changes, 

The PID-model and process model is simulated in a closed loop with no disturbance acting on it, but where the actual setpoint 
signals and any external model inputs u are applied. This results in a simulation of "what the output ``y_meas`` would have been
if the disturbance was zero" that can be repeated for different gains of the PID-input u_pid in the process model. 

It has been observed that the process gain that results int the smallest
"mean-absolute-diff" of ``u_pid`` (referred to as ``u_pid_adjusted`` in code) 
in this "disturbance free" simulation is a good esimate of the true process gain for datasets
where there are changes in the setpoint, found as_

``var uPidVariance = vec.Mean(vec.Abs(vec.Diff(u_pid_adjusted))).Value``

The algorithm seems to in general give better estiamtes of the process if there are step changes in the external inputs 
or in the pid-setpoint, and the algorithm appears to be able to handle both cases. 

The above works equally well in the case that process to be simualted is a multiple-input system and the other non-pid inputs are changed during 
the tuning set. 

#### Finding the gain that gives the disturbance with the ``shortest travelled distance``

If there are no setpoint changes, the above method of determining the process gain will not succede, as the objective function will be flat. 

In this case, the global search instead attempts to find the process gain that results in the disturbance that "travels the shortest distance", 
expressed as: 

`` var dEstVariance = vec.Mean(vec.Abs(vec.Diff(dEst))).Value''

Some obervations:

- this method is heurisitic
- the objective has a minumum
- the objective space is fairly flat, the minum has a fairly low ``strength'', i.e neighborhing process gains have almost equally low objectives
- the objective space seems to be more concave ("stronger" i.e. more significant minimums) when the process gain is higher.

It could be an idea to count the number of zero crossing of e in the dataset, if it is very low, that may be an indication that the process gain is low.
 




### Estimating upper-and lower bounds on the process gain 

It may be that the algorithm has a better accuracy when there are setpoint changes in the dataset.
It may also be that there the model will do better when there are large "wavy" disturbances than when the
disturbances are small and noisy. 

An online identification can choose among different datasets to do identification from, and if estimates of the upper- and lower bounds
on the gain were available, this knowledge could guide the choice of tuning data set. 

Knowing upper- and lower bounds could also be a useful guide for subsequent manual fine-tuning of the process model. 



### Determing the dynamic parameters of the process 

If the process is actually dynamic yet is modeled as static, then the above methodology will 
result into un-modeled transients bleeding into the estimated disturbance, where they will appear as 
"overshoots" 2.order dynamics in the estimated disturbance.

If every change in ``e`` is followed by similar transient in ``d`` then this is a sure sign that there is un-modeled dynamics,
if these "transients" can be described by adding dynamic terms to models and this causes a "flatter" estimated disturbance,
then this is usually preferable. 

These un-modeled transients may also cause process gains and disturbance estimates to be skewed slightly too large.

In the final step, the *ClosedLoopUnitIdentifier* tries to modify the static models identifier previously by adding larger and larger
time constants to the identified model, and observing if this reduces the *absolute, sample-over-sample variance in the disturbance".


### Algorithm

(In the case of no setpoint changes in the data set)
- parse data and remove bad data points 
- "step 1":
	- estimate the disturbance (preferably with a pre-specified *PidModel*)
	- estimate the model for the given disturbance (*UnitIdentifier.IdentifyLinearAndStatic*)
- "step 2": 
	- global search for linear gain, first pass (using gain from step 1 as inital guess)
	- global search for linear gain, second pass (finer grid size around result from above)
- "step 3":
	- estimate the disturbance using the model from step 2
	- while loop: increase time constant ``T_c'' as long as the deviation of the disturbance *decreases*.






