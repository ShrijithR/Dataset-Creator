import numpy as np
import pandas as pd
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.naive_bayes import MultinomialNB
from sklearn.pipeline import make_pipeline
from sklearn.metrics import confusion_matrix, accuracy_score
from sklearn.feature_extraction.text import CountVectorizer
from sklearn.feature_extraction.text import TfidfTransformer

import json
import spacy
import pickle
import random
import pandas as pd
import os

print('All dependencies were installed')

def convert_list_to_string(keywords_list):
    # str = ''
    return (' '.join([str(e) for e in keywords_list]))

def flatten_list(lists_of_lists):

    temp = []
    
    for element in lists_of_lists:
        if type(element) is list :
            for secondary_layer_elements in element:
                temp.append(secondary_layer_elements)
    
    return temp

def extract_fields_JO(reference_data):
    Jo_row = []
    row = []
    
    for index,fields in reference_data.iterrows():
        row.append(fields['Generic'])
        row.append(fields['Education'])
        row.append(fields['Mandatory'])
        row.append(fields['Skills'])
        row.append(fields['Reccomendation_by_Client'])
       
        Jo_row.append(row)
        row = []
    return(Jo_row)

def extract_fields_resume(reference_data):
    Resume_row = []
    row = []
    
    structured_resume = []
    row_data = []
    temp = []
    skill_names = []

    neglect_index = []

    for index,fields in reference_data.iterrows():
        structured_resume = fields['StructuredResume']
       
        try :
            data = json.loads(structured_resume) # converts string to dict 
        except Exception:
            neglect_index.append(index)
            continue

        ## Skills Extraction :

        if 'Competency' in data['Resume']['StructuredResume'] : 
            competency_dict = data['Resume']['StructuredResume']['Competency']
        else : 
            competency_dict = {}
                
        for value in competency_dict:
            for element in value.items():
                if element[0] == 'skillAliasArray' :
                    temp.append(element[1])
        
        for member in temp:
            skill_names.append(member) 
            
        temp = []
        test = flatten_list(skill_names)
        # print('')
        str_skills_record = convert_list_to_string(test)
        skill_names = []
        
        ## Description Extraction :

        if 'EmploymentHistory' in data['Resume']['StructuredResume'] :
            employement_details_dict = data['Resume']['StructuredResume']['EmploymentHistory']
        else : 
            employement_details_dict = {}
        
        description_temp = []

        for element in employement_details_dict:
            description_list = element.get('Description','no description')       
            description_temp.append(description_list)
            

        # print(description_temp)
        description_str_row = convert_list_to_string(description_temp)
       
        ## Job role Extraction :

        roles_temp = []
        for element in employement_details_dict :
            
            roles_list = element.get('Title','No_Title')
            # print(roles_list)
            for role in roles_list:
                roles_temp.append(role)
                                       
        roles_str_row = convert_list_to_string(roles_temp)
        
        ## Education extraction :

        education_temp = []

        if 'EducationHistory' in data['Resume']['StructuredResume'] :
            education_details_dict = data['Resume']['StructuredResume']['EducationHistory']
        else : 
            education_details_dict = {}

        for element in education_details_dict:
            degree_type = element.get('Degree','None')
            if degree_type != 'None':
                degree_type = degree_type.get('degreeType','aaa')
                if degree_type == 'aaa' :
                    degree_type = element.get('schoolType')
                
                education_temp.append(degree_type)
            else :
                continue
        
        degree_str_row = convert_list_to_string(education_temp)
        temp.append(str_skills_record)
        temp.append(description_str_row)   
        temp.append(roles_str_row)    
        temp.append(degree_str_row) 
        # temp.append()
        # temp.append(flag)   

        row_data.append(temp)
        temp = []

    df_resume = pd.DataFrame(row_data , columns= ['skills_resume','description_resume','job_tile_resume','education_resume'])

    return df_resume , neglect_index

def create_dataframe_JO(Jo_data , neglect_index):
    # print(Jo_data)
    df_JO = pd.DataFrame(Jo_data , columns= ['generic_JO','education_JO','mandatory_JO','skills_JO','reccomendation_by_Client'])
    
    # print(neglect_index)

    df_JO = df_JO.drop(neglect_index)
    
    print(df_JO.shape)
    
    return df_JO

def create_combined_dataframe(df_Jo, df_Resume):

    df_combined = pd.concat([df_Jo, df_Resume], axis=1)

    print('the combined Dataframe is printed below :')
    print('')
    print(df_combined.head())
    
    # df[df.isna().any(axis=1)]

    # df_nan = df_combined[df_combined.isna().any(axis=1)]
    # print(df_nan.shape)
    
    df_combined = df_combined.dropna()
    df_combined = df_combined.reset_index(drop=True)

    print('the dataframe after removing all nans ' ,df_combined.shape)

    return df_combined

