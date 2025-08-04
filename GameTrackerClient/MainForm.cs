using System.Diagnostics;
using System.Reflection;
using GameTrackerClient.Properties;

namespace GameTrackerClient;

public partial class MainForm : Form
{
    private readonly IpcClient _ipcClient = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private bool _isUpdating = false;
    private Dictionary<string, ProcessStats>? _fullProcessStatsCache;
    
    public MainForm()
    {
        InitializeComponent();
        processListView.SelectedIndexChanged += ProcessListView_SelectedIndexChanged;

        SetupTrayIcon();

        // Добавляем обработчик загрузки формы и настраиваем таймер.
        // Чтобы добавить обработчик Load, можно в дизайнере выбрать форму,
        // зайти в панель Properties -> Events (значок молнии) и дважды кликнуть на событие "Load".
        this.Load += MainForm_Load;
        _refreshTimer.Interval = 5000; // Обновление каждые 5 секунд
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    private void ProcessListView_SelectedIndexChanged(object sender, EventArgs e)
    {
        var hasSelection = processListView.SelectedItems.Count > 0;
        renameButton.Enabled = hasSelection;
        hideButton.Enabled = hasSelection;
    }
    
    private void SetupTrayIcon()
    {
        // В дизайнере нужно добавить NotifyIcon (trayIcon) и ContextMenuStrip (trayMenu)
        // и связать их. Здесь мы настраиваем их поведение.
        // trayIcon.Icon = SystemIcons.Application; // Временно, потом заменим на свою иконку
        
        // Загружаем иконку из встроенных ресурсов
        var assembly = Assembly.GetExecutingAssembly();
        // Имя ресурса формируется как: {DefaultNamespace}.{Папки}.{ИмяФайла}
        using var stream = assembly.GetManifestResourceStream("GameTrackerClient.Assets.app.ico");
        if (stream != null)
        {
            trayIcon.Icon = new Icon(stream);
        }
        
        trayIcon.Text = "Game Tracker";
        trayIcon.Visible = true;
 
        trayMenu.Items.Clear();
        trayMenu.Items.Add("Show", null, OnShow);
        trayMenu.Items.Add("Exit", null, OnExit);
 
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.DoubleClick += OnShow; // DoubleClick уже был, оставляем
    }
    
    private async Task FetchDataAndRefreshUiAsync()
    {
        if (_isUpdating) return;

        _isUpdating = true;
        refreshButton.Enabled = false;
        
        try
        {
            // 1. Получаем ПОЛНЫЙ список от службы и кэшируем его
            _fullProcessStatsCache = await Task.Run(() => _ipcClient.GetFullProcessStatsAsync());
            // 2. Применяем фильтры и обновляем UI
            ApplyFilterAndRefreshUi();
        }
        catch (Exception ex)
        {
            processListView.Items.Clear();
            var errorItem = new ListViewItem($"Ошибка: {ex.Message}");
            errorItem.ForeColor = Color.Red;
            processListView.Items.Add(errorItem);
        }
        finally
        {
            _isUpdating = false;
            refreshButton.Enabled = true;
        }
    }

    private void ApplyFilterAndRefreshUi()
    {
        if (_fullProcessStatsCache == null) return;

        var showHidden = Settings.Default.ShowHiddenApps;

        // Фильтруем на основе настройки. Скрытые - это те, у которых IsTracked = false
        var processesToDisplay = _fullProcessStatsCache
            .Where(p => showHidden || p.Value.IsTracked)
            .OrderBy(p => p.Value.DisplayNameOrPath);

        processListView.BeginUpdate(); // Для оптимизации обновления UI
        try
        {
            processListView.Items.Clear();
            foreach (var proc in processesToDisplay)
            {
                var stats = proc.Value;
                var displayName = stats.DisplayName ?? Path.GetFileName(proc.Key);

                // Для запущенных процессов добавляем время с момента запуска
                var totalSeconds = stats.TotalSeconds;
                if (stats.IsRunning && stats.LastStartTime.HasValue)
                {
                    var runningTime = (int)(DateTime.UtcNow - stats.LastStartTime.Value).TotalSeconds;
                    totalSeconds += runningTime;
                }

                var timeSpan = TimeSpan.FromSeconds(totalSeconds);
                var timeFormatted = timeSpan.ToString(@"hh\:mm\:ss");

                var item = new ListViewItem(displayName);
                item.SubItems.Add(timeFormatted);
                item.Tag = proc.Key;

                if (!stats.IsTracked)
                {
                    item.ForeColor = Color.Gray;
                }
                if (stats.IsRunning)
                {
                    item.Font = new Font(item.Font, FontStyle.Bold);
                }

                processListView.Items.Add(item);
            }
        }
        finally
        {
            processListView.EndUpdate();
        }
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        await FetchDataAndRefreshUiAsync();
        _refreshTimer.Start();
    }

    private async void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        // Обновляем, только если окно видимо, чтобы не тратить ресурсы впустую
        if (this.Visible)
        {
            await FetchDataAndRefreshUiAsync();
        }
    }
    
