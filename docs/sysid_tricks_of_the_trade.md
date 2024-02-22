# System identification: tricks of the trade

While it is true that a static model can be turned into a dynamic model by simply adding a *forgetting factor*, an industrial implementation of this idea that is numerically robust
and is well-behaved enough to either succeed or fail gracefully for any given industrial dataset without human supervision requires a non-trivial bit of additional coding. 

Based on experience, a number of design choices have been made in the design of ``UnitModel`` and ``UnitIdentifier``, these tricks are presented as a numbered list below:

1. **base identification on linear regression**: choose models that are *linear-in-parameters*, as you will need to do a large number of identification runs, and linear regression is both fast, robust  and easy to analyze. Note that *it will still be possible to express nonlinearities, but you avoid the complications of introducing nonlinear optimization solvers*.
2. **add regularization** that will bring parameters to zero in the case the dataset contains no information on this parameter
3. **use a robust solver** (such as *Singular Value Decomposition*,SVD) in combination with regularization, avoid that the parameters will take on extreme values in the case that there is little information on some variables,
4. **choose a model structure that is an extension of a linear, static model**: the model should be possible to simplify to a normal static, linear model by striking terms - this way there is a fall-back in cases of little information in the data ,
5. **prune away additive terms that add little to the model**: to avoid over-fitting and poor out-of-sample performance, only keep terms in the model where analysis clearly show that model works better with than without the term. In this code, this means:
	
	- **add nonlinearity and time constants through separate additive terms that are linear-in-parameters**, this way you can start with a linear static model and compare model performance by adding terms one-at-a-time.
6. **keep a set of metrics for assessing the fit**: metrics like *R-squared* and objective function value should be calculated for each run and used to avoid over-complicating a model
with term that contribute little or nothing
7. **use an objective function on difference form**: when identifying a dynamic model, you are likely to get better performance by expressing the objective in terms of the *differences* between neighboring samples rather than absolute values(or, you can do both!)
8. **use estimated parameter confidence intervals to automatically determine if to keep a model term**, if the confidence interval of a parameter estimate includes zero, and at value zero the term is stricken, the term can likely be stricken,
9. **re-formulate model parameters into the most human-intuitive possible**: this might mean that after solving identification with the model structure that is most beneficial for the solver, you re-formulate the model to a form that is more human-readable. 
This is a core principle in the **"grey-box"** approach - *it needs to be possible for humans to easily understand and interpret the models*. In our case this means:
	- **re-formulate your forgetting factor to time-constant** : a forgetting factor is hard to interpret, as it has no dimension. Do parameter conversion to time-constants, which has a time-unit and is easy to interpret, and is often used to tune 
		PID-controllers. 
	- **re-formulate other factors into "gains"**: in a dynamic model, the "gain" from an input to the output will depend on the forgetting factor, do this conversion so that model "gains" can be assessed. 
10. **add support for removing bad or irrelevant data points throughout**: real data has bad or missing data points, and you may want to remove startups/shutdowns or other non-representative data. This means that all your methods for regression 
and analysis of the result need to support filtering out data points. This is especially important for dynamic identification,as a single bad data point can linger in the internal state of a dynamic model, usually destroying the identification run.
11. **creating synthetic datasets to verify your method**: if you create a dataset with known parameters and your identification method identifies these correctly, you can have confidence it works.
12. **testing needs to be automated**: you will need to create hundreds of test datasets to test all aspects and edge cases likely to be encountered in autonomous identification. Tests need to be re-run every time any part of the code is changed, thus it needs to be automatic. 
13. **time-delay estimation can be brute-forced** finding the time delay(an integer) together with continuous parameters creates a *mixed-integer problem*. Rather than introduce mixed-integer solvers, you can try evaluating the fit of the model for increasing 
time delays, to see if it improves fit (in effect, solving the mixed-integer problem with an ad-hoc mixed-integer solver.)
14. **automate the model structure selection**: there is no rule that says you only need to do one identification run for each dataset. If you follow all the above items, turning on and off model terms is easy, you have multiple ways of assessing the model quality of each run and 
each identification run is cheap computationally. Thus you can automate model selection by re-running identification for different model structures and choose the best, and this process can be automated. 

*It is the joint integration of all of these design choices together that makes for the value proposition of using ``UnitModel`` and ``UnitIdentifier``  which can enable deploying advanced analytics-methods(which by definition require **autonomous** or **semi-autonomous modeling**)*.