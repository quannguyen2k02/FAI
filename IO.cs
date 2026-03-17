//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;

//namespace FAI
//{
//    public static class BlackEdge
//    {

//        public static List<string> ProcessLogs(string logPath, string destinationPath, DateTime? date = null)
//        {
//            var resultFiles = new List<string>();
//            DateTime targetDate = date ?? DateTime.Today;

//            // Xây dựng đường dẫn file nguồn
//            string yearMonthDay = targetDate.ToString("yyyyMMdd");
//            string sourceFile = Path.Combine(logPath, yearMonthDay, $"{yearMonthDay}.csv");

//            if (!File.Exists(sourceFile))
//            {
//                throw new FileNotFoundException($"Không tìm thấy file nguồn: {sourceFile}");
//            }

//            // Đọc và phân tích CSV
//            var records = ReadCsv(sourceFile);

//            // Sắp xếp theo thời gian thực (mới nhất lên đầu)
//            var sorted = records.OrderByDescending(r => r.Date).ToList();

//            // Lấy 3 bản ghi Pass mới nhất
//            var passRecords = sorted.Where(r => r.Result == "PASS").Take(3).ToList();

//            // Lấy 1 bản ghi mới nhất có chứa "Fail"
//            var failRecord = sorted.FirstOrDefault(r => r.Result.Contains("Fail", StringComparison.OrdinalIgnoreCase));

//            // Thư mục đầu ra theo ngày
//            string dateFolder = targetDate.ToString("yyyy_MM_dd");
//            string baseOutput = Path.Combine(destinationPath, dateFolder);

//            // Thời gian gốc và offset (mỗi bản ghi cách nhau 1 phút)
//            DateTime baseTime = DateTime.Now;
//            int minuteOffset = 0;
//            Random rand = new Random(); // Khởi tạo Random

//            // Lưu các bản ghi Pass (mỗi Pass tạo một thư mục riêng)
//            foreach (var rec in passRecords)
//            {
//                string sn = rec.SN;
//                string outputDir = Path.Combine(baseOutput, "OK", sn);
//                Directory.CreateDirectory(outputDir);
//                string outputFile = Path.Combine(outputDir, $"{sn}.csv");
//                WriteSingleRecord(outputFile, rec);

//                // Set thời gian modified cho thư mục và file
//                DateTime targetTime = baseTime.AddMinutes(minuteOffset);
//                Directory.SetLastWriteTime(outputDir, targetTime);
//                File.SetLastWriteTime(outputFile, targetTime);

//                resultFiles.Add(outputFile);
//                minuteOffset += rand.Next(1, 6);
//            }

//            // Lưu bản ghi Fail: tạo 3 thư mục con (theo yêu cầu)
//            if (failRecord != null)
//            {
//                string sn = failRecord.SN;

//                for (int i = 0; i < 3; i++)
//                {
//                    string folderName = $"{sn}_{i}";
//                    string outputDir = Path.Combine(baseOutput, "NG", folderName);
//                    Directory.CreateDirectory(outputDir);
//                    string outputFile = Path.Combine(outputDir, $"{sn}.csv");
//                    WriteSingleRecord(outputFile, failRecord);

//                    // Set thời gian modified cho thư mục và file
//                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
//                    Directory.SetLastWriteTime(outputDir, targetTime);
//                    File.SetLastWriteTime(outputFile, targetTime);

//                    resultFiles.Add(outputFile);
//                    minuteOffset += rand.Next(1, 6);
//                }
//            }

//            return resultFiles;
//        }

//        /// <summary>
//        /// Phiên bản bất đồng bộ của ProcessLogs, phù hợp cho WPF để không block UI.
//        /// </summary>
//        public static Task<List<string>> ProcessLogsAsync(string logPath, string destinationPath, DateTime? date = null)
//        {
//            return Task.Run(() => ProcessLogs(logPath, destinationPath, date));
//        }

//        private static List<TestRecord> ReadCsv(string filePath)
//        {
//            var records = new List<TestRecord>();
//            var lines = File.ReadAllLines(filePath);
//            if (lines.Length == 0) return records;

//            string header = lines[0];
//            var columns = header.Split(',');

//            // Các định dạng ngày tháng có thể có trong file
//            string[] formats = {
//                "M/d/yyyy h:mm:ss tt",   // 2/10/2026 8:40:00 AM
//                "M/d/yyyy h:mm tt",       // 2/10/2026 8:40 AM
//                "M/d/yyyy H:mm",           // 2/10/2026 8:40 (24h)
//                "M/d/yyyy H:mm:ss",     // 2/10/2026 8:40:00 (24h)
//                "h:mm:ss tt",  // 12:40:11 AM
//                "h:mm:ss"  // 12:40:11 AM
//            };

//            for (int i = 1; i < lines.Length; i++)
//            {
//                string line = lines[i];
//                if (string.IsNullOrWhiteSpace(line)) continue;

//                var values = line.Split(',');
//                if (values.Length != columns.Length) continue;
//                string timeStr = values[2].Trim();
//                if (!DateTime.TryParseExact(timeStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
//                {
//                    // Bỏ qua dòng lỗi (có thể log nếu cần)
//                    continue;
//                }