    private async void refreshButton_Click(object sender, EventArgs e)
    {
        await FetchDataAndRefreshUiAsync();
    }
    
    private async void renameButton_Click(object sender, EventArgs e)
    {
        if (processListView.SelectedItems.Count == 0) return;

        var selectedItem = processListView.SelectedItems[0];
        var path = (string)selectedItem.Tag;
        var currentName = selectedItem.Text;

        // Создаем кастомную форму для ввода, так как MessageBox не поддерживает поля ввода.
        using var dialogForm = new Form
        {
            Text = "Rename Game",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(350, 130),
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false
        };

        var promptLabel = new Label
        {
            Text = "Enter new name for the game:",
            Location = new Point(12, 15),
            AutoSize = true
        };

        var nameTextBox = new TextBox
        {
            Text = currentName,
            Location = new Point(15, 40),
            Size = new Size(320, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(179, 85), Size = new Size(75, 25) };
        dialogForm.AcceptButton = okButton;

        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(260, 85), Size = new Size(75, 25) };
        dialogForm.CancelButton = cancelButton;

        // Добавляем созданные элементы на форму
        dialogForm.Controls.Add(promptLabel);
        dialogForm.Controls.Add(nameTextBox);
        dialogForm.Controls.Add(okButton);
        dialogForm.Controls.Add(cancelButton);

        // Показываем диалог и ждем, пока пользователь нажмет OK или Cancel
        if (dialogForm.ShowDialog(this) == DialogResult.OK)
        {
            var newName = nameTextBox.Text;
            if (!string.IsNullOrWhiteSpace(newName))
            {
                var success = await _ipcClient.SetProcessNameAsync(path, newName);
                if (!success)
                {
                    MessageBox.Show("Failed to rename the game.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    await FetchDataAndRefreshUiAsync();
                }
            }
        }
    }

    private async void hideButton_Click(object sender, EventArgs e)
    {
        if (processListView.SelectedItems.Count == 0) return;

        var selectedItem = processListView.SelectedItems[0];
        var path = (string)selectedItem.Tag;

        var result = MessageBox.Show(
            "Are you sure you want to hide this game?\nIt will no longer be tracked.",
            "Hide Game",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            var success = await _ipcClient.SetProcessTrackingAsync(path, false);
            if (!success)
            {
                MessageBox.Show("Failed to hide the game.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                await FetchDataAndRefreshUiAsync();
            }
        }
    }
    
    private async void OnShow(object? sender, EventArgs e)
    {
        this.Show();
        this.WindowState = FormWindowState.Normal;
    }
 
    private void OnExit(object? sender, EventArgs e)
    {
        // Прячем иконку перед выходом, чтобы она не "зависала" в трее
        trayIcon.Visible = false;
        Application.Exit();
    }
 
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Если пользователь нажал на "крестик" (UserClosing), 
        // мы не закрываем приложение, а просто прячем форму. 
        // Выход будет через меню в трее (что вызовет Application.Exit() и другую причину закрытия).
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    private async void settingsButton_Click(object sender, EventArgs e)
    {
        using var settingsForm = new SettingsForm(_ipcClient);
        if (settingsForm.ShowDialog(this) == DialogResult.OK)
        {
            // Настройки (папки или фильтр) могли измениться.
            // Выполняем полное обновление данных и перерисовку.
            await FetchDataAndRefreshUiAsync();
        }
    }
}