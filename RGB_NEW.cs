//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;

//namespace FAI
//{
//    public static class RGB_NEW

//    {

//        public static List<string> CopyLatestFolders(string logPath, string destinationPath, DateTime? date = null)
//        {
//            var resultFiles = new List<string>();
//            DateTime targetDate = date ?? DateTime.Today;
//            string dateFolder = targetDate.ToString("yyyy_MM_dd");
//            string baseOutput = Path.Combine(destinationPath, dateFolder);

//            DateTime baseTime = DateTime.Now;
//            int minuteOffset = 0;
//            Random rand = new Random();
//            // Xử lý thư mục PASS
//            string passSource = Path.Combine(logPath, "PASS");  
//            if (Directory.Exists(passSource))
//            {
//                var passSubDirs = new DirectoryInfo(passSource).GetDirectories()
//                                    .OrderByDescending(d => d.LastWriteTime)
//                                    .Take(3)
//                                    .ToList();

//                foreach (var dir in passSubDirs)
//                {
//                    string destDir = Path.Combine(baseOutput, "OK", dir.Name);
//                    CopyDirectoryContents(dir.FullName, destDir, "*.log", resultFiles);
//                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
//                    SetLastWriteTimeRecursive(destDir, targetTime);

//                    minuteOffset += rand.Next(1, 3); // Tăng ngẫu nhiên 1-5 phút
//                }
//            }

//            // Xử lý thư mục FAIL
//            string failSource = Path.Combine(logPath, "FAIL");
//            if (Directory.Exists(failSource))
//            {
//                var failSubDirs = new DirectoryInfo(failSource).GetDirectories()
//                                    .OrderByDescending(d => d.LastWriteTime)
//                                    .Take(1)
//                                    .ToList();

//                for (int i=0;i<3;i++)
//                {
//                    string destDir = Path.Combine(baseOutput, "NG", failSubDirs[0].Name+"_"+i);
//                    CopyDirectoryContents(failSubDirs[0].FullName, destDir, "*.log", resultFiles);
//                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
//                    SetLastWriteTimeRecursive(destDir, targetTime);

//                    minuteOffset += rand.Next(1, 4); // Tăng ngẫu nhiên 1-5 phút
//                }
//            }

//            return resultFiles;
//        }

//        /// <summary>
//        /// Copy toàn bộ file .txt từ thư mục nguồn sang thư mục đích (giữ nguyên cấu trúc thư mục con)
//        /// </summary>
//        private static void CopyDirectoryContents(string sourceDir, string destDir, string searchPattern, List<string> resultFiles)
//        {
//            // Tạo thư mục đích nếu chưa tồn tại
//            Directory.CreateDirectory(destDir);

//            // Copy các file .txt trong thư mục hiện tại
//            foreach (var file in Directory.GetFiles(sourceDir, searchPattern, SearchOption.TopDirectoryOnly))
//            {
//                string destFile = Path.Combine(destDir, Path.GetFileName(file));
//                File.Copy(file, destFile, overwrite: true);
//                resultFiles.Add(destFile);
//            }

//            // Đệ quy xử lý các thư mục con
//            foreach (var subDir in Directory.GetDirectories(sourceDir))
//            {
//                string subDirName = Path.GetFileName(subDir);
//                string destSubDir = Path.Combine(destDir, subDirName);
//                CopyDirectoryContents(subDir, destSubDir, searchPattern, resultFiles);
//            }
//        }
//        private static void SetLastWriteTimeRecursive(string path, DateTime time)
//        {
//            // Set cho chính thư mục
//            Directory.SetLastWriteTime(path, time);

//            // Set cho tất cả file trong thư mục
//            foreach (string file in Directory.GetFiles(path))
//            {
//                File.SetLastWriteTime(file, time);
//            }

//            // Đệ quy cho các thư mục con
//            foreach (string subDir in Directory.GetDirectories(path))
//            {
//                SetLastWriteTimeRecursive(subDir, time);
//            }
//        }
//    }
//}


using System.IO;
using System.Reflection;

namespace FAI
{
    public static class RGB_NEW
    {
        private static readonly object logLock = new object();

