using System.Globalization;
using System.IO;
using System.Reflection;

namespace FAI
{
    public static class LED
    {
        private static readonly object logLock = new object();

        public static List<string> ProcessLogs(string logPath, string destinationPath, DateTime? date = null)
        {
            var resultFiles = new List<string>();
            DateTime targetDate = date ?? DateTime.Today;

            // Đường dẫn file log
            string logFile = GetLogFilePath(targetDate);
            WriteLog(logFile, $"=== Bắt đầu xử lý LED lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            WriteLog(logFile, $"LogPath: {logPath}, DestinationPath: {destinationPath}, TargetDate: {targetDate:yyyy-MM-dd}");

            // Xây dựng đường dẫn file nguồn
            string yearMonth = targetDate.ToString("yyyyMM");
            string yearMonthDay = targetDate.ToString("yyyyMMdd");
            string sourceFile = Path.Combine(logPath, yearMonth, $"{yearMonthDay}.csv");

            if (!File.Exists(sourceFile))
            {
                WriteLog(logFile, $"Lỗi: Không tìm thấy file nguồn: {sourceFile}");
                throw new FileNotFoundException($"Không tìm thấy file nguồn: {sourceFile}");
            }

            // Đọc và phân tích CSV
            var records = ReadCsv(sourceFile);
            WriteLog(logFile, $"Đọc {records.Count} dòng từ file CSV.");

            // Sắp xếp theo thời gian thực (mới nhất lên đầu)
            var sorted = records.OrderByDescending(r => r.DateTime).ToList();

            // Lấy 3 bản ghi Pass mới nhất
            var passRecords = sorted.Where(r => r.Result == "Pass").Take(3).ToList();
            WriteLog(logFile, $"Tìm thấy {passRecords.Count} bản ghi Pass mới nhất.");

            // Lấy 1 bản ghi mới nhất có chứa "Fail"
            var failRecord = sorted.FirstOrDefault(r => r.Result.Contains("Fail", StringComparison.OrdinalIgnoreCase));
            if (failRecord != null)
                WriteLog(logFile, $"Tìm thấy bản ghi Fail: {failRecord.SN}");
            else
                WriteLog(logFile, "Không tìm thấy bản ghi Fail.");

            // Thư mục đầu ra theo ngày
            string dateFolder = targetDate.ToString("yyyy_MM_dd");
            string baseOutput = Path.Combine(destinationPath, dateFolder);
            WriteLog(logFile, $"Thư mục đích: {baseOutput}");

            // Thời gian gốc và offset
            DateTime baseTime = DateTime.Now;
            int minuteOffset = 0;
            Random rand = new Random();

            // Lưu các bản ghi Pass
            foreach (var rec in passRecords)
            {
                string sn = rec.SN;
                string outputDir = Path.Combine(baseOutput, "OK", sn);
                Directory.CreateDirectory(outputDir);
                string outputFile = Path.Combine(outputDir, $"{sn}.csv");
                WriteSingleRecord(outputFile, rec);
                resultFiles.Add(outputFile);

                DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                Directory.SetLastWriteTime(outputDir, targetTime);
                File.SetLastWriteTime(outputFile, targetTime);

                WriteLog(logFile, $"Đã tạo thư mục Pass: {outputDir}, set thời gian: {targetTime:HH:mm:ss}");
                minuteOffset += rand.Next(1, 6);
            }

            // Lưu bản ghi Fail: tạo 3 thư mục con
            if (failRecord != null)
            {
                string sn = failRecord.SN;

                for (int i = 0; i < 3; i++)
                {
                    string folderName = $"{sn}_{i}";
                    string outputDir = Path.Combine(baseOutput, "NG", folderName);
                    Directory.CreateDirectory(outputDir);
                    string outputFile = Path.Combine(outputDir, $"{sn}.csv");
                    WriteSingleRecord(outputFile, failRecord);

                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                    Directory.SetLastWriteTime(outputDir, targetTime);
                    File.SetLastWriteTime(outputFile, targetTime);

                    resultFiles.Add(outputFile);
                    WriteLog(logFile, $"Đã tạo thư mục Fail: {outputDir}, set thời gian: {targetTime:HH:mm:ss}");
                    minuteOffset += rand.Next(1, 6);
                }
            }

            WriteLog(logFile, $"Hoàn thành. Tổng số file đã tạo: {resultFiles.Count}");
            WriteLog(logFile, $"=== Kết thúc lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" + Environment.NewLine);
            return resultFiles;
        }

        // New: create folder for specific SN (manual mode). Returns a status string.
        public static string CreateFolderForSN(string logPath, string destinationPath, string sn, DateTime? date = null)
        {
            DateTime targetDate = date ?? DateTime.Today;
            string yearMonth = targetDate.ToString("yyyyMM");
            string yearMonthDay = targetDate.ToString("yyyyMMdd");
            string sourceFile = Path.Combine(logPath, yearMonth, $"{yearMonthDay}.csv");

            string logFile = GetLogFilePath(targetDate);

            if (!File.Exists(sourceFile))
            {
                WriteLog(logFile, $"Lỗi: Không tìm thấy file nguồn: {sourceFile}");
                return $"Không tìm thấy file nguồn: {sourceFile}";
            }

            var records = ReadCsv(sourceFile);
            var found = records.FirstOrDefault(r => string.Equals(r.SN, sn, StringComparison.OrdinalIgnoreCase));
            if (found == null)
            {
                WriteLog(logFile, $"SN {sn} không tồn tại trong file {sourceFile}");
                return $"SN {sn} không tồn tại trong báo cáo.";
            }

            string dateFolder = targetDate.ToString("yyyy_MM_dd");
            string baseOutput = Path.Combine(destinationPath, dateFolder);

            DateTime baseTime = DateTime.Now;

            if (found.Result.Contains("Fail", StringComparison.OrdinalIgnoreCase))
            {
                // create 3 NG folders
                for (int i = 0; i < 3; i++)
                {
                    string folderName = $"{found.SN}_{i}";
                    string outputDir = Path.Combine(baseOutput, "NG", folderName);
                    Directory.CreateDirectory(outputDir);
                    string outputFile = Path.Combine(outputDir, $"{found.SN}.csv");
                    WriteSingleRecord(outputFile, found);

                    DateTime targetTime = baseTime.AddMinutes(i + 1);
                    Directory.SetLastWriteTime(outputDir, targetTime);
                    File.SetLastWriteTime(outputFile, targetTime);
                    WriteLog(logFile, $"(Manual) Tạo NG: {outputDir}");
                }

                return $"SN {sn} là FAIL - đã tạo 3 thư mục NG.";
            }
            else
            {
                // create single OK folder
                string outputDir = Path.Combine(baseOutput, "OK", found.SN);
                Directory.CreateDirectory(outputDir);
                string outputFile = Path.Combine(outputDir, $"{found.SN}.csv");
                WriteSingleRecord(outputFile, found);

                DateTime targetTime = baseTime;
                Directory.SetLastWriteTime(outputDir, targetTime);
                File.SetLastWriteTime(outputFile, targetTime);
                WriteLog(logFile, $"(Manual) Tạo OK: {outputDir}");

                return $"SN {sn} là PASS - đã tạo thư mục OK.";
            }
        }

        private static string GetLogFilePath(DateTime date)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string logDir = Path.Combine(exeDir, "Log");
            Directory.CreateDirectory(logDir);
            return Path.Combine(logDir, $"log_{date:yyyyMMdd}.txt");
        }

