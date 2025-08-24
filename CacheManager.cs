using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecoStaScan
{
    public class CachedFileInfo
    {
        public string FilePath { get; set; } = "";
        public DateTime LastModified { get; set; }
        public long FileSize { get; set; }
        public CcprojData Data { get; set; } = new CcprojData();
        public DateTime CachedAt { get; set; }
    }

    public class ScanCache
    {
        public Dictionary<string, CachedFileInfo> Files { get; set; } = new Dictionary<string, CachedFileInfo>();
        public DateTime LastScanTime { get; set; }
        public string ScanDirectory { get; set; } = "";
    }

    public static class CacheManager
    {
        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecoStaScan",
            "scan_cache.json"
        );

        public static ScanCache LoadCache()
        {
            try
            {
                if (File.Exists(CacheFilePath))
                {
                    var json = File.ReadAllText(CacheFilePath);
                    return JsonConvert.DeserializeObject<ScanCache>(json) ?? new ScanCache();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"キャッシュ読み込みエラー: {ex.Message}");
            }
            
            return new ScanCache();
        }

        public static void SaveCache(ScanCache cache)
        {
            try
            {
                var directory = Path.GetDirectoryName(CacheFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(cache, Formatting.Indented);
                File.WriteAllText(CacheFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"キャッシュ保存エラー: {ex.Message}");
            }
        }

        public static void ClearCache()
        {
            try
            {
                if (File.Exists(CacheFilePath))
                {
                    File.Delete(CacheFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"キャッシュクリアエラー: {ex.Message}");
            }
        }

        public static bool IsFileCached(ScanCache cache, string filePath)
        {
            if (!cache.Files.ContainsKey(filePath))
                return false;

            var cachedInfo = cache.Files[filePath];
            var fileInfo = new FileInfo(filePath);
            
            if (!fileInfo.Exists)
                return false;

            // ファイルの更新日時とサイズをチェック
            return cachedInfo.LastModified == fileInfo.LastWriteTime && 
                   cachedInfo.FileSize == fileInfo.Length;
        }

        public static void UpdateFileCache(ScanCache cache, string filePath, CcprojData data)
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                cache.Files[filePath] = new CachedFileInfo
                {
                    FilePath = filePath,
                    LastModified = fileInfo.LastWriteTime,
                    FileSize = fileInfo.Length,
                    Data = data,
                    CachedAt = DateTime.Now
                };
            }
        }

        public static CcprojData? GetCachedData(ScanCache cache, string filePath)
        {
            if (IsFileCached(cache, filePath))
            {
                return cache.Files[filePath].Data;
            }
            return null;
        }

        public static void RemoveObsoleteEntries(ScanCache cache, IEnumerable<string> currentFiles)
        {
            var currentFileSet = new HashSet<string>(currentFiles);
            var keysToRemove = cache.Files.Keys.Where(key => !currentFileSet.Contains(key)).ToList();
            
            foreach (var key in keysToRemove)
            {
                cache.Files.Remove(key);
            }
        }

        public static CacheStats GetCacheStats(ScanCache cache)
        {
            var now = DateTime.Now;
            var totalFiles = cache.Files.Count;
            var recentFiles = cache.Files.Values.Count(f => (now - f.CachedAt).TotalDays <= 7);
            var cacheSize = EstimateCacheSize(cache);

            return new CacheStats
            {
                TotalCachedFiles = totalFiles,
                RecentlyUpdatedFiles = recentFiles,
                CacheSizeKB = cacheSize,
                LastScanTime = cache.LastScanTime,
                CacheDirectory = Path.GetDirectoryName(CacheFilePath) ?? ""
            };
        }

        private static long EstimateCacheSize(ScanCache cache)
        {
            try
            {
                if (File.Exists(CacheFilePath))
                {
                    return new FileInfo(CacheFilePath).Length / 1024; // KB
                }
            }
            catch
            {
                // エラーの場合は推定値を返す
            }
            
            return cache.Files.Count * 2; // 大雑把な推定値 (KB)
        }
    }

    public class CacheStats
    {
        public int TotalCachedFiles { get; set; }
        public int RecentlyUpdatedFiles { get; set; }
        public long CacheSizeKB { get; set; }
        public DateTime LastScanTime { get; set; }
        public string CacheDirectory { get; set; } = "";
    }
}