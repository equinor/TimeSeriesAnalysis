## Setting up and running unit tests 

A ``unit test`` is a small, automated test that verifies the behavior of a single, isolated unit of code, usually to 
verify the correctness of core logic. 

> [!Note]
>**Unit tests as documentation**
> Unit tests are an important part of the documenting this class library, as they give examples of how to run the public interface of the library, and document 
> the expected output. Thus, unit tests are worth studying even for users who do not intend to write or modify unit tests.

Unit tests are implemented using NUnit 3.

Many if not most of the tests in this repository do not obey the definition of a unit test. 
Some are large and test multiple units of code together, and quite often involve creating a scenario with 
synthetic data using the ``PlantSimulator`` to gauge the *performance* of the system. 
Most of the tests are thus more correctly termed ``Scenario tests``, ``Acceptance tests`` or ``Performance tests`` and 
sometimes some of these tests represent stretch goals that may need to be checked-in to the code in a non-passing state. 

> [!Note]
> **Make non-functioning tests ``Explicit`` rather than commenting out**
>
> Note that some tests related to plotting are ``Explicit``, and will need be run 
> one-by-one. This has been done on the "Getting Started" unit test, to avoid 
> needlessly creating plots on "Run All" unit tests. 
> 
> It is considered preferable to make a test explicit rather than deleting it or commenting it out if it represents a scenario test that fail, as this stores what has been attempted, and is an important record. 
> By keeping the code commented, in the code avoids becoming stale in case any methods are
> re-factored.


## Visual Studio

In ``Visual Studio`` you should be able to browse the unit tests in the window ``Tests>>Test Explorer``. In the ``Test Explorer`` window, pressing ``Run All tests`` should cause
all tests to turn ``green``. 

In some cases, it may be that the tests appear grayed or or with a blue exclamation point beside them. That indicate an issue with the installation of the ``Nunit3TestAdapter`` 
package through NuGet, which is required for integration NUnit with Visual Studio. 

![Test Explorer in Visual Studio](./images/unit_test.png)

##  VS Code

VS code should be able to recognize the unit tests as long as the ``#C# Dev Kit`` is installed, and should look similar to below:

![Testing Window in VS Code](./images/unit_test_vscode.png)












	