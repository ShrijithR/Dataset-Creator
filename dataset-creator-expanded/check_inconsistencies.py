def check_inconsistencies(dataframe, column_to_check, column_to_display):
    df = dataframe
    df_resume_content = df[column_to_check]
    df_resume_content_dup = df[df_resume_content.duplicated(keep=False)]
    df_resume_content_empty = df[df_resume_content == '']
    df_resume_content_dup = list(df_resume_content_dup[column_to_display])
    df_resume_content_empty = list(df_resume_content_empty[column_to_display])
    if not any([df_resume_content_empty, df_resume_content_dup]):
        return "No inconsistencies"
    else:
        return [i for i in [df_resume_content_dup, df_resume_content_empty] if i]
