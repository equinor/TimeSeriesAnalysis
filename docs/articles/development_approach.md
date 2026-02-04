
## Unit testing as documentation

A ``unit test`` is a small, automated test that verifies the behavior of a single, isolated unit of code, usually to 
verify the correctness of core logic. 

> [!Note]
>**Unit tests as documentation**
> Unit tests are an important part of the documenting this class library, as they give examples of how to run the public interface of the library, and document 
> the expected output. Thus, unit tests are worth studying even for users who do not intend to write or modify unit tests.


## Scenario testing, not just unit testing

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


## Tests as a tool for tuning methods and refactoring without performance deteriorating

Many of the tests that are ``scenario tests`` broadly speaking follow the pattern below. 

```csharp
[TestCase(10,20)]
[TestCase(5,20)]
TestName(double trueParameter ,double tolerancePrc)
{
    var syntheticDataSet = GenerateDataSet(trueParameter);
    var estimatedParameter = MethodToBeTested(syntheticDataSet); 
    Assert.IsTrue(Math.Abs(estimatedParameter/trueParameter-1)*100<tolerancePrc);
}
```

- A scenario (basically a time-series data set) is generated based on a model, usually run with ``PlantSimulator``. 
- An ``Assert`` is used to ensure that the estimated parameter is within a certain percentage of the true value
- typically the tolerance ``tolerancePrc`` is set to narrowly above the actual error percentage when running the simulator (if it is considered acceptable, say within 5%, 10% or 20% depending on the difficulty of the scenario)
- The unit test is *parametrized*, so the same test can test multiple different values of the true parameter or parameters, representing different scenarios. 

**The result is that when hundreds of test are made, with tolerance set to narrowly bound the actual algorithm performance, they act as a safety-net for 
working on the algorithm performance, as any change in the ``MethodToBeTested`` that results in *worse* performance in any scenario will immediately be detected as they cause unit tests to fail.**

The resulting set of unit tests are also useful for determining the value of design constants in methods, as the effect of a change in a a design constant can 
quickly be gauged on the suite of tests. 

>[!Note]
>
> The above code includes a tolerance for acceptable deviation from the actual value.
> This is more typical of ``acceptance`` or ``scenario`` testing of estimation, on the other hand for a unit test
> you typically test more for exactness. 
