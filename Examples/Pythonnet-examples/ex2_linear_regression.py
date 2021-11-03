import sys

import clr
import matplotlib.pyplot as plt

assembly_path = r"C:\Appl\TimeSeriesAnalysisBuild"


sys.path.append(assembly_path)

clr.AddReference("TimeSeriesAnalysis")
clr.AddReference("System.Collections")


from System import Array, DateTime, Double, String
from System.Collections.Generic import List
from TimeSeriesAnalysis import Vec
from TimeSeriesAnalysis.Utility import Plot, TimeSeriesCreator

true_gains = [1, 2, 3]
true_bias = 5
noise_amplitude = 0.1

timeBase_s = 1
vec_sum = Vec().Add([1, 2], [3, 4])

u1 = TimeSeriesCreator.Step(11, 61, 0, 1)
u2 = TimeSeriesCreator.Step(31, 61, 1, 2)
u3 = TimeSeriesCreator.Step(21, 61, 1, -1)

y = []
noise = Vec().Mult(Vec.Rand(u1.Length, -1, 1, 0), noise_amplitude)
for k in range(u1.Length):
    y.append(
        true_gains[0] * u1[k]
        + true_gains[1] * u2[k]
        + true_gains[2] * u3[k]
        + true_bias
        + noise[k]
    )

U = Array[Array[Double]]([u1, u2, u3])

results = Vec().Regress(y, U)

fig, (ax1, ax2) = plt.subplots(2, 1, sharex=True)
fig.suptitle("Example 2: Model fitting")
ax1.plot(list(y), label="y1")
ax1.plot(list(u1), label="u1")
ax1.plot(list(u2), label="u2")
ax1.plot(list(u3), label="u3")
ax1.grid()
ax1.legend(loc="upper right")
ax1.set_title("Dataset")

ax2.plot(list(y), label="y_meas")
ax2.plot(list(results.Y_modelled), label="y_mod")
ax2.grid()
ax2.legend(loc="upper right")
ax2.set_title("Resulting model")
plt.show()
