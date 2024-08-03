﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;

namespace Xenia_Manager_Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public string path = AppDomain.CurrentDomain.BaseDirectory;
        public string url = "https://github.com/xenia-manager/xenia-manager/releases/latest/download/xenia_manager.zip";

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Download new version of Xenia Manager
        /// </summary>
        private async Task DownloadNewVersion()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(path + @"\xenia_manager.zip", FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var totalBytes = response.Content.Headers.ContentLength ?? -1;
                            var buffer = new byte[8192];
                            var bytesRead = 0;
                            var totalRead = 0;
                            do
                            {
                                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                                if (bytesRead > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                                    totalRead += bytesRead;

                                    // Calculate progress percentage
                                    var progressPercentage = totalBytes == -1 ? 0 : (int)((double)totalRead / totalBytes * 100);
                                    Progress.Value = progressPercentage;
                                }
                            } while (bytesRead > 0);
                        }
                    }
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex);
                return;
            }
        }

        /// <summary>
        /// Delete old version of Xenia Manager
        /// </summary>
        /// <returns></returns>
        private async Task DeleteOldVersion()
        {
            try
            {
                if (File.Exists(path + @"\Xenia Manager.exe"))
                {
                    File.Delete(path + @"\Xenia Manager.exe");
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex);
                return;
            }
        }

        /// <summary>
        /// Installation of new Xenia Manager version
        /// </summary>
        /// <returns></returns>
        private async Task Installation()
        {
            try
            {
                if (!Directory.Exists(path + @"\Update"))
                {
                    Directory.CreateDirectory(path + @"\Update");
                }
                await Extract(path + @"\xenia_manager.zip", path + @"\Update");
                await Move();
                await Cleanup();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Extracts the zip file
        /// </summary>
        /// <param name="fullPath">Path to the zip file</param>
        /// <param name="directory">Path to the extraction directory</param>
        private async Task Extract(string fullPath, string directory)
        {
            try
            {
                ZipFile.ExtractToDirectory(fullPath, directory, true);
                GC.Collect();
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Moves all of the updated files to the correct location
        /// </summary>
        private async Task Move()
        {
            try
            {
                if (Directory.Exists(path + @"\Update"))
                {
                    foreach (string file in Directory.GetFiles(path + @"\Update"))
                    {
                        if (System.IO.Path.GetFileName(file) != "Xenia Manager Updater.exe")
                        {
                            System.IO.File.Move(file, path + @"\" + System.IO.Path.GetFileName(file), true);
                        }
                    }
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Cleans up after updating to the latest version
        /// </summary>
        /// <returns></returns>
        private async Task Cleanup()
        {
            try
            {
                if (File.Exists(path + @"\xenia_manager.zip"))
                {
                    File.Delete(path + @"\xenia_manager.zip");
                }
                if (Directory.Exists(path + @"\Update"))
                {
                    Directory.Delete(path + @"\Update", true);
                }

                await Task.Delay(10);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Launches Xenia Manager and closes the updater
        /// </summary>
        /// <returns></returns>
        private async Task LaunchXeniaManager()
        {
            try
            {
                await Task.Delay(1);
                Process Launcher = new Process();
                Launcher.StartInfo.WorkingDirectory = path;
                Launcher.StartInfo.FileName = "Xenia Manager.exe";
                Launcher.StartInfo.UseShellExecute = true;
                Launcher.Start();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex);
            }
        }

        /// <summary>
        /// This is executed when this window is opened and loaded
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await DownloadNewVersion();
                await DeleteOldVersion();
                await Installation();
                await LaunchXeniaManager();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex);
            }
        }
    }
}