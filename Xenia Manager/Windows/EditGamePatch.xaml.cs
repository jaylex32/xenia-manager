﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

// Imported
using Serilog;
using Tomlyn;
using Tomlyn.Model;
using Xenia_Manager.Classes;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for EditGamePatch.xaml
    /// </summary>
    public partial class EditGamePatch : Window
    {
        /// <summary>
        /// Holds every patch as a Patch class
        /// </summary>
        public ObservableCollection<Patch> Patches = new ObservableCollection<Patch>();

        /// <summary>
        /// Used to send a signal that this window has been closed
        /// </summary>
        public TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Location to the game specific patch
        /// </summary>
        private string patchFilePath { get; set; }

        /// <summary>
        /// Constructor of this window 
        /// </summary>
        /// <param name="selectedGame">Game whose patch will be edited</param>
        public EditGamePatch(InstalledGame selectedGame)
        {
            InitializeComponent();
            this.DataContext = this;
            InitializeAsync();
            this.Title = $"Xenia Manager - Editing {selectedGame.Title} Patch";
            GameTitle.Text = selectedGame.Title;
            this.patchFilePath = Path.Combine(App.baseDirectory, selectedGame.PatchFilePath);
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
        /// Reads game patch into the Patches ObservableCollection
        /// </summary>
        private void ReadGamePatch()
        {
            try
            {
                if (File.Exists(patchFilePath))
                {
                    Patches.Clear();
                    string content = File.ReadAllText(patchFilePath);
                    TomlTable model = Toml.ToModel(content);
                    TomlTableArray patches = model["patch"] as TomlTableArray;
                    foreach (var patch in patches)
                    {
                        Patch newPatch = new Patch();
                        newPatch.Name = patch["name"].ToString();
                        newPatch.IsEnabled = bool.Parse(patch["is_enabled"].ToString());
                        if (patch.ContainsKey("desc"))
                        {
                            newPatch.Description = patch["desc"].ToString();
                        }
                        else
                        {
                            newPatch.Description = "No description";
                        }
                        Patches.Add(newPatch);
                    }
                }
                ListOfPatches.ItemsSource = Patches;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
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
                    ReadGamePatch();
                });
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
        /// Saves the game patches into the .toml file
        /// </summary>
        private async Task SaveGamePatch()
        {
            try
            {
                if (File.Exists(patchFilePath))
                {
                    string content = File.ReadAllText(patchFilePath);
                    TomlTable model = Toml.ToModel(content);

                    TomlTableArray patches = model["patch"] as TomlTableArray;
                    foreach (var patch in Patches)
                    {
                        foreach (TomlTable patchTable in patches)
                        {
                            if (patchTable.ContainsKey("name") && patchTable["name"].Equals(patch.Name))
                            {
                                patchTable["is_enabled"] = patch.IsEnabled;
                                break;
                            }
                        }
                    }

                    // Serialize the TOML model back to a string
                    string updatedContent = Toml.FromModel(model);

                    // Write the updated TOML content back to the file
                    File.WriteAllText(patchFilePath, updatedContent);
                    Log.Information("Patches saved successfully");
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Does fade out animation before closing the window
        /// </summary>
        private async Task ClosingAnimation()
        {
            Storyboard FadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            FadeOutClosingAnimation.Completed += (sender, e) =>
            {
                Log.Information("Closing EditGamePatch window");
                this.Close();
            };

            FadeOutClosingAnimation.Begin(this);
            await Task.Delay(1);
        }

        /// <summary>
        /// Exits this window
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Saving changes");
            await SaveGamePatch();
            await ClosingAnimation();
        }

        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return _closeTaskCompletionSource.Task;
        }
    }
}
