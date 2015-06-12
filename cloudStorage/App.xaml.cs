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

        // Overriding OnStartup to be able to store azure-specific data outside the logic.
        // Data is reachable in code through (App.Current as App).[variable]
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            csa = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            cbc = csa.CreateCloudBlobClient();
            blobcontainer = cbc.GetContainerReference("azurestorage");
            // codeblocks run to check connection to azure storage
            try
            {
                ping = blobcontainer.Exists();
            }
            catch
            {
                return;
            }
            if (ping)
            {
                blobcontainer.CreateIfNotExists();
                blobcontainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            }
        }
    }
}