        public static List<string> CopyLatestFolders(string logPath, string destinationPath, DateTime? date = null)
        {
            var resultFiles = new List<string>();
            DateTime targetDate = date ?? DateTime.Today;
            string dateFolder = targetDate.ToString("yyyy_MM_dd");
            string baseOutput = Path.Combine(destinationPath, dateFolder);

            // Đường dẫn file log trong thư mục Log (cùng thư mục exe)
            string logFile = GetLogFilePath(targetDate);
            WriteLog(logFile, $"=== Bắt đầu xử lý lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            WriteLog(logFile, $"LogPath: {logPath}, DestinationPath: {destinationPath}");

            DateTime baseTime = DateTime.Now;
            int minuteOffset = 0;
            Random rand = new Random();

            // Xử lý thư mục PASS
            string passSource = Path.Combine(logPath, "PASS");
            if (Directory.Exists(passSource))
            {
                var passSubDirs = new DirectoryInfo(passSource).GetDirectories()
                                    .OrderByDescending(d => d.LastWriteTime)
                                    .Take(3)
                                    .ToList();

                WriteLog(logFile, $"Tìm thấy {passSubDirs.Count} thư mục PASS mới nhất.");

                foreach (var dir in passSubDirs)
                {
                    string destDir = Path.Combine(baseOutput, "OK", dir.Name);
                    WriteLog(logFile, $"Đang xử lý thư mục PASS: {dir.Name} -> {destDir}");

                    CopyDirectoryContents(dir.FullName, destDir, "*.log", resultFiles, logFile);
                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                    SetLastWriteTimeRecursive(destDir, targetTime);

                    minuteOffset += rand.Next(1, 6); // Random 1-5 phút
                }
            }
            else
            {
                WriteLog(logFile, $"Thư mục PASS không tồn tại: {passSource}");
            }

            // Xử lý thư mục FAIL
            string failSource = Path.Combine(logPath, "FAIL");
            if (Directory.Exists(failSource))
            {
                var failSubDirs = new DirectoryInfo(failSource).GetDirectories()
                                    .OrderByDescending(d => d.LastWriteTime)
                                    .Take(1)
                                    .ToList();

                if (failSubDirs.Count > 0)
                {
                    WriteLog(logFile, $"Tìm thấy thư mục FAIL mới nhất: {failSubDirs[0].Name}");

                    for (int i = 0; i < 3; i++)
                    {
                        string destDir = Path.Combine(baseOutput, "NG", failSubDirs[0].Name + "_" + i);
                        WriteLog(logFile, $"Tạo bản sao FAIL thứ {i + 1}: {destDir}");

                        CopyDirectoryContents(failSubDirs[0].FullName, destDir, "*.log", resultFiles, logFile);
                        DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                        SetLastWriteTimeRecursive(destDir, targetTime);

                        minuteOffset += rand.Next(1, 6); // Random 1-5 phút
                    }
                }
                else
                {
                    WriteLog(logFile, "Không có thư mục con nào trong FAIL.");
                }
            }
            else
            {
                WriteLog(logFile, $"Thư mục FAIL không tồn tại: {failSource}");
            }

            WriteLog(logFile, $"Hoàn thành. Tổng số file đã copy: {resultFiles.Count}");
            WriteLog(logFile, $"=== Kết thúc lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" + Environment.NewLine);

            return resultFiles;
        }

        /// <summary>
        /// Lấy đường dẫn file log trong thư mục Log (cùng thư mục exe)
        /// </summary>
        private static string GetLogFilePath(DateTime date)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string logDir = Path.Combine(exeDir, "Log");
            Directory.CreateDirectory(logDir);
            return Path.Combine(logDir, $"log_{date:yyyyMMdd}.txt");
        }

        /// <summary>
        /// Ghi log an toàn (thread-safe)
        /// </summary>
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

        /// <summary>
        /// Copy toàn bộ file .log từ thư mục nguồn sang đích (giữ cấu trúc thư mục con)
        /// </summary>
        private static void CopyDirectoryContents(string sourceDir, string destDir, string searchPattern, List<string> resultFiles, string logFile)
        {
            Directory.CreateDirectory(destDir);
            int fileCount = 0;

            foreach (var file in Directory.GetFiles(sourceDir, searchPattern, SearchOption.TopDirectoryOnly))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
                resultFiles.Add(destFile);
                fileCount++;
            }

            WriteLog(logFile, $"  Copy {fileCount} file .log từ {sourceDir}");

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, subDirName);
                CopyDirectoryContents(subDir, destSubDir, searchPattern, resultFiles, logFile);
            }
        }

        /// <summary>
        /// Set LastWriteTime cho toàn bộ cây thư mục (bao gồm các file)
        /// </summary>
        private static void SetLastWriteTimeRecursive(string path, DateTime time)
        {
            try
            {
                Directory.SetLastWriteTime(path, time);
                foreach (string file in Directory.GetFiles(path))
                {
                    File.SetLastWriteTime(file, time);
                }
                foreach (string subDir in Directory.GetDirectories(path))
                {
                    SetLastWriteTimeRecursive(subDir, time);
                }
            }
            catch { /* Bỏ qua lỗi set thời gian nếu không có quyền */ }
        }
    }
}