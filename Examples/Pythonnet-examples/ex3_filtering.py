import sys

import clr
import matplotlib.pyplot as plt

assembly_path = r"C:\Appl\PythonNETtest\tsabuild"

sys.path.append(assembly_path)

clr.AddReference("System")
clr.AddReference("System.Collections")
clr.AddReference("TimeSeriesAnalysis")

from TimeSeriesAnalysis import Vec
from TimeSeriesAnalysis.Dynamic import HighPass, LowPass
from TimeSeriesAnalysis.Utility import TimeSeriesCreator

timeBase_s = 1
nStepsDuration = 2000

sinus1 = TimeSeriesCreator.Sinus(10, 400, timeBase_s, nStepsDuration)
sinus2 = TimeSeriesCreator.Sinus(1, 25, timeBase_s, nStepsDuration)
y_sim = Vec().Add(sinus1, sinus2)

lpFilter = LowPass(timeBase_s)
lpFiltered = lpFilter.Filter(y_sim, 40.0, 1)

hpFilter = HighPass(timeBase_s)
hpFiltered = hpFilter.Filter(y_sim, 3.0, 1)


fig, (ax1, ax2) = plt.subplots(2, 1, sharex=True)
fig.suptitle("Example 3: Filtering")

# Plot generated dataset
ax1.plot(list(y_sim), label="y_sim", color="C0")
ax1.grid()
ax1.legend(loc="upper right")


# Plot filtered dataset
ax2.plot(list(lpFiltered), label="y_lowpass", color="C1")
ax2.plot(list(hpFiltered), label="y_highpass", color="C2")
ax2.grid()
ax2.legend(loc="upper right")


plt.show()
