import pandas as pd
import os, shutil
import re
from pandas.core.frame import DataFrame

class EmptyResumeError(Exception):
    pass

class ResultDataset:
    def __init__(self, results_file: str) -> None:
        self.df = pd.read_excel(results_file)
    
    def pre_process_src_file(self, drop_column_list: list) -> DataFrame:
        print('Pre-processing...')        
        self.df = self.df.drop(columns=drop_column_list)
        self.df = self.df.drop_duplicates()
        self.df = self.df.sort_values(['CandidateName', 'Recruiter Feedback'])

    def separate_pass_fail(self) -> tuple[DataFrame]:
        print('Separating dataframe into pass and reject...')
        df_pass = self.df.loc[self.df['Recruiter Feedback'] == 'Pass']
        df_pass = df_pass.sort_values(by='CandidateName')

        df_fail = self.df.loc[self.df['Recruiter Feedback'] == 'Reject']
        df_fail = df_fail.sort_values(by='CandidateName')
        
        return df_pass, df_fail

    def prepare_regex(self, df:DataFrame) -> list[list[str]]:
        print('preparing regex')
        candidates = df['CandidateName']
        candidates = [cand.split() for cand in candidates]
        return candidates

    def get_files(self, cand_type: list[list[str]], src_path, dest_path) -> tuple[list[str]]:
        print('getting file list...')
        file_path_list = []
        files_missing_list = []
        os.makedirs(dest_path, exist_ok=True)
        
        for cand in cand_type:
            missing_flag = True
            filename_re = re.compile(f"""
            ([^0-9a-zA-Z])*?
            ({cand[0]}|{cand[1]})
            ([^0-9a-zA-Z])*?
            ({cand[1]}|{cand[0]})
            ([^0-9a-zA-Z])*?
            .pdf
            """, re.IGNORECASE | re.MULTILINE | re.VERBOSE)

            output_file_name = f'{cand[0].lower().capitalize()} {cand[1].lower().capitalize()}.pdf'
            for file in os.listdir(src_path):
                if filename_re.match(file):
                    file_path_list.append((src_path+file, dest_path+output_file_name))
                    missing_flag = False    
                    break
            if missing_flag:
                files_missing_list.append(output_file_name)
        
        return file_path_list, files_missing_list

    def copy_file(self, src_dest_path: list[tuple[str, str]]):
        print('copying files...')
        for cand in src_dest_path:
            shutil.copy(*cand)

    def remove_missing(self, data_frame, cand_list: list[str]):
        df = data_frame
        
        cand_list = [x.lower()[:-4] for x in cand_list]
        print("The total number of missing candidates is:", len(cand_list))
        # filter = df['CandidateName'].str.contains(can)
        filter = df['CandidateName'].str.lower().isin(cand_list)
        df = df[~filter]
        self.df = df
        return self.df


from pdfminer.pdfinterp import PDFResourceManager, PDFPageInterpreter
from pdfminer.converter import TextConverter
from pdfminer.layout import LAParams
from pdfminer.pdfpage import PDFPage
from io import StringIO
import os
import glob
import argparse
import pandas as pd

from io import StringIO
import os
import glob
import argparse
import pandas as pd
import json

class TestDataset:
    def __init__(self) -> None:
        self.corrupted_file_list = []

    def pdf_text_extracter(self, path):
        rsrcmgr = PDFResourceManager()
        retstr = StringIO()
        laparams = LAParams()
        device = TextConverter(rsrcmgr, retstr, laparams=laparams)
        fp = open(path, 'rb')
        interpreter = PDFPageInterpreter(rsrcmgr, device)
        password = ""
        maxpages = 0
        caching = True
        pagenos=set()

        for page in PDFPage.get_pages(fp, pagenos, maxpages=maxpages, password=password,caching=caching, check_extractable=True):
            interpreter.process_page(page)
        
        text = retstr.getvalue()

        fp.close()
        device.close()
        retstr.close()
        return text

    def create_pdf_df(self, src_path, target: int, col_a, col_b):
        print(f'creating pdf dataframe for {src_path}')
        df=pd.DataFrame(columns=[col_a,col_b])
        filepaths  = [os.path.join(src_path, name) for name in os.listdir(src_path)]
        for i,path in enumerate(filepaths):
            if '.pdf' in path:
                try:
                    text=self.pdf_text_extracter(path)
                    if text == '':
                        raise EmptyResumeError
                    pdf_name=os.path.basename(path)
                    pdf_name = pdf_name.rsplit('.',1)[0]
                    pdf_name = ' '.join(pdf_name.split('_'))
                    pdf_name = pdf_name.lower().title()
                    pdf_name = '_'.join(pdf_name.split())

                    df.loc[i,:]=[pdf_name,text]
                
                except EmptyResumeError:
                    self.corrupted_file_list.append(os.path.basename(path))
                    print(os.path.basename(path) + ' has empty content')
                
                except Exception as e:
                    print(e, 'for the file', os.path.basename(path))
                    self.corrupted_file_list.append(os.path.basename(path))

        df['target']= int(target)
        return df

    def concat_df(self, file1, file2):
        df1=pd.read_csv(file1)
        df2=pd.read_csv(file2)
        test_df=pd.concat([df1,df2],0)
        test_df.sort_values('Candidate Name')
        test_df.sort_values('target')
        return test_df

    def create_columns(self, dataframe, *columns_dicts):
        print("Creating columns...")
        df = dataframe
        column_title_list = list(columns_dicts)
        for title_dict in column_title_list:
            for keys, values in title_dict.items():
                df[keys] = values
        print("Completed")
        return df

    def json_text_extracter(self, src_path_json):
        print(f"Extracting JSON text for {src_path_json}")
        df=pd.DataFrame(columns=['resume_name','structured_resumes'])
        json_files = [pos_json for pos_json in os.listdir(src_path_json) if pos_json.endswith('.json')]
        for index, js in enumerate(json_files):
            with open(os.path.join(src_path_json, js)) as json_file:
                try:
                    json_text = json.load(json_file)
                    json_dump = json.dumps(json_text) #use this after loading
                    resume_name = os.path.basename(js)
                    resume_name = resume_name.rsplit('.',1)[0]
                    resume_name = ' '.join(resume_name.split('_'))
                    resume_name = resume_name.lower().title()
                    resume_name = '_'.join(resume_name.split())
                    df.loc[index,:]=[resume_name,json_dump]
                except Exception as e:
                    print(e)
        df.sort_values('resume_name')
        return df

    def json_df_creator(self, src_path_json_selected, src_path_json_rejected):
        print("Creating JSON dataset")

