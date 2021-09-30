# Example 3 : Filtering

``TimeSeriesAnalysis`` includes basic recursive filtering through the classes ``LowPass``, ``HighPass`` and ``BandPass``.

To illustrate their capabilities, we create an artificial dataset which is made of two sinusoids ``sinus1`` and ``sinus2``of different amplitudes and 
frequencies. Sinuses are implemented in a ``SinusModel`` class, and can be simulated by ``ProcessSimulator``. 
The two sinues are added together by calling ``ProcessSimulator.Simulate()`` twice on the same ``ProcessDataSet``. 

Now, we want to define a ``LowPass`` and ``HighPass`` filter that can separate out the high-frequency and low-frequency sinuses, and this 
is mainly a matter of choosing appropriate filter time constants of either filter. 

``sinus1`` has an period of ``400`` seconds, and thus goes from maximum to minimum amplitude in about ``~200`` seconds. 
Remembering that a first-order system by rule-of-thumb will take about 5 time-constants to implement 99% of a change,
motivates a time constant of ``200/5=40`` seconds for the low-pass filter. 

By a similar logic,as ``sinus2`` has a period of ``25`` seconds will go from maximum to minimum amplitude in about 12 seconcds, 
thus motivating a filter time-constant of about ``~3`` seconds.

The example code(runnable through the ``Test Explorer``):
```
public void Ex3_filters()
{
    double timeBase_s = 1;
        int nStepsDuration = 2000;
    var sinus1 = new SinusModel(new SinusModelParameters 
        { amplitude = 10, period_s = 400 },timeBase_s);
    var sinus2 = new SinusModel(new SinusModelParameters 
       { amplitude = 1, period_s = 25 }, timeBase_s);

    var dataset = new ProcessDataSet(timeBase_s, nStepsDuration);
    ProcessSimulator<SinusModel, SinusModelParameters>.Simulate(sinus1, ref dataset);
    ProcessSimulator<SinusModel, SinusModelParameters>.Simulate(sinus2, ref dataset);

    var lpFilter = new LowPass(timeBase_s);
    var lpFiltered = lpFilter.Filter(dataset.Y_sim,40,1);

    var hpFilter = new HighPass(timeBase_s);
    var hpFiltered = hpFilter.Filter(dataset.Y_sim,3,1);

    Plot.FromList(new List<double[]> { dataset.Y_sim, lpFiltered, hpFiltered },
         new List<string> { "y1=y","y3=y_lowpass","y3=y_highpass" }, (int)timeBase_s);
 }
```

Running the above code results in the below plot. 

In the top plot ``y`` that is two sinusoids overlayed. 

The below plot shows the highpass- and lowpass-filtered versions of ``y`` and by the naked eye you can see that
the filters have approximately managed to capture and separate out  the two components. 

![Example 3](images/ex3_filters.png)


> [!Note]
> If you look closely, you will notice that ``y_highpass`` and ``y_lowpass`` sinusoids are delayed slightly in comparison to ``y``, and also that the amplitudes of the two signals do not completely match the originals. This is due to the *phase-shift* and *attenuation* causes by the recurisve filters, and is good to be aware of. It would be possible to get even *smoother* filtered signals by increasing the filter order from ``1`` to ``2`` for either filter, but the penalty would be increased phase-shift.
   
