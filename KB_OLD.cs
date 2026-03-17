//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//namespace FAI
//{
//    public static class KB_OLD
//    {
//        public static List<string> CreateFoldersFromFiles(string logPath, string destinationPath, DateTime? date = null)
//        {
//            var resultFolders = new List<string>();
//            DateTime targetDate = date ?? DateTime.Today;
//            string dateFolder = targetDate.ToString("yyyyMMdd");
//            string sourceDir = Path.Combine(logPath, dateFolder,"OK"); // D:\pic\20260317
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
    public static class KB_OLD
    {
        private static readonly object logLock = new object();

        public static List<string> CreateFoldersFromFiles(string logPath, string destinationPath, DateTime? date = null)
        {
            var resultFolders = new List<string>();
            DateTime targetDate = date ?? DateTime.Today;
            string dateFolder = targetDate.ToString("yyyyMMdd");
            string sourceDir = Path.Combine(logPath, dateFolder, "OK"); // D:\pic\20260317\OK
            string baseOutput = Path.Combine(destinationPath, dateFolder);

            // Đường dẫn file log
            string logFile = GetLogFilePath(targetDate);
            WriteLog(logFile, $"=== Bắt đầu xử lý KB_OLD lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            WriteLog(logFile, $"LogPath: {logPath}, DestinationPath: {destinationPath}, TargetDate: {targetDate:yyyy-MM-dd}");
            WriteLog(logFile, $"Thư mục nguồn: {sourceDir}");
            WriteLog(logFile, $"Thư mục đích cơ sở: {baseOutput}");

            if (!Directory.Exists(sourceDir))
            {
                WriteLog(logFile, $"Lỗi: Không tìm thấy thư mục nguồn: {sourceDir}");
                throw new DirectoryNotFoundException($"Không tìm thấy thư mục nguồn: {sourceDir}");
            }

            // Lấy tất cả file .jpg trong thư mục nguồn
            var files = new DirectoryInfo(sourceDir).GetFiles("*")
                        .OrderByDescending(f => f.LastWriteTime)
                        .Take(3)
                        .ToList();

            WriteLog(logFile, $"Tìm thấy {files.Count} file mới nhất trong thư mục nguồn.");

            DateTime baseTime = DateTime.Now;
            int minuteOffset = 0;
            Random rand = new Random();

            // Xử lý OK
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
                WriteLog(logFile, $"Đã tạo thư mục OK: {destDir}, set thời gian: {targetTime:HH:mm:ss} (dựa trên file {file.Name})");

                minuteOffset += rand.Next(1, 6); // random 1-5 phút
            }

            // Xử lý NG (tạo 3 thư mục từ file đầu tiên)
            if (files.Count > 0)
            {
                var firstFile = files[0];
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(firstFile.Name);
                string baseFolderName = fileNameWithoutExt.Contains('_')
                    ? fileNameWithoutExt.Substring(0, fileNameWithoutExt.IndexOf('_'))
                    : fileNameWithoutExt;

                WriteLog(logFile, $"Tạo 3 thư mục NG dựa trên file đầu tiên: {firstFile.Name}");

                for (int i = 0; i < 3; i++)
                {
                    string destDir = Path.Combine(baseOutput, "NG", baseFolderName + "_" + i);
                    Directory.CreateDirectory(destDir);
                    resultFolders.Add(destDir);

                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                    Directory.SetLastWriteTime(destDir, targetTime);
                    WriteLog(logFile, $"Đã tạo thư mục NG: {destDir}, set thời gian: {targetTime:HH:mm:ss}");

                    minuteOffset += rand.Next(1, 3); // random 1-2 phút
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
            catch { }
        }
    }
}