### Gain-scheduled models

Gain-scheduled models have more parameters than unit-models and thus require more information to identify. They are harder to identify and harder to maintain, and it is recommended to apply these only 
in cases where the unit-model has been attempted and found insufficient.

Gain-scheduled models need to have specified
- a set of $N_G$ gains
- a set of $N_G-1$ gain-thresholds

A gain-scheduled model can be static, but it can also have one or multiple time-constants, optionally 
- a set of $N_T$
 time-constants ($N_T$ can be 1)
- a set of $N_G-1$ gain-thresholds 
- a time-delay
