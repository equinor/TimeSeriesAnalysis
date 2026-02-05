# Ranking the fit of models and plants to data and tracking changes over time using FitScore

## Interpretation of the "Fit Score" value for single models

The fit score is a *normalized* objective function, that excludes all indices flagged to be ignored in a given dataset. 
The fit score is normalized in the sense that values are comparable for datasets with different numbers of data points.
The fit score is *also* normalized in that it represents percentage, 100% represents a perfect fit, and 0% represents the fit of 
a straight line through data.

A fit score is  

``"deviation between measured and simulated output" 
divided by
 "deviation between measured output value and the average output value"``, 

both values are summed over a period, while ignoring data points that are flagged to be ignored (bad data, or out of range data, pre-determined).

> [!Note]
> By convention, the library currently supports models with one output only. 

Calculating the fit score is handle by the class ``FitScoreCalculator`` and the details can be examined there. 

This library uses "Fit Scores" to rank how well a given model matches a given dataset, and is interpreted as follows:

- A fit score of 100% indicates a perfect match of model and data. 
- A fit score of 0% indicates the fit of the best straight line through the dataset
- A negative fit score indicates a model that fits worse than a straight line through data

Internally, different identification methods in the library use variants of sum of least squares as objectives to minimize in 
regression, then the FitScore is post-calculated and stored and saved, 
as the absolute value of the objective function is often a very large or very small number that is hard to interpret by humans. 

> [!Note]
> All identification methods in the library should return a fit score by convention, preferably as part of a ``FittingInfo`` object 
> that includes other information about the fit, and this information needs to be stored for later comparison. 

## Detecting model degradation  

One of the major motivations for the fit score is to attempt to compare model fit at different times (for datsets that may be of
different lengths, with differing amounts of indices to be ignored.)

*Storing the fit score* in the model allows comparison of model fit at different times, and is intended to be used to detect model degradation.

## Cross-validation

Another use for the fit score is to cross-validate the model. 

Cross-validation is evaluating a model by the fit of a dataset other than the tuning dataset, and is common practice in system identification.

If the model structure and parameters represent the actual reality well
then the fit score when calculated validation datasets should be close to the value found for the dataset the model was fitted to. 

However, if the fit score for validation data sets is much lower, or if this fit score varies significantly from dataset to dataset,
it may indicate that the parameters found are not representative, structural model error, or both. 

One of the causes of low or varying fit score could be that the model originally has been fitted to a dataset that has low 
information content, or which in system identification terms may mean that the model was "poorly identifiable" for the tuning dataset. 

## Plant-wide "Fit Score"

Fit scores are intended to be generalized when evaluating "plants" of multiple models. 
When evaluating multiple models, the fit score at the output of each model is averaged to give a 
"plant-wide" fit score. 

A plant-wide fit score should by convention be calculated from "co-simulating" all models (``PlantSimulator.Simulate``) not by 
simulating each plant in isolation (``PlantSimulator.SimulateSingle``). 

Plant-wide fit scores should be retrieved by ``FitScoreCalculator.GetPlantWideSimulated`` and is always returned by default when 
running ``PlantSimulator.Simulate``.

> [!Note]
> By comparing the plant-wide fit score for different plants, it is possible to rank different plant models by their overall fit, and this 
> can be used to rank and choose among multiple candidate plant model structures 

 




