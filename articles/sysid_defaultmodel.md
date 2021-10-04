# The default model

The "default" model in this library is intended to be a model that can describe most average process systems. 
It aims to *avoid over-parametrization*, as this is known to cause issues on real-world datasets with limited excitation.

As a design choice, the model has built in support for *explicitly treating* **measured/modeled external disturbances**, for use in analysis of closed-loop systems.

The model is on the form
```
y[k] = x[k] + d[k]
```
where ``x[k]`` is the *state* at time ``k``, ``d[k]`` is an *external disturbance* at time ``k``,

furthermore the *state* ``x[k]`` is modeled as
```
x[k] = a•x[k-1] + b•(u[k-td]-u0) + q					(linear in u)
```
The parameter ``a`` in the above equation should always be between ``[0;0.9999]`` . 
> [!Note]
> Notice that if ``a=0`` the the recursive term is stricken, the disturbance is neglected and time delay is zero, then the model reverts to standard linear static model``y[k] = b•u[k]``.

Optionally the default model can be extended with a square term:
``` 
x[k] = a•x[k-1] + b•(u[k-t_d]-u_0) + c•(u[k-t_d]-u_0)^2 + q		(non-linear in u)
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
> dynamics, then this should be modeled by two or more separate "default models" that are then added together - in identification the input of other 
> sub-models can be accounted for by the disturbance-term ``d``
  
  
##  Process gain and time constant
  

It is related to the *time-constant* ``Tc`` by the equation
```
a = 1/(1+Ts/Tc)
```  
where ``Ts`` is the *sampling time*, so that:
```
Tc = Ts/(1/a-1).
```  

> [!Note]
>The time-constant is far easier to interpret intuitively, and is much easier to relate to PID-controller terms or filters than ``a`` directly.

The *process gain* ``G1`` between an input ``u1`` and the output ``y`` is: 
```
G1 = b1/(1-a).
```
If the model has two inputs, the second input ``u2`` will have a process gain that depends on ``b2``
```
G2 = b2/(1-a).
```

If the default model has a *second-order* term with parameters ``c1,c2,...``,the the process gains are no longer constant, but will vary dependent on the value of the inputs ``u``.
Thus, these models are *"nonlinear in u"*, but the beauty of this model is that these model are still *"linear in parameters ``[a, b1,..bN, c1,...CN]``"* so that the identifier can be 
based on linear regression.
The process gains become functions of ``u``, so for instance for ``u1``, the process gain will be ``G1(u1)`` becomes:
```
G1(u1) = b1/(1-a) + c1/(1-a)•2•u1.
```



##  Solver

Determining ``[a,b,c,q,t_d]`` can be expressed as a **linear mixed-integer** problem.
If the intenger term ``t_d`` is given, then determing the *continous paramters* ``[a,b,c,q]`` is a *linear* optimizaion problem.

The solver ``DefaultProcessModelIdentifier`` takes a *sequential* approach to solving the parameter estimation problem.
It solves for the continous paramters starting at zero time delay and then for increasing time-delays, until the logic determines that 
attempting to solve for larger time delays is not neccessary. The solver then selects the ``best`` time delay and the associated continous paramters.
This logic is implemented in the class ``ProcessTimeDelayIdentifier``.

Because the optimization problem of finding ``[a,b,c,q,t_d]`` requires solving a difference equation and the solver looks at difference between subsequent datapoints, the formulation favors estimating ``[a,b,c,]``, but at the expense of the value of the bias ``q``. Thus once the parameters ``[a,b,c,]`` which express the dynamics are found, the bias ``q`` is found in a subsequent calculation 
``DefaultProcessModelIdentifier.ReEstimateBias()``to minimize the overall diference between measured and modelled outputs ``y``.

The flow of the solver is as below:

- start at zero time delay ``t_d``,
- while( ``TimeDelay`` object says to continue ),
	- Solve for ``[a,b,c,]`` for a given ``t_d``,
	- for a given ``[a,c,d,t_q]`` find ``q``,
	- save the model run in ``TimeDelay`` class object, and
	- increase time delay.
- ``TimeDelay`` object chooses the best model run out of all the saved runs.








