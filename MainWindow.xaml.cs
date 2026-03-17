using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace FAI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Backing references to UI controls (avoid name collision with generated fields)
        private System.Windows.Controls.CheckBox _chkAutoCreate;
        private System.Windows.Controls.TextBox _txtSN;
        private System.Windows.Controls.Button _btnCheckSN;
        private System.Windows.Controls.TextBlock _txtManualResult;
        private System.Windows.Controls.TextBlock _txtNextRun;
        private System.Windows.Controls.ListBox _lstLog;

        private DispatcherTimer timer;
        private DateTime nextRunTime;
        private string logPath;
        private string destPath;
        private string deviceName;
        private string line;
        private TimeSpan scheduledTimeOfDay = new TimeSpan(16, 28, 0); // default

        public MainWindow()
        {
            InitializeComponent();

            // Initialize control references
            _chkAutoCreate = this.FindName("chkAutoCreate") as System.Windows.Controls.CheckBox;
            _txtSN = this.FindName("txtSN") as System.Windows.Controls.TextBox;
            _btnCheckSN = this.FindName("btnCheckSN") as System.Windows.Controls.Button;
            _txtManualResult = this.FindName("txtManualResult") as System.Windows.Controls.TextBlock;
            _txtNextRun = this.FindName("txtNextRun") as System.Windows.Controls.TextBlock;
            _lstLog = this.FindName("lstLog") as System.Windows.Controls.ListBox;

            logPath = ConfigurationManager.AppSettings["LogPath"];
            destPath = ConfigurationManager.AppSettings["DesFolderPath"];
            deviceName = ConfigurationManager.AppSettings["DeviceName"];
            line = ConfigurationManager.AppSettings["Line"];
            string runTimeStr = ConfigurationManager.AppSettings["RunTime"];

            // Parse RunTime from config (supports "HH:mm", "HH:mm:ss" or full DateTime)
            if (!string.IsNullOrEmpty(runTimeStr))
            {
                if (!TimeSpan.TryParse(runTimeStr, out var parsedTime))
                {
                    if (System.DateTime.TryParse(runTimeStr, out var dt))
                    {
                        parsedTime = dt.TimeOfDay;
                    }
                }

                // If parsedTime is valid (non-zero) use it; otherwise keep default
                if (parsedTime != default(TimeSpan))
                    scheduledTimeOfDay = parsedTime;
            }

            if (string.IsNullOrEmpty(logPath) || string.IsNullOrEmpty(destPath) || string.IsNullOrEmpty(deviceName))
            {
                MessageBox.Show("Thiếu thông tin cấu hình trong App.config", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }
            // Khởi tạo timer
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            SetNextRunTime();
            timer.Start();

            UpdateNextRunDisplay();

            // Ensure manual controls start in a clean state
            if (_txtManualResult != null)
                _txtManualResult.Text = string.Empty;
        }
        private void SetNextRunTime()
        {
            DateTime now = DateTime.Now;

            DateTime scheduledTime = new DateTime(now.Year, now.Month, now.Day, scheduledTimeOfDay.Hours, scheduledTimeOfDay.Minutes, scheduledTimeOfDay.Seconds);
            if (now > scheduledTime)
                scheduledTime = scheduledTime.AddDays(1);
            nextRunTime = scheduledTime;
            timer.Interval = nextRunTime - now;
            if (timer.Interval < TimeSpan.Zero)
                timer.Interval = TimeSpan.FromMinutes(1);
        }
        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now >= nextRunTime)
            {
                // Respect the Auto Create checkbox
                try
                {
                    bool autoCreate = _chkAutoCreate != null && _chkAutoCreate.IsChecked == true;
                    if (autoCreate)
                    {
                        await Task.Run(() => RunTask());
                    }
                    else
                    {
                        AddLog($"Tự động tạo thư mục bị tắt - bỏ qua lần chạy lúc {DateTime.Now:HH:mm:ss}");
                    }
                }
                catch (System.Exception ex)
                {
                    AddLog($"Lỗi khi thực hiện tác vụ tự động: {ex.Message}");
                    File.AppendAllText("error.log", $"{DateTime.Now}: {ex}\n");
                }

                SetNextRunTime();
                UpdateNextRunDisplay();
            }
        }
        private void RunTask()
        {
            try
            {
                AddLog($"Bắt đầu xử lý lúc {DateTime.Now:HH:mm:ss}");

                switch (deviceName)
                {
                    case "KB_NEW":
                        KB_NEW.CreateFoldersFromFiles(logPath, destPath);
                        break;
                    case "KB_OLD":
                        KB_OLD.CreateFoldersFromFiles(logPath, destPath);
                        break;
                    case "LED":
                        LED.ProcessLogs(logPath, destPath);
                        break;
                    case "RGB_NEW":
                        RGB_NEW.CopyLatestFolders(logPath, destPath);
                        break;
                    case "RGB_OLD":
                        RGB_OLD.CopyLatestFolders(logPath, destPath, line);
                        break;
                    case "BlackEdge":
                        BlackEdge.ProcessLogs(logPath, destPath);
                        break;
                    case "IO":
                        IO.ProcessLogs(logPath, destPath);
                        break;
                    default:
                        throw new System.Exception($"Không hỗ trợ deviceName: {deviceName}");
                }

                AddLog($"Hoàn thành lúc {DateTime.Now:HH:mm:ss}");
            }
            catch (System.Exception ex)
            {
                AddLog($"Lỗi lúc {DateTime.Now:HH:mm:ss}: {ex.Message}");
                File.AppendAllText("error.log", $"{DateTime.Now}: {ex}\n");
            }
        }

        // Manual SN check handler (wired from XAML)
        private async void btnCheckSN_Click(object sender, RoutedEventArgs e)
        {
            string sn = _txtSN != null ? _txtSN.Text?.Trim() : string.Empty;
            if (string.IsNullOrEmpty(sn))
            {
                if (_txtManualResult != null) _txtManualResult.Text = "Vui lòng nhập SN.";
                return;
            }

            AddLog($"Kiểm tra SN thủ công: {sn}");

            try
            {
                if (deviceName == "LED")
                {
                    if (_txtManualResult != null) _txtManualResult.Text = "Đang kiểm tra...";
                    var result = await Task.Run(() => LED.CreateFolderForSN(logPath, destPath, sn));
                    if (_txtManualResult != null) _txtManualResult.Text = result;
                    AddLog(result);
                }
                else if (deviceName == "IO")
                {
                    if (_txtManualResult != null) _txtManualResult.Text = "Đang kiểm tra...";
                    var result = await Task.Run(() => IO.CreateFolderForSN(logPath, destPath, sn));
                    if (_txtManualResult != null) _txtManualResult.Text = result;
                    AddLog(result);
                }
                else if (deviceName == "RGB_NEW")
                {
                    if (_txtManualResult != null) _txtManualResult.Text = "Đang kiểm tra...";
                    var result = await Task.Run(() => RGB_NEW.CreateFolderForSN(logPath, destPath, sn));
                    if (_txtManualResult != null) _txtManualResult.Text = result;
                    AddLog(result);
                }
                else
                {
                    if (_txtManualResult != null) _txtManualResult.Text = $"Manual SN chỉ hỗ trợ LED và IO hiện tại (device={deviceName}).";
                }
            }
            catch (System.Exception ex)
            {
                if (_txtManualResult != null) _txtManualResult.Text = $"Lỗi: {ex.Message}";
                AddLog($"Lỗi khi kiểm tra SN: {ex.Message}");
                File.AppendAllText("error.log", $"{DateTime.Now}: {ex}\n");
            }
        }

        public void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (_lstLog != null)
                {
                    _lstLog.Items.Insert(0, message);
                    while (_lstLog.Items.Count > 100)
                        _lstLog.Items.RemoveAt(_lstLog.Items.Count - 1);
                }
            });
        }

        public void UpdateNextRunDisplay()
        {
            if (_txtNextRun != null)
                _txtNextRun.Text = $"Lần chạy tiếp theo: {nextRunTime:dd/MM/yyyy HH:mm}";
        }
    }
}