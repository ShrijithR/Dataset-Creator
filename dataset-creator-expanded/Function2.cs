using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using RestSharp;
using Azure.Storage.Blobs;
using System.Linq;
using System.Text;

namespace DaxtraAPIJoParser
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //fetch details from trigger body 
            // GetAllFiles();
            GetSelectedFiles();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string filename = null ?? data?.filename;
            string from = null ?? data?.from;
            string inbox = null ?? data?.inbox;
            string subject = null ?? data?.subject;
            string domainName = string.Empty;
            bool isCandidateAddedSuccessfully = false;
            CompassBody compassbody = new CompassBody();
            string resumeJsonResponse = string.Empty;
            bool isCandidateRejectedByTheRankingAlgorithm = false;

            try
            {
                string identifierToSeparateSubjectFromOriginalFrom = "Original From EmailId:";
                int requiredLengthToSplitTheRequiredAttributes = subject.IndexOf("Original From EmailId:") + identifierToSeparateSubjectFromOriginalFrom.Length;

                string originalSubject = subject.Substring(0, subject.IndexOf(identifierToSeparateSubjectFromOriginalFrom));
                string originalFromAccount = subject.Substring(requiredLengthToSplitTheRequiredAttributes, (subject.Length - requiredLengthToSplitTheRequiredAttributes));

                string identifierToSeparateFrom = "Original Account:";
                string originalFrom = originalFromAccount.Substring(0, originalFromAccount.IndexOf(identifierToSeparateFrom));

                if (!string.IsNullOrEmpty(originalFrom))
                {
                    string[] emailDomain = originalFrom.Split('@');

                    if (emailDomain != null && emailDomain.Count() > 1)
                    {
                        string[] requiredEmailPart = emailDomain[1].Split('.');

                        if (requiredEmailPart != null && requiredEmailPart.Count() > 0)
                        {
                            domainName = requiredEmailPart[0];
                        }
                    }
                }

                // extract the file type details 
                string applicationtype;
                string fileExtension = string.Empty;

                string[] requiredfileExtensions = filename.Split(".");

                if (requiredfileExtensions != null && requiredfileExtensions.Count() > 0)
                {
                    fileExtension = requiredfileExtensions[requiredfileExtensions.Length - 1];
                }

                if (fileExtension == "pdf")
                {
                    applicationtype = "application/pdf";
                }
                else
                {
                    applicationtype = "application/msword";
                }

                //fetch from blob 
                MemoryStream resume = Get_resume(filename);
                String file = Convert.ToBase64String(resume.ToArray());

                string jobordertitle = string.Empty;
                var subjectRequiredSections = originalSubject.Split("for");

                if (null != subjectRequiredSections && subjectRequiredSections.Count() > 0)
                {
                    jobordertitle = subjectRequiredSections.LastOrDefault();
                    jobordertitle = jobordertitle.Trim();
                    jobordertitle = jobordertitle.Trim(new char[] { '\'', '"' });
                }
                JoDetails jodetails = fetch_details(jobordertitle, inbox, domainName);

                // call daxtra with the resume details 
                Root struct_resume = Daxtra_api(file, out resumeJsonResponse);
                resumeJsonResponse = resumeJsonResponse.Replace("\n", "");

                // map to compass body 

                ResumeRankerResponse resumeRankerResponse = null;

                if (!string.IsNullOrEmpty(jodetails.JobDescription))
                {
                    RequiredFieldToGetTheRannkingScore requiredFieldsFromCandidateProfile = new RequiredFieldToGetTheRannkingScore();
                    requiredFieldsFromCandidateProfile.jobDescription = jodetails.JobDescription;//jodetails.JobDescription;
                    requiredFieldsFromCandidateProfile.resumeJsonResponse = resumeJsonResponse;//JsonConvert.SerializeObject(resumeJsonResponse);
                    resumeRankerResponse = GetRankingScoreForCandidateProfile(requiredFieldsFromCandidateProfile);

                }

                //if (string.IsNullOrEmpty(jodetails.JobDescription) || resumeRankerResponse == null || resumeRankerResponse.recommendation.ToLower().Contains(RequiredConstants.AcceptResponse.ToLower()))
                //{
                //call compass api  to fetch results 

                compassbody.AccountId = jodetails.AccountId;
                compassbody.JobOrderId = jodetails.JoborderId;
                compassbody.Source = jodetails.CandidateSource;
                compassbody.Comments = jobordertitle;
                compassbody.FirstName = struct_resume.Resume.StructuredResume.PersonName.GivenName;
                compassbody.LastName = struct_resume.Resume.StructuredResume.PersonName.FamilyName;
                compassbody.PhoneNumber1 = !string.IsNullOrEmpty(struct_resume.Resume.StructuredResume.ContactMethod.Telephone_mobile) ? struct_resume.Resume.StructuredResume.ContactMethod.Telephone_mobile
                    : !string.IsNullOrEmpty(struct_resume.Resume.StructuredResume.ContactMethod.Telephone_home) ? struct_resume.Resume.StructuredResume.ContactMethod.Telephone_home
                    : struct_resume.Resume.StructuredResume.ContactMethod.Telephone_alt;// or struct_resume.Resume.StructuredResume.ContactMethod.Telephone_home;
                compassbody.Email = struct_resume.Resume.StructuredResume.ContactMethod.InternetEmailAddress_main;

                if ((string.IsNullOrEmpty(compassbody.FirstName) && string.IsNullOrEmpty(compassbody.LastName)) || string.IsNullOrEmpty(compassbody.Email))
                {
                    var result = new OkObjectResult("Candidate's Name and Email could not be fetched. Please add candidate manually");
                    result.StatusCode = StatusCodes.Status206PartialContent;
                    await MoveResume(isCandidateAddedSuccessfully, filename);
                    return result;
                }

                compassbody.PostalCode = struct_resume.Resume.StructuredResume.ContactMethod.PostalAddress_main.PostalCode;
                compassbody.State = struct_resume.Resume.StructuredResume.ContactMethod.PostalAddress_main.Region;
                compassbody.City = struct_resume.Resume.StructuredResume.ContactMethod.PostalAddress_main.Municipality;
                compassbody.Country = struct_resume.Resume.StructuredResume.ContactMethod.PostalAddress_main.CountryCode;
                compassbody.Address = struct_resume.Resume.StructuredResume.ContactMethod.PostalAddress_main.AddressLine;
                compassbody.Resume.TextResume = file;
                compassbody.Resume.Extension = "." + fileExtension;

                if (resumeRankerResponse != null)
                {
                    compassbody.RankingScore = resumeRankerResponse.score;
                    compassbody.IsAccepted = resumeRankerResponse.recommendation.ToLower().Equals(RequiredConstants.AcceptResponse.ToLower());
                }

                compassbody.Resume.contentType = applicationtype;
                //post to compass
                IRestResponse compasspost = PostDetails(compassbody);

                if (compasspost.IsSuccessful)
                {
                    dynamic responseFromCompass = JsonConvert.DeserializeObject(compasspost.Content);

                    long? candidateApplicationId = null ?? responseFromCompass?.CandidateApplicationId;

                    if (candidateApplicationId != null && candidateApplicationId > 0)
                        isCandidateAddedSuccessfully = true;
                }
                //}
                //else
                //{
                //    isCandidateRejectedByTheRankingAlgorithm = true;
                //}
                isCandidateRejectedByTheRankingAlgorithm = compassbody.IsAccepted;
            }

            catch (Exception ex)
            {
                isCandidateAddedSuccessfully = false;
                Console.WriteLine("General exception. {0}", ex.Message);
            }

            //move from one blob to another 
            await MoveResume(isCandidateAddedSuccessfully, filename);

            if (isCandidateAddedSuccessfully)
            {
                var result = new OkObjectResult("Candidate is added successfully");
                result.StatusCode = !string.IsNullOrEmpty(compassbody.PhoneNumber1) ? StatusCodes.Status200OK : StatusCodes.Status206PartialContent;
                return result;
            }
            else if (isCandidateRejectedByTheRankingAlgorithm)
            {
                var result = new OkObjectResult("Candidate is rejected by the Ranking Algorithm");
                result.StatusCode = StatusCodes.Status200OK;
                return result;
            }
            else
            {
                var result = new OkObjectResult("Candidate is not added successfully. Please add candidate manually");
                result.StatusCode = StatusCodes.Status204NoContent;
                return result;
            }

        }

        public static JoDetails fetch_details(string jobordertitle, string mailbox, string domainname)
        {
            var client = new RestClient("https://psgqaapiservice.azurewebsites.net/WebApi/GetAccountMailBoxDetails");
            //var client = new RestClient("https://psgtalentcubeapiservice.azurewebsites.net/WebApi/GetAccountMailBoxDetails");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddQueryParameter("jobOrderTitle", jobordertitle);
            request.AddQueryParameter("domainName", domainname);
            request.AddQueryParameter("mailBox", mailbox);
            IRestResponse response = client.Execute(request);
            JoDetails details = JsonConvert.DeserializeObject<JoDetails>(response.Content);
            return details;
        }


        public static IRestResponse PostDetails(CompassBody body)
        {
            var client = new RestClient("https://psgqaapiservice.azurewebsites.net/WebApi/RegisterInboundCandidate");
            //var client = new RestClient("https://psgtalentcubeapiservice.azurewebsites.net/WebApi/RegisterInboundCandidate");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            // var test = JsonConvert.SerializeObject(body);

            //QA:
            //    APP ID 3B217B49 - 571D - 4B2F - 90D8 - 56D9F96A9A7A
            //            APP Key 3FB7D326 - A5AA - 4961 - 9EAA - F9DF5933502C
            request.AddJsonBody(body);
            request.AddHeader("Authorization", "basic");
            request.AddHeader("Content-Type", "application/json");
            //request.AddHeader("APPID", "AB098238-B287-4582-A392-A207090876CB");
            //request.AddHeader("APPKey", "kmRF1v0ehwFuuuod7LNgc/mr3nrUPBTmx25dHayBavM=");
            request.AddHeader("APPID", "3B217B49-571D-4B2F-90D8-56D9F96A9A7A");
            request.AddHeader("APPKey", "3FB7D326-A5AA-4961-9EAA-F9DF5933502C");
            IRestResponse response = client.Execute(request);
            return response;
        }

        public static ResumeRankerResponse GetRankingScoreForCandidateProfile(RequiredFieldToGetTheRannkingScore requiredFieldTGetTheData)
        {
            try
            {
                var client = new RestClient("https://compass-resume-ranker.azurewebsites.net/api/ranker");

                client.Timeout = -1;
                var request = new RestRequest(Method.POST);

                request.AddJsonBody(requiredFieldTGetTheData);
                request.AddHeader("Content-Type", "application/json");

                IRestResponse response = client.Execute(request);

                ResumeRankerResponse resumeRankerResponse = JsonConvert.DeserializeObject<ResumeRankerResponse>(response.Content);
                return resumeRankerResponse;
            }
            catch
            {
                //Ignore exception
            }

            return null;
        }

        public static Root Daxtra_api(string filename, out string resumeJsonResponse)
        {
            var client = new RestClient("https://cvx-psgglobal.daxtra.com/cvx/rest/api/v1/profile/full/json");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddParameter("account", "PSGGLOBAL");
            request.AddParameter("file", filename);
            IRestResponse response = client.Execute(request);

            resumeJsonResponse = response.Content;

            Root struct_resume = JsonConvert.DeserializeObject<Root>(response.Content);
            return struct_resume;
        }


        public static MemoryStream Get_resume(string file_name)

        {
            var ms = new MemoryStream();
            // fetch the environment variables 
            string connection_string = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            try
            {
                // Create a container client
                BlobClient blob_client = new BlobClient(connection_string, "joparser", file_name);
                if (blob_client.Exists())
                {
                    blob_client.DownloadTo(ms);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"error: {e}");
            }    
            return ms;
        }

        public static async void GetAllFiles()
        {
            // var ms = new MemoryStream();
            // fetch the environment variables 
            string connection_string = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            CloudStorageAccount backupStorageAccount = CloudStorageAccount.Parse(connection_string);
            var backupBlobClient = backupStorageAccount.CreateCloudBlobClient();
            var backupContainer = backupBlobClient.GetContainerReference("test-reason");
            BlobResultSegment resultSegment = await backupContainer.ListBlobsSegmentedAsync(currentToken: null);
            IEnumerable<IListBlobItem> blobItems = resultSegment.Results;

            var allBlobUris = blobItems.Select(b => b.Uri).ToList();
            // apply regex and get all the files that we need. 
            foreach (var fileUri in allBlobUris)
            {
                var ms = new MemoryStream();
                var testblob = backupContainer.ServiceClient.GetBlobReferenceFromServerAsync(fileUri);
                var blobResult = testblob.Result;
                string resumeJsonResponse = string.Empty;

                if (blobResult.ExistsAsync().Result)
                {
                    try
                    {
                        await blobResult.DownloadToStreamAsync(ms);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"the following error occured '{e}'");
                    }
                    String file = Convert.ToBase64String(ms.ToArray());
                    Root struct_resume = Daxtra_api(file, out resumeJsonResponse);
                    var testArray = fileUri.ToString().Split('/');
                    var testArray2 = testArray.LastOrDefault().Split('.');
                    var fileName = testArray2.FirstOrDefault();
                    CloudBlockBlob blob = backupContainer.GetBlockBlobReference(fileName + ".json");
                    await blob.DeleteIfExistsAsync();

                    await blob.UploadTextAsync(resumeJsonResponse);
                }
            }
        }

        public static async void GetSelectedFiles()
        {
            string connection_string = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            CloudStorageAccount backupStorageAccount = CloudStorageAccount.Parse(connection_string);
            var backupBlobClient = backupStorageAccount.CreateCloudBlobClient();
            var backupContainer = backupBlobClient.GetContainerReference("joparser-success");
            // BlobResultSegment resultSegment = await backupContainer.ListBlobsSegmentedAsync(currentToken: null);
            // Console.WriteLine(allBlobUris.Count());
            // apply regex and get all the files that we need. 

            DataMatch datamatch = new DataMatch();
            List<List<string>> req_cand_list = datamatch.CreateRequiredCandidateList();
            List<string> storage_name_list = new List<string>();            
            List<string> download_list = new List<string>();
            foreach (var fn_ln in req_cand_list)
            {
                foreach (var name in fn_ln)
                {
                    string prefix = name[0].ToString();
                    BlobResultSegment resultSegment = await backupContainer.ListBlobsSegmentedAsync(prefix: prefix, currentToken: null);
                    IEnumerable<IListBlobItem> blobItems = resultSegment.Results;
                    var allBlobUris = blobItems.Select(b => b.Uri).ToList();
                    foreach (var fileUri in allBlobUris)
                    {
                        var uri_split = fileUri.ToString().Split('/').ToList();
                        storage_name_list.Add(uri_split[uri_split.Count()-1]);
                    }
                    string matches_result = datamatch.GetDownloadList(storage_name_list, fn_ln);
                    if (matches_result != "false")
                    {
                        download_list.Add(matches_result);
                        break;
                    }
                    
                }
            }
            DataTransfer datatransfer = new DataTransfer();
            foreach (var name in download_list)
            {
                datatransfer.transfer(name);
            }
            string downloadpath = "Q:/Thinkbridge/OneDrive - Thinkbridge/Projects/JO Parser/ml/test-dataset-creator/files/";
            DataDownload datadownload = new DataDownload();
            foreach (var name in download_list)
            {
                datadownload.download(name, downloadpath);
            }
            Console.WriteLine("The end.");
        }

        public static async Task MoveResume(bool successFlag, string filename)
        {
            string target;
            if (successFlag)
            {
                target = "joparser-success";
            }
            else
            {
                target = "joparser-failed";
            }
            // fetch the environment variables 
            string connection_string = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            CloudStorageAccount sourceAccount = CloudStorageAccount.Parse(connection_string);
            CloudBlobClient blobClient = sourceAccount.CreateCloudBlobClient();


            CloudBlobContainer sourceContainer = blobClient.GetContainerReference("joparser");
            CloudBlobContainer targetContainer = blobClient.GetContainerReference(target);

            CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(filename);
            CloudBlockBlob targetBlob = targetContainer.GetBlockBlobReference(filename);
            await targetBlob.StartCopyAsync(sourceBlob);
            bool blobExisted = await sourceBlob.DeleteIfExistsAsync();
        }

        public static class RequiredConstants
        {
            public const string AcceptResponse = "NOT REJECT";

            public const string RehjectResponse = "REJECT";
        }
        public class JoDetails
        {
            public string AccountId { get; set; }
            public string JoborderId { get; set; }
            public string MailBox { get; set; }
            public string CandidateSource { get; set; }
            public string JobDescription { get; set; }

        }


        public class CompassResume
        {
            public string Extension { get; set; }
            public string contentType { get; set; }
            public string TextResume { get; set; }
        }

        public class CompassBody
        {
            public string FirstName { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string LastName { get; set; }
            public string MiddleName { get; set; }
            public string Source { get; set; }
            public string Email { get; set; }
            //public string CandidateApplicationId { get; set; }
            public string PostalCode { get; set; }
            public CompassResume Resume = new CompassResume();
            public string Country { get; set; }
            public string PhoneNumber1 { get; set; }
            public string PhoneNumber2 { get; set; }
            public string JobOrderId { get; set; }
            public string AccountId { get; set; }
            public string Comments { get; set; }
            public decimal? RankingScore { get; set; }
            public bool IsAccepted { get; set; }
        }

        public class RequiredFieldToGetTheRannkingScore
        {
            public string jobDescription { get; set; }
            public string resumeJsonResponse { get; set; }
        }

        public class ResumeRankerResponse
        {
            public decimal score { get; set; }
            public string recommendation { get; set; }

        }

        public class PostalAddressMain
        {
            public string PostalCode { get; set; }
            public string AddressLine { get; set; }
            public string Municipality { get; set; }
            public string Region { get; set; }
            public string CountryCode { get; set; }
        }

        public class ContactMethod
        {
            public string InternetEmailAddress_main { get; set; }
            public PostalAddressMain PostalAddress_main = new PostalAddressMain();
            public string Telephone_alt { get; set; }
            public string Telephone_mobile { get; set; }
            public string Telephone_home { get; set; }
        }

        public class PersonName
        {
            public string FormattedName { get; set; }
            public string FamilyName { get; set; }
            public string GivenName { get; set; }
            public string sex { get; set; }
        }

        public class TaxonomyId
        {
            public string idOwner { get; set; }
            public string description { get; set; }
        }

        public class Competency
        {
            public bool auth { get; set; }
            public int skillLevel { get; set; }
            public string skillName { get; set; }
            public string description { get; set; }
            public List<string> skillAliasArray { get; set; }
            public TaxonomyId TaxonomyId { get; set; }
            public int? skillCount { get; set; }
        }

        public class Language
        {
            public bool Speak { get; set; }
            public string LanguageCode { get; set; }
            public bool Write { get; set; }
            public bool Read { get; set; }
        }

        public class LocationSummary
        {
            public string Municipality { get; set; }
            public string Region { get; set; }
            public string CountryCode { get; set; }
        }

        public class Degree
        {
            public string degreeType { get; set; }
            public string DegreeName { get; set; }
            public string DegreeDate { get; set; }
        }

        public class EducationHistory
        {
            public LocationSummary LocationSummary { get; set; }
            public string schoolType { get; set; }
            public string Major { get; set; }
            public Degree Degree { get; set; }
            public string SchoolName { get; set; }
            public string EndDate { get; set; }
            public string Comments { get; set; }
            public string StartDate { get; set; }
        }

        public class StructuredResume
        {
            public ContactMethod ContactMethod = new ContactMethod();
            public string ExecutiveSummary { get; set; }
            public PersonName PersonName = new PersonName();
            public string RevisionDate { get; set; }
            public string lang { get; set; }
            public List<Competency> Competency { get; set; }
            public string DOB { get; set; }
            public List<Language> Languages { get; set; }
            public List<EducationHistory> EducationHistory { get; set; }
            public string MaritalStatus { get; set; }
        }

        public class ExperienceSummary
        {
            public string ExecutiveBrief { get; set; }
            public string HighestEducationalLevel { get; set; }
        }

        public class Attachment
        {
            public string conv { get; set; }
            public string lang { get; set; }
            public string doc_type { get; set; }
            public string fname { get; set; }
            public string content { get; set; }
            public string ftype { get; set; }
        }

        public class FileStruct
        {
            public List<Attachment> attachment { get; set; }
            public string filename { get; set; }
        }

        public class ParserInfo
        {
            public string ConverterRelease { get; set; }
            public string ParserConfiguration { get; set; }
            public string ParserRelease { get; set; }
            public string ParserReleaseDate { get; set; }
            public string ConverterReleaseDate { get; set; }
            public string ParserSchema { get; set; }
        }

        public class Resume
        {
            public StructuredResume StructuredResume = new StructuredResume();
            public ExperienceSummary ExperienceSummary = new ExperienceSummary();
            public FileStruct FileStruct = new FileStruct();
            public string src { get; set; }
            public string TextResume { get; set; }
            public ParserInfo ParserInfo = new ParserInfo();
        }

        public class Root
        {
            public Resume Resume = new Resume();
        }

    }
}
