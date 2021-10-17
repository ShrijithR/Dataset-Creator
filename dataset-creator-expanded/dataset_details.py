
path_dict = {
    'data_path': "datasets/input/results_from_client.xlsx",
    'dest_path_sel': '../testing/files/output/lia_candidates/selected/',
    'dest_path_rej': '../testing/files/output/lia_candidates/rejected/',
    'datasets_output_path_emt': '../testing/datasets/output_emt/',
    'datasets_output_path_ma': '../testing/datasets/output_ma/',
    'datasets_output_path_lia': '../testing/datasets/output_lia/',
    'datasets_output_path': '../testing/datasets/output/',
    'src_path': 'Q:/Thinkbridge/OneDrive - Thinkbridge/Projects/JO Parser/Implementation_ml_model/jo_resume_selector/Data input/resume_success_collection_blob/'
}

drop_column_list = ['AccountName', 'JO Title', 'RankingScore', 'Reject Reason', 'Date']

labels = {
    "Generic":
    "customer service', 'Customer Service Representative', 'inbound', 'outbound', 'customer care', 'customer service', 'incoming calls', 'customers', 'inbound calls', 'customer calls'",
    "Education":
    "High School Diploma ",
    "Mandatory":
    "1-2 years of Customer Service",
    "Skills":
    "troubleshoot', 'Microsoft Office', 'Excel', 'Powerpoint', 'Outlook', 'Word', 'Data Entry', 'Sales'"
}

job_details_emt = {
    "Job Order":
    "Emergency Medical Technician",
    "Job Order Title":
    "Emergency Medical Technician",
    "Good JO": 
    """\
About Us:
COVID Clinic was founded by Dr. Matthew Abinante from Elevated Health in Huntington Beach, CA. We are a California non-profit public benefit corporation. There is a group of individuals dedicated to providing COVID-19 testing for all and we are excited to offer new work opportunities! Patient testing is a key component of care, and we take this role very seriously.
What we are looking for:
COVID Clinic is excited to offer new work opportunities! We are looking to hire an "Emergency Medical Technician (EMT)" to conduct Covid-19 Test.
Job Duties include but are not limited to:
Perform Covid-19 Testing (Nasal Swabs)
Assist Provider
Other Duties Assigned
Requirements/ Skills:
Associates degree in related field
Valid EMT Certification (EMT/ NREMT)
Current CPR Certification for Healthcare Workers
1+ year of experience, New Grads welcome
Highly organized and self-motivated
Ability to work independently on assigned projects
Ability to prioritize tasks based on importance and deadlines
Must be flexible and be able to adapt to a changing work environment
Ability to be discrete and maintain patient confidentiality
Exhibits excellent verbal and written communication via email and in-person
Job Types: Full-time, Part-time, Temporary
Pay: $18.00 per hour
COVID-19 considerations:
Disclaimer: You will be testing patients who may have contracted COVID-19. This means you WILL be at risk of being infected, however, we will provide all necessary PPE and training to avoid such events from occurring. Site locations are outside and we are open rain or shine!
Schedule:
8 hour shift
Monday to Friday
On call
Weekends
Education:
High school or equivalent (Preferred)
License/Certification:
BLS Certification (Preferred)
EMT Certification (Preferred)
NREMT (Preferred)
Work Location:
One location
Company's website:
covidclinic.org    
    """
    }   

job_details_ma = {
    "Job Order":
    "Medical Assistant",
    "Job Order Title":
    "Medical Assistant",
    "Good JO": 
    """\
Job Title: Medical Assistant
About Us:
COVID Clinic was founded by Dr. Matthew Abinante from Elevated Health in Huntington Beach, CA. We are a California non-profit public benefit corporation. There is a group of individuals dedicated to providing COVID-19 testing for all and we are excited to offer new work opportunities! Patient testing is a key component of care, and we take this role very seriously.
What we are looking for:
COVID Clinic is excited to offer new work opportunities! We are looking to hire a Medical Assistant to assist in a broad range of tasks including but not limited to:
Work on Sofia Machine (process results for COVID-19 test)
Works collaboratively with physicians/ Registered Nurse and other staff to ensure the patient’s needs are met
Maintain a clean and safe environment
Applicants MUST:
Have a current medical assistant license/certification
Be a healthy worker who is in the 'low risk' category of contracting COVID-19
Be available to work at least 3 days a week
Be able to stand for long periods of time
Wear comfortable, movable, and washable clothing. Scrubs are ideal!
Have reliable transportation
Be a team player!
Job Types: Full-time
Pay: $17.00 per hour
Schedule:
Day shift
Evening shift
Monday to Friday
Weekends
COVID-19 considerations:
Disclaimer: You will be testing patients who may have contracted COVID-19. This means you WILL be at risk of being infected, however, we will provide all necessary PPE and training to avoid such events from occurring. Site locations are outside and we are
Education:
High school or equivalent (Preferred)
License/Certification:
BLS Certification (Preferred)
Certified Medical Assistant (Preferred)
Work Location:
One location
Company's website:
covidclinic.org
Work Remotely:
No
    """
    }   

job_details_lia = {
    "Job Order":
    "Licensed Insurance Agent",
    "Job Order Title":
    "Licensed Insurance Agent",
    "Good JO": 
    """\
Now Hiring – Independent Medicare Agents! 
Work from Home and Build your Book of Business with us.
Earn 1099 Full Commission option
Connie Health is looking for experienced Independent Medicare Agents to drive growth in assigned market(s). The ideal candidate would be a self-starter with a strong Medicare background who can build relationships and earn trust with potential members and the community. Your primary role includes meeting with individuals who need your expertise in selecting a plan that best meets their needs. 
Connie’s platform combines technology with local healthcare expertise to help consumers choose the right insurance plans, find the best doctors, and make the right healthcare choices. Our model is based on local agents, ongoing support and is built on personal relationships and trust. Our free advisory services are honest and our business model supports this. Imagine yourself as part of a team that truly cares about a mission to improve how older Americans navigate the Medicare healthcare system. 
Why work with Connie? 
Increase Your Earnings Working with Connie can significantly increase your income. With Connie you will receive full commission and renewals on any business you generate. In addition, Connie will schedule qualified appointments with consumers ready to purchase. For those Connie generated appointments you will receive a competitive flat fee, and yearly renewal as long as the customer is a Connie member. Access to Technology All of our agents receive an iPad and access to the Connie Health Agent Platform. Our proprietary platform features a recommendation engine and quoting tools that will make selling easier. You will be able to focus on building a relationship with your customers, as you provide speedy, invaluable service with a click of a button. 
The Benefits and Perks
● Receive Qualified appointments 
● Proprietary technology to increase productivity 
● Ongoing training and support 
● Company provided iPad and Phone
● Lucrative payment options 
● Internal customer service team 
● Mission driven organization with a great team
Requirements 
● 1-3 years minimum Medicare sales experience 
● Strong proficiency with Medicare Communications and Marketing Guidelines (MCMG) 
● Must live in their territory and have deep knowledge of the local healthcare landscape 
● Comfortable with flexible work schedules, especially in AEP 
● Passionate about improving healthcare in America 
● Entrepreneurial, go getter that works independently with minimal oversight 
● Must have active Health and Life license and valid driver's license 
● Spanish/English Bilingual a plus but not required
    """
}

model_details = {
    'Stacking NRN': 'test_Predictions_NRN_Stacking_v1.csv',
    'LGBM NRN': 'test_Predictions_NRN_LGBM_v1.csv',
    'LGBM PCM': 'test_predictions_LGBM_v1.csv',
    'Stacking PCM': 'test_predictions_Stacking_v1.csv'
}

