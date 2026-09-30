
## Condition monitoring

To apply the methods of this library to modeling a larger plant, several techniques may be needed:
- *Regressor/input transformations*: capture non-linearities by transforming inputs, for instance by raising an input to a power.
- *Choice of inputs*: selecting which input(s) to use to predict each output is a design choice, and these choices have implications for simulation boundary conditions.
What design choices are made during modeling may depend on the intended purpose of the model. 

Use-cases can be broadly separated into
- *"condition monitoring"* : the model is only intended to run concurrently with a given dataset
- *"what-if" simulations*: the model is intended to be used to evaluate different hypothetical scenarios that don't match the given data(i.e. some variables are *free variables*).


### Boundary conditions for condition monitoring

The choice of input to models is a design decision, as when an input is included it must either be supplied to the simulation, or further models may need to be added to relate this input
to other boundary conditions.

For example, the flow through a choke can be described using the choke opening $z$ alone, but most choke equations include both the choke opening $z$ and the differential pressure $\sqrt{\Delta p}$

For condition monitoring, any available time-series can be used as boundary conditions for the model, but for a what-if simulation, *only boundary variables that are independent 
of the free variables* should be included.


> [!NOTE]
>**Example**
> Most physical equations for mass through a choke are of the form $\dot{m} = f(z,\Delta p)$. 
> For *condition monitoring* it makes sense to feed $\Delta p$ time-series as a boundary condition into this equation along with $z$ to estimate a mass flow. 
> However, this choice of input is problematic for  *what-if simulations*, as the differential pressure depends on the choke opening, and so to allow the choke opening to 
> vary freely, one would need to model how $\Delta p$ changes with $z$ as well ($\Delta p = g(z)$), in effect turning $\dot{m} = f(z,\Delta p) =  f(z,g(z)) = h(z)$. 





