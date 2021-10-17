# Test Dataset Converter

## Objective
_To calculate the accuracy of the ML model to predict candidate result._

## To do
_Create a test dataset with the appropriate columns required for the ML model and use the model to predict the results. 
Compare these results with the client results for the candidates and calculate the accuracy score. 
Steps for creating test data set_

<img src="https://i.imgur.com/Z0DfUyr.png" alt="drawing" height="300"/>

## Getting started
1. Gather the resumes and store it in local storage.
2. Create the folder/ file structure as directed and mention the specific paths in the main module.
3. Follow the steps mentioned in the doc strings. 

## Folder structure
```
Root/
│   check_inconsistencies.py
│   main.py
│   pre_process_pipeline.py
│   README.md
│   requirements.txt
│   resume_selector.py
│   score_calc.py
├───datasets
│   ├───input
│   │       results_from_client.xlsx
│   └───output
│       │   missing_resumes.py
│       └───predicted
├───files
│   ├───input
│   │   ├───structured_rejected
│   │   └───structured_selected
│   └───output
│       ├───rejected
│       └───selected
```
### _Have fun testing!_
