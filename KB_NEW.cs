//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//namespace FAI
//{
//    public static class KB_NEW
//    {
//        public static List<string> CreateFoldersFromFiles(string logPath, string destinationPath, DateTime? date = null)
//        {
//            var resultFolders = new List<string>();
//            DateTime targetDate = date ?? DateTime.Today;
//            string dateFolder = targetDate.ToString("yyyyMMdd");
//            string sourceDateDir = Path.Combine(logPath, dateFolder); // D:\pic\20260317
//            string sourceDir = Directory.GetDirectories(sourceDateDir)
//                                   .OrderByDescending(dir => Directory.GetLastWriteTime(dir))
//                                   .FirstOrDefault();
//            string baseOutput = Path.Combine(destinationPath, dateFolder); 

//            if (!Directory.Exists(sourceDir))
//            {
//                throw new DirectoryNotFoundException($"Không tìm thấy thư mục nguồn: {sourceDir}");
//            }

//            // Lấy tất cả file .jpg trong thư mục nguồn
//            var files = new DirectoryInfo(sourceDir).GetFiles("*")
//                        .OrderByDescending(f => f.LastWriteTime)
//                        .Take(3)
//                        .ToList();

//            DateTime baseTime = DateTime.Now;
//            int minuteOffset = 0;
//            Random rand = new Random();

//            foreach (var file in files)
//            {
//                // Lấy tên file không có phần mở rộng
//                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
//                // Tách phần trước dấu '_' nếu có
//                string folderName = fileNameWithoutExt.Contains('_') ? fileNameWithoutExt.Substring(0, fileNameWithoutExt.IndexOf('_')) : fileNameWithoutExt;
//                // Tạo thư mục đích trong OK
//                string destDir = Path.Combine(baseOutput, "OK", folderName);
//                Directory.CreateDirectory(destDir);
//                resultFolders.Add(destDir);

//                // Set thời gian cho thư mục
//                DateTime targetTime = baseTime.AddMinutes(minuteOffset);
//                Directory.SetLastWriteTime(destDir, targetTime);
//                // Không copy file, nếu muốn copy file thì thêm dòng:
//                // File.Copy(file.FullName, Path.Combine(destDir, file.Name));

//                minuteOffset += rand.Next(1, 6); // random 1-5 phút
//            }
//            //Fail
//            for(int i = 0; i < 3; i++)
//            {
//                // Lấy tên file không có phần mở rộng
//                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(files[0].Name);
//                // Tách phần trước dấu '_' nếu có
//                string folderName = fileNameWithoutExt.Contains('_') ? fileNameWithoutExt.Substring(0, fileNameWithoutExt.IndexOf('_')) : fileNameWithoutExt;
//                // Tạo thư mục đích trong OK
//                string destDir = Path.Combine(baseOutput, "NG", folderName + "_" + i);
//                Directory.CreateDirectory(destDir);
//                resultFolders.Add(destDir);

//                // Set thời gian cho thư mục
//                DateTime targetTime = baseTime.AddMinutes(minuteOffset);
//                Directory.SetLastWriteTime(destDir, targetTime);
//                // Không copy file, nếu muốn copy file thì thêm dòng:
//                // File.Copy(file.FullName, Path.Combine(destDir, file.Name));

//                minuteOffset += rand.Next(1, 2); // random 1-5 phút
//            }

//            return resultFolders;
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FAI
{
    public static class KB_NEW
    {
        private static readonly object logLock = new object();

        public static List<string> CreateFoldersFromFiles(string logPath, string destinationPath, DateTime? date = null)
        {
            var resultFolders = new List<string>();
            DateTime targetDate = date ?? DateTime.Today;
            string dateFolder = targetDate.ToString("yyyyMMdd");
            string sourceDateDir = Path.Combine(logPath, dateFolder); // D:\pic\20260317

            // Lấy thư mục con mới nhất trong sourceDateDir
            string sourceDir = Directory.GetDirectories(sourceDateDir)
                                   .OrderByDescending(dir => Directory.GetLastWriteTime(dir))
                                   .FirstOrDefault();
            string baseOutput = Path.Combine(destinationPath, dateFolder);

            // Đường dẫn file log
            string logFile = GetLogFilePath(targetDate);
            WriteLog(logFile, $"=== Bắt đầu xử lý KB_NEW lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            WriteLog(logFile, $"SourceDateDir: {sourceDateDir}, DestinationPath: {destinationPath}, TargetDate: {targetDate:yyyy-MM-dd}");

            if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
            {
                WriteLog(logFile, $"Lỗi: Không tìm thấy thư mục nguồn: {sourceDir}");
                throw new DirectoryNotFoundException($"Không tìm thấy thư mục nguồn: {sourceDir}");
            }

            WriteLog(logFile, $"Thư mục nguồn được chọn: {sourceDir}");

            // Lấy tất cả file (có thể lọc *.jpg nếu cần) trong thư mục nguồn, lấy 3 file mới nhất
            var files = new DirectoryInfo(sourceDir).GetFiles("*")
                        .OrderByDescending(f => f.LastWriteTime)
                        .Take(3)
                        .ToList();

            WriteLog(logFile, $"Tìm thấy {files.Count} file mới nhất trong thư mục nguồn.");

            DateTime baseTime = DateTime.Now;
            int minuteOffset = 0;
            Random rand = new Random();

            // Xử lý OK (dùng 3 file để tạo 3 thư mục OK)
            foreach (var file in files)
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                string folderName = fileNameWithoutExt.Contains('_')
                                    ? fileNameWithoutExt.Substring(0, fileNameWithoutExt.IndexOf('_'))
                                    : fileNameWithoutExt;
                string destDir = Path.Combine(baseOutput, "OK", folderName);
                Directory.CreateDirectory(destDir);
                resultFolders.Add(destDir);

                DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                Directory.SetLastWriteTime(destDir, targetTime);

                WriteLog(logFile, $"Tạo thư mục OK: {destDir}, dựa trên file: {file.Name}, set thời gian: {targetTime:HH:mm:ss}");

                minuteOffset += rand.Next(1, 6); // random 1-5 phút
            }

            // Xử lý NG: tạo 3 thư mục dựa trên file đầu tiên (files[0])
            if (files.Count > 0)
            {
                string firstFileName = files[0].Name;
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(firstFileName);
                string baseFolderName = fileNameWithoutExt.Contains('_')
                                        ? fileNameWithoutExt.Substring(0, fileNameWithoutExt.IndexOf('_'))
                                        : fileNameWithoutExt;

                WriteLog(logFile, $"Tạo 3 thư mục NG dựa trên file: {firstFileName}");

                for (int i = 0; i < 3; i++)
                {
                    string destDir = Path.Combine(baseOutput, "NG", baseFolderName + "_" + i);
                    Directory.CreateDirectory(destDir);
                    resultFolders.Add(destDir);

                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                    Directory.SetLastWriteTime(destDir, targetTime);

                    WriteLog(logFile, $"Tạo thư mục NG: {destDir}, set thời gian: {targetTime:HH:mm:ss}");

                    minuteOffset += rand.Next(1, 6); // random 1-5 phút (sửa lại thành 1-6 cho đồng bộ)
                }
            }
            else
            {
                WriteLog(logFile, "Không có file nào để tạo thư mục NG.");
            }

            WriteLog(logFile, $"Hoàn thành. Tổng số thư mục đã tạo: {resultFolders.Count}");
            WriteLog(logFile, $"=== Kết thúc lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" + Environment.NewLine);

            return resultFolders;
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
            catch { /* Bỏ qua lỗi ghi log */ }
        }
    }
}