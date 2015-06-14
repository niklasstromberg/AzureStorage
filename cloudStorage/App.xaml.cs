using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System.Net.NetworkInformation;


namespace cloudStorage
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Declaring variables
        CloudStorageAccount csa;
        CloudBlobClient cbc;
        public CloudBlobContainer blobcontainer;
        public bool ping;
        public BlobRequestOptions bro = new BlobRequestOptions();
        public OperationContext oc = new OperationContext();


        // method to check connection to azure storage
        public async Task<bool> checkConnection()
        {
            try
            {
                ping = await blobcontainer.ExistsAsync(bro, oc);
                return ping;
            }
            catch
            {
                ping = false;
                return ping;
            }
        }

        public bool getPing()
        {
            return ping;
        }

        // Overriding OnStartup to be able to store azure-specific data outside the logic.
        // Data is reachable in code through (App.Current as App).[variable]
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            csa = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            cbc = csa.CreateCloudBlobClient();
            blobcontainer = cbc.GetContainerReference("azurestorage");
            bro.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(2), 3);
            // If connection is established
            if (await checkConnection())
            {
                blobcontainer.CreateIfNotExists();
                blobcontainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                ping = getPing();//await checkConnection();
            }
        }
    }
}
