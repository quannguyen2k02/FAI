using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FAI
{
    public static class BlackEdge

    {

        public static List<string> ProcessLogs(string logPath, string destinationPath, DateTime? date = null)
        {
            var resultFiles = new List<string>();
            DateTime targetDate = date ?? DateTime.Today;

            // Xây dựng đường dẫn file nguồn
            string yearMonth = targetDate.ToString("yyyyMM");
            string yearMonthDay = targetDate.ToString("yyyyMMdd");
            string sourceFolder = Path.Combine(logPath, yearMonth);
            string pattern = $"*{yearMonthDay}*.csv";
            string[] files = Directory.GetFiles(sourceFolder, pattern);
            if (files.Length == 0)
            {
                throw new FileNotFoundException($"Không tìm thấy file CSV nào chứa ngày {yearMonthDay} trong thư mục {sourceFolder}");
            }

            // Giả sử chỉ có một file phù hợp, lấy file đầu tiên
            string sourceFile = files[0];
            if (!File.Exists(sourceFile))
            {
                throw new FileNotFoundException($"Không tìm thấy file nguồn: {sourceFile}");
            }

            // Đọc và phân tích CSV
            var records = ReadCsv(sourceFile);

            // Sắp xếp theo thời gian thực (mới nhất lên đầu)
            var sorted = records.OrderByDescending(r => r.Date).ToList();

            // Lấy 3 bản ghi Pass mới nhất
            var passRecords = sorted.Where(r => r.Result == "PASS").Take(3).ToList();

            // Lấy 1 bản ghi mới nhất có chứa "Fail"
            var failRecord = sorted.FirstOrDefault(r => r.Result.Contains("Fail", StringComparison.OrdinalIgnoreCase));

            // Thư mục đầu ra theo ngày
            string dateFolder = targetDate.ToString("yyyy_MM_dd");
            string baseOutput = Path.Combine(destinationPath, dateFolder);

            // Thời gian gốc và offset (mỗi bản ghi cách nhau 1 phút)
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

            // Lưu bản ghi Fail: tạo 3 thư mục con (theo yêu cầu)
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

                    // Set thời gian modified cho thư mục và file
                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                    Directory.SetLastWriteTime(outputDir, targetTime);
                    File.SetLastWriteTime(outputFile, targetTime);

                    resultFiles.Add(outputFile);
                    minuteOffset += rand.Next(1, 6);
                }
            }

            return resultFiles;
        }

        /// <summary>
        /// Phiên bản bất đồng bộ của ProcessLogs, phù hợp cho WPF để không block UI.
        /// </summary>
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

            // Các định dạng ngày tháng có thể có trong file
            string[] formats = {
                "M/d/yyyy h:mm:ss tt",   // 2/10/2026 8:40:00 AM
                "M/d/yyyy h:mm tt",       // 2/10/2026 8:40 AM
                "M/d/yyyy H:mm",           // 2/10/2026 8:40 (24h)
                "M/d/yyyy H:mm:ss",     // 2/10/2026 8:40:00 (24h)
                "h:mm:ss tt",  // 12:40:11 AM
                "h:mm:ss"  // 12:40:11 AM
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
                    // Bỏ qua dòng lỗi (có thể log nếu cần)
                    continue;
                }

                var record = new TestRecord
                {
                    Line = values[0],
                    Product_Type = values[3],
                    Date = parsedDate,
                    SN = values[4],
                    Result = values[5],
                    TopLeft = values[6],
                    TopRight = values[7],
                    BottomLeft = values[9],
                    BottomRight = values[11],
                    BottomMiddle = values[10],
                    LeftTop = values[13],
                    LeftBottom = values[14],
                    RightTop = values[16],
                    RightBottom = values[17]
                    // Bạn có thể thêm các trường khác nếu cần
                };
                records.Add(record);
            }


            return records;
        }

        private static void WriteSingleRecord(string outputFile, TestRecord record)
        {
            using (var writer = new StreamWriter(outputFile))
            {
                // Header (chỉ ghi các cột bạn muốn)
                writer.WriteLine("Line,ModelName,SN,Result,TopLeft,TopRight,BottomLeft,BottomRight,BottomMiddle,LeftTop,LeftBottom,RightTop,RightBottom");
                // Dòng dữ liệu
                writer.WriteLine($"{record.Line},{record.Product_Type},{record.SN},{record.Result},{record.TopLeft},{record.TopRight},{record.BottomLeft},{record.BottomRight},{record.BottomMiddle},{record.LeftTop},{record.LeftBottom},{record.RightTop},{record.RightBottom}");
            }
        }

        private class TestRecord
        {
            public string Line { get; set; }
            public string Product_Type { get; set; }
            public DateTime Date { get; set; }
            public string SN { get; set; }
            public string Result { get; set; }
            public string TopLeft { get; set; }
            public string TopRight { get; set; }
            public string BottomLeft { get; set; }
            public string BottomMiddle { get; set; }
            public string BottomRight { get; set; }
            public string LeftTop { get; set; }
            public string LeftBottom { get; set; }
            public string RightTop { get; set; }
            public string RightBottom { get; set; }
        }
    }
}