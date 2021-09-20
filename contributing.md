# Contributing

This project welcomes contributions and suggestions. 

When contributing to this repository, please first discuss the change you wish to make via issue, or by the github discussions forum,
or any other method with the owners of this repository before making a change.

## What kind of contributions?

### Expanding on capability of the array/matrix classes

Array/Matrix/Vector classes do not contain every conceivalbe operation. If you find that the method you need is missing, addition of new methods that perform new operations 
are appreciated.

### Expaning on the (dynamic) system-identification toolset 

Do you have a great idea for how better to identify models for dynamic systems? If you would like to contribute your own method into the toolset, that sort of method is much
appreciated

### Expanding on the capability of the pid-controller 

It will always be possible to add more functionality to the PID-controller, to accomodate different types of advanced control that it may not currently support.

### Benchmarking and academic comparisons

If you would like to compare and benchmar other methods in this project, such as the PID-controller or system identification, 
 that would be much appreciated (even if the other methods you tried appear to be better). For academic use, this project could be used as a reference
 for academics who are developing their own methods. 

### Bugfixes

Any bugs you may find, you are encouraged to report using our issue tracker, and if you can propose a fix, that is much apperciatd


## Pull request

**Proposed code changes to this project should be submitted for review as pull requests.**

Requirements for pull requests:
- pull request should only address a single feature/issue
- all existing tests should pass
- any new feature should be supported by at least one new unit tests, that both shows that the new feature works and documents how to use the new feature
- be available for questions of the reviewer.
- for complex methods such as those related to filtering, dynamic models or PID-control, unit tests should use the ``Plot`` class to plot the time-series that illustrate
the test data sets used and the results of any new calculation. It is much easier to understand capability visually both for code review but also for other users.

