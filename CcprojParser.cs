using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecoStaScan
{
    public class CcprojData
    {
        public string FileName { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public List<Speaker> Speakers { get; set; } = new List<Speaker>();
        public List<TextData> Texts { get; set; } = new List<TextData>();
    }

    public class Speaker
    {
        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public class TextData
    {
        public string Text { get; set; } = "";
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string SpeakerName { get; set; } = "";
    }

    public class ScanResult
    {
        public List<CcprojData> Data { get; set; } = new List<CcprojData>();
        public TimeSpan ScanDuration { get; set; }
        public CacheStats CacheStats { get; set; } = new CacheStats();
        public bool CacheUsed { get; set; }
        public List<DuplicateGroup> DuplicateGroups { get; set; } = new List<DuplicateGroup>();
        public int SkippedDuplicates { get; set; }
    }

    public class DuplicateGroup
    {
        public string ProjectName { get; set; } = "";
        public List<FileEntry> Files { get; set; } = new List<FileEntry>();
        public FileEntry LatestFile { get; set; } = new FileEntry();
    }

    public class FileEntry
    {
        public string FilePath { get; set; } = "";
        public DateTime LastModified { get; set; }
        public long FileSize { get; set; }
        public bool IsSelected { get; set; }
    }

    public class DuplicateFilterResult
    {
        public List<CcprojData> FilteredData { get; set; } = new List<CcprojData>();
        public List<DuplicateGroup> DuplicateGroups { get; set; } = new List<DuplicateGroup>();
        public int SkippedCount { get; set; }
    }

    public static class CcprojParser
    {
        public static CcprojData ParseFile(string filePath)
        {
            try
            {
                var jsonText = File.ReadAllText(filePath);
                var jsonObj = JObject.Parse(jsonText);

                var result = new CcprojData
                {
                    FileName = Path.GetFileName(filePath)
                };

                // プロジェクト名を取得
                if (jsonObj["setting"]?["project-name"] != null)
                {
                    result.ProjectName = jsonObj["setting"]["project-name"].ToString();
                }

                // スピーカー情報を取得
                if (jsonObj["speakers"] is JArray speakers)
                {
                    foreach (var speaker in speakers)
                    {
                        var name = speaker["name"]?.ToString() ?? "";
                        var file = speaker["file"]?.ToString() ?? "";
                        
                        if (!string.IsNullOrEmpty(name))
                        {
                            result.Speakers.Add(new Speaker { Name = name, FilePath = file });
                        }
                    }
                }

                // レイヤーからテキストを抽出
                if (jsonObj["layers"] is JArray layers)
                {
                    foreach (var layer in layers)
                    {
                        ExtractTextFromLayer(layer, result);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"ファイル解析エラー: {filePath}\n{ex.Message}");
            }
        }

        private static void ExtractTextFromLayer(JToken layer, CcprojData result)
        {
            // レイヤーオブジェクトからテキストを抽出
            if (layer["layer-objects"] is JArray layerObjects)
            {
                foreach (var obj in layerObjects)
                {
                    // Speaker Voiceオブジェクトからテキストを抽出
                    if (obj["type"]?.ToString() == "Speaker Voice")
                    {
                        var text = obj["name"]?.ToString() ?? "";
                        
                        // textプロパティからもテキストを取得
                        if (string.IsNullOrEmpty(text) && obj["text"] != null)
                        {
                            text = obj["text"]["text"]?.ToString() ?? "";
                        }
                        
                        if (!string.IsNullOrEmpty(text))
                        {
                            var textData = new TextData
                            {
                                Text = text,
                                StartTime = obj["start-time"]?.ToObject<double>() ?? 0,
                                EndTime = obj["end-time"]?.ToObject<double>() ?? 0
                            };

                            // スピーカー名を推定（レイヤー名から）
                            var layerName = layer["name"]?.ToString() ?? "";
                            textData.SpeakerName = layerName;

                            result.Texts.Add(textData);
                        }
                    }
                }
            }

            // 子レイヤーを再帰的に処理
            if (layer["layers"] is JArray childLayers)
            {
                foreach (var childLayer in childLayers)
                {
                    ExtractTextFromLayer(childLayer, result);
                }
            }
        }

        public static List<CcprojData> ScanDirectory(string directoryPath, IProgress<string>? progress = null, bool useCache = true)
        {
            var results = new List<CcprojData>();
            var files = Directory.GetFiles(directoryPath, "*.ccproj");
            
            ScanCache cache = new ScanCache();
            if (useCache)
            {
                cache = CacheManager.LoadCache();
                cache.ScanDirectory = directoryPath;
                
                // 削除されたファイルのキャッシュエントリを除去
                CacheManager.RemoveObsoleteEntries(cache, files);
            }

            int totalFiles = files.Length;
            int processedFiles = 0;
            int cachedFiles = 0;

            foreach (var file in files)
            {
                try
                {
                    processedFiles++;
                    var fileName = Path.GetFileName(file);
                    
                    CcprojData? data = null;
                    
                    if (useCache)
                    {
                        data = CacheManager.GetCachedData(cache, file);
                        if (data != null)
                        {
                            cachedFiles++;
                            progress?.Report($"キャッシュから読み込み: {fileName} ({processedFiles}/{totalFiles})");
                            results.Add(data);
                            continue;
                        }
                    }
                    
                    progress?.Report($"解析中: {fileName} ({processedFiles}/{totalFiles})");
                    data = ParseFile(file);
                    results.Add(data);
                    
                    if (useCache)
                    {
                        CacheManager.UpdateFileCache(cache, file, data);
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"エラー: {Path.GetFileName(file)} - {ex.Message}");
                }
            }

            if (useCache)
            {
                cache.LastScanTime = DateTime.Now;
                CacheManager.SaveCache(cache);
                
                if (cachedFiles > 0)
                {
                    progress?.Report($"完了: {results.Count}件のファイルを読み込みました (キャッシュ使用: {cachedFiles}件)");
                }
                else
                {
                    progress?.Report($"完了: {results.Count}件のファイルを読み込みました");
                }
            }
            else
            {
                progress?.Report($"完了: {results.Count}件のファイルを読み込みました (キャッシュ無効)");
            }

            return results;
        }

        public static ScanResult ScanDirectoryWithStats(string directoryPath, IProgress<string>? progress = null, bool useCache = true, bool filterDuplicates = true)
        {
            var startTime = DateTime.Now;
            var allData = ScanDirectory(directoryPath, progress, useCache);
            
            var duplicateGroups = new List<DuplicateGroup>();
            var finalData = allData;
            var skippedCount = 0;
            
            if (filterDuplicates)
            {
                var duplicateResult = FilterDuplicateProjects(allData, directoryPath);
                finalData = duplicateResult.FilteredData;
                duplicateGroups = duplicateResult.DuplicateGroups;
                skippedCount = duplicateResult.SkippedCount;
                
                if (skippedCount > 0)
                {
                    progress?.Report($"重複ファイル処理完了: {skippedCount}件のファイルをスキップしました");
                }
            }
            
            var endTime = DateTime.Now;
            var cache = useCache ? CacheManager.LoadCache() : new ScanCache();
            var stats = CacheManager.GetCacheStats(cache);
            
            return new ScanResult
            {
                Data = finalData,
                ScanDuration = endTime - startTime,
                CacheStats = stats,
                CacheUsed = useCache,
                DuplicateGroups = duplicateGroups,
                SkippedDuplicates = skippedCount
            };
        }

        public static DuplicateFilterResult FilterDuplicateProjects(List<CcprojData> allData, string directoryPath)
        {
            var files = Directory.GetFiles(directoryPath, "*.ccproj");
            var fileInfoMap = files.ToDictionary(f => f, f => new FileInfo(f));
            
            // プロジェクト名でグループ化
            var projectGroups = allData.GroupBy(d => 
                string.IsNullOrEmpty(d.ProjectName) ? Path.GetFileNameWithoutExtension(d.FileName) : d.ProjectName
            ).ToList();
            
            var duplicateGroups = new List<DuplicateGroup>();
            var filteredData = new List<CcprojData>();
            var skippedCount = 0;
            
            foreach (var group in projectGroups)
            {
                if (group.Count() > 1)
                {
                    // 重複グループを作成
                    var duplicateGroup = new DuplicateGroup
                    {
                        ProjectName = group.Key
                    };
                    
                    var fileEntries = new List<FileEntry>();
                    CcprojData? latestData = null;
                    DateTime latestTime = DateTime.MinValue;
                    
                    foreach (var data in group)
                    {
                        var fullPath = Path.Combine(directoryPath, data.FileName);
                        if (fileInfoMap.TryGetValue(fullPath, out var fileInfo))
                        {
                            var entry = new FileEntry
                            {
                                FilePath = fullPath,
                                LastModified = fileInfo.LastWriteTime,
                                FileSize = fileInfo.Length,
                                IsSelected = false
                            };
                            fileEntries.Add(entry);
                            
                            if (fileInfo.LastWriteTime > latestTime)
                            {
                                latestTime = fileInfo.LastWriteTime;
                                latestData = data;
                                entry.IsSelected = true;
                            }
                        }
                    }
                    
                    // 他のファイルの選択状態をリセット
                    foreach (var entry in fileEntries)
                    {
                        if (entry.LastModified != latestTime)
                        {
                            entry.IsSelected = false;
                        }
                    }
                    
                    duplicateGroup.Files = fileEntries.OrderByDescending(f => f.LastModified).ToList();
                    duplicateGroup.LatestFile = fileEntries.First(f => f.IsSelected);
                    duplicateGroups.Add(duplicateGroup);
                    
                    // 最新のファイルのみを結果に含める
                    if (latestData != null)
                    {
                        filteredData.Add(latestData);
                    }
                    
                    skippedCount += group.Count() - 1;
                }
                else
                {
                    // 重複していないファイルはそのまま追加
                    filteredData.Add(group.First());
                }
            }
            
            return new DuplicateFilterResult
            {
                FilteredData = filteredData,
                DuplicateGroups = duplicateGroups,
                SkippedCount = skippedCount
            };
        }

        public static List<CcprojData> SearchByCharacter(List<CcprojData> data, string characterName)
        {
            return data.Where(d => 
                d.Speakers.Any(s => s.Name.Contains(characterName, StringComparison.OrdinalIgnoreCase)) ||
                d.Texts.Any(t => t.SpeakerName.Contains(characterName, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        public static List<CcprojData> SearchByText(List<CcprojData> data, string searchText)
        {
            return data.Where(d => 
                d.Texts.Any(t => t.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
    }
}