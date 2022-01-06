# System identification: tricks of the trade

While it is true that a static model can be turned into a dynamic model by simply adding a ``forgetting factor``, an industrial implementation of this idea that is numerically robust
and is well-behaved enough to either succeed or fail gracefully for any given industrial dataset requires a non-trivial bit of coding. 

Based on experience, a number of design choices have been made in the design of ``UnitModel`` and ``UnitIdentifier``, these tricks are presented as a numbered list below:

1. *base it on linear regression*: base the identification on linear regression(linear-in-parameters), as you will need to do a large number of identification runs, and linear regression is both fast, robust  
and easy to analyze
2. *regularization* add regularization-terms that will bring parameters to zero in the case the dataset contains no information on this parameter
3. *use a robust solver* by using a robust SVD-solver, in combination with regularization we avoid that the parameters will take on extreme values in the case that there is little information on some variables
4. *choose a model structure that is an extension of a linear,static model*: the model should be simplifiable to a normal static, linear model if terms are stricken,
5. *add nonlinearity and time constants through separate additive terms that are linear-in-parameters*, this way you can start with a linear static model and compare model performance by adding terms one-at-a-time.
6  *prune away additive terms that add little to the model*: to avoid over-fitting and poor out-of-sample performance, only keep terms in the model where analysis clearly show that model works better with than without the term
7. *keep a set of metrics for assessing the fit*: metrics like R-squared and  objective function value should be calculated for each run and used to avoid over-complicating a model
with term that contribute little or nothing
8. *use an objective function on difference form* when identifying a dynamic model, you are likely to get better performance by expressing the objective in terms of the *differences* between neighboring samples rather than absolute values
9. *use estimated parameters confidence intervals to automatically determine if to keep a model term*, if the confidence interval of a parameter includes zero, and at value zero the term is stricken, the term can likely be stricken
10.*re-formulate your forgetting factor to time-constant* : a forgetting factor is hard to interpret, as it has no dimension. Do parameter conversion to time-constants, which has a time-unit and is easy to interpret, and is often used to tune 
PID-controllers
11. *re-formulate other factors into "gains"*: in a dynamic model, the "gain" from an input to the output will depend on the forgetting factor, do this conversion so that model "gains" can be assessed. 
12. *add support for removing bad or irrelevant data points throughout*: real data has bad or missing data points, and you may want to remove startups/shutdowns or other non-representative data. This means that all your methods for regression 
and analysis of the result need to support filtering out data points. This is especially important for dynamic identification. 
13. *creating synthetic datasets to verify your method*: if you create a dataset with known parameters and your identification method identifies these correctly, you have confidence it works.
14. *testing needs to be automated*: you will need to create hundreds of test datasets to test all aspects of identification, and you need to re-run these every time you change anything to be sure that nothing broke. 
15. *time-delay estimation can be brute-forced* finding the time delay together with other problems creates a mixed-integer problem. Rather than introduce mixed-integer solvers, you can try evaluating the fit of the model for increasing 
time delays, to see if it improves fit (in effect, solving the mixed-integer problem with your own solver.)
16. *automate the model structure selection*: by re-running the identification with and without different terms like time-constants, or nonlinerities, you can automate the selection of the "best" models structure. 