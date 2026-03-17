using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace FAI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private DateTime nextRunTime;
        private string logPath;
        private string destPath;
        private string deviceName;
        private string line;
        public MainWindow()
        {
            InitializeComponent();
            logPath = ConfigurationManager.AppSettings["LogPath"];
            destPath = ConfigurationManager.AppSettings["DesFolderPath"];
            deviceName = ConfigurationManager.AppSettings["DeviceName"];
            line = ConfigurationManager.AppSettings["Line"];
            string runTimeStr = ConfigurationManager.AppSettings["RunTime"];
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



        }
        private void SetNextRunTime()
        {
            DateTime now = DateTime.Now;

            DateTime scheduledTime = new DateTime(now.Year, now.Month, now.Day, 16, 28, 0);
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
                await Task.Run(() => RunTask());
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
                        throw new Exception($"Không hỗ trợ deviceName: {deviceName}");
                }

                AddLog($"Hoàn thành lúc {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                AddLog($"Lỗi lúc {DateTime.Now:HH:mm:ss}: {ex.Message}");
                File.AppendAllText("error.log", $"{DateTime.Now}: {ex}\n");
            }
        }
        public void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                lstLog.Items.Insert(0, message);
                while (lstLog.Items.Count > 100)
                    lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
            });
        }

        public void UpdateNextRunDisplay()
        {
            txtNextRun.Text = $"Lần chạy tiếp theo: {nextRunTime:dd/MM/yyyy HH:mm}";
        }
    }
}