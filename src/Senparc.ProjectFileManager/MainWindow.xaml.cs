﻿using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.ProjectFileManager.Helpers;
using Senparc.ProjectFileManager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Xml.Linq;

namespace Senparc.ProjectFileManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PropertyGroup SelectedFile { get; set; } = new PropertyGroup() { FullFilePath = $"[ no file selectd ] - {SystemTime.Now}" };
        //public ObservableCollection<KeyValuePair<string, string>> BindFileData { get; set; }
        public ObservableCollection<PropertyGroup> ProjectFiles { get; set; } = new ObservableCollection<PropertyGroup>();
        public List<XDocument> ProjectDocuments { get; set; } = new List<XDocument>();

        private bool _inited = false;

        public MainWindow()
        {
            InitializeComponent();
            SenparcTrace.SendCustomLog("System", "Window opened.");

            txtPath.Text = Environment.CurrentDirectory;
            Init();
        }

        private void Init()
        {
            tabPropertyGroup.Visibility = Visibility.Hidden;
            //BindFileData = new ObservableCollection<KeyValuePair<string, string>>();
            ProjectFiles.Clear();
            ProjectDocuments.Clear();

            lbFiles.ItemsSource = ProjectFiles;

            if (!_inited)
            {
                lbFiles.DataContext = ProjectFiles;
                _inited = true;
            }

            SelectedFile.FullFilePath = $"[ no file selectd ] - {SystemTime.Now}";
        }


        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Init();

            var path = txtPath.Text?.Trim();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                MessageBox.Show("Please input the correct path which includes .csproj files！", "error");
                return;
            }

            SenparcTrace.SendCustomLog("Task", "Search .csproj files begin.");

            var csprojFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);
            if (csprojFiles == null || csprojFiles.Length == 0)
            {
                MessageBox.Show("No valiable .csproj file！", "error");
                return;
            }

            foreach (var file in csprojFiles)
            {
                try
                {
                    var doc = XDocument.Load(file);
                    var propertyGroup = doc.Root.Elements("PropertyGroup").FirstOrDefault();
                    if (propertyGroup == null)
                    {
                        SenparcTrace.SendCustomLog("Task Falid", $"{file} is not a valid xml.csproj file.");
                        continue;
                    }

                    var projectFile = PropertyGroup.GetObjet(propertyGroup, file);
                    ProjectFiles.Add(projectFile);
                    ProjectDocuments.Add(doc);

                    SenparcTrace.SendCustomLog("Task", $"[Success] Load file:{file}");
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("Task", $"[Faild] Load file:{file}");
                    SenparcTrace.SendCustomLog("Error", ex.Message);
                }
            }

            if (ProjectFiles.Count == 0)
            {
                MessageBox.Show("No valiable .csproj file！", "error");
                return;
            }

            if (lbFiles.Items.Count > 0)
            {
                lbFiles.SelectedIndex = 0;//default select the first item.
            }

            //lbFiles.DataContext = ProjectFiles;
            //lbFiles.ItemsSource = ProjectFiles;

            //foreach (var projectFile in ProjectFiles)
            //{
            //    BindFileData.Add(new KeyValuePair<string, string>(projectFile.FileName, projectFile.FullFilePath));
            //}
            //lbFiles.ItemsSource = BindFileData;
        }

        private void lbFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;//when re-research the files, the list will be cleared at first.
            }
            var selectedData = (PropertyGroup)e.AddedItems[0];
            SelectedFile = selectedData;

            tbFilePath.DataContext = SelectedFile;

            #region TabItems

            //Version
            txtVersion.DataContext = SelectedFile;
            try
            {
                var selectedVersion = VersionHelper.GetVersionObject(SelectedFile.Version);
                txtQualifier.Text = selectedVersion.QualifierVersion;

            }
            catch
            {
                txtQualifier.Text = "";
            }


            //PackageReleaseNotes
            txtPackageReleaseNotes.DataContext = SelectedFile;

            //Introductions
            txtTitle.DataContext = SelectedFile;
            txtCopyright.DataContext = SelectedFile;
            txtAuthors.DataContext = SelectedFile;
            txtDescription.DataContext = SelectedFile;
            txtOwners.DataContext = SelectedFile;
            txtSummary.DataContext = SelectedFile;

            //Package
            txtPackageTags.DataContext = SelectedFile;
            txtPackageLicenseUrl.DataContext = SelectedFile;
            txtProjectUrl.DataContext = SelectedFile;
            txtPackageProjectUrl.DataContext = SelectedFile;
            txtPackageIconUrl.DataContext = SelectedFile;
            txtRepositoryUrl.DataContext = SelectedFile;

            //Assembly
            txtTargetFramework.DataContext = SelectedFile;
            txtTargetFramework.IsEnabled = lblTargetFramework.IsEnabled = !SelectedFile.TargetFramework.IsNullOrEmpty();

            txtTargetFrameworks.DataContext = SelectedFile;
            txtTargetFrameworks.IsEnabled = lblTargetFrameworks.IsEnabled = !SelectedFile.TargetFrameworks.IsNullOrEmpty();

            txtAssemblyName.DataContext = SelectedFile;
            txtRootNamespace.DataContext = SelectedFile;

            #endregion

            tabPropertyGroup.Visibility = Visibility.Visible;
        }

        private void linkSourceCode_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;
            System.Diagnostics.Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri));
            e.Handled = true;
        }

        #region Change Version
        private void ChangeFileVersion(PropertyGroup propertyGroup, Action<VersionObject> versionOperate)
        {
            try
            {
                var version = VersionHelper.GetVersionObject(propertyGroup.Version);
                versionOperate(version);
                propertyGroup.Version = version.ToString();

            }
            catch (Exception ex)
            {
                //some projects many not have a invalid verion number.
                SenparcTrace.SendCustomLog("version not changed", ex.Message);
            }
            finally
            {
                VersionObject version = null;
                try
                {
                    version = VersionHelper.GetVersionObject(SelectedFile.Version);
                }
                catch
                {
                    version = new VersionObject();
                }
                txtVersion.Dispatcher.Invoke(() => txtVersion.Text = SelectedFile.Version);
                txtQualifier.Dispatcher.Invoke(() => txtQualifier.Text = version.QualifierVersion);
            }
        }

        private void SyncAllFileVersion(PropertyGroup propertyGroup,string versionName, Func<VersionObject,int> newVersionNumberFunc,Action<VersionObject,int> versionOperate)
        {
            if (SelectedFile.Version.IsNullOrEmpty())
            {
                MessageBox.Show("Current project doesn't have a valid version number!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBoxResult.Yes != MessageBox.Show($"Are you sure you want to synchronize the {versionName} of the current project to all projects?","Confirm", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
            {
                return;
            }

            var currentVersion = VersionHelper.GetVersionObject(SelectedFile.Version);
            var newVersionNumber = newVersionNumberFunc(currentVersion);
            ProjectFiles.ToList().ForEach(pgFile => ChangeFileVersion(pgFile, pg => versionOperate(pg, newVersionNumber)));

            MessageBox.Show($"All project verion numbers({versionName}) have been changed to {currentVersion}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #region Current Project


        private void btnCurrentMajorVersionPlus_Click(object sender, RoutedEventArgs e)
        {
            ChangeFileVersion(SelectedFile, pg => pg.MajorVersion++);
            //SelectedFile.Version = "changed";
        }

        private void btnCurrentMinorVersionPlus_Click(object sender, RoutedEventArgs e)
        {
            ChangeFileVersion(SelectedFile, pg => pg.MinorVersion++);
        }

        private void btnCurrentIncrementalVersionPlus_Click(object sender, RoutedEventArgs e)
        {
            ChangeFileVersion(SelectedFile, pg => pg.RevisionVersion++);
        }

        private void btnCurrenBuildVersionPlus_Click(object sender, RoutedEventArgs e)
        {
            ChangeFileVersion(SelectedFile, pg => pg.BuildNumberVersion++);
        }
        #endregion

        #region All Projects

        #region Sync
        private void btnSyncMajorVersion_Click(object sender, RoutedEventArgs e)
        {
            SyncAllFileVersion(SelectedFile, "Major Version",currentVersion=>currentVersion.MajorVersion, (pg, versionNumber) => pg.MajorVersion = versionNumber);
        }

        private void btnSyncMinorVersion_Click(object sender, RoutedEventArgs e)
        {
            SyncAllFileVersion(SelectedFile, "Minor Version", currentVersion => currentVersion.MinorVersion, (pg, versionNumber) => pg.MinorVersion = versionNumber);
        }

        private void btnSyncIncrementalVersion_Click(object sender, RoutedEventArgs e)
        {
            SyncAllFileVersion(SelectedFile, "Revision Version", currentVersion => currentVersion.RevisionVersion, (pg, versionNumber) => pg.RevisionVersion = versionNumber);
        }

        private void btnSyncBuildVersion_Click(object sender, RoutedEventArgs e)
        {
            SyncAllFileVersion(SelectedFile, "BuildNumber Version", currentVersion => currentVersion.BuildNumberVersion, (pg, versionNumber) => pg.BuildNumberVersion = versionNumber);
        }
        #endregion

        #region Plus

        private void btnAllMajorVersionPlus_Click(object sender, RoutedEventArgs e)
        {
            ProjectFiles.ToList().ForEach(pgFile => ChangeFileVersion(pgFile, pg => pg.MajorVersion++));
        }


        private void btnAllMinorVersionPlus_Click(object sender, RoutedEventArgs e)
        {
            ProjectFiles.ToList().ForEach(pgFile => ChangeFileVersion(pgFile, pg => pg.MinorVersion++));
        }

        private void btnAllIncrementalVersionPlus_Click(object sender, RoutedEventArgs e)
        {
            ProjectFiles.ToList().ForEach(pgFile => ChangeFileVersion(pgFile, pg => pg.RevisionVersion++));
        }

        private void btnAllBuildVersionPlus_Click(object sender, RoutedEventArgs e)
        {
            ProjectFiles.ToList().ForEach(pgFile => ChangeFileVersion(pgFile, pg => pg.BuildNumberVersion++));
        }

        private void btnAllQualifierVersion_Click(object sender, RoutedEventArgs e)
        {
            var qualifierVersion = txtQualifier.Text;

            if (qualifierVersion.Length > 0 && int.TryParse(qualifierVersion.Substring(0, 1), out _))
            {
                qualifierVersion = "-" + qualifierVersion;//qualifier version can not start with a number
            }

            ProjectFiles.ToList().ForEach(pgFile => ChangeFileVersion(pgFile, pg => pg.QualifierVersion = qualifierVersion));
        }

        #endregion


        #endregion

        #endregion

        private void menuSearch_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                string filePath = dialog.FileName;
                txtPath.Text = filePath;
                btnSearch_Click(btnSearch, e);
            }
        }

        private void menuSourceCode_Click(object sender, RoutedEventArgs e)
        {
            var iePath = Environment.ExpandEnvironmentVariables(
         @"%PROGRAMFILES%\Internet Explorer\iexplore.exe");
            System.Diagnostics.Process.Start(iePath, "https://github.com/Senparc/ProjectFileManager");
            e.Handled = true;
        }

        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(@"      Senparc.ProjectFileManager can help developers to manage .csproj files under the certain path.
      You can use this tool to modify project file information or manage version information individually or in bulk.", "About Senparc.ProjectFileManager");
        }

        #region Save

        private void menuSaveOne_Click(object sender, RoutedEventArgs e)
        {
            txtPath.Focus();
            if (SelectedFile == null)
            {
                MessageBox.Show("Please choose one project!");
            }

            try
            {
                SelectedFile.Save();
                MessageBox.Show($"File saved:\r\n{SelectedFile.FullFilePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"File save faild:\r\n{SelectedFile.FullFilePath}

{ex.Message}", "Success", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void menuSaveAll_Click(object sender, RoutedEventArgs e)
        {
            txtPath.Focus();

            int i = 0;
            List<string> notSaved = new List<string>();
            foreach (var projectFile in ProjectFiles)
            {
                try
                {
                    projectFile.Save();
                    i++;
                }
                catch (Exception ex)
                {
                    notSaved.Add($@"{projectFile.FileName}
[{ex.Message}]");
                }
            }
            var msg = $"All files saved: {i}/{ProjectFiles.Count}";
            if (i < ProjectFiles.Count)
            {
                msg += @"

The following files are not saved:

";
                foreach (var file in notSaved)
                {
                    msg += file + Environment.NewLine + Environment.NewLine;
                }
            }

            MessageBox.Show(msg, "Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        #endregion

        private void btnRemoveKeywords_Click(object sender, RoutedEventArgs e)
        {
            txtRemoveKeywords.Text.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .ToList()
                .ForEach(kw =>
                {
                    var keyword = kw.Trim();
                    if (keyword.Length == 0)
                    {
                        return;
                    }

                    var tobeRemvoe = ProjectFiles.Where(z => z.FileName.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                    tobeRemvoe.ForEach(z => ProjectFiles.Remove(z));
                });
        }

        private void btnRemoveFileItem_Click(object sender, RoutedEventArgs e)
        {
            var propertyGroup = (PropertyGroup)((Button)e.OriginalSource).DataContext;
            ProjectFiles.Remove(propertyGroup);
            e.Handled = true;
        }

      
    }
}
