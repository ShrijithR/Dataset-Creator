from resume_selector import TestDataset, ResultDataset
import os, shutil

create_pdf = TestDataset()
data_path = 'Q:/Thinkbridge/OneDrive - Thinkbridge/Projects/JO Parser/Implementation_ml_model/test-dataset-creator/datasets/input/results_from_client.xlsx'
resume = ResultDataset(data_path)

src_path =  '../testing_files/resume_collection_covclin/files_experiment/'
word_list = ['EMT', 'CPR', 'emt', 'cpr']

def check_keywords(word_list, file_dest):
    word_list = word_list    
    selected_resumes = []
    filepaths  = [os.path.join(file_dest, name) for name in os.listdir(file_dest)]

    for path in filepaths:
        content=create_pdf.pdf_text_extracter(path)
        if any(words in content for words in word_list):
            selected_resumes.append(path)

    return selected_resumes

selected_resumes = check_keywords(word_list, src_path)
print(selected_resumes)
dest_path = '../testing_files/resume_collection_covclin/selected'
for resumes in selected_resumes:
    shutil.copy(resumes, dest_path)
