import sys

import clr
import matplotlib.pyplot as plt

assembly_path = r"C:\Appl\PythonNETtest\tsabuild"

sys.path.append(assembly_path)

clr.AddReference("System")
clr.AddReference("System.Collections")
clr.AddReference("TimeSeriesAnalysis")


from System import Array, Double
from System.Collections.Generic import List
from TimeSeriesAnalysis import Array2D, Vec
from TimeSeriesAnalysis.Dynamic import (
    DefaultProcessModel,
    DefaultProcessModelIdentifier,
    DefaultProcessModelParameters,
    UnitDataSet,
    UnitSimulator,
)
from TimeSeriesAnalysis.Utility import TimeSeriesCreator

timeBase_s = 1.0
noiseAmplitude = 0.05

parameters = DefaultProcessModelParameters()

parameters.WasAbleToIdentify = True
parameters.TimeConstant_s = 15.0
parameters.ProcessGains = [1.0, 2.0]
parameters.TimeDelay = 5.0
parameters.Bias = 5.0


model = DefaultProcessModel(parameters, timeBase_s, ID="not_named")

model.modelParameters = parameters
model.timeBase_s = timeBase_s
model.ID = "not_named"


u1 = TimeSeriesCreator.Step(40, 200, 0, 1)
u2 = TimeSeriesCreator.Step(105, 200, 2, 1)


u_list = List[Array[Double]](range(2))
u_list.Add(u1)
u_list.Add(u2)


U = Array2D[Double].InitFromColumnList(u_list)

dataSet = UnitDataSet(timeBase_s, U)
simulator = UnitSimulator(model)

simulator.EmulateYmeas(dataSet, noiseAmplitude)

modelId = DefaultProcessModelIdentifier()
identifiedModel, subprosDataSet = modelId.Identify(dataSet)

regResults = Vec().Regress(dataSet.Y_meas, U)


# Plot generated dataset
fig, (ax1, ax2, ax3, ax4) = plt.subplots(4, 1, sharex=True)
ax1.plot(list(dataSet.Y_meas), label="y_sim", color="Black")
ax1.grid()
ax1.legend(loc="upper right")
ax1.set_title("ex4_data")

ax2.plot(list(u1), label="u1")
ax2.plot(list(u2), label="u2")
ax2.grid()
ax2.legend(loc="upper right")


# Plot fitted model
ax3.plot(list(identifiedModel.FittedDataSet.Y_meas), label="y_meas")
ax3.plot(list(identifiedModel.FittedDataSet.Y_sim), label="y_sim")
ax3.grid()
ax3.legend(loc="upper right")
ax3.set_title("ex4_results")

# Compared to static model found by linear regression
ax4.plot(list(identifiedModel.FittedDataSet.Y_meas), label="y_meas")
ax4.plot(list(identifiedModel.FittedDataSet.Y_sim), label="y_sim")
ax4.plot(list(regResults.Y_modelled), label="y_static")
ax4.grid()
ax4.legend(loc="upper right")
ax4.set_title("ex4_static_vs_dynamic")

plt.suptitle("Example 4: Model fitting")
plt.show()
