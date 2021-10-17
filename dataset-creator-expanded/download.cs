using Microsoft.Azure;
using System;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
namespace DaxtraAPIJoParser
{
    class DataDownload
    {
        public void download(string filename, string downloadpath)
        {

                Console.WriteLine("Downloading {0}", filename);

                string storageAccount_connectionString = "DefaultEndpointsProtocol=https;AccountName=joparsereverisemh;AccountKey=hQoE8a7j10M8SaZUxQ/ee1rpkNuoA0yEteHdJyaGSUpMwI0usmbUj5v5QIEBnNGeMuuAzDal/nxr0G2iQjuUmA==;EndpointSuffix=core.windows.net;";
                var container_name = "test-dest";
                var files = filename;
                CloudStorageAccount mycloudStorageAccount = CloudStorageAccount.Parse(storageAccount_connectionString);
                CloudBlobClient blobClient = mycloudStorageAccount.CreateCloudBlobClient();

                CloudBlobContainer container = blobClient.GetContainerReference(container_name);
                CloudBlockBlob BlockBlob = container.GetBlockBlobReference(files);

                // provide the file download location below            
                var file = File.OpenWrite(downloadpath + files);

                BlockBlob.DownloadToStreamAsync(file);

                Console.WriteLine("Download completed!");

            }
        }
    }

