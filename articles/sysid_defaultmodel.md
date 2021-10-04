# The default model

The "default" model in this library is inteded to be a model that can describe most average process systems. 
It aims to *avoid over-paramterization*, as this is known to cause issues on real-world datasets with limited excitation.

As a design chokce, the model has built in support for *explicitly treating* **measured/modelled external disturbances**, for use in analysis of closed-loop systems.

The model is on the form
```
y[k] = x[k] + d[k]
```
where ``x[k]`` is the *state* at time ``k``, ``d[k]`` is an *external disturbance* at time ``k``.

furthermore the *state* ``x[k]`` is modelled as
```
x[k] = a•x[k-1] + b•(u[k-td]-u0) + q		(linear in u)
```
or optionally as
``` 
x[k] = a•x[k-1] + b•(u[k-t_d]-u_0) + c•(u[k-t_d]-u_0)^2 + q	(non-linear in u)
```
where ``t_d`` here denotes the time-delay in samples.


The model 
- is a ``difference model`` as ``x[k]`` depends on the previous value ``x[k-1]``.
- is ``local`` as the terms ``b`` and optionally ``c`` apply locally around the operating point ``u0``. 

The above shows the model for a single input ``u``, but the model excepts any number of inputs, so in the case of two inputs ``u1`` and ``u2`` for instance
```
x[k] = a•x[k-1] + b1•(u1[k-t_d]-u_10)+ b2•(u2[k-t_d]-u_20) + q		(linear in u)
```

> [!Note]
> All inputs in the same model by design share the same dynamic parameters ``a`` and ``t_d``. If multiple inputs act on a single output ``y`` with different
> dyanmics, then this should be modlelled by two or more separate "default models" that are then added together - in identification the input of other 
> sub-models can be accounted for by the disturbance-term ``d``
  
The paramter ``a`` in the above equation shoul always be between ``[0;0.9999]`` . 
It is related to the *time-constant* ``Tc`` by the equation
```
a = 1/(1+Ts/Tc)
```  
where ``Ts`` is the *sampling time*, so that
```
Tc = Ts/(1/a-1)
```  
> [!Note]
>The time-constant is far easier to interpret intuitivly, and is much easier to relate to PID-controller terms or filters than ``a`` directly.

##  Solver

Determining ``[a,b,c,q,t_d]`` can be expressed as a **linear mixed-integer** problem.
If the intenger term ``t_d`` is given, then determing the *continous paramters* ``[a,b,c,q]`` is a *linear* optimizaion problem.

The solver ``DefaultProcessModelIdentifier`` takes a *sequential* approach to solving the parameter estimation problem.
It solves for the continous paramters starting at zero time delay and then for increasing time-delays, until the logic determines that 
attempting to solve for larger time delays is not neccessary. The solver then selects the ``best`` time delay and the associated continous paramters.
This logic is implemented in the class ``ProcessTimeDelayIdentifier``.

Because the optimization problem of finding ``[a,b,c,q,t]`` requires solving a difference equation and the solver looks at difference between subsequent datapoints, the formulation favors estimating ``[a,b,c,]``, but at the expense of the value of the bias ``q``. Thus once the parameters ``[a,b,c,]`` which express the dynamics are found, the bias ``q`` is found in a subsequent calculation to minimize the overall diference between measured and modelled outputs ``y``.

The flow of the solver is as below:

- start at zero time delay ``t_d``,
- while( ``TimeDelay`` object says to continue ),
	- Solve for ``[a,b,c,]`` for a given ``t_d``,
	- for a given ``[a,c,d,t_q]`` find ``q``,
	- save the model run in ``TimeDelay`` class object, and
	- increase time delay.
- ``TimeDelay`` object chooses the best model run out of all the saved runs.








