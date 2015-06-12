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

        // Code to initialize app, build the list of files in the cloud and hide the progressanimation
        public MainWindow()
        {
            InitializeComponent();
            ImgProgress.Visibility = Visibility.Collapsed;
            // if no connection to azure storage is available
            if (!(App.Current as App).ping)
            {
                notConnected();
            }
            else
            {
                lblStatus.Content = "Cloud Storage is ready.";
                lblConn.Content = "Connected";
                lblConn.Foreground = new SolidColorBrush(Colors.Green);
                PopulateListAsync();
            }
        }

        // Helpermethods

        public void notConnected()
        {
            lblStatus.Content = "No internetconnection or storagecontainer unavailable.";
            btnDelete.IsEnabled = false;
            btnDownload.IsEnabled = false;
            btnUpload.IsEnabled = false;
            btnUploadFolder.IsEnabled = false;
            lblConn.Content = "Not Connected";
            lblConn.Foreground= new SolidColorBrush(Colors.Red);
        }

        // Method for populating the listview with all files in the storage container
        // in the cloud asynchroniously
        public async void PopulateListAsync()
        {
            // Check the connection to Azure
            if (await (App.Current as App).checkConnection())
            {
                // New tmp and clear lvCloudStorage.Items to make sure no duplicates are added
                List<string> tmp = new List<string>();
                lvCloudStorage.Items.Clear();
                ImgProgress.Visibility = Visibility.Visible;
                // Loop through blobs in container and add to tmp
                foreach (IListBlobItem item in (App.Current as App).blobcontainer.ListBlobs(null, true))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        tmp.Add(blob.Name);
                    }
                }
                tmp.Sort();
                foreach (var v in tmp)
                {
                    lvCloudStorage.Items.Add(v);
                }
                ImgProgress.Visibility = Visibility.Collapsed;
            }
            else
            {
                notConnected();
            }
        }

        // GUI elements

        // Method for uploading selected files from device to storage container
        // in the cloud asynchroniously
        private async void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            // Check the connection to Azure
            if (await (App.Current as App).checkConnection())
            {
                var cofd = new CommonOpenFileDialog();
                cofd.Multiselect = true;
                cofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    // Loop through files chosen by user, loop through and upload to cloud
                    foreach (string filename in cofd.FileNames)
                    {
                        lblStatus.Content = "Uploading files ...";
                        ImgProgress.Visibility = Visibility.Visible;
                        string name = System.IO.Path.GetFileName(filename);
                        CloudBlockBlob blockBlob = (App.Current as App).blobcontainer.GetBlockBlobReference(name);
                        using (var fileStream = System.IO.File.OpenRead(filename))
                        {
                            lblStatus.Content = "Uploading " + name + " ...";
                            await blockBlob.UploadFromStreamAsync(fileStream);
                            PopulateListAsync();
                        }
                    }
                    lblStatus.Content = "Upload complete.";
                    ImgProgress.Visibility = Visibility.Collapsed;
                }
                else
                {
                    lblStatus.Content = "Chose one or more files.";
                }
            }
            else
            {
                notConnected();
            }
        }

        // Method for uploading the entire selected folder from device to cloud storage,
        // looping through files asynchroniously
        private async void btnUploadFolder_Click(object sender, RoutedEventArgs e)
        {
            // Check the connection to Azure
            if (await (App.Current as App).checkConnection())
            {
                List<string> tmpFiles = new List<string>();
                var cofd = new CommonOpenFileDialog();
                cofd.IsFolderPicker = true;
                cofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string folder = cofd.FileName.Split('\\').Last() + @"\";
                    // Loop through the files in the folder and add them to a list
                    foreach (var f in Directory.GetFiles(cofd.FileName))
                    {
                        string filename = System.IO.Path.GetFileName(f);
                        tmpFiles.Add(filename);
                    }
                    // Loop through files in the folder chosen by user, rename them and upload to cloud asynchroniously
                    foreach (string file in tmpFiles)
                    {
                        lblStatus.Content = "Uploading folder " + folder + " ...";
                        ImgProgress.Visibility = Visibility.Visible;
                        string name = System.IO.Path.Combine(folder, file);
                        CloudBlockBlob blockBlob = (App.Current as App).blobcontainer.GetBlockBlobReference(name);
                        string fullpath = System.IO.Path.Combine(cofd.FileName, file);
                        using (var fileStream = System.IO.File.OpenRead(fullpath))
                        {
                            lblStatus.Content = "Uploading " + name + " ...";
                            await blockBlob.UploadFromStreamAsync(fileStream);
                            PopulateListAsync();
                        }
                    }
                    lblStatus.Content = "Upload complete.";
                    ImgProgress.Visibility = Visibility.Collapsed;
                }
                else
                {
                    lblStatus.Content = "Choose a folder.";
                }
            }
            else
            {
                notConnected();
            }
        }



        // Method allowing selection from listview
        private void lvCloudStorage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No code is needed here
        }

        // Method for downloading selected files from listview to folder on device
        // asynchroniously
        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            // Check the connection to Azure
            if (await (App.Current as App).checkConnection())
            {
                bool recreate = false;
                string fullpath;
                if (lvCloudStorage.SelectedItems.Count > 0)
                {
                    lblStatus.Content = "Downloading " + lvCloudStorage.SelectedItems.Count + " file(s).";
                    var cofd = new CommonOpenFileDialog();
                    cofd.IsFolderPicker = true;
                    cofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        // Open a messagebox, asking the user to confirm recreating the folderstructure
                        MessageBoxResult result = MessageBox.Show("Recreate the folderstructure of the chosen files (if any)?",
                            "Cload Storage", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            recreate = true;
                        }
                        ImgProgress.Visibility = Visibility.Visible;
                        // Loop through files chosen by user, loop through them and download to device
                        foreach (var file in lvCloudStorage.SelectedItems)
                        {
                            CloudBlockBlob blockBlob = (App.Current as App).blobcontainer.GetBlockBlobReference(file.ToString());
                            // Set and create filenames and folderstructure based on userchoice
                            if (recreate)
                            {
                                fullpath = System.IO.Path.Combine(cofd.FileName, file.ToString());
                                if (file.ToString().Contains('/'))
                                {
                                    string folder = file.ToString().Split('/').First();
                                    Directory.CreateDirectory(cofd.FileName + System.IO.Path.DirectorySeparatorChar + folder);
                                }
                            }
                            else
                            {
                                fullpath = System.IO.Path.Combine(cofd.FileName, file.ToString().Split('/').Last());
                            }
                            // Write the chosen files to device
                            using (var fileStream = System.IO.File.OpenWrite(fullpath))
                            {
                                await blockBlob.DownloadToStreamAsync(fileStream);
                                lblStatus.Content = file.ToString() + " was downloaded.";
                            }
                        }
                        lvCloudStorage.SelectedItems.Clear();
                        lblStatus.Content = "Download complete.";
                        ImgProgress.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    lblStatus.Content = "Chose one or more files.";
                }
            }
            else
            {
                notConnected();
            }
        }


        // Method for deleting the files selected by the user from the cloud storage
        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            // Check the connection to Azure
            if (await (App.Current as App).checkConnection())
            {
                if (lvCloudStorage.SelectedItems.Count > 0)
                {
                    List<string> tmpFiles = new List<string>();
                    lblStatus.Content = "Deleting " + lvCloudStorage.SelectedItems.Count + " files.";
                    foreach (var v in lvCloudStorage.SelectedItems)
                    {
                        tmpFiles.Add(v.ToString());
                    }
                    lvCloudStorage.SelectedItems.Clear();
                    ImgProgress.Visibility = Visibility.Visible;
                    foreach (var file in tmpFiles)
                    {
                        CloudBlockBlob blockBlob = (App.Current as App).blobcontainer.GetBlockBlobReference(file);
                        lblStatus.Content = file + " deleted.";
                        await blockBlob.DeleteAsync();
                    }
                    lblStatus.Content = tmpFiles.Count + " file(s) deleted successfully.";
                    ImgProgress.Visibility = Visibility.Collapsed;
                    tmpFiles.Clear();
                    PopulateListAsync();
                }
                else
                {
                    lblStatus.Content = "Choose one or more files.";
                }
            }
            else
            {
                notConnected();
            }
        }

        private async void btbRetry_Click(object sender, RoutedEventArgs e)
        {
            // Check the connection to Azure
            if (await(App.Current as App).checkConnection())
            {
                lblStatus.Content = "Cloud Storage is ready.";
                btnDelete.IsEnabled = true;
                btnDownload.IsEnabled = true;
                btnUpload.IsEnabled = true;
                btnUploadFolder.IsEnabled = true;
                lblConn.Content = "Connected";
                lblConn.Foreground = new SolidColorBrush(Colors.Green);
                PopulateListAsync();
            }
            else
            {
                notConnected();
            }
        }
    }
}
