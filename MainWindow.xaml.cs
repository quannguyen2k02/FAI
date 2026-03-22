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
        private System.Windows.Controls.TextBox _txtSN;
        private System.Windows.Controls.Button _btnCheckSN;
        private System.Windows.Controls.TextBlock _txtManualResult;
        private System.Windows.Controls.TextBlock _txtNextRun;


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

            _txtSN = this.FindName("txtSN") as System.Windows.Controls.TextBox;
            _btnCheckSN = this.FindName("btnCheckSN") as System.Windows.Controls.Button;
            _txtManualResult = this.FindName("txtManualResult") as System.Windows.Controls.TextBlock;



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
                try
                {
                    await Task.Run(() => RunTask());
                }
                catch (System.Exception ex)
                {

                    File.AppendAllText("error.log", $"{DateTime.Now}: {ex}\n");
                }

                SetNextRunTime();

            }
        }
        private void RunTask()
        {
            try
            {

                switch (deviceName)
                {
                    case "KB_NEW":
                        KB_NEW.ProcessLogs(logPath, destPath);
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
                    case "LCD":
                        LCD.ProcessLogs(logPath, destPath);
                        break;
                    case "IO":
                        IO.ProcessLogs(logPath, destPath);
                        break;
                    default:
                        throw new System.Exception($"Không hỗ trợ deviceName: {deviceName}");
                }


            }
            catch (System.Exception ex)
            {

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



            try
            {
                if (deviceName == "LED")
                {
                    if (_txtManualResult != null) _txtManualResult.Text = "Đang kiểm tra...";
                    var result = await Task.Run(() => LED.CreateFolderForSN(logPath, destPath, sn));
                    if (_txtManualResult != null) _txtManualResult.Text = result;

                }
                else if (deviceName == "IO")
                {
                    if (_txtManualResult != null) _txtManualResult.Text = "Đang kiểm tra...";
                    var result = await Task.Run(() => IO.CreateFolderForSN(logPath, destPath, sn));
                    if (_txtManualResult != null) _txtManualResult.Text = result;

                }

                else if (deviceName == "RGB_NEW")
                {
                    if (_txtManualResult != null) _txtManualResult.Text = "Đang kiểm tra...";
                    var result = await Task.Run(() => RGB_NEW.CreateFolderForSN(logPath, destPath, sn));
                    if (_txtManualResult != null) _txtManualResult.Text = result;

                }
                else if (deviceName == "RGB_OLD")
                {
                    if (_txtManualResult != null) _txtManualResult.Text = "Đang kiểm tra...";
                    var result = await Task.Run(() => RGB_OLD.CreateFolderForSN(logPath, destPath,line, sn));
                    if (_txtManualResult != null) _txtManualResult.Text = result;

                }
                else if (deviceName == "BlackEdge")
                {
                    if (_txtManualResult != null) _txtManualResult.Text = "Đang kiểm tra...";
                    var result = await Task.Run(() => BlackEdge.CreateFolderForSN(logPath, destPath, sn));
                    if (_txtManualResult != null) _txtManualResult.Text = result;

                }
                else
                {
                    if (_txtManualResult != null) _txtManualResult.Text = $"Manual SN chỉ hỗ trợ LED và IO hiện tại (device={deviceName}).";
                }
            }
            catch (System.Exception ex)
            {
                if (_txtManualResult != null) _txtManualResult.Text = $"Lỗi: {ex.Message}";

                File.AppendAllText("error.log", $"{DateTime.Now}: {ex}\n");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                switch (deviceName)
                {
                    case "KB_NEW":
                        KB_NEW.ProcessLogs(logPath, destPath);
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
                    case "LCD":
                        LCD.ProcessLogs(logPath, destPath);
                        break;
                    case "IO":
                        IO.ProcessLogs(logPath, destPath);
                        break;
                    default:
                        throw new System.Exception($"Không hỗ trợ deviceName: {deviceName}");
                }


            }
            catch (System.Exception ex)
            {

                File.AppendAllText("error.log", $"{DateTime.Now}: {ex}\n");
            }
        }
    }
}