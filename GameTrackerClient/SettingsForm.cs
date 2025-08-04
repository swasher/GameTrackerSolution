using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTrackerClient.Properties;
using System.Windows.Forms;

namespace GameTrackerClient;

public partial class SettingsForm : Form
{
    private readonly IpcClient _ipcClient;

    public SettingsForm(IpcClient ipcClient)
    {
        InitializeComponent();
        _ipcClient = ipcClient;

        // Настраиваем кнопки для стандартного поведения диалогового окна
        saveButton.DialogResult = DialogResult.OK;
        cancelButton.DialogResult = DialogResult.Cancel;
    }

    private async void SettingsForm_Load(object sender, EventArgs e)
    {
        // Загружаем состояние чекбокса из настроек
        showHiddenCheckBox.Checked = Settings.Default.ShowHiddenApps;

        try
        {
            var directories = await _ipcClient.GetWatchedDirectoriesAsync();
            foldersListBox.Items.Clear();
            foreach (var dir in directories)
            {
                foldersListBox.Items.Add(dir);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to load settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Закрываем форму, так как без данных она бесполезна
            this.DialogResult = DialogResult.Cancel;
        }
    }

    private void addFolderButton_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = "Select a folder to watch for games";
        dialog.ShowNewFolderButton = true;

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            foldersListBox.Items.Add(dialog.SelectedPath);
        }
    }

    private void removeFolderButton_Click(object sender, EventArgs e)
    {
        if (foldersListBox.SelectedItem != null)
        {
            foldersListBox.Items.Remove(foldersListBox.SelectedItem);
        }
    }

    private async void saveButton_Click(object sender, EventArgs e)
    {
        var newDirectories = foldersListBox.Items.Cast<string>().ToList();
        var success = await _ipcClient.SetWatchedDirectoriesAsync(newDirectories);

        if (!success)
        {
            MessageBox.Show("Failed to save settings to the service.", "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            // Предотвращаем закрытие окна, если сохранение не удалось
            this.DialogResult = DialogResult.None;
        }
        else
        {
            // Сохраняем состояние чекбокса в настройки
            Settings.Default.ShowHiddenApps = showHiddenCheckBox.Checked;
            Settings.Default.Save();
        }
    }
}