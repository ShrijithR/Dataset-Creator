import pickle
import pandas as pd
import check_inconsistencies as chi

from function_definitions import (    
    gather_resumes_of_candidates,
    create_cleaned_dataset,
    add_resume_content_test_dataset,
    add_columns_test_dataset,
    create_json_dataset,
    merge_dataset,
    vertical_concat,
    rename_columns,
    calculate_accuracy,
    display_scores,
)

from dataset_details import (
    path_dict, 
    job_details_emt, 
    job_details_lia, 
    job_details_ma, 
    drop_column_list,
    model_details,
    )

# gather_resumes_of_candidates()

# with open (path_dict['datasets_output_path']+'missing_resumes.py', 'rb') as f:
    # missing_list = pickle.load(f)
# df_removed_missing = create_cleaned_dataset(missing_list)
# df_removed_missing.to_csv(path_dict['datasets_output_path']+'client_results_v1(removed_missing_resumes).csv', index=False)

test_dataset_dest =  path_dict['datasets_output_path_lia'] + 'training_data_v1.csv'
df_concat = add_resume_content_test_dataset()
df_concat.to_csv(test_dataset_dest, index=False)

df_concat = pd.read_csv(path_dict['datasets_output_path_lia'] + 'training_data_v1.csv')
print(chi.check_inconsistencies(df_concat, 'Candidate Name', 'Resume'))

df_added_columns = add_columns_test_dataset(df_concat)
df_added_columns.to_csv(path_dict['datasets_output_path_lia']+'training_dataset_v2(added_columns).csv')

src_path_json_selected = '../testing/files/input/lia_structured_training/selected/'
src_path_json_rejected = '../testing/files/input/lia_structured_training/rejected/'

json_df = create_json_dataset()
json_df.to_csv(path_dict['datasets_output_path_lia']+'json_dataset_training_v1.csv')

json_df = pd.read_csv(path_dict['datasets_output_path_lia']+'json_dataset_training_v1.csv')
print(chi.check_inconsistencies(json_df, 'resume_name', 'structured_resumes'))

"""
STEP 6:
MERGE JSON DATA WITH THE TEST DATASET
"""
df_added_columns = pd.read_csv(path_dict['datasets_output_path_lia']+'training_dataset_v2(added_columns).csv')

added_json_column = merge_dataset(
    df_added_columns, json_df, 'structured_resumes', 'StructuredResume', 'Candidate Name', 'resume_name'
    )

added_json_column.to_csv(path_dict['datasets_output_path_lia']+'training_dataset_v2(added_structured).csv')

df_added_json_column = pd.read_csv(path_dict['datasets_output_path_lia']+'training_dataset_v2(added_structured).csv')

df_emt = pd.read_csv(path_dict['datasets_output_path_emt']+'training_dataset_v2(added_structured).csv') 
df_ma = pd.read_csv(path_dict['datasets_output_path_ma']+'training_dataset_v2(added_structured).csv')
df_lia = pd.read_csv(path_dict['datasets_output_path_lia']+'training_dataset_v2(added_structured).csv')

df_training = vertical_concat([df_emt, df_ma, df_lia])
df_training.to_csv(path_dict['datasets_output_path'] + 'training_data_combined.csv')

"""
STEP --
CHECK INCONSISTENCIES
"""
print(df_added_json_column.shape)
print(chi.check_inconsistencies(df_added_json_column, 'StructuredResume', 'Candidate Name'))

"""
STEP 7:
RENAME COLUMNS, IF NECESSARY, ACCORDING TO THE REQUIREMENTS
"""
column_list_original = list(df_added_json_column.columns.values)
print(column_list_original)

column_list_updated = [
    'Unnamed: 0', 'Candidate Name', 'Resume', 'Reccomendation_by_Client', 'Generic', 'Education',
    'Mandatory', 'Skills', 'Job Order', 'Job Order Title', 'Good JO',
    'StructuredResume']

df_added_json_column_renamed = rename_columns(df_added_json_column, column_list_updated)
column_list_updated = list(df_added_json_column_renamed.columns.values)
print(column_list_updated)

df_added_json_column_renamed.to_csv(path_dict['datasets_output_path']+'test_dataset_v4(final_dataset).csv')

"""
STEP --
CHECK INCONSISTENCIES
"""
df_test_dataset = pd.read_csv(path_dict['datasets_output_path']+'test_dataset_v4(final_dataset).csv')
print(chi.check_inconsistencies(df_test_dataset, 'StructuredResume', 'Candidate Name'))

"""
STEP 8:
PRE-PROCESS DATASETS FOR TRAINING AND TESTING THE MODEL V2
"""

import pre_process_pipeline as ppp
training_dataset = pd.read_csv('datasets/input/dataset_train_nrn.csv')

train_vectorized_dataset, test_vectorized_dataset = ppp.preprocessing(training_dataset, df_test_dataset)

train_vectorized_dataset.to_csv(path_dict['datasets_output_path']+'pp_train_dataset_v1.csv')
test_vectorized_dataset.to_csv(path_dict['datasets_output_path']+'pp_test_dataset_v1.csv')


"""
STEP 9:
CALCULATE AND DISPLAY THE SCORES
"""
df_target = pd.read_csv(path_dict['datasets_output_path']+'test_dataset_v4(final_dataset).csv')

for model, filename in model_details.items():
    dataset_path = path_dict['datasets_output_path']+'predicted/'+filename
    df_predicted = pd.read_csv(dataset_path)
    score_accept, score_reject, score_total = calculate_accuracy(df_predicted['Predictions'], df_target['Reccomendation_by_Client']) 
    display_scores(model, score_accept, score_reject, score_total)
    
