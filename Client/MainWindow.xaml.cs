using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Win32;

namespace Client
{
    public partial class MainWindow : Window
    {
        private HttpClient _client;
        private string _baseUrl;

        public MainWindow()
        {
            InitializeComponent();
            _client = new HttpClient();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            _baseUrl = ServerUrlTextBox.Text.Trim();
            try
            {
                var response = await _client.GetAsync($"{_baseUrl}/api/files");

                if (response.IsSuccessStatusCode)
                {
                    StatusTextBlock.Text = "Connected";
                    Log("Successfully connected to server");
                    await RefreshFileList();
                }
                else
                {
                    StatusTextBlock.Text = "Connection failed";
                    Log($"Server responded with: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Connection error";
                Log($"Error: {ex.Message}");
                Log($"Full URL attempted: {_baseUrl}/api/files");
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var fileStream = File.OpenRead(openFileDialog.FileName))
                    using (var content = new StreamContent(fileStream))
                    {
                        var filename = Path.GetFileName(openFileDialog.FileName);
                        var response = await _client.PutAsync($"{_baseUrl}/api/files/{filename}", content);
                        var responseText = await response.Content.ReadAsStringAsync();
                        Log(responseText);
                        await RefreshFileList();
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error uploading file: {ex.Message}");
                }
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListView.SelectedItem == null)
            {
                Log("Please select a file to download");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                FileName = FilesListView.SelectedItem.ToString()
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filename = FilesListView.SelectedItem.ToString();
                    var response = await _client.GetAsync($"{_baseUrl}/api/files/{filename}");

                    if (response.IsSuccessStatusCode)
                    {
                        using (var fileStream = File.Create(saveFileDialog.FileName))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                        Log($"File {filename} downloaded successfully");
                    }
                    else
                    {
                        Log($"Error downloading file: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error downloading file: {ex.Message}");
                }
            }
        }

        private async void AppendButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListView.SelectedItem == null)
            {
                Log("Please select a file to append to");
                return;
            }

            var inputDialog = new InputDialog("Enter text to append:");
            if (inputDialog.ShowDialog() == true)
            {
                try
                {
                    var filename = FilesListView.SelectedItem.ToString();
                    using (var content = new StringContent(inputDialog.Answer))
                    {
                        var response = await _client.PostAsync($"{_baseUrl}/api/files/{filename}", content);
                        var responseText = await response.Content.ReadAsStringAsync();
                        Log(responseText);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error appending to file: {ex.Message}");
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListView.SelectedItem == null)
            {
                Log("Please select a file to delete");
                return;
            }

            try
            {
                var filename = FilesListView.SelectedItem.ToString();
                var response = await _client.DeleteAsync($"{_baseUrl}/api/files/{filename}");
                var responseText = await response.Content.ReadAsStringAsync();
                Log(responseText);
                await RefreshFileList();
            }
            catch (Exception ex)
            {
                Log($"Error deleting file: {ex.Message}");
            }
        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListView.SelectedItem == null)
            {
                Log("Please select a file to copy");
                return;
            }

            var inputDialog = new InputDialog("Enter destination filename:");
            if (inputDialog.ShowDialog() == true)
            {
                try
                {
                    var sourceFilename = FilesListView.SelectedItem.ToString();
                    var response = await _client.PostAsync(
                        $"{_baseUrl}/api/files/copy?source={sourceFilename}&destination={inputDialog.Answer}", null);
                    var responseText = await response.Content.ReadAsStringAsync();
                    Log(responseText);
                    await RefreshFileList();
                }
                catch (Exception ex)
                {
                    Log($"Error copying file: {ex.Message}");
                }
            }
        }

        private async void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListView.SelectedItem == null)
            {
                Log("Please select a file to move");
                return;
            }

            var inputDialog = new InputDialog("Enter destination filename:");
            if (inputDialog.ShowDialog() == true)
            {
                try
                {
                    var sourceFilename = FilesListView.SelectedItem.ToString();
                    var response = await _client.PostAsync(
                        $"{_baseUrl}/api/files/move?source={sourceFilename}&destination={inputDialog.Answer}", null);
                    var responseText = await response.Content.ReadAsStringAsync();
                    Log(responseText);
                    await RefreshFileList();
                }
                catch (Exception ex)
                {
                    Log($"Error moving file: {ex.Message}");
                }
            }
        }

        private async Task RefreshFileList()
        {
            try
            {
                var response = await _client.GetAsync($"{_baseUrl}/api/files");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var files = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                    FilesListView.ItemsSource = files;
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing file list: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");
            LogTextBox.ScrollToEnd();
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                Log("Error: Not connected to any server. Connect first.");
                return;
            }

            Log("Reloading file list...");
            await RefreshFileList();
        }
    }
}