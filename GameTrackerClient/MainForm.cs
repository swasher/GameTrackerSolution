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
        
        // Включаем двойную буферизацию для ListView, чтобы убрать мерцание.
        // Свойство DoubleBuffered защищено, поэтому используем рефлексию.
        typeof(ListView).InvokeMember("DoubleBuffered",
            BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
            null, processListView, new object[] { true });
        
        processListView.SelectedIndexChanged += ProcessListView_SelectedIndexChanged;

        SetupTrayIcon();

        // Добавляем обработчик загрузки формы и настраиваем таймер.
        // Чтобы добавить обработчик Load, можно в дизайнере выбрать форму,
        // зайти в панель Properties -> Events (значок молнии) и дважды кликнуть на событие "Load".
        this.Load += MainForm_Load;
        _refreshTimer.Interval = 1000; // Обновление каждые 5 секунд
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
            // 1. Создаем словарь существующих элементов для быстрого доступа по ключу (пути)
            var existingItemsMap = processListView.Items.Cast<ListViewItem>()
                .ToDictionary(item => (string)item.Tag, item => item);
     
            // 2. Обновляем существующие и добавляем новые элементы
            foreach (var proc in processesToDisplay)
            {
                var stats = proc.Value;
                if (existingItemsMap.TryGetValue(stats.Path, out var existingItem))
                {
                    // Элемент уже есть, обновляем его
                    UpdateListViewItem(existingItem, stats);
                    // Удаляем из словаря, чтобы пометить его как "обработанный"
                    existingItemsMap.Remove(stats.Path);
                }
                else
                {
                    // Элемента нет, создаем и добавляем новый
                    var newItem = CreateListViewItem(stats);
                    processListView.Items.Add(newItem);
                }
            }
     
            // 3. Удаляем элементы, которые остались в словаре (т.е. их нет в новом списке)
            foreach (var itemToRemove in existingItemsMap.Values)
            {
                processListView.Items.Remove(itemToRemove);
            }
        }
        finally
        {
            processListView.EndUpdate();
        }
    }

    
    private ListViewItem CreateListViewItem(ProcessStats stats)
    {
        // Создаем пустой элемент с ключом (Tag)
        var item = new ListViewItem { Tag = stats.Path };
        // Заполняем его данными
        UpdateListViewItem(item, stats);
        return item;
    }
 
    private void UpdateListViewItem(ListViewItem item, ProcessStats stats)
    {
        var displayName = stats.DisplayName ?? Path.GetFileName(stats.Path);
 
        var totalSeconds = stats.TotalSeconds;
        if (stats.IsRunning && stats.LastStartTime.HasValue)
        {
            totalSeconds += (int)(DateTime.UtcNow - stats.LastStartTime.Value).TotalSeconds;
        }
 
        var timeFormatted = TimeSpan.FromSeconds(totalSeconds).ToString(@"hh\:mm\:ss");
 
        // Обновляем данные, только если они изменились, чтобы избежать лишней перерисовки
        if (item.Text != displayName) item.Text = displayName;
 
        if (item.SubItems.Count < 2) item.SubItems.Add(timeFormatted);
        else if (item.SubItems[1].Text != timeFormatted) item.SubItems[1].Text = timeFormatted;
 
        var newFont = stats.IsRunning ? new Font(processListView.Font, FontStyle.Bold) : processListView.Font;
        if (!item.Font.Equals(newFont)) item.Font = newFont;
 
        var newColor = stats.IsTracked ? processListView.ForeColor : Color.Gray;
        if (item.ForeColor != newColor) item.ForeColor = newColor;
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