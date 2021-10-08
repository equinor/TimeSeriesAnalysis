# Design principles 

## System identification

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
