import pandas as pd

def convert_columns(dataframe, src_column, src_value_yes, src_value_no, dest_column):
    df = dataframe
    df[dest_column] = ''
    df.loc[df[src_column] == src_value_no, dest_column] = 0
    df.loc[df[src_column] == src_value_yes, dest_column] = 1
    return df

def check_congruence(column_a, column_b):
    accept_congruence_list = list((column_a == 1) & (column_b == 1))
    reject_congruence_list = list((column_a == 0) & (column_b == 0))
    total_congruence_list = list(column_a == column_b)
    return accept_congruence_list, reject_congruence_list, total_congruence_list

def convert_format_list_values(l: list):
    l = [1 if s is True else 0 for s in l]
    return l 

from collections import Counter

def calculate_score(target_list, accept_congruence_list, reject_congruence_list, total_congruence_list):
    target_dict = Counter(target_list)
    accept_dict = Counter(accept_congruence_list)
    reject_dict = Counter(reject_congruence_list)
    total_dict = Counter(total_congruence_list)
    score_accept = accept_dict[1]/target_dict[1]
    score_reject = reject_dict[1]/target_dict[0]
    score_total = total_dict[1]/len(target_list)

    return score_accept, score_reject, score_total
    # return accept_dict[1], reject_dict[1], total_dict[1], target_dict[1], target_dict[0]
