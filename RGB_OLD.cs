//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using FluentFTP;

//public static class RGB_OLD
//{
//    /// <summary>
//    /// Copy 3 thư mục con mới nhất từ thư mục PASS và FAIL trên FTP vào thư mục đích cục bộ.
//    /// </summary>
//    /// <param name="ftpUrl">Đường dẫn FTP, ví dụ: ftp://ATD@10.69.230.66/ATD/Marc/FAI/RGB_OLD/</param>
//    /// <param name="destPath">Thư mục đích cục bộ, ví dụ: D:\RGB_FAI</param>
//    /// <param name="date">Ngày cần xử lý (mặc định hôm nay)</param>
//    /// <param name="lineName">Ngày cần xử lý (mặc định hôm nay)</param>
//    /// <returns>Danh sách các file đã tải về</returns>
//    public static List<string> CopyLatestFolders(string ftpUrl, string destPath, string lineName, DateTime? date = null)
//    {
//        var resultFiles = new List<string>();
//        DateTime targetDate = date ?? DateTime.Today;
//        string dateFolder = targetDate.ToString("yyyy_MM_dd");
//        string formatDate = targetDate.ToString("yyyy-MM-dd");
//        string baseOutput = Path.Combine(destPath, dateFolder);

//        DateTime baseTime = DateTime.Now;
//        int minuteOffset = 0;
//        Random rand = new Random();

//        // Parse thông tin từ URL
//        Uri uri = new Uri(ftpUrl);
//        string host = uri.Host;
//        string username = uri.UserInfo; // "ATD"
//        // Nếu có mật khẩu, bạn cần xử lý riêng. Ở đây giả sử không cần mật khẩu.
//        string password = "ATD"; // Nếu có, lấy từ config hoặc hardcode.
//        string path = uri.PathAndQuery.TrimStart('/').TrimEnd('/'); // "ATD/Marc/FAI/RGB_OLD"
//        string basePath = Path.Combine(path, lineName, formatDate);
//        using (var client = new FtpClient(host, username, password))
//        {
//            client.Connect();

//            // Xử lý thư mục PASS
//            string passPath = basePath + "/PASS";
//            if (client.DirectoryExists(passPath))
//            {
//                var passSubDirs = client.GetListing(passPath)
//                                        .Where(i => i.Type == FtpObjectType.Directory)
//                                        .OrderByDescending(d => d.Modified)
//                                        .Take(3)
//                                        .ToList();

//                foreach (var dir in passSubDirs)
//                {
//                    string destDir = Path.Combine(baseOutput, "OK", dir.Name);
//                    DownloadDirectoryContents(client, dir.FullName, destDir, "*.log", resultFiles);
//                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
//                    SetLastWriteTimeRecursive(destDir, targetTime);

//                    minuteOffset += rand.Next(1, 4); // Tăng ngẫu nhiên 1-5 phút
//                }
//            }

//            // Xử lý thư mục FAIL
//            string failPath = basePath + "/FAIL";
//            if (client.DirectoryExists(failPath))
//            {
//                var failSubDirs = client.GetListing(failPath)
//                                        .Where(i => i.Type == FtpObjectType.Directory)
//                                        .OrderByDescending(d => d.Modified)
//                                        .Take(1)
//                                        .ToList();

//                for (int i=0;i<3;i++)
//                {
//                    string destDir = Path.Combine(baseOutput, "NG", failSubDirs[0].Name + "_" +i);
//                    DownloadDirectoryContents(client, failSubDirs[0].FullName, destDir, "*.log", resultFiles);
//                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
//                    SetLastWriteTimeRecursive(destDir, targetTime);

//                    minuteOffset += rand.Next(1, 4); // Tăng ngẫu nhiên 1-5 phút
//                }
//            }

//            client.Disconnect();
//        }

//        return resultFiles;
//    }

