# Design principles 

## System identification

``The goal of the identification procedure is, in loose terms, to *obtain a good and reliable model with a reasonable amount of work.*``(Lennart Ljung, Theory for the user 2ed, p.399)
This quote displays the trade-off of the three different and sometimes opposing goals of identification:
1. a **good**(accurate, descriptive) model,
2. a **reliable** model, and 
3. a model developed with **a reasonable amount of work**.
This library intends to focus on methods that give deliver a good balance of *all three goals*. 
Conversely this means that models that are either
- not *good enough* 
- not *relivable enough*, or
- *cannot be developed with a reasonable amount of work* 
will not be considered.

Further the choice of the model should ideally be based on 
``posing a criterion for what is a *good* model and to list the constraints that are imposed on the design by limited time and cost`` (p.406)

Thus system identification is a practical field that acknowledges that better models take more time and cost more money, and that these factors need to be taken into consideration.

This class library is built on the following principles/assertions:
- most time-series are *not* designed for identification, and may have less than ideal amount of excitation, hence 
**handling parameter uncertainty** and **avoiding over-parametrization** are important,
- almost all systems are actually **nonlinear**, many but not all can be considered **locally linear**,
- real-world data will contain bad data points that need to be **filtered** out , and you may need to manually remove further non-representative data. 
Especially for recursive models, a single spurious value can destroy an entire model run, thus the tooling need to support cleaning data to avoid garbage getting into models.
- expect *parameter uncertainty*, treat it explicitly.

## Code design

- use *dependency injection*, *generics* and *interfaces* to make the process model easily replaceable - if you provide a new process model that implements the correct interfaces, it should immediately be compatible with re-usable functionality such as PID-control or simulation. 
- do not use inheritance - this kind of code is hard to understand for others.
