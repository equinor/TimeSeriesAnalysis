# System identification: litterature

For an introduction to the litterature that has influenced this library refer the below citations.

The paper

Jonas Sjöberg, Qinghua Zhang, Lennart Ljung, Albert Benveniste, Bernard Delyon, Pierre-Yves Glorennec, Håkan Hjalmarsson, and Anatoli Juditsky. 1995. *Nonlinear black-box modeling in system identification: a unified overview.* Automatica 31, 12 (Dec. 1995), 1691–1724. 

and the text-book:

*System Identification: Theory for the User*, 2nd Edition Lennart Ljung, Prentice-Hall, UpperSaddle River, New Jersey, Copyright1999, Prentice Hall PTR, 609 pages

disucss some common themes that have guided the development of this library: 

- the key problem in system identification is to find a suitable model structure (fitting a given model structure is a lesser problem),
- do not estimate what you already know (the concept of grey-box models),
- start by looking at the data,
- try simple things first,
- look into the physics,
- the bias-variance tradefoff,
- validation and estimation data, and
- the notion of the efficient number of parameters (regularization/pruning).

In the review paper

Ljung, Lennart. (2007). *Perspectives on System Identification*. Annual Reviews in Control. 34. 10.1016/j.arcontrol.2009.12.001. 

the author mentions the following open issue within the topic of system identifcation
- How to identify a nonlinear system that operates in closed loop and is stabilized by an unknown regulator?

The author also highlights four central problems for industrial use of system identification

- automatically polishing dta and finding informative portions,
- an efficient integration of system identification into engineers daily modeling tools,
- taking care of structural information (i.e. knowledge of what is connected to what in a plant),and
- creating models and tools for tuning and monitoring PID-feedback loops.
