using Microsoft.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;



namespace DaxtraAPIJoParser
{
    class DataTransfer
    {
        public void transfer(String filename)
        {
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=joparsereverisemh;AccountKey=hQoE8a7j10M8SaZUxQ/ee1rpkNuoA0yEteHdJyaGSUpMwI0usmbUj5v5QIEBnNGeMuuAzDal/nxr0G2iQjuUmA==;EndpointSuffix=core.windows.net;"; //blob connection string
            string sourceContainerName = "joparser-success";//source blob container name
            string destinationContainerName = "test-dest"; //destination blob container name
            string sourceBlobFileName = filename; //source blob name            

            //Gets a reference to the CloudBlobContainer
            var sourceBlobContainerReference = GetBlobContainerReference(connectionString, sourceContainerName);
            var destinationBlobContainerReference = GetBlobContainerReference(connectionString, destinationContainerName);

            var sourceBlob = sourceBlobContainerReference.GetBlockBlobReference(sourceBlobFileName);

            MoveBlobBetweenContainers(sourceBlob, destinationBlobContainerReference);

        }

        private void MoveBlobBetweenContainers(CloudBlockBlob srcBlob, CloudBlobContainer destContainer)
        {
            CloudBlockBlob destBlob;

            //Copy source blob to destination container
            using (MemoryStream memoryStream = new MemoryStream())
            {
                srcBlob.DownloadToStream(memoryStream);

                //put the time stamp
                // var newBlobName = srcBlob.Name.Split('.')[0] + "_" + DateTime.Now.ToString("ddMMyyyy", CultureInfo.InvariantCulture) + "." + srcBlob.Name.Split('.')[1];

                destBlob = destContainer.GetBlockBlobReference(srcBlob.Name);

                //copy source blob content to destination blob
                destBlob.StartCopy(srcBlob);

            }
            //remove source blob after copy is done.
            // srcBlob.Delete();
        }

        private CloudBlobContainer GetBlobContainerReference(string connectionString, string containerName)
        {
            //Get the Microsoft Azure Storage account
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            //Create the Blob service client.
            CloudBlobClient blobclient = storageAccount.CreateCloudBlobClient();

            //Returns a reference to a Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer object with the specified name.
            CloudBlobContainer blobcontainer = blobclient.GetContainerReference(containerName);


            return blobcontainer;
        }
    }
}


