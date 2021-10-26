# Cascade control

This example considers a cascade control scheme consisting of a rapid inner loop and a slower outer loop.
In a real-world case, the inner loop could for instance be a (rapid) valve flow-rate controller, while the outer loop could for instance be a level that depends on the flow rate. 

![Cascade system](images/ex_cascade.png)

[!code-csharp[Example](../Examples/ProcessControl.cs?name=CascadeControl)]


