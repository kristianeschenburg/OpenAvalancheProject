using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net;
using System.IO;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Handlers;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class DownloadFromQueueToBlob
    {
        //create a shared httpclient so as not to exhaust resources
        //https://docs.microsoft.com/en-us/azure/architecture/antipatterns/improper-instantiation/
        private static readonly HttpClient client;
        static DownloadFromQueueToBlob()
        {
            client = new HttpClient();
        }
        
        [FunctionName("DownloadFromQueueToBlob")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run([QueueTrigger("filereadytodownloadqueue", Connection = "AzureWebJobsStorage")]FileReadyToDownloadQueueMessage myQueueItem,
                          [Blob("nam-grib-westus-v1/{UniqueFileName}", FileAccess.Write)] Stream myOutputBlob,
                          TraceWriter log)
        {
            string partitionName = "nam-grib-westus-v1";

            log.Info($"C# DownloadFromQueueToBlob queue trigger function processing: {myQueueItem.UniqueFileName}");

            if (myQueueItem.Filetype == partitionName)
            {
                var urlToDownload = myQueueItem.Url; 
                log.Info($"Downloading Url {urlToDownload}");
             
                client.GetByteArrayAsync(urlToDownload).ContinueWith(
                    (requestTask) =>
                    {
                        byte[] buffer = requestTask.Result;
                        myOutputBlob.Write(buffer, 0, buffer.Length);
                        myOutputBlob.Flush();
                        myOutputBlob.Close();
                    }
                );
            }
            else
            {
                log.Info($"Have unknown filetype {myQueueItem.Filetype}");
            }
        }
    }
}