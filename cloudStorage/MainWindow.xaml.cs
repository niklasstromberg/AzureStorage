using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace cloudStorage
{
    public partial class MainWindow : Window
    {
        // Declaring variables
        public List<string> downloadList = new List<string>();
        public List<string> tmpFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            PopulateListAsync();
            //ImgProgress.IsEnabled = false;
            ImgProgress.Visibility = Visibility.Collapsed;
        }

        // Helpermethods

        // Method for populating the listview with all files in the storage container
        // in the cloud asynchroniously
        public async void PopulateListAsync()
        {
            lvCloudStorage.Items.Clear();
            ImgProgress.Visibility = Visibility.Visible;
            foreach (IListBlobItem item in (App.Current as App).blobcontainer.ListBlobs(null, true))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    // Make sure no duplicates end up in the listview
                    if (!lvCloudStorage.Items.Contains(blob.Name))
                    {
                        lvCloudStorage.Items.Add(blob.Name);
                    }
                }
            }
            ImgProgress.Visibility = Visibility.Collapsed;
        }

        // GUI elements

        // Method for uploading selected files from device to storage container
        // in the cloud asynchroniously
        private async void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            var cofd = new CommonOpenFileDialog();
            cofd.Multiselect = true;
            cofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                // Loop through files chosen by user, loop through and upload to cloud
                foreach (string filename in cofd.FileNames)
                {
                    lblFileToUpload.Content = "Uploading files ...";
                    ImgProgress.Visibility = Visibility.Visible;
                    string name = System.IO.Path.GetFileName(filename);
                    CloudBlockBlob blockBlob = (App.Current as App).blobcontainer.GetBlockBlobReference(name);
                    using (var fileStream = System.IO.File.OpenRead(filename))
                    {
                        lblFileToUpload.Content = "Uploading " + name + " ...";
                        await blockBlob.UploadFromStreamAsync(fileStream);
                        PopulateListAsync();
                    }
                }
                lblFileToUpload.Content = "Upload complete.";
                ImgProgress.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblFileToUpload.Content = "Chose one or more files.";
            }
        }

        private void lvCloudStorage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        // Method for downloading selected files from listview to folder on device
        // asynchroniously
        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (lvCloudStorage.SelectedItems.Count > 0)
            {
                lblFileToUpload.Content = "Downloading " + lvCloudStorage.SelectedItems.Count + " files.";
                var cofd = new CommonOpenFileDialog();
                cofd.IsFolderPicker = true;
                cofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    ImgProgress.Visibility = Visibility.Visible;
                    // Loop through files chosen by user, loop through them and download to device
                    foreach (var file in lvCloudStorage.SelectedItems)
                    {
                        CloudBlockBlob blockBlob = (App.Current as App).blobcontainer.GetBlockBlobReference(file.ToString());
                        string fullpath = System.IO.Path.Combine(cofd.FileName, file.ToString());
                        using (var fileStream = System.IO.File.OpenWrite(fullpath))
                        {
                            await blockBlob.DownloadToStreamAsync(fileStream);
                            lblFileToUpload.Content = file.ToString() + " was downloaded.";
                        }
                    }
                    lvCloudStorage.SelectedItems.Clear();
                    lblFileToUpload.Content = "Download complete.";
                    ImgProgress.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                lblFileToUpload.Content = "Chose one or more files.";
            }
        }

        // Method for deleting the files selected by the user from the cloud storage
        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lvCloudStorage.SelectedItems.Count > 0)
            {
                lblFileToUpload.Content = "Deleting " + lvCloudStorage.SelectedItems.Count + " files.";
                foreach (var v in lvCloudStorage.SelectedItems)
                {
                    tmpFiles.Add(v.ToString());
                }
                lvCloudStorage.SelectedItems.Clear();
                ImgProgress.Visibility = Visibility.Visible;
                foreach (var file in tmpFiles)
                {
                    CloudBlockBlob blockBlob = (App.Current as App).blobcontainer.GetBlockBlobReference(file);
                    lblFileToUpload.Content = file + " deleted.";
                    await blockBlob.DeleteAsync();
                }
                lblFileToUpload.Content = tmpFiles.Count + " file(s) deleted successfully.";
                ImgProgress.Visibility = Visibility.Collapsed;
                tmpFiles.Clear();
                PopulateListAsync();
            }
            else
            {
                lblFileToUpload.Content = "Choose one or more files.";
            }
        }
    }
}
