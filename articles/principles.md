# Design principles 

This class library is built on the following principles/assertions:
- most time-series are *not* designed for identification, and may have less than ideal amount of excitation, hence 
**handling parameter uncertainty** and **avoiding over-parametrization** are important,
- almost all systems are actually **nonlinear**, many but not all can be considered **locally linear**,
- real-world data will contain bad data points that need to be **filtered** out , and you may need to manually remove further non-representative data. 
Espeically for recursive models, a single spurious value can destroy an entire model run, thus the tooling need to support cleaning data to avoid garbage getting into models.