//    /// <summary>
//    /// Tải toàn bộ file .txt từ thư mục FTP (bao gồm thư mục con) về máy.
//    /// </summary>
//    private static void DownloadDirectoryContents(FtpClient client, string ftpDir, string localDir, string pattern, List<string> resultFiles)
//    {
//        // Tạo thư mục local nếu chưa có
//        Directory.CreateDirectory(localDir);

//        // Lấy danh sách các file và thư mục con
//        var items = client.GetListing(ftpDir);

//        // Tải các file .txt
//        foreach (var item in items.Where(i => i.Type == FtpObjectType.File && i.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)))
//        {
//            string localFile = Path.Combine(localDir, item.Name);
//            client.DownloadFile(localFile, item.FullName);
//            resultFiles.Add(localFile);
//        }

//        // Đệ quy xử lý thư mục con
//        foreach (var subDir in items.Where(i => i.Type == FtpObjectType.Directory))
//        {
//            string localSubDir = Path.Combine(localDir, subDir.Name);
//            DownloadDirectoryContents(client, subDir.FullName, localSubDir, pattern, resultFiles);
//        }
//    }
//    private static void SetLastWriteTimeRecursive(string path, DateTime time)
//    {
//        // Set cho chính thư mục
//        Directory.SetLastWriteTime(path, time);

//        // Set cho tất cả file trong thư mục
//        foreach (string file in Directory.GetFiles(path))
//        {
//            File.SetLastWriteTime(file, time);
//        }

//        // Đệ quy cho các thư mục con
//        foreach (string subDir in Directory.GetDirectories(path))
//        {
//            SetLastWriteTimeRecursive(subDir, time);
//        }
//    }
//}



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentFTP;

public static class RGB_OLD
{
    private static readonly object logLock = new object();

