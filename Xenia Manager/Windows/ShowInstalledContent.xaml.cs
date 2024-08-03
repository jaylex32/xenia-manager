﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

// Imported
using Serilog;
using Xenia_Manager.Classes;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for ShowInstalledContent.xaml
    /// </summary>
    public partial class ShowInstalledContent : Window
    {
        /// <summary>
        /// Used to show only the file name on the listbox and still have access to the path to it
        /// </summary>
        public class FileItem
        {
            public string Name { get; set; }
            public string FullPath { get; set; }
        }

        /// <summary>
        /// Enumeration of all supported content types by Xenia according to their FAQ.
        /// </summary>
        public enum ContentType : uint
        {
            /// <summary>
            /// Saved game data.
            /// </summary>
            Saved_Game = 0x0000001,

            /// <summary>
            /// Content available on the marketplace.
            /// </summary>
            Downloadable_Content = 0x0000002,

            /// <summary>
            /// Content published by a third party.
            /// </summary>
            //Publisher = 0x0000003,

            /// <summary>
            /// Xbox 360 title.
            /// </summary>
            Xbox360_Title = 0x0001000,

            /// <summary>
            /// Installed game.
            /// </summary>
            Installed_Game = 0x0004000,

            /// <summary>
            /// Xbox Original game.
            /// </summary>
            //XboxOriginalGame = 0x0005000,

            /// <summary>
            /// Xbox Title, also used for Xbox Original games.
            /// </summary>
            //XboxTitle = 0x0005000,

            /// <summary>
            /// Game on Demand content.
            /// </summary>
            Game_On_Demand = 0x0007000,

            /// <summary>
            /// Avatar item.
            /// </summary>
            //AvatarItem = 0x0009000,

            /// <summary>
            /// User profile data.
            /// </summary>
            //Profile = 0x0010000,

            /// <summary>
            /// Gamer picture.
            /// </summary>
            //GamerPicture = 0x0020000,

            /// <summary>
            /// Theme for Xbox dashboard or games.
            /// </summary>
            //Theme = 0x0030000,

            /// <summary>
            /// Storage download, typically for storage devices.
            /// </summary>
            //StorageDownload = 0x0050000,

            /// <summary>
            /// Xbox saved game data.
            /// </summary>
            //XboxSavedGame = 0x0060000,

            /// <summary>
            /// Downloadable content for Xbox.
            /// </summary>
            //XboxDownload = 0x0070000,

            /// <summary>
            /// Game demo content.
            /// </summary>
            //GameDemo = 0x0080000,

            /// <summary>
            /// Full game title.
            /// </summary>
            //GameTitle = 0x00A0000,

            /// <summary>
            /// Installer for games or applications.
            /// </summary>
            Installer = 0x00B0000,

            /// <summary>
            /// Arcade title, typically a game from the Xbox Live Arcade.
            /// </summary>
            Arcade_Title = 0x00D0000,
        }

        // Selected game
        private InstalledGame game = new InstalledGame();

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        public ShowInstalledContent(InstalledGame game)
        {
            InitializeComponent();
            this.game = game;
            InitializeAsync();
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Used for dragging the window around
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Populates the ContentType combobox with items
        /// </summary>
        private void LoadContentTypes()
        {
            // Populate the ComboBox with the names of the ContentType enum, with underscores replaced by spaces
            var contentTypes = Enum.GetValues(typeof(ContentType)).Cast<ContentType>()
                .Select(ct => new { Value = ct, DisplayName = ct.ToString().Replace("_", " ") })
                .ToList();

            ContentTypeList.ItemsSource = contentTypes;
            ContentTypeList.DisplayMemberPath = "DisplayName";
            ContentTypeList.SelectedValuePath = "Value";
        }

        /// <summary>
        /// Function that executes other functions asynchronously
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Hidden;
                    Mouse.OverrideCursor = Cursors.Wait;
                });
                LoadContentTypes();
                ContentTypeList.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Visible;
                    Mouse.OverrideCursor = null;
                });
            }
        }

        /// <summary>
        /// Used to execute fade in animation when loading is finished
        /// </summary>
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                Storyboard fadeInStoryboard = ((Storyboard)Application.Current.FindResource("FadeInAnimation")).Clone();
                if (fadeInStoryboard != null)
                {
                    fadeInStoryboard.Begin(this);
                }
            }
        }

        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return _closeTaskCompletionSource.Task;
        }

        /// <summary>
        /// Does fade out animation before closing the window
        /// </summary>
        private async Task ClosingAnimation()
        {
            Storyboard FadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            FadeOutClosingAnimation.Completed += (sender, e) =>
            {
                Log.Information("Closing SelectGame window");
                this.Close();
            };

            FadeOutClosingAnimation.Begin(this);
            await Task.Delay(1);
        }

        // UI
        /// <summary>
        /// Updates the ListBox with content that is inside of the selected content type
        /// </summary>
        /// <param name="contentType"></param>
        private void UpdateListBox(ContentType contentType)
        {
            // Get the folder path based on the selected ContentType enum value
            string folderPath = "";
            switch (game.EmulatorVersion)
            {
                case "Stable":
                    folderPath = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation, $@"content\{game.GameId}\{((uint)contentType).ToString("X8")}");
                    break;
                case "Canary":
                    folderPath = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation, $@"content\{game.GameId}\{((uint)contentType).ToString("X8")}");
                    break;
                case "Netplay":
                    folderPath = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.EmulatorLocation, $@"content\{game.GameId}\{((uint)contentType).ToString("X8")}");
                    break;
                default:
                    break;
            }

            if (Directory.Exists(folderPath))
            {
                // Get the list of files and directories in the selected folder
                var items = Directory.EnumerateFileSystemEntries(folderPath)
                                    .Select(path => new FileItem { Name = Path.GetFileName(path), FullPath = path })
                                    .ToList();

                InstalledContentList.ItemsSource = items;
            }
            else
            {
                InstalledContentList.ItemsSource = null;
            }
        }

        /// <summary>
        /// Executes when user changes selected ContentType
        /// </summary>
        private void ContentTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ContentTypeList.SelectedIndex >= 0)
                {
                    if (ContentTypeList.SelectedValue is ContentType selectedContentType)
                    {
                        if (selectedContentType == ContentType.Saved_Game)
                        {
                            SavedGamesButtons.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            SavedGamesButtons.Visibility = Visibility.Collapsed;
                        }
                        UpdateListBox(selectedContentType);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Traverses the visual tree to find an ancestor of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the ancestor to find.</typeparam>
        /// <param name="current">The starting element to begin the search from.</param>
        /// <returns>The found ancestor of type T, or null if not found.</returns>
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            // Traverse the visual tree to find an ancestor of the specified type
            while (current != null)
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Handles the PreviewMouseDown event for the ListBox.
        /// Clears the selection if the click is outside of any ListBoxItem.
        /// </summary>
        /// <param name="sender">The source of the event, which is the ListBox.</param>
        /// <param name="e">The MouseButtonEventArgs that contains the event data.</param>
        private void InstalledContentList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Get the ListBox
            ListBox listBox = sender as ListBox;

            // Get the clicked point
            Point point = e.GetPosition(listBox);

            // Get the element under the mouse at the clicked point
            var result = VisualTreeHelper.HitTest(listBox, point);

            if (result != null)
            {
                // Check if the clicked element is a ListBoxItem
                ListBoxItem listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)result.VisualHit);
                if (listBoxItem == null)
                {
                    // If no ListBoxItem found, clear the selection
                    listBox.SelectedIndex = -1;
                }
            }
        }

        // Buttons
        /// <summary>
        /// Opens the selected storage folder
        /// </summary>
        private void OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ContentTypeList.SelectedIndex >= 0)
                {
                    if (ContentTypeList.SelectedValue is ContentType selectedContentType)
                    {
                        Process process = new Process();
                        process.StartInfo.FileName = "explorer.exe";
                        string directoryPath = "";
                        // Checking what version of Xenia the game uses and then launching the correct directory
                        switch (game.EmulatorVersion)
                        {
                            case "Stable":
                                directoryPath = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation, $@"content\{game.GameId}\{((uint)selectedContentType).ToString("X8")}");
                                break;
                            case "Canary":
                                directoryPath = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation, $@"content\{game.GameId}\{((uint)selectedContentType).ToString("X8")}");
                                break;
                            case "Netplay":
                                directoryPath = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.EmulatorLocation, $@"content\{game.GameId}\{((uint)selectedContentType).ToString("X8")}");
                                break;
                            default:
                                break;
                        }
                        if (Directory.Exists(directoryPath))
                        {
                            process.StartInfo.Arguments = directoryPath;
                            process.Start();
                        }
                        else
                        {
                            MessageBox.Show($"This game has no directory called '{selectedContentType.ToString().Replace("_", " ")}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Closes this window
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            await ClosingAnimation();
        }

        /// <summary>
        /// Removes selected item/items from the ListBox
        /// </summary>
        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Grabbing all of the selected items");
                // Grabbing all of the selected items to delete
                List<FileItem> selectedItems = InstalledContentList.SelectedItems.Cast<FileItem>().ToList();

                // Checking if there is something selected
                if (selectedItems.Count > 0)
                {
                    Log.Information($"There are {selectedItems.Count} items to delete");
                    // If there are items selected, go through the list and delete each one seperately
                    foreach (FileItem item in selectedItems)
                    {
                        Log.Information($"Deleting: {item.Name}");

                        // Checking if it's a folder or a file
                        if (Directory.Exists(item.FullPath))
                        {
                            Directory.Delete(item.FullPath, true); // Delete the directory recursively
                        }
                        else if (File.Exists(item.FullPath))
                        {
                            File.Delete(item.FullPath); // Delete the file
                        }
                    }
                }
                else
                {
                    Log.Information($"No items have been selected to delete");
                }

                // Update UI by reading again
                UpdateListBox((ContentType)ContentTypeList.SelectedValue);
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Exports the entire save file folder or if there are selected items, it'll only export those items
        /// </summary>
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                // Grabbing all of the selected items to delete
                List<FileItem> selectedItems = InstalledContentList.SelectedItems.Cast<FileItem>().ToList();

                string destination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{DateTime.Now:yyyyMMdd_HHmmss} - {game.Title} Save File.zip");
                string saveFileLocation = "";
                switch (game.EmulatorVersion)
                {
                    case "Stable":
                        saveFileLocation = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation, $@"content\{game.GameId}\00000001");
                        break;
                    case "Canary":
                        saveFileLocation = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation, $@"content\{game.GameId}\00000001");
                        break;
                    case "Netplay":
                        saveFileLocation = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.EmulatorLocation, $@"content\{game.GameId}\00000001");
                        break;
                    default:
                        break;
                }
                using (FileStream fs = new FileStream(destination, FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
                    {
                        Log.Information("Checking if there are any selected items");
                        if (selectedItems.Any())
                        {
                            Log.Information($"There are {selectedItems.Count} selected items");
                            foreach (var item in selectedItems)
                            {
                                string relativePath = item.FullPath.Substring(saveFileLocation.Length + 1);
                                string entryName = Path.Combine($"{game.GameId}/00000001", relativePath).Replace('\\', '/');

                                if (Directory.Exists(item.FullPath))
                                {
                                    // If item is a directory, recursively add all files
                                    foreach (var filePath in Directory.GetFiles(item.FullPath, "*.*", SearchOption.AllDirectories))
                                    {
                                        string relativeFilePath = filePath.Substring(saveFileLocation.Length + 1);
                                        string entryFileName = Path.Combine($"{game.GameId}/00000001", relativeFilePath).Replace('\\', '/');
                                        archive.CreateEntryFromFile(filePath, entryFileName);
                                    }
                                }
                                else
                                {
                                    // If item is a file
                                    archive.CreateEntryFromFile(item.FullPath, entryName);
                                }
                            }
                        }
                        else
                        {
                            Log.Information("Exporting all files");
                            foreach (string filePath in Directory.GetFiles(saveFileLocation, "*.*", SearchOption.AllDirectories))
                            {
                                string entryName = Path.Combine($"{game.GameId}/00000001", filePath.Substring(saveFileLocation.Length + 1).Replace('\\', '/'));
                                archive.CreateEntryFromFile(filePath, entryName);
                            }
                        }
                    }
                }
                Mouse.OverrideCursor = null;
                Log.Information($"The save file for '{game.Title}' has been successfully exported to the desktop");
                MessageBox.Show($"The save file for '{game.Title}' has been successfully exported to the desktop");
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Imports teh save file into proper folder and then refreshes the UI
        /// </summary>
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select a save file",
                    Filter = "All Files|*"
                };
                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    string saveFileLocation = "";
                    switch (game.EmulatorVersion)
                    {
                        case "Stable":
                            saveFileLocation = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation, $@"content\");
                            break;
                        case "Canary":
                            saveFileLocation = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation, $@"content\");
                            break;
                        case "Netplay":
                            saveFileLocation = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.EmulatorLocation, $@"content\");
                            break;
                        default:
                            break;
                    }

                    if (!Directory.Exists(saveFileLocation))
                    {
                        Directory.CreateDirectory(saveFileLocation);
                    }

                    // Extract the save file to the correct folder
                    try
                    {
                        ZipFile.ExtractToDirectory(openFileDialog.FileName, saveFileLocation, true);

                        // Reload UI
                        UpdateListBox((ContentType)ContentTypeList.SelectedValue);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message + "\nFull Error:\n" + ex);
                        MessageBox.Show(ex.Message);
                    }
                    Mouse.OverrideCursor = null;
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}
