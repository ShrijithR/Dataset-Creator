using System;
using System.IO;
using System.Net;
using System.Text;  // for class Encoding
using System.Threading.Tasks;

namespace DaxtraServices
{
    public class CVXtractorService
    {
        public string service_url { get; private set; } = null;
        public string account { get; private set; } = null;
        private bool json_out = false;
        private string error_buf = null;
        private string phase2_token = null;
        private int timeout_msecs = 0;
        private const string rest_api_version = "/cvx/rest/api/v1/";
        private System.Collections.Generic.Dictionary<string, Task> await_objects = new System.Collections.Generic.Dictionary<string, Task>();

        public event ProcessingFinished onProcessingFinished = null;

        public CVXtractorService(string surl, string service_account, bool jo)
        {
            service_url = surl;
            account = service_account;
            json_out = jo;
        }
        ~CVXtractorService()
        {
            await_objects.Clear();
            await_objects = null;
        }
        public bool isReadyToUse() { return (account != "") ? true : false; }
        public string getLastError() { return error_buf; }
        public string getPhase2Token() { return phase2_token; }
        public void setTimeout(int msecs) { timeout_msecs = msecs; }
        public void notWaitToFinish(string async_call_id) { _waitToFinish(async_call_id, true); }
        public void waitToFinish(string async_call_id) { _waitToFinish(async_call_id, false); }

        public string ProcessCV(string file_to_proces)
        {
            error_buf = "";
            byte[] b = readFile(file_to_proces);
            if (b == null) { return ""; }
            return ProcessCV(b, file_to_proces);
        }
        public string ProcessCV(byte[] b)
        {
            return ProcessCV(b, "unknown_name");
        }
        public string ProcessCV(byte[] b, string file_name)
        {
            error_buf = "";
            String hrxml_profile;
            try
            {
                hrxml_profile = executeStringPostCall("profile/full/" + ((json_out) ? "json" : "xml"), b, file_name);
            }
            catch (Exception e)
            {
                hrxml_profile = "";
                error_buf = "Exception :: " + e.Message;
            }
            finally
            {
            }
            return hrxml_profile;
        }

        public string ProcessJobOrder(String file_to_proces)
        {
            error_buf = "";
            byte[] b = readFile(file_to_proces);
            if (b == null) { return ""; }
            return ProcessJobOrder(b, file_to_proces);
        }
        public string ProcessJobOrder(byte[] b)
        {
            return ProcessJobOrder(b, "unknown_name");
        }
        public string ProcessJobOrder(byte[] b, String file_name)
        {
            error_buf = "";
            String hrxml_profile;
            try
            {
                hrxml_profile = executeStringPostCall("joborder/" + ((json_out) ? "json" : "xml"), b, file_name);
            }
            catch (Exception e)
            {
                hrxml_profile = "";
                error_buf = "Exception :: " + e.Message;
            }
            finally
            {
            }
            return hrxml_profile;
        }

        public string Convert(String file_to_proces, String to)
        {
            error_buf = "";
            byte[] b = readFile(file_to_proces);
            if (b == null) { return ""; }
            return Convert(b, file_to_proces, to);
        }
        public string Convert(byte[] b, String to)
        {
            return Convert(b, "unknown_name", to);
        }
        public string Convert(byte[] b, String file_name, String to)
        {
            error_buf = "";
            String hrxml_profile;
            try
            {
                hrxml_profile = executeStringPostCall("convert2" + to, b, file_name);
            }
            catch (Exception e)
            {
                hrxml_profile = "";
                error_buf = "Exception :: " + e.Message;
            }
            finally
            {
            }
            return hrxml_profile;
        }


