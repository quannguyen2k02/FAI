using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FAI
{
    public static class IO
    {
        private static readonly object logLock = new object();

        public static List<string> ProcessLogs(string logPath, string destinationPath, DateTime? date = null)
        {
            var resultFiles = new List<string>();
            DateTime targetDate = date ?? DateTime.Today;

            // Đường dẫn file log
            string logFile = GetLogFilePath(targetDate);
            WriteLog(logFile, $"=== Bắt đầu xử lý IO lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            WriteLog(logFile, $"LogPath: {logPath}, DestinationPath: {destinationPath}, TargetDate: {targetDate:yyyy-MM-dd}");

            // Xây dựng đường dẫn file nguồn
            string yearMonth = targetDate.ToString("yyyyMM");
            string yearMonthDay = targetDate.ToString("yyyyMMdd");
            string sourceFolder = Path.Combine(logPath, yearMonth);
            string pattern = $"*{yearMonthDay}*.csv";
            string[] files = Array.Empty<string>();
            try
            {
                if (Directory.Exists(sourceFolder))
                    files = Directory.GetFiles(sourceFolder, pattern);
            }
            catch (Exception ex)
            {
                WriteLog(logFile, $"Lỗi khi truy cập thư mục nguồn: {ex.Message}");
                throw;
            }

            if (files.Length == 0)
            {
                WriteLog(logFile, $"Không tìm thấy file CSV nào chứa ngày {yearMonthDay} trong thư mục {sourceFolder}");
                throw new FileNotFoundException($"Không tìm thấy file CSV nào chứa ngày {yearMonthDay} trong thư mục {sourceFolder}");
            }

            // Giả sử chỉ có một file phù hợp, lấy file đầu tiên
            string sourceFile = files[0];
            if (!File.Exists(sourceFile))
            {
                WriteLog(logFile, $"Lỗi: Không tìm thấy file nguồn: {sourceFile}");
                throw new FileNotFoundException($"Không tìm thấy file nguồn: {sourceFile}");
            }

            // Đọc và phân tích CSV
            var records = ReadCsv(sourceFile);
            WriteLog(logFile, $"Đọc {records.Count} dòng từ file CSV: {Path.GetFileName(sourceFile)}.");

            // Sắp xếp theo thời gian thực (mới nhất lên đầu)

            // Lấy 3 bản ghi Pass mới nhất
            var passRecords = records.Where(r => string.Equals(r.Result, "PASS", StringComparison.OrdinalIgnoreCase)).Take(5).ToList();
            WriteLog(logFile, $"Tìm thấy {passRecords.Count} bản ghi PASS mới nhất.");

            // Lấy 1 bản ghi mới nhất có chứa "Fail"
            var failRecord = records.LastOrDefault(r => r.Result != null && r.Result.IndexOf("Fail", StringComparison.OrdinalIgnoreCase) >= 0);
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
            Random rand = new Random(); // Khởi tạo Random

            // Lưu các bản ghi Pass (mỗi Pass tạo một thư mục riêng)
            foreach (var rec in passRecords)
            {
                string sn = rec.SN;
                string outputDir = Path.Combine(baseOutput, "OK", sn);
                Directory.CreateDirectory(outputDir);
                string outputFile = Path.Combine(outputDir, $"{sn}.csv");
                WriteSingleRecord(outputFile, rec);

                // Set thời gian modified cho thư mục và file
                DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                Directory.SetLastWriteTime(outputDir, targetTime);
                File.SetLastWriteTime(outputFile, targetTime);

                resultFiles.Add(outputFile);
                minuteOffset += rand.Next(1, 6);
            }

            // Lưu bản ghi Fail: tạo 3 thư mục con (mỗi fail tạo 3 thư mục)
            if (failRecord != null)
            {
                string sn = failRecord.SN;

                for (int i = 0; i < 5; i++)
                {
                    string folderName = $"{sn}_{i}";
                    string outputDir = Path.Combine(baseOutput, "NG", folderName);
                    Directory.CreateDirectory(outputDir);
                    string outputFile = Path.Combine(outputDir, $"{sn}.csv");
                    WriteSingleRecord(outputFile, failRecord);

                    // Set thời gian modified cho thư mục và file
                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                    Directory.SetLastWriteTime(outputDir, targetTime);
                    File.SetLastWriteTime(outputFile, targetTime);

                    resultFiles.Add(outputFile);
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

        // Manual SN creation for IO device
        public static string CreateFolderForSN(string logPath, string destinationPath, string sn, DateTime? date = null)
        {
            DateTime targetDate = date ?? DateTime.Today;
            string yearMonth = targetDate.ToString("yyyyMM");
            string yearMonthDay = targetDate.ToString("yyyyMMdd");
            string sourceFolder = Path.Combine(logPath, yearMonth);
            string pattern = $"*{yearMonthDay}*.csv";
            string logFile = GetLogFilePath(targetDate);

            string[] files = Array.Empty<string>();
            try
            {
                if (Directory.Exists(sourceFolder))
                    files = Directory.GetFiles(sourceFolder, pattern);
            }
            catch (Exception ex)
            {
                WriteLog(logFile, $"Lỗi khi truy cập thư mục nguồn: {ex.Message}");
                return $"Lỗi khi truy cập thư mục nguồn: {ex.Message}";
            }

            if (files.Length == 0)
            {
                WriteLog(logFile, $"Không tìm thấy file CSV ngày {yearMonthDay} trong {sourceFolder}");
                return $"Không tìm thấy file CSV ngày {yearMonthDay} trong {sourceFolder}";
            }

            string sourceFile = files[0];
            if (!File.Exists(sourceFile))
            {
                WriteLog(logFile, $"Không tìm thấy file nguồn: {sourceFile}");
                return $"Không tìm thấy file nguồn: {sourceFile}";
            }

            var records = ReadCsv(sourceFile);
            var found = records.FirstOrDefault(r => string.Equals(r.SN, sn, StringComparison.OrdinalIgnoreCase));
            if (found == null)
            {
                WriteLog(logFile, $"SN {sn} không tồn tại trong file {Path.GetFileName(sourceFile)}");
                return $"SN {sn} không tồn tại trong báo cáo.";
            }

            string dateFolder = targetDate.ToString("yyyy_MM_dd");
            string baseOutput = Path.Combine(destinationPath, dateFolder);
            DateTime baseTime = DateTime.Now;

            if (found.Result != null && found.Result.IndexOf("Fail", StringComparison.OrdinalIgnoreCase) >= 0)
            {

                string folderName = $"{found.SN}";
                string outputDir = Path.Combine(baseOutput, "NG", folderName);
                Directory.CreateDirectory(outputDir);
                string outputFile = Path.Combine(outputDir, $"{found.SN}.csv");
                WriteSingleRecord(outputFile, found);

                DateTime targetTime = baseTime.AddMinutes(1);
                Directory.SetLastWriteTime(outputDir, targetTime);
                File.SetLastWriteTime(outputFile, targetTime);

                WriteLog(logFile, $"(Manual) Tạo NG: {outputDir}");

                return $"SN {sn} là FAIL - đã tạo  thư mục NG.";
            }
            else
            {
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


                records.Add(new TestRecord
                {
                    Line = values[0],
                    Product_Type = values[3],

                    CycleTime = values[7],
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