    /// <summary>
    /// Copy 3 thư mục con mới nhất từ thư mục PASS và FAIL trên FTP vào thư mục đích cục bộ.
    /// </summary>
    /// <param name="ftpUrl">Đường dẫn FTP, ví dụ: ftp://ATD@10.69.230.66/ATD/Marc/FAI/RGB_OLD/</param>
    /// <param name="destPath">Thư mục đích cục bộ, ví dụ: D:\RGB_FAI</param>
    /// <param name="lineName">Tên line (ví dụ: A5B#2)</param>
    /// <param name="date">Ngày cần xử lý (mặc định hôm nay)</param>
    /// <returns>Danh sách các file đã tải về</returns>
    public static List<string> CopyLatestFolders(string ftpUrl, string destPath, string lineName, DateTime? date = null)
    {
        var resultFiles = new List<string>();
        DateTime targetDate = date ?? DateTime.Today;
        string dateFolder = targetDate.ToString("yyyy_MM_dd");
        string formatDate = targetDate.ToString("yyyy-MM-dd");
        string baseOutput = Path.Combine(destPath, dateFolder);

        // Đường dẫn file log trong thư mục Log (cùng thư mục exe)
        string logFile = GetLogFilePath(targetDate);
        WriteLog(logFile, $"=== Bắt đầu xử lý RGB_OLD lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        WriteLog(logFile, $"FTP Url: {ftpUrl}, Line: {lineName}, Destination: {destPath}");

        DateTime baseTime = DateTime.Now;
        int minuteOffset = 0;
        Random rand = new Random();

        // Parse thông tin từ URL
        Uri uri = new Uri(ftpUrl);
        string host = uri.Host;
        string username = uri.UserInfo; // "ATD"
        string password = "ATD"; // Nếu có, lấy từ config hoặc hardcode.
        string path = uri.PathAndQuery.TrimStart('/').TrimEnd('/'); // "ATD/Marc/FAI/RGB_OLD"
        string basePath = Path.Combine(path, lineName, formatDate).Replace('\\', '/'); // Đảm bảo dùng '/' cho FTP

        WriteLog(logFile, $"Kết nối FTP: host={host}, username={username}, basePath={basePath}");

        using (var client = new FtpClient(host, username, password))
        {
            client.Connect();
            WriteLog(logFile, "Đã kết nối FTP thành công.");

            // Xử lý thư mục PASS
            string passPath = basePath + "/PASS";
            if (client.DirectoryExists(passPath))
            {
                var passSubDirs = client.GetListing(passPath)
                                        .Where(i => i.Type == FtpObjectType.Directory)
                                        .OrderByDescending(d => d.Modified)
                                        .Take(3)
                                        .ToList();

                WriteLog(logFile, $"Tìm thấy {passSubDirs.Count} thư mục PASS mới nhất trên FTP.");

                foreach (var dir in passSubDirs)
                {
                    string destDir = Path.Combine(baseOutput, "OK", dir.Name);
                    WriteLog(logFile, $"Đang xử lý thư mục PASS: {dir.Name} -> {destDir}");

                    DownloadDirectoryContents(client, dir.FullName, destDir, "*.log", resultFiles, logFile);
                    DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                    SetLastWriteTimeRecursive(destDir, targetTime);

                    minuteOffset += rand.Next(1, 6); // Random 1-5 phút
                }
            }
            else
            {
                WriteLog(logFile, $"Thư mục PASS không tồn tại trên FTP: {passPath}");
            }

            // Xử lý thư mục FAIL
            string failPath = basePath + "/FAIL";
            if (client.DirectoryExists(failPath))
            {
                var failSubDirs = client.GetListing(failPath)
                                        .Where(i => i.Type == FtpObjectType.Directory)
                                        .OrderByDescending(d => d.Modified)
                                        .Take(1)
                                        .ToList();

                if (failSubDirs.Count > 0)
                {
                    WriteLog(logFile, $"Tìm thấy thư mục FAIL mới nhất: {failSubDirs[0].Name}");

                    for (int i = 0; i < 3; i++)
                    {
                        string destDir = Path.Combine(baseOutput, "NG", failSubDirs[0].Name + "_" + i);
                        WriteLog(logFile, $"Tạo bản sao FAIL thứ {i + 1}: {destDir}");

                        DownloadDirectoryContents(client, failSubDirs[0].FullName, destDir, "*.log", resultFiles, logFile);
                        DateTime targetTime = baseTime.AddMinutes(minuteOffset);
                        SetLastWriteTimeRecursive(destDir, targetTime);

                        minuteOffset += rand.Next(1, 6); // Random 1-5 phút
                    }
                }
                else
                {
                    WriteLog(logFile, "Không có thư mục con nào trong FAIL trên FTP.");
                }
            }
            else
            {
                WriteLog(logFile, $"Thư mục FAIL không tồn tại trên FTP: {failPath}");
            }

            client.Disconnect();
            WriteLog(logFile, "Đã ngắt kết nối FTP.");
        }

        WriteLog(logFile, $"Hoàn thành. Tổng số file đã tải: {resultFiles.Count}");
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
    /// Tải toàn bộ file .log từ thư mục FTP (bao gồm thư mục con) về máy.
    /// </summary>
    private static void DownloadDirectoryContents(FtpClient client, string ftpDir, string localDir, string pattern, List<string> resultFiles, string logFile)
    {
        Directory.CreateDirectory(localDir);
        var items = client.GetListing(ftpDir);
        int fileCount = 0;

        foreach (var item in items.Where(i => i.Type == FtpObjectType.File && i.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)))
        {
            string localFile = Path.Combine(localDir, item.Name);
            client.DownloadFile(localFile, item.FullName);
            resultFiles.Add(localFile);
            fileCount++;
        }

        WriteLog(logFile, $"  Đã tải {fileCount} file .log từ {ftpDir}");

        foreach (var subDir in items.Where(i => i.Type == FtpObjectType.Directory))
        {
            string localSubDir = Path.Combine(localDir, subDir.Name);
            DownloadDirectoryContents(client, subDir.FullName, localSubDir, pattern, resultFiles, logFile);
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