## Setting up and running unit tests

> [!Note]
> Unit tests are an important part of the documenting this class library, as they give examples of how to run the public interface of the library, and document 
> the expected output. Thus, unit tests are worth studying even for users who do not intend to write or modify unit tests.

Unit tests are implemented using NUnit 3.
In ``Visual Studio`` you shoudl be able to browse the unit tests in the winow ``Tests>>Test Exploer``. In the ``Test Explorer`` window, pressing ``Run All tests`` should cause
all tests to turn ``green``. 

In some cases, it may be that the tests appear greyed or or with a blue excalamation point beside them. That indicate an issue with the installation of the ``Nunit3TestAdapter`` 
package through nuget, which is required for integration NUnit with Visual Studio. 

> [!Note]
> If you want to submit a pull request for new functionality, writing a new a unit test is required.

> [!Note]
> Note that some tests related to plotting are ``Explicit``, and will need be run one-by-one. This has been done this way as this tests require manual inspection, and to avoid
> drowing the user in plots when re-running unit tests. 
