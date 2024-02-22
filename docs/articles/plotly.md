# Plotly.js

Plotting of time-series is based on the ``plotly.js`` library, which is released under the ``MIT license``
and is free to distribute and use commerically.

No changes are made or will be made to the original source files, but this class
library is distributed with some additional javascript code that handles reading the URL and reading 
text files from disk containing the time-series information and calling the ``plotly.js`` library.

Plotting is not a major part of this library's functionality, and the library can be used completely without 
this plotting. 

The plotting is also intended mostly as support during testing, for test-driven development, but since the 
plots are in web-page form they could also be used as a component in web-based dashboards or advisory tools. 

> [!Note]
> If calling on this library from Matlab or Python, users may choose to use the built-in plotting functionalities
> of those languages and their tools, the inclusion of 
> ``plotly.js`` is to make the choice of plotting tool voluntary and to not *require* 
> installing additional programs.