def vectorising_combined_df(combined_df, combined_df_test) :
    # Manually vectorising every column in the dataframe :
    
    #generic_JO
    cvec = CountVectorizer(stop_words = 'english')
    
    X = combined_df.generic_JO
    cvec_a = cvec.fit_transform(X)
    generic_JO_vec = pd.DataFrame(cvec_a.todense(), columns=cvec.get_feature_names()) 
    
    test = combined_df_test.generic_JO
    cvec_test = cvec.transform(test)
    generic_JO_vec_test = pd.DataFrame(cvec_test.todense(), columns = cvec.get_feature_names())
   
    #education_JO
    cvec = CountVectorizer(stop_words = 'english')
    
    X = combined_df.education_JO
    cvec_a = cvec.fit_transform(X)
    education_JO_vec = pd.DataFrame(cvec_a.todense(), columns = cvec.get_feature_names())

    test = combined_df_test.education_JO
    cvec_test = cvec.transform(test)
    education_JO_vec_test = pd.DataFrame(cvec_test.todense(), columns = cvec.get_feature_names())

    #mandatory_JO
    cvec = CountVectorizer(stop_words = 'english')
    
    X = combined_df.mandatory_JO
    cvec_a = cvec.fit_transform(X)
    mandatory_JO_vec = pd.DataFrame(cvec_a.todense(), columns = cvec.get_feature_names())

    test = combined_df_test.mandatory_JO
    cvec_test = cvec.transform(test)
    mandatory_JO_vec_test = pd.DataFrame(cvec_test.todense(), columns = cvec.get_feature_names())

    #skills_JO
    cvec = CountVectorizer(stop_words = 'english')
    
    X = combined_df.skills_JO
    cvec_a = cvec.fit_transform(X)
    skills_JO_vec = pd.DataFrame(cvec_a.todense(), columns = cvec.get_feature_names())

    test = combined_df_test.skills_JO
    cvec_test = cvec.transform(test)
    skills_JO_vec_test = pd.DataFrame(cvec_test.todense(), columns = cvec.get_feature_names())

    #skills_resume 
    cvec = CountVectorizer(stop_words = 'english')
    
    X = combined_df.skills_resume
    cvec_a = cvec.fit_transform(X)
    skills_resume_vec = pd.DataFrame(cvec_a.todense(), columns = cvec.get_feature_names())

    test = combined_df_test.skills_resume
    cvec_test = cvec.transform(test)
    skills_resume_vec_test = pd.DataFrame(cvec_test.todense(), columns = cvec.get_feature_names())

    #description_resume
    cvec = CountVectorizer(stop_words = 'english')
    
    X = combined_df.description_resume
    cvec_a = cvec.fit_transform(X)
    description_resume_vec = pd.DataFrame(cvec_a.todense(), columns = cvec.get_feature_names())

    test = combined_df_test.description_resume
    cvec_test = cvec.transform(test)
    description_resume_vec_test = pd.DataFrame(cvec_test.todense(), columns = cvec.get_feature_names())

    # job_tile_resume
    cvec = CountVectorizer(stop_words = 'english')
    
    X = combined_df.job_tile_resume
    cvec_a = cvec.fit_transform(X)
    job_tile_resume_vec = pd.DataFrame(cvec_a.todense(), columns = cvec.get_feature_names())

    test = combined_df_test.job_tile_resume
    cvec_test = cvec.transform(test)
    job_tile_resume_vec_test = pd.DataFrame(cvec_test.todense(), columns = cvec.get_feature_names())

    #education_resume
    cvec = CountVectorizer(stop_words = 'english')
    
    X = combined_df.education_resume
    cvec_a = cvec.fit_transform(X)
    education_resume_vec = pd.DataFrame(cvec_a.todense(), columns = cvec.get_feature_names())

    test = combined_df_test.education_resume
    cvec_test = cvec.transform(test)
    education_resume_vec_test = pd.DataFrame(cvec_test.todense(), columns = cvec.get_feature_names())

    df_combined_vec = pd.concat([generic_JO_vec,education_JO_vec,mandatory_JO_vec,skills_JO_vec,skills_resume_vec,description_resume_vec,job_tile_resume_vec,education_resume_vec, combined_df['reccomendation_by_Client']], axis=1)
    df_combined_vec_test = pd.concat([generic_JO_vec,education_JO_vec_test,mandatory_JO_vec_test,skills_JO_vec_test,skills_resume_vec_test,description_resume_vec_test,job_tile_resume_vec_test,education_resume_vec_test], axis=1)
    
    print('The shape of the combined vectorised dataframe is :', df_combined_vec.shape)
    print('The shape of the combined vectorised test dataframe is :', df_combined_vec_test.shape)
    return df_combined_vec, df_combined_vec_test

def preprocessing(training_dataset, test_dataset):
    reference_data_test = pd.read_csv(test_dataset)  # Extract data from excel.
    reference_data = pd.read_csv(training_dataset)  # Extract data from excel.
    
    Jo_data = extract_fields_JO(reference_data)
    Jo_data_test = extract_fields_JO(reference_data_test)

    df_resume , neglect_index = extract_fields_resume(reference_data)
    df_resume_test , neglect_index_test = extract_fields_resume(reference_data_test)    
    
    df_JO = create_dataframe_JO(Jo_data,neglect_index)
    df_JO_test = create_dataframe_JO(Jo_data_test,neglect_index_test)

    print('The dimensions of JO df is :',df_JO.shape)
    print('The dimensions of Resume df is :', df_resume.shape)

    print('The dimensions of JO df test is :',df_JO_test.shape)
    print('The dimensions of Resume df tes is :', df_resume_test.shape)
    
    df_combined = create_combined_dataframe(df_JO,df_resume)
    df_combined_test = create_combined_dataframe(df_JO_test,df_resume_test)
    
    print(df_combined.columns)
    print(df_combined_test.columns)
    ## Convert Jo data into a dataframe.

    df_combined_vec, df_combined_vec_test = vectorising_combined_df(df_combined, df_combined_test)
    return df_combined_vec, df_combined_vec_test
    # df_combined.to_excel('pre_prosd.xlsx')
    # df_combined_vec.to_csv('data/output/pre_prosd_train_v1.csv')
    # df_combined_vec_test.to_csv('data/output/pre_prosd_test_v3.csv')
    
# test = preprocessing()