# Contributing

This project welcomes contributions and suggestions. 
We believe in collaboration. Collaboration with developers and users, vendors and educational institutions, partners and competitors. Within and outside our industry.

## Contribution code-of-conduct

1. When contributing to this repository, please **first discuss the change you wish to make** via issue, or by the GitHub discussions forum,
or any other method with the owners of this repository **before** making a change.
2. **Proposed code changes to this project should be submitted for review as pull requests.**

### Pull request

Requirements for pull requests:
- should follow the coding convention below
- pull request should only address a single feature/issue,
- all existing tests should pass,
- any new feature should be supported by at least one new unit test, that both shows that the new feature works and documents how to use the new feature,
- consider using ``TestCase`` to re-run a new unit test for more than one set of input parameters (remember: testing edge-cases and testing the negative.)
- the submitter must be available for questions of the reviewer, and 
- for complex methods such as those related to filtering, dynamic models or PID-control, unit tests should use the ``Plot4Test`` class to plot the time-series that illustrate
the test data sets used and the results of any new calculation. It is much easier to understand capability visually both for code review but also for other users. 
Code to plot the time-series should be available in the code, but should either be disabled with logic by default or commented out, this is to avoid swamping the user with plots 
if re-running all tests.

## Avoiding feature-creep: not an encyclopedia of every possible method

The more code this repository has, the more time it will take to maintain it and the more things that can go wrong. 

An important part of Lean is *"maximizing the work not done"* - we want to apply this idea to this repository as well, or "just because something *can* be added, does not mean it *should*".

This repository should not become an encyclopedia of everything time-series related.

It is hard to give absolute rules, but some guidelines to consider before proposing to add new functionality to this class library:
- If the functionality is useful on its own, could the functionality instead be a stand-alone repository and ``NuGet`` package?
- If the stand-alone functionality *already exists* on ``NuGet`` in another repository, it is probably better for users to just pull in that package.
- If the new feature does a task like process modeling, filtering or PID-control in a different way than what the repository already does, there should be a clear benefit of the 
new approach in terms of *performance* or *features*.
- This repository is aimed toward **industrial applications**, so esoteric methods(i.e. of specicial, rare or unusual interest) 
that have academic interest but lack proven practical benefit on real-world data from real-world systems are better left outside this package.
- If the new feature *builds on top* of existing functionality it could be interesting to add in, but only if it is 
	- **generally applicable methodology** with 
	- **practical and industrial** use-cases.

## What kind of contributions?

### Reporting issues 

If you during the use of this library discover that something is not working, you are encouraged to report it as an issue in the github issues page.
If you can propose how to fix it that increases the likelihood it will be fixed. If you have made a fix that you would like to propse be merged in, 
make a push request.

### Data mining/ advanced analytics methods that build on top of existing methods 

**If you would like to contribute on this, this is very welcome.** Developing the repository in this direction is on the roadmap toward versions ``2.x``.

### Benchmarking and academic comparisons

If you would like to compare and benchmark other methods in this project, such as the PID-controller or system identification, 
 that would be much appreciated (even if the other methods you tried appear to be better). For academic use, this project could be used as a reference
 for academics who are developing their own methods. 

### Expanding on the (dynamic) system-identification tool set 

Do you have a great idea for how better to identify models for dynamic systems? If you would like to contribute your own method into the tool set, that sort of method is much
appreciated. 

## Coding convention

- All code should follow conventional C# naming conventions, please refer to https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines, 
	- this includes using camelCase or PascalCase as appropriate for varibale names, and
   	- names of variables and classes should prioritize readability over brevity, and not include abbreviations or underscores
   	- name new classes and variables in ways that are consistent with the names already used. 
- Organize code so that others can easily undertand, maintain and extend it:
 	- Favor smaller classes and methods that have a single specific purpose, clearly expressed in the name. 
- Use unit test framework during development, and keep tests in project as part of documentation and as a "fail-safe" mechanism for other developers
	- To understand how code works, how it is called and what it should do, refer to the tests
   	- When re-writing functionality, unit-tests are important fail-safe to check that nothing is broken. Unit tests are important for refactoring.
   	- Unit tests need to be quick, so that they can be run frequently without hurting developer output.
   	- During development, it is advised to use the ``Plot.FromList()`` to plot time-series together, but care should be taken to not check in code that will cause plotting.