        public string ProcessCVphase1(String file_to_proces)
        {
            error_buf = "";
            byte[] b = readFile(file_to_proces);
            if (b == null) { return ""; }
            return ProcessCVphase1(b, file_to_proces);
        }
        public string ProcessCVphase1(byte[] b)
        {
            return ProcessCVphase1(b, "unknown_name");
        }
        public string ProcessCVphase1(byte[] b, String file_name)
        {
            error_buf = "";
            String hrxml_profile = "";
            /*
             * for not to include JSON processing classes instead of calling
             * "profile/personal/json" 
             * we will call "profile/personal/xml"
             * which wil return XML outer structure but we will specify actual JSON output
             * for profile through adding ";-OUT_SCHEMA json" instruction to account
            */
            String account_saved = account;
            if (json_out)
            {
                if (account.Contains(".*;.*") == false) { account += ";"; }
                account += " -OUT_SCHEMA json";
            }
            try
            {
                string multi_hrxml_profile = executeStringPostCall("profile/personal/xml", b, file_name);
                if (multi_hrxml_profile == null || multi_hrxml_profile == "") return "";
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.LoadXml(multi_hrxml_profile);
                System.Xml.XmlNodeList list_tok = doc.GetElementsByTagName("full_profile_token");
                if (list_tok.Count > 0)
                {
                    phase2_token = list_tok.Item(0).InnerText;
                }
                System.Xml.XmlNodeList list_hrxml = doc.GetElementsByTagName("hrxml");
                if (list_hrxml.Count > 0)
                {
                    hrxml_profile = list_hrxml.Item(0).InnerText;
                }
            }
            catch (Exception e)
            {
                hrxml_profile = "";
                error_buf = "Exception :: " + e.Message;
            }
            finally
            {
                account = account_saved;
            }
            return hrxml_profile;
        }
        public string ProcessCVphase2()
        {
            if (phase2_token == "")
            {
                error_buf = "No succesful ProcessCVphase1 call registred so cannot do ProcessCVphase2";
                return "";
            }
            error_buf = "";
            return executeStringGetCall("data?token=" + phase2_token);
        }

        public byte[] ReformatCV(string file_to_proces, string template)
        {
            return ReformatCV(file_to_proces, template, null, null);
        }
        public byte[] ReformatCV(string file_to_proces, string template, string options, string hrxml)
        {
            error_buf = "";
            byte[] b = readFile(file_to_proces);
            if (b == null) { return null; }
            return ReformatCV(b, file_to_proces, template, options, hrxml);
        }
        public byte[] ReformatCV(byte[] b, string file_name, string template, string options, string hrxml)
        {
            error_buf = "";
            byte[] reformatted_document = null;
            try
            {
                reformatted_document = executePostCall("styler", b, file_name, template, options, hrxml);
            }
            catch (Exception e)
            {
                reformatted_document = null;
                error_buf = "Exception :: " + e.Message;
            }
            return reformatted_document;
        }

        public string InstallStylerTemplate(string new_template_file, string template)
        {
            error_buf = "";
            byte[] b = readFile(new_template_file);
            if (b == null) { return ""; }
            return InstallStylerTemplate(b, new_template_file, template);
        }
        public string InstallStylerTemplate(byte[] b, string template)
        {
            return InstallStylerTemplate(b, "unknown_name", template);
        }
        public string InstallStylerTemplate(byte[] b, string file_name, string template)
        {
            error_buf = "";
            String response = "";
            try
            {
                response = executeStringPostCall("styler/template", b, file_name, template, null, null);
            }
            catch (Exception e)
            {
                response = "";
                error_buf = "Exception :: " + e.Message;
            }

            return response;
        }

        public byte[] RetrieveStylerTemplate(string template)
        {
            error_buf = "";
            byte[] response = null;
            try
            {
                response = executeGetCall("styler/template", template);
            }
            catch (Exception e)
            {
                response = null;
                error_buf = "Exception :: " + e.Message;
            }
            return response;
        }

        private string executeStringPostCall(string rest_call, byte[] b, string file_name)
        {
            return executeStringPostCall(rest_call, b, file_name, null, null, null);
        }

        private string executeStringPostCall(string rest_call, byte[] b, string file_name, string template, string options, string hrxml)
        {
            byte[] response = executePostCall(rest_call, b, file_name, template, options, hrxml);
            return (response != null) ? Encoding.UTF8.GetString(response) : "";
        }

        private byte[] executePostCall(string rest_call, byte[] b, string file_name)
        {
            return executePostCall(rest_call, b, file_name, null, null, null);
        }

