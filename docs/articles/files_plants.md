# Loading and saving PlantSimulator objects to and from json-files

The library is intended to be able to read and write ("serialize and de-serialize") ``PlantSimulator`` objects to
and from json files, useful both for persisting to disk or for transmitting models across APIs.

``PlantSimulator.Serialize()`` can be used to serialize the PlantSimulator object directly into a file, or 
``PlantSimulator.SerializeTxt()`` can return the json text to a string.

A ``PlantSimulator`` represented as a json text can be serialized into an object by 
``PlantSimulatorSerializer.LoadFromJsonTxt()``.

Take an an example the uni test ``DeserializedPlantSimulatorAndTimeSeriesDataObjects_AreAbleToSimulate`` where the variable
``plantsimJsonTxt`` contains the json text. 

Below is the content of that variable:

```
{
  "comments": [],
  "modelDict": {
    "SubProcess1": {
      "$type": "TimeSeriesAnalysis.Dynamic.UnitModel, TimeSeriesAnalysis",
      "modelParameters": {
        "Y_min": "NaN",
        "Y_max": "NaN",
        "FittingSpecs": {
          "u0": null,
          "uNorm": null,
          "Y_min_fit": null,
          "Y_max_fit": null,
          "U_min_fit": null,
          "U_max_fit": null
        },
        "LinearGains": [
          1.0,
          0.5
        ],
        "LinearGainUnc": null,
        "Curvatures": null,
        "CurvatureUnc": null,
        "TimeConstant_s": 10.0,
        "TimeConstantUnc_s": null,
        "TimeDelay_s": 5.0,
        "DampingRatio": 0.0,
        "Bias": 5.0,
        "BiasUnc": null,
        "U0": null,
        "UNorm": null,
        "Fitting": null
      },
      "ModelInputIDs": [
        "SubProcess1-External_U",
        "PID1-PID_U"
      ],
      "additiveInputIDs": null,
      "outputID": "SubProcess1-Output_Y",
      "outputIdentID": null,
      "processModelType": 2,
      "comment": null,
      "x": null,
      "y": null,
      "color": null,
      "ID": "SubProcess1"
    },
    "PID1": {
      "$type": "TimeSeriesAnalysis.Dynamic.PidModel, TimeSeriesAnalysis",
      "pidParameters": {
        "DelayOutputOneSample": false,
        "Kp": 0.5,
        "Ti_s": 20.0,
        "Td_s": 0.0,
        "Scaling": {
          "doesSasPidScaleKp": false,
          "y_min": 0.0,
          "y_max": 100.0,
          "u_min": 0.0,
          "u_max": 100.0,
          "isDefault": true,
          "isEstimated": false
        },
        "Filtering": {
          "TimeConstant_s": 0.0,
          "FilterOrder": 0,
          "IsEnabled": false
        },
        "GainScheduling": {
          "GSActive_b": false,
          "GS_x_Min": 0.0,
          "GS_x_1": 0.0,
          "GS_x_2": 0.0,
          "GS_x_Max": 0.0,
          "GS_Kp_Min": 0.0,
          "GS_Kp_1": 0.0,
          "GS_Kp_2": 0.0,
          "GS_Kp_Max": 0.0,
          "GSActiveTi_b": false,
          "GS_Ti_Min": 0.0,
          "GS_Ti_1": 0.0,
          "GS_Ti_2": 0.0,
          "GS_Ti_Max": 0.0
        },
        "FeedForward": {
          "isFFActive": false,
          "FFHP_filter_order": 1,
          "FFLP_filter_order": 1,
          "FF_LP_Tc_s": 0.0,
          "FF_HP_Tc_s": 0.0,
          "FF_Gain": 0.0
        },
        "AntiSurgeParams": null,
        "u0": 50.0,
        "NanValue": -9999.0,
        "Fitting": null
      },
      "ModelInputIDs": [
        "SubProcess2-Output_Y",
        "PID1-Setpoint_Yset"
      ],
      "additiveInputIDs": null,
      "outputID": "PID1-PID_U",
      "outputIdentID": null,
      "processModelType": 1,
      "comment": null,
      "x": null,
      "y": null,
      "color": null,
      "ID": "PID1"
    },
    "SubProcess2": {
      "$type": "TimeSeriesAnalysis.Dynamic.UnitModel, TimeSeriesAnalysis",
      "modelParameters": {
        "Y_min": "NaN",
        "Y_max": "NaN",
        "FittingSpecs": {
          "u0": null,
          "uNorm": null,
          "Y_min_fit": null,
          "Y_max_fit": null,
          "U_min_fit": null,
          "U_max_fit": null
        },
        "LinearGains": [
          1.1,
          0.6
        ],
        "LinearGainUnc": null,
        "Curvatures": null,
        "CurvatureUnc": null,
        "TimeConstant_s": 20.0,
        "TimeConstantUnc_s": null,
        "TimeDelay_s": 10.0,
        "DampingRatio": 0.0,
        "Bias": 5.0,
        "BiasUnc": null,
        "U0": null,
        "UNorm": null,
        "Fitting": null
      },
      "ModelInputIDs": [
        "SubProcess1-Output_Y",
        "SubProcess2-External_U-1"
      ],
      "additiveInputIDs": null,
      "outputID": "SubProcess2-Output_Y",
      "outputIdentID": null,
      "processModelType": 2,
      "comment": null,
      "x": null,
      "y": null,
      "color": null,
      "ID": "SubProcess2"
    },
    "SubProcess3": {
      "$type": "TimeSeriesAnalysis.Dynamic.UnitModel, TimeSeriesAnalysis",
      "modelParameters": {
        "Y_min": "NaN",
        "Y_max": "NaN",
        "FittingSpecs": {
          "u0": null,
          "uNorm": null,
          "Y_min_fit": null,
          "Y_max_fit": null,
          "U_min_fit": null,
          "U_max_fit": null
        },
        "LinearGains": [
          0.8,
          0.7
        ],
        "LinearGainUnc": null,
        "Curvatures": null,
        "CurvatureUnc": null,
        "TimeConstant_s": 20.0,
        "TimeConstantUnc_s": null,
        "TimeDelay_s": 10.0,
        "DampingRatio": 0.0,
        "Bias": 5.0,
        "BiasUnc": null,
        "U0": null,
        "UNorm": null,
        "Fitting": null
      },
      "ModelInputIDs": [
        "SubProcess1-Output_Y",
        "SubProcess3-External_U-1"
      ],
      "additiveInputIDs": null,
      "outputID": "SubProcess3-Output_Y",
      "outputIdentID": null,
      "processModelType": 2,
      "comment": null,
      "x": null,
      "y": null,
      "color": null,
      "ID": "SubProcess3"
    }
  },
  "externalInputSignalIDs": [
    "PID1-Setpoint_Yset",
    "SubProcess1-External_U",
    "SubProcess2-External_U-1",
    "SubProcess3-External_U-1"
  ],
  "connections": {
    "connections": []
  },
  "PlantFitScore": 0.0,
  "plantName": "DeserializedTest",
  "plantDescription": "a test",
  "date": "0001-01-01T00:00:00"
}
```
