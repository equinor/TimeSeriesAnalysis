# Contributing

This project welcomes contributions and suggestions. 
We believe in collaboration. Collaboration with developers and users, vendors and educational institutions, partners and competitors. Within and outside our industry.

When contributing to this repository, please first discuss the change you wish to make via issue, or by the GitHub discussions forum,
or any other method with the owners of this repository before making a change.

## Avoiding feature-creep: not an encyclopedia of every possible method

The more code this repository has, the more time it will take to maintain it and the more things that can go wrong. 

An important part of Lean is *"maximizing the work not done"* - we want to apply this idea to this repository as well, or "just because something *can* be added, does not mean it *should*".

This repository should not become an encyclopedia of everything time-series related.

You may of course pull this library into your own projects and in those you can mix and match and add functionality on top of or beside of to meet your needs. 
The question is though *under what circumstances should something be added back into this repository*?

Take for instance **file input**: Time-series can be stored on file in *endless* different ways, does that mean that we should add drivers for reading every kind of file format?

*No*

To keep things simple and maintainable, adding file drivers is something we would like to keep outside of the repository (even though we have broken our own rule and added 
support for comma-separated-variable files). 

The same goes for other areas than file IO. **Filters** for instance, they are many different ways to make a filter, but this repository is not an encyclopedia of every possible such methods. 

It is hard to give absolute rules, but some guidelines to consider before proposing to add new functionality to this class library:
- If the functionality is useful on its won, could the functionality instead be a stand-alone repository and ``NuGet`` package?
- If the stand-alone functionality *already exists* on ``NuGet`` in another repository, it is probably better for users to just pull in that package.
- If the new feature does a task like process modeling, filtering or PID-control in a different way than what the repository already does, there should be a clear benefit of the 
new approach in terms of *performance* or *features*.
- If the new feature *builds on top* of existing functionality it could be interesting to add in, but only if it is 
	- **generally applicable methodology** with 
	- **practical and industrial** use-cases.

In general the repository should be kept as *"narrow"* as possible, so we do not want to add in different ways of accomplishing the same things, **but** methods that build on top of existing methods
in the repository to go **"deeper"** are very welcome.


## What kind of contributions?

### Data mining/ advanced analytics methods that build on top of existing methods 

**If you would like to contribute on this, this is very welcome.** Developing the repository in this direction is on the roadmap toward versions ``2.x``.

### Benchmarking and academic comparisons

If you would like to compare and benchmark other methods in this project, such as the PID-controller or system identification, 
 that would be much appreciated (even if the other methods you tried appear to be better). For academic use, this project could be used as a reference
 for academics who are developing their own methods. 

### Bug fixes

Any bugs you may find, you are encouraged to report using our issue tracker, and if you can propose a fix, that is much appreciated

### Expanding on the capability of the PID-controller 

It will always be possible to add more functionality to the PID-controller, to accommodate different types of advanced control that it may not currently support.

### Expanding on capability of the array/matrix classes

Array/Matrix/Vector classes do not contain every conceivable operation. If you find that the method you need is missing, addition of new methods that perform new operations 
are appreciated. Be aware that ``Accord.Math`` already contains a ``Matrix`` namespace with many familiar operators - instead of just duplicating functionality, consider if you can 
use ``Accord.Math.Matrix`` directly.

### Expanding on the (dynamic) system-identification tool set 

Do you have a great idea for how better to identify models for dynamic systems? If you would like to contribute your own method into the tool set, that sort of method is much
appreciated. 

This library was initially developed with the ``DefaultProcessModel`` in mind, but is should be possible to extend the library with other process model parametrization.
To do so, you should replicate how the ``DefaultProcessModel`` has been implemented, and your model classes should implement the interfaces specified.

In general, this class library is intended to be applied to industrial data where excitation may be less-than-ideal. For that reason the focus should be on parametric identification, **not** non-parametric
models like Finite-Impulse Repsonse(FIR) models.

## Pull request

**Proposed code changes to this project should be submitted for review as pull requests.**

Requirements for pull requests:
- pull request should only address a single feature/issue
- all existing tests should pass
- any new feature should be supported by at least one new unit tests, that both shows that the new feature works and documents how to use the new feature
- be available for questions of the reviewer.
- for complex methods such as those related to filtering, dynamic models or PID-control, unit tests should use the ``Plot`` class to plot the time-series that illustrate
the test data sets used and the results of any new calculation. It is much easier to understand capability visually both for code review but also for other users.

