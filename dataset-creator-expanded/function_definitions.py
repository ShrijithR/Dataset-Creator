from pandas.core.frame import DataFrame
import pandas as pd
import pickle

from resume_selector import ResultDataset, TestDataset
import score_calc as sc
from dataset_details import (
    path_dict, 
    job_details_emt, 
    job_details_lia, 
    job_details_ma, 
    drop_column_list
    )

resume = ResultDataset(path_dict['data_path'])
test_data = TestDataset()

def gather_resumes_of_candidates():
    resume.pre_process_src_file(drop_column_list)

    df_pass, df_fail = resume.separate_pass_fail()

    # df_pass.to_excel(datasets_output_path+'selected_cand.xlsx')
    # df_fail.to_excel(datasets_output_path+'rejected_cand.xlsx')

    sel_cand = resume.prepare_regex(df_pass)
    rej_cand = resume.prepare_regex(df_fail)

    selected_files, missing_sel_list_success = resume.get_files(
        sel_cand, path_dict['src_path'], path_dict['dest_path_sel']
        )
    rejected_files, missing_rej_list_success = resume.get_files(
        rej_cand, path_dict['src_path'], path_dict['dest_path_rej']
    )

    # resume.copy_file(selected_files)
    # resume.copy_file(rejected_files)

    missing_list = missing_sel_list_success + missing_rej_list_success 
    with open(path_dict['datasets_output_path']+'missing_resumes.py', 'wb') as f:
        pickle.dump(missing_list, f)

def create_cleaned_dataset(missing_list):
    df_removed_missing = resume.remove_missing(resume.df, missing_list)
    return df_removed_missing

def add_resume_content_test_dataset():
    df_accept = test_data.create_pdf_df(path_dict['dest_path_sel'], 1, 'Candidate Name', 'Resume')
    df_reject = test_data.create_pdf_df(path_dict['dest_path_rej'], 0, 'Candidate Name', 'Resume')
    df_concat = test_data.concat_df(df_accept, df_reject)
    print(df_concat.shape)
    return df_concat

def add_columns_test_dataset(df_concat):
    df_added_columns = test_data.create_columns(df_concat, path_dict['labels_dict'], path_dict['job_details_dict'])
    df_added_columns.sort_values('Candidate Name')
    df_added_columns.sort_values('target')  
    df_added_columns = df_added_columns.loc[:, ~df_added_columns.columns.str.contains('^Unnamed')]
    print(df_added_columns.columns, df_added_columns.shape)    
    return df_added_columns

def create_json_dataset():
    # json_df = test_data.json_df_creator(src_path_json_selected, src_path_json_rejected)
    df_selected = test_data.json_text_extracter(path_dict['src_path_json_selected'])
    df_rejected = test_data.json_text_extracter(path_dict['src_path_json_rejected'])

    json_df = pd.concat([df_selected, df_rejected], 0)   
    print("Completed")
    return json_df

def merge_dataset(self, df_src, df_dest, col_src, col_dest, col_map, col_index):    
    # print(f"Merging datasets {df_src} and {df_dest}")
    df_src[col_dest] = ''
    df_src[col_dest] = df_src[col_map].map(df_dest.set_index(col_index)[col_src])
    df_merged = df_src.loc[:, ~df_src.columns.str.contains('^Unnamed')]
    return df_merged

def vertical_concat(df_list: list[DataFrame]):
    df_concat = pd.concat(df_list, 0)   
    df_concat.sort_values('Candidate Name')
    df_concat.sort_values('target')  
    df_concat = df_concat.loc[:, ~df_concat.columns.str.contains('^Unnamed')]
    return df_concat

def rename_columns(self, dataframe_src, column_list_updated):
    dataframe_src.columns = column_list_updated
    dataframe_src = dataframe_src.loc[:, ~dataframe_src.columns.str.contains('^Unnamed')]
    return dataframe_src

def calculate_accuracy(df_predicted_column, df_target_column):    
    accept_congruence_list, reject_congruence_list, total_congruence_list = sc.check_congruence(df_target_column, df_predicted_column)
    accept_congruence_list = sc.convert_format_list_values(accept_congruence_list)
    reject_congruence_list = sc.convert_format_list_values(reject_congruence_list)
    total_congruence_list = sc.convert_format_list_values(total_congruence_list)
    target_list = list(df_target_column)
    # score_accept, score_reject, score_total, total_accept, total_reject  = sc.calculate_score(
    score_accept, score_reject, score_total = sc.calculate_score(        
    target_list, accept_congruence_list, reject_congruence_list, total_congruence_list
    )
    return score_accept, score_reject, score_total

def display_scores(model, score_total, score_accept, score_reject):
    print(f"{model} - 'Total: '{round(score_total, 2)}'Accept: ' {round(score_accept, 2)}'Reject: ' {round(score_reject, 2)}")