        private static void WriteLog(string logFile, string message)
        {
            try
            {
                lock (logLock)
                {
                    using (var writer = new StreamWriter(logFile, true))
                    {
                        writer.WriteLine($"{DateTime.Now:HH:mm:ss} - {message}");
                    }
                }
            }
            catch { }
        }

        private static List<TestRecord> ReadCsv(string filePath)
        {
            var records = new List<TestRecord>();
            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0) return records;

            string header = lines[0];
            var columns = header.Split(',');

            string[] formats = {
                "M/d/yyyy h:mm:ss tt",
                "M/d/yyyy h:mm tt",
                "M/d/yyyy H:mm",
                "M/d/yyyy H:mm:ss"
            };

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Split(',');
                if (values.Length != columns.Length) continue;

                if (!DateTime.TryParseExact(values[0], formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    continue;
                }

                var record = new TestRecord
                {
                    DateTime = parsedDate,
                    SN = values[1],
                    ModelName = values[2],
                    TypeModel = values[3],
                    DeviceName = values[4],
                    Result = values[5]
                };
                records.Add(record);
            }

            return records;
        }

        private static void WriteSingleRecord(string outputFile, TestRecord record)
        {
            using (var writer = new StreamWriter(outputFile))
            {
                writer.WriteLine("SN,ModelName,TypeModel,DeviceName,Result");
                writer.WriteLine($"{record.SN},{record.ModelName},{record.TypeModel},{record.DeviceName},{record.Result}");
            }
        }

        private class TestRecord
        {
            public DateTime DateTime { get; set; }
            public string SN { get; set; }
            public string ModelName { get; set; }
            public string TypeModel { get; set; }
            public string DeviceName { get; set; }
            public string Result { get; set; }
        }
    }
}