# The UnitIdentifier - identification of UnitModels

The ``UnitIdentifier`` is based on the idea of starting with a static linear regression models and 
to by trial-and-error add in additional terms in the model given that it doing so improves the fit of the model
by given criteria. This approach is based on the recommended work-flow in the field of system identification.

The idea is to automate the work-flow of selecting the best model structure in the set of model structures supported by the ``UnitModel``.

> [!Note]
> There is a chance of over-fitting if a model has too many parameters, thus it is not always given that adding in more
> terms is better, in fact doing so can degrade model performance. 

## First run

Identification starts with finding a linear static model, which acts as a reference against which all subsequent identification runs is to be compared.

> [!Note]
> If the linear,static fit between the output and the given inputs does not at least broadly follow the measured output, then adding nonlinearities 
> time-delays or time-constants rarely if ever is meaningful. 

## Time constant and bias 

The solver solves for terms $[a,b,c]$ jointly. The objective function is formulated in terms of **differences**, finding the parameters which
produce the minimum sum of square differences $\sum_{k=2}^N(y[k]-y[k-1])^2$.

Because the optimization problem of finding $[a,b,c,q,t_d]$ requires solving a **difference** equation and the solver looks at difference between subsequent data points, the formulation favors estimating ``[a,b,c,]``, but at the expense of the value of the bias ``q``. Thus once the parameters ``[a,b,c,]`` which express the dynamics are found, the bias ``q`` is found in a subsequent calculation 
``UnitIdentifier.ReEstimateBias()``to minimize the overall difference between measured and modeled outputs ``y``.

## Nonlinear terms 

``UnitIdentifier`` will *first* try finding a model that is *linear* in inputs u, it will then re-solve the identification for every combination
possible of curvatures for each input turned on/off, and chooses a model with one or more nonlinear terms if it results in a higher R-squared and a 
lower value of the objective function.

To avoid over-fitting, a model with more nonlinear terms is only selected over a less complicated model if the improvement in objective 
function and R-squared is **significant**, i.e. over a threshold.


## Time delay 

Determining $[a,b,c,q,t_d]$` can be expressed as a **linear mixed-integer** problem.
If the integer term $t_d$ is given, then determining the *continuous parameters* $[a,b,c,q]$ is a *linear* optimization problem.

The solver ``UnitIdentifier`` takes a *sequential* approach to solving the joint estimation problem.
It solves for the *continuous* parameters starting at zero time delay and then for ever-increasing time-delays identifies and compares, until the logic determines that 
attempting to solve for larger time delays is not necessary. 

The solver then selects the ``best`` time delay and the associated continuous parameters.
This logic is implemented in the class ``ProcessTimeDelayIdentifier``.


## Flow of solver

The flow of the solver is as below:

- start at zero time delay $t_d$`,
- while( ``TimeDelay`` object says to continue ),
	- solve the model with no curvatures $c=0$
	- Solve for $[a,b,c,]$ with every combination of curvatures on/off for a given $t_d$,
	- for each given $[a,c,d,t_q]$ find the bias $q$,
	- choose the best model of the above for the given time delay
	- save the best model run for the given time-delay in the `TimeDelay`` class object, and
	- increase time delay.
- ``TimeDelay`` object chooses the best model run out of all the saved runs.

> [!Note]
> **The solver may thus run tens or hundreds separate identifications with different model terms turned on/off for each run of the ``UnitModelIdentifier``,
but without requiring any user-interaction.**