        private byte[] executePostCall(string rest_call, byte[] b, string file_name, string template, string options, string hrxml)
        {
            if (file_name == "") { file_name = "some_file"; }

            String full_call = service_url + rest_api_version + rest_call;

            /*
              The POST request may be compressed using the gzip algorithm, in which case the HTTP request 
              header Content�Encoding must be present and have "gzip" as value. A POST request compressed 
              with gzip must be compressed by the client in its entirety (i.e. the whole message must be 
              compressed, not the single parts of the multipart/form�data content). It is a client responsibility to 
              check whether the server supports content compression, this is done by checking the value of the 
              HTTP response header X�AlpineBits�Server�Accept�Encoding which is set to "gzip" by servers who 
              support this feature. The so called "Housekeeping" actions must not be compressed.
            */
            bool use_gzip = false;  //- we will zip the file itself instead
                                    //-- actually we will use "Content-Transfer-Encoding: gzip" on file part
            byte[] b_zip = (!use_gzip) ? compressByteArray(b, "gzip", file_name) : null; //-- 

            string boundary = "************123456789**********";
            HttpWebRequest urlc = null;
            try
            {
                if (full_call.StartsWith("https:") || full_call.StartsWith("HTTPS:"))
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                }
                Uri uri = new Uri(full_call);
                urlc = (HttpWebRequest)WebRequest.Create(uri);
                //urlc.setDoOutput(true);
                //urlc.setAllowUserInteraction(false);
                urlc.KeepAlive = true;
                urlc.CachePolicy = new System.Net.Cache.HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel.NoCacheNoStore);  //urlc.setUseCaches(false);
                urlc.Headers.Add("charset", "utf-8");
                if (use_gzip) { urlc.Headers.Add("Content-Encoding", "gzip"); }
                //urlc.Headers.Add("Accept-Encoding", "gzip");  // "deflate"//- ask CVX server to compress
                urlc.AutomaticDecompression = DecompressionMethods.GZip; //- use this instead of "Accept-Encoding", "gzip"
                urlc.ContentType = "multipart/form-data;boundary=" + boundary;
                urlc.Method = "POST";
                if (timeout_msecs > 0) { urlc.Timeout = timeout_msecs; } // urlc.ReadWriteTimeout = timeout_msecs; }
                //urlc.connect();

                //------ send request data ------------
                System.IO.Stream requestStream;

                if (use_gzip) //- not used
                {
                    requestStream = new System.IO.Compression.GZipStream(urlc.GetRequestStream(), System.IO.Compression.CompressionMode.Compress);
                }
                else
                {
                    requestStream = urlc.GetRequestStream(); // new System.IO.Stream(urlc.GetRequestStream());
                }


                String twoHyphens = "--";
                String crlf = "\r\n";
                //-- file part
                writeBytes(requestStream, twoHyphens + boundary + crlf);
                writeBytes(requestStream, "Content-Disposition: form-data; name=\"file\"; filename=\"" + file_name + "\"" + crlf);
                writeBytes(requestStream, "Content-Type: application/octet-stream" + crlf);
                if (b_zip != null) writeBytes(requestStream, "Content-Transfer-Encoding: gzip" + crlf);
                writeBytes(requestStream, crlf);
                writeBytes(requestStream, (b_zip != null) ? b_zip : b);
                writeBytes(requestStream, crlf);
                //-- account part
                writeBytes(requestStream, twoHyphens + boundary + crlf);
                writeBytes(requestStream, "Content-Disposition: form-data; name=\"account\"" + crlf);
                writeBytes(requestStream, "Content-Type: text/plain; charset=UTF-8" + crlf);
                writeBytes(requestStream, crlf);
                writeBytes(requestStream, account);
                writeBytes(requestStream, crlf);
                //-- reformatter template part
                if (template != null)
                {
                    writeBytes(requestStream, twoHyphens + boundary + crlf);
                    writeBytes(requestStream, "Content-Disposition: form-data; name=\"template\"" + crlf);
                    writeBytes(requestStream, "Content-Type: text/plain; charset=UTF-8" + crlf);
                    writeBytes(requestStream, crlf);
                    writeBytes(requestStream, template);
                    writeBytes(requestStream, crlf);
                }
                //-- reformatter options part
                if (options != null)
                {
                    writeBytes(requestStream, twoHyphens + boundary + crlf);
                    writeBytes(requestStream, "Content-Disposition: form-data; name=\"options\"" + crlf);
                    writeBytes(requestStream, "Content-Type: text/plain; charset=UTF-8" + crlf);
                    writeBytes(requestStream, crlf);
                    writeBytes(requestStream, options);
                    writeBytes(requestStream, crlf);
                }
                //-- reformatter hrxml part
                if (hrxml != null)
                {
                    writeBytes(requestStream, twoHyphens + boundary + crlf);
                    writeBytes(requestStream, "Content-Disposition: form-data; name=\"hrxml\"" + crlf);
                    writeBytes(requestStream, "Content-Type: text/plain; charset=UTF-8" + crlf);
                    writeBytes(requestStream, crlf);
                    writeBytes(requestStream, hrxml);
                    writeBytes(requestStream, crlf);
                }
                //-- closing boundary (with -- at the end) to signal end of form 
                writeBytes(requestStream, twoHyphens + boundary + twoHyphens + crlf);
                //-- finish with thre request
                requestStream.Flush();
                requestStream.Close();
            }
            catch (Exception e)
            {
                error_buf = "Exception :: " + e.Message;
                urlc = null;
            }