//                var record = new TestRecord
//                {
//                    Line = values[0],
//                    Product_Type = values[1],
//                    Date = parsedDate,
//                    CycleTime = values[3],
//                    SN = values[4],
//                    Result = values[5]
//                    // Bạn có thể thêm các trường khác nếu cần
//                };
//                records.Add(record);
//            }


//            return records;
//        }

//        private static void WriteSingleRecord(string outputFile, TestRecord record)
//        {
//            using (var writer = new StreamWriter(outputFile))
//            {
//                // Header (chỉ ghi các cột bạn muốn)
//                writer.WriteLine("Line,ModelName,SN,Result");
//                // Dòng dữ liệu
//                writer.WriteLine($"{record.Line},{record.Product_Type},{record.SN},{record.Result}");
//            }
//        }

//        private class TestRecord
//        {
//            public string Line { get; set; }
//            public string Product_Type { get; set; }
//            public DateTime Date { get; set; }
//            public string CycleTime { get; set; }
//            public string SN { get; set; }
//            public string Result { get; set; }
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FAI
{
    public static class BlackEdge
    {
        private static readonly object logLock = new object();

        public static List<string> ProcessLogs(string logPath, string destinationPath, DateTime? date = null)
        {
            var resultFiles = new List<string>();
            DateTime targetDate = date ?? DateTime.Today;

            // Đường dẫn file log
            string logFile = GetLogFilePath(targetDate);
            WriteLog(logFile, $"=== Bắt đầu xử lý BlackEdge lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            WriteLog(logFile, $"LogPath: {logPath}, DestinationPath: {destinationPath}, TargetDate: {targetDate:yyyy-MM-dd}");

            // Xây dựng đường dẫn file nguồn
            string yearMonthDay = targetDate.ToString("yyyyMMdd");
            string sourceFile = Path.Combine(logPath, yearMonthDay, $"{yearMonthDay}.csv");

            if (!File.Exists(sourceFile))
            {
                WriteLog(logFile, $"Lỗi: Không tìm thấy file nguồn: {sourceFile}");
                throw new FileNotFoundException($"Không tìm thấy file nguồn: {sourceFile}");
            }

            // Đọc và phân tích CSV
            var records = ReadCsv(sourceFile);
            WriteLog(logFile, $"Đọc {records.Count} dòng từ file CSV.");

            // Sắp xếp theo thời gian thực (mới nhất lên đầu)
            var sorted = records.OrderByDescending(r => r.Date).ToList();

            // Lấy 3 bản ghi Pass mới nhất
            var passRecords = sorted.Where(r => r.Result == "PASS").Take(3).ToList();
            WriteLog(logFile, $"Tìm thấy {passRecords.Count} bản ghi PASS mới nhất.");

            // Lấy 1 bản ghi mới nhất có chứa "Fail"
            var failRecord = sorted.FirstOrDefault(r => r.Result.Contains("Fail", StringComparison.OrdinalIgnoreCase));
            if (failRecord != null)
                WriteLog(logFile, $"Tìm thấy bản ghi FAIL: {failRecord.SN}");
            else
                WriteLog(logFile, "Không tìm thấy bản ghi FAIL.");

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

                DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                Directory.SetLastWriteTime(outputDir, targetTime);
                File.SetLastWriteTime(outputFile, targetTime);

                resultFiles.Add(outputFile);
                WriteLog(logFile, $"Đã tạo thư mục PASS: {outputDir}, set thời gian: {targetTime:HH:mm:ss}");
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
                    WriteLog(logFile, $"Đã tạo thư mục FAIL: {outputDir}, set thời gian: {targetTime:HH:mm:ss}");
                    minuteOffset += rand.Next(1, 6);
                }
            }

            WriteLog(logFile, $"Hoàn thành. Tổng số file đã tạo: {resultFiles.Count}");
            WriteLog(logFile, $"=== Kết thúc lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" + Environment.NewLine);
            return resultFiles;
        }

        public static Task<List<string>> ProcessLogsAsync(string logPath, string destinationPath, DateTime? date = null)
        {
            return Task.Run(() => ProcessLogs(logPath, destinationPath, date));
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
                "M/d/yyyy H:mm:ss",
                "h:mm:ss tt",
                "h:mm:ss"
            };

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Split(',');
                if (values.Length != columns.Length) continue;

                string timeStr = values[2].Trim();
                if (!DateTime.TryParseExact(timeStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    continue;
                }

                records.Add(new TestRecord
                {
                    Line = values[0],
                    Product_Type = values[1],
                    Date = parsedDate,
                    CycleTime = values[3],
                    SN = values[4],
                    Result = values[5]
                });
            }

            return records;
        }

        private static void WriteSingleRecord(string outputFile, TestRecord record)
        {
            using (var writer = new StreamWriter(outputFile))
            {
                writer.WriteLine("Line,ModelName,SN,Result");
                writer.WriteLine($"{record.Line},{record.Product_Type},{record.SN},{record.Result}");
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

        private class TestRecord
        {
            public string Line { get; set; }
            public string Product_Type { get; set; }
            public DateTime Date { get; set; }
            public string CycleTime { get; set; }
            public string SN { get; set; }
            public string Result { get; set; }
        }
    }
}