            if (urlc == null) return null;
            if (onProcessingFinished != null)
            {
                string ASYNC_ID = Guid.NewGuid().ToString();
                Task ao = readResponseAsync(urlc, ASYNC_ID);
                lock (await_objects) { await_objects.Add(ASYNC_ID, ao); }
                return Encoding.Default.GetBytes(ASYNC_ID);
            }
            return readResponse(urlc);
        }

        private string executeStringGetCall(string rest_call)
        {
            byte[] response = executeGetCall(rest_call);
            return (response != null) ? Encoding.UTF8.GetString(response) : "";
        }

        private byte[] executeGetCall(string rest_call)
        {
            return executeGetCall(rest_call, null);
        }

        private byte[] executeGetCall(string rest_call, string template)
        {
            String full_call = service_url + rest_api_version + rest_call;
            if (template != null)
            {
                full_call += "&account=" + account + "&template=" + template;
            }

            HttpWebRequest urlc = null;
            try
            {
                if (full_call.StartsWith("https:") || full_call.StartsWith("HTTPS:"))
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                }
                Uri uri = new Uri(full_call);
                urlc = (HttpWebRequest)WebRequest.Create(uri);
                //urlc.setDoOutput(true);
                //urlc.setAllowUserInteraction(false);
                urlc.KeepAlive = true;
                urlc.CachePolicy = new System.Net.Cache.HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel.NoCacheNoStore);  //urlc.setUseCaches(false);
                urlc.Headers.Add("charset", "utf-8");
                urlc.Headers.Add("Accept-Encoding", "gzip"); //- ask CVX server to compress
                //urlc.AutomaticDecompression = DecompressionMethods.GZip; //- use this instead of "Accept-Encoding", "gzip"
                urlc.Method = "GET";
                if (timeout_msecs > 0) { urlc.Timeout = timeout_msecs; } // urlc.ReadWriteTimeout = timeout_msecs; }
                //  urlc.connect();
            }
            catch (Exception e)
            {
                error_buf = "Exception :: " + e.Message;
            }
            return (urlc != null) ? readResponse(urlc) : null;
        }

        private async System.Threading.Tasks.Task readResponseAsync(HttpWebRequest urlc, string async_id)
        {
            try
            {
                WebResponse wr = await urlc.GetResponseAsync();
                byte[] output = readResponse(wr as HttpWebResponse);
                string err_str = null;
                if (Encoding.UTF8.GetString(output).StartsWith("ERROR:"))
                {
                    err_str = Encoding.UTF8.GetString(output);
                    output = null;
                }
                ProcessingFinishedEventArgs eargs = new ProcessingFinishedEventArgs(async_id, output, err_str);
                onProcessingFinished(this, eargs);
            }
            catch (Exception ex)
            {
                ProcessingFinishedEventArgs eargs = new ProcessingFinishedEventArgs(async_id, null, "ERROR:" + ex.Message);
                onProcessingFinished(this, eargs);
            }
        }
        private byte[] readResponse(HttpWebRequest urlc)
        {
            try
            {
                HttpWebResponse webResponse = (HttpWebResponse)urlc.GetResponse();
                byte[] output = readResponse(webResponse);
                if (Encoding.UTF8.GetString(output).StartsWith("ERROR:"))
                {
                    error_buf = Encoding.UTF8.GetString(output);
                    output = null;
                }
                return output;
            }
            catch (Exception ex)
            {
                error_buf = "Exception :: " + ex.Message;
                return null;
            }
        }
        private static byte[] readResponse(HttpWebResponse webResponse)
        {
            byte[] output = null;
            try
            {

                //-- check response ------
                System.IO.Stream responseStream = null;
                if ((int)webResponse.StatusCode >= 200 && (int)webResponse.StatusCode < 400)
                {
                    responseStream = GetStreamForResponse(webResponse); // webResponse.GetResponseStream();
                }
                else
                {
                    responseStream = GetStreamForResponse(webResponse);  //urlc.getErrorStream();
                }


                //----------- read result
                using (MemoryStream ms = new MemoryStream())
                {
                    responseStream.CopyTo(ms);
                    output = ms.ToArray();
                }
                webResponse.Close();
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    using (Stream data = response.GetResponseStream())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            data.CopyTo(ms);
                            output = ms.ToArray();
                        }
                    }
                    if (Encoding.UTF8.GetString(output).Contains("CSERROR") == false)
                    {
                        output = Encoding.UTF8.GetBytes("ERROR:" + e.Message + " " + output);
                    }
                    httpResponse.Close();
                }
            }
            catch (Exception e)
            {
                output = Encoding.UTF8.GetBytes("ERROR:" + e.Message);
            }
            finally
            {
                //    urlc.disconnect();
            }

            return output;
        }


        private void _waitToFinish(string async_call_id, bool do_not_wait = false)
        {
            Task ao;
            if (await_objects.TryGetValue(async_call_id, out ao))
            {
                if (ao != null && do_not_wait == false) ao.Wait();
                lock (await_objects)
                {
                    await_objects.Remove(async_call_id);
                }
            }
        }

        private byte[] readFile(string aInputFileName)
        {
            byte[] b;
            try
            {
                string res = ReadBinFile(aInputFileName, out b);
                if (res != "") { error_buf = "Exception :: Cannot read file :: " + res; return null; }
            }
            catch (Exception e)
            {
                error_buf = "Exception :: Cannot read file ::" + e.Message;
                return null;
            }
            return b;
        }

        static string FileToBase64ByteArray(string inputFileName, out byte[] outArray)
        {
            byte[] binaryData;
            string res = ReadBinFile(inputFileName, out binaryData);
            if (res != "") { outArray = null; return res; }
            return ByteArrayToBase64ByteArray(binaryData, out outArray);
        }
        static string ReadBinFile(string inputFileName, out byte[] binaryData)
        {
            //byte [] binaryData;											   
            try
            {
                System.IO.FileStream oStream = new System.IO.FileStream(inputFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                System.IO.BinaryReader oBinaryReader = new System.IO.BinaryReader(oStream);
                binaryData = new Byte[oStream.Length];
                binaryData = oBinaryReader.ReadBytes((int)oStream.Length);
                oBinaryReader.Close();
                oStream.Close();
            }
            catch (System.Exception exp)
            {
                binaryData = null;
                return exp.Message;
            }

            return "";
        }

        static string ByteArrayToBase64ByteArray(byte[] binaryData, out byte[] outArray)
        {
            string base64String;
            try
            {
                base64String = System.Convert.ToBase64String(binaryData, 0, binaryData.Length);
            }
            catch (System.Exception exp)
            {
                outArray = null;
                return exp.Message;
            }

            try
            {
                outArray = System.Convert.FromBase64String(base64String);
            }
            catch (System.Exception exp)
            {
                outArray = null;
                return exp.Message;
            }

            return ""; //no error message on return
        }
        public static byte[] compressByteArray(byte[] raw, string ctype, string file_name)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (System.IO.Compression.GZipStream gzipStream = new System.IO.Compression.GZipStream(memoryStream, System.IO.Compression.CompressionMode.Compress, true))
                {
                    gzipStream.Write(raw, 0, raw.Length);
                    //CopyStream(msi, gzip);
                }
                return memoryStream.ToArray();
            }
        }

        private static void CopyStream(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }


        private static System.IO.Stream GetStreamForResponse(HttpWebResponse webResponse, int readTimeOut = 0)
        {
            System.IO.Stream stream;
            if (webResponse.ContentEncoding == null)
            {
                stream = new System.IO.Compression.DeflateStream(webResponse.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
            }
            else
            {
                switch (webResponse.ContentEncoding.ToUpperInvariant())
                {
                    case "GZIP":
                        stream = new System.IO.Compression.GZipStream(webResponse.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                        break;
                    case "DEFLATE":
                        stream = new System.IO.Compression.DeflateStream(webResponse.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                        break;

                    default:
                        stream = webResponse.GetResponseStream();
                        if (readTimeOut > 0) stream.ReadTimeout = readTimeOut;
                        break;
                }
            }
            return stream;
        }

        private static void writeBytes(System.IO.Stream s, string txt)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(txt);
            s.Write(bytes, 0, bytes.Length);
        }
        private static void writeBytes(System.IO.Stream s, byte[] bytes)
        {
            s.Write(bytes, 0, bytes.Length);
        }

        /*
        " -a process/process2/vacancy/batch -id ACCOUNT -host IP [-json] [[-async] file_to_process\n";      
        string[] args_my = { "-a", "process2", "-id", "demo", "-host", daxtra_server , "-json", process_file };
        DaxtraServices.CVXtractorService.console_driver(args_my);
        */
        public static void console_driver(string[] args)
        {
            string usage = " -a process/process2/vacancy/batch/convert2html/convert2pdf -id ACCOUNT -host IP [-json] file_to_process\n";
            string host = null;
            string account = null;
            string file = null;
            bool json_out = false;
            bool do_async = false;
            string action = "process";

            for (int j = 0; j < args.Length; j++)
            {
                if (args[j] == "-help")
                {
                    Console.Error.WriteLine(usage);
                    System.Environment.Exit(1);
                }
                if (args[j] == "-host")
                { host = args[++j]; continue; }
                if (args[j] == "-id")
                { account = args[++j]; continue; }
                if (args[j] == "-a")
                { action = args[++j]; continue; }
                if (args[j] == "-json")
                { json_out = true; continue; }
                if (args[j] == "-async")
                { do_async = true; continue; }
                if (file == null)
                { file = args[j]; continue; }
                Console.Error.WriteLine("Unknown argument " + args[j]);
                Console.Error.WriteLine(usage);
                System.Environment.Exit(1);

            }

            if (file == null || host == null || account == null)
            {
                Console.Error.WriteLine(usage);
                System.Environment.Exit(1);
            }
            try
            {
                CVXtractorService cvx = new CVXtractorService(host, account, json_out);

                if (do_async)
                {
                    cvx.onProcessingFinished += processingFinishedHandlerExample;
                }

                String hrxml = "";
                if (action == "process")
                {
                    hrxml = cvx.ProcessCV(file);
                }
                if (action == "vacancy")
                {
                    hrxml = cvx.ProcessJobOrder(file);
                }
                if (action == "convert2html")
                {
                    hrxml = cvx.Convert(file, "html");
                }
                if (action == "convert2html_q") //- extra quality
                {
                    hrxml = cvx.Convert(file, "html_q");
                }
                if (action == "convert2pdf")
                {
                    hrxml = cvx.Convert(file, "pdf");
                }
                if (action == "convert2pdf_q")
                {
                    hrxml = cvx.Convert(file, "pdf_q");
                }
                if (action == "process2")
                {
                    hrxml = cvx.ProcessCVphase1(file);
                    if (hrxml != "" && cvx.getLastError() == "")
                    {
                        Console.Out.WriteLine(hrxml);
                        hrxml = cvx.ProcessCVphase2();
                    }
                }

                if (do_async)
                {   //- hrxml here is asymc_call_id
                    if (hrxml == null || hrxml == "")
                    {
                        Console.Error.WriteLine("Error at processing :: " + cvx.getLastError());
                    }
                    else
                    {
                        //-  wait for it to complete - results printed in  processingFinishedHandlerExample
                        cvx.waitToFinish(hrxml); //cvx.notWaitToFinish(hrxml);
                    }
                    return;
                }

                //if(action.equals("batch")) {
                //    hrxml  = cvx.ProcessCVbatch(file);
                //}
                if (hrxml == "")
                {
                    Console.Error.WriteLine("Error at processing :: " + cvx.getLastError());
                }
                else
                {
                    Console.Out.WriteLine(hrxml);
                }

            }
            catch (Exception Exception)
            {
                Console.Error.WriteLine("Caught an Exception!" + Exception.Message);
                //Exception.printStackTrace();
            }
        }

        static void processingFinishedHandlerExample(Object sender, ProcessingFinishedEventArgs e)
        {
            Console.Out.WriteLine("Received results from async call " + e.async_call_id);
            if (e.error == null)
                Console.Out.WriteLine(e.result);
            else Console.Out.WriteLine(e.error);
        }
        /*
        public void Exit()
        {
            if (System.Windows.Forms.Application.MessageLoop)
            {
                // WinForms app
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                // Console app
                System.Environment.Exit(1);
            }
        }
        */
    }

    public delegate void ProcessingFinished(Object sender, ProcessingFinishedEventArgs e);
    public class ProcessingFinishedEventArgs : EventArgs
    {
        public string async_call_id { get; private set; } = null;
        public byte[] result { get; private set; } = null;
        public string error { get; private set; } = null;
        public ProcessingFinishedEventArgs(string c, byte[] r, string e = null)
        {
            async_call_id = c; result = r; error = e;
        }
    }

}
