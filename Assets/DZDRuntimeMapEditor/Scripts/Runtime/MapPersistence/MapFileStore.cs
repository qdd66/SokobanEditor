using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DZDMapEditor
{
    public static class MapFileStore
    {
        const int Magic0 = 'T';
        const int Magic1 = 'M';
        const int Magic2 = 'A';
        const int Magic3 = 'P';
        const int BinaryVersion = 1;

        public static string ResolveDirectory(MapPersistenceConfig config)
        {
#if UNITY_EDITOR
            if (config != null && config.UseProjectMapsFolderInEditor)
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Maps"));
#endif
            return Path.Combine(Application.persistentDataPath, "Maps");
        }

        public static string ResolvePath(MapPersistenceConfig config, string fileName)
        {
            var stem = SanitizeStem(fileName);
            if (string.IsNullOrEmpty(stem) && config != null)
                stem = SanitizeStem(config.DefaultFileName);
            if (string.IsNullOrEmpty(stem))
                stem = "Map";
            return Path.Combine(ResolveDirectory(config), stem + MapPersistenceConfig.FileExtension);
        }

        public static string SanitizeStem(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            var name = fileName.Trim();
            if (name.EndsWith(MapPersistenceConfig.FileExtension, StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - MapPersistenceConfig.FileExtension.Length);
            name = name.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            while (name.EndsWith(".", StringComparison.Ordinal) || name.EndsWith(" ", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 1).TrimEnd();
            if (name.Length > 80)
                name = name.Substring(0, 80).Trim();
            return name;
        }

        public static bool IsSamePath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;
            try
            {
                return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool TryRename(string path, string newName, out string newPath, out string error)
        {
            newPath = path;
            error = null;
            if (!IsMapFile(path))
            {
                error = MapEditorLocale.Pick("Only map saves can be renamed", "只能改名地图存档");
                return false;
            }

            if (!File.Exists(path))
            {
                error = MapEditorLocale.Pick("Save file not found", "找不到存档文件");
                return false;
            }

            var stem = SanitizeStem(newName);
            if (string.IsNullOrEmpty(stem))
            {
                error = MapEditorLocale.Pick("Please enter a new name", "请输入新名称");
                return false;
            }

            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir))
            {
                error = MapEditorLocale.Pick("Save folder not found", "找不到存档目录");
                return false;
            }

            var dest = Path.Combine(dir, stem + MapPersistenceConfig.FileExtension);
            if (!IsSamePath(path, dest) && File.Exists(dest))
            {
                error = MapEditorLocale.Pick("A save with that name already exists", "已有同名存档");
                return false;
            }

            if (TryReadParts(path, out var json, out _))
            {
                if (!string.Equals(json.mapName, stem, StringComparison.Ordinal))
                {
                    json.mapName = stem;
                    try
                    {
                        WriteBinary(path, json);
                    }
                    catch (Exception exception)
                    {
                        error = exception.Message;
                        return false;
                    }
                }
            }

            if (IsSamePath(path, dest) &&
                string.Equals(Path.GetFileName(path), Path.GetFileName(dest), StringComparison.Ordinal))
            {
                newPath = path;
                return true;
            }

            if (!TryMoveFile(path, dest, out error))
                return false;

            newPath = dest;
            return true;
        }

        public static bool TryDelete(string path, out string error)
        {
            error = null;
            if (!IsMapFile(path))
            {
                error = MapEditorLocale.Pick("Only map saves can be deleted", "只能删除地图存档");
                return false;
            }

            if (!File.Exists(path))
                return true;

            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool IsMapFile(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.EndsWith(MapPersistenceConfig.FileExtension, StringComparison.OrdinalIgnoreCase);
        }

        static bool TryMoveFile(string source, string dest, out string error)
        {
            error = null;
            try
            {
                if (IsSamePath(source, dest))
                {
                    var dir = Path.GetDirectoryName(dest);
                    var temp = Path.Combine(
                        dir,
                        ".rename_" + Guid.NewGuid().ToString("N") + MapPersistenceConfig.FileExtension);
                    File.Move(source, temp);
                    File.Move(temp, dest);
                    return true;
                }

                File.Move(source, dest);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static List<MapFileInfo> ListMaps(MapPersistenceConfig config)
        {
            var list = new List<MapFileInfo>();
            var dir = ResolveDirectory(config);
            if (!Directory.Exists(dir))
                return list;

            var files = Directory.GetFiles(dir, "*" + MapPersistenceConfig.FileExtension);
            for (var i = 0; i < files.Length; i++)
            {
                if (TryPeek(files[i], out var info))
                    list.Add(info);
            }

            list.Sort((a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
            return list;
        }

        public static bool TryPeek(string path, out MapFileInfo info)
        {
            info = new MapFileInfo
            {
                Path = path,
                FileName = Path.GetFileName(path),
                DisplayName = Path.GetFileNameWithoutExtension(path)
            };

            try
            {
                var file = new FileInfo(path);
                info.LastWriteTimeUtc = file.LastWriteTimeUtc;
                info.ByteLength = file.Length;
                if (!TryReadHeader(path, out var json, out var error))
                {
                    info.IsReadable = false;
                    info.Error = error;
                    return true;
                }

                info.DisplayName = string.IsNullOrEmpty(json.mapName)
                    ? Path.GetFileNameWithoutExtension(path)
                    : json.mapName;
                info.ItemCount = json.items != null ? json.items.Length : 0;
                info.FormatVersion = json.formatVersion;
                info.IsReadable = json.formatVersion <= MapPersistenceConfig.CurrentFormatVersion;
                if (!info.IsReadable)
                    info.Error = MapEditorLocale.Pick(
                        "This save uses a newer format than this editor",
                        "版本过新，当前编辑器无法读取");
                return true;
            }
            catch (Exception exception)
            {
                info.IsReadable = false;
                info.Error = exception.Message;
                return true;
            }
        }

        public static void Write(string path, MapDocumentJson json)
        {
            WriteBinary(path, json);
        }

        static void WriteBinary(string path, MapDocumentJson json)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var jsonBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(json));
            using (var stream = File.Create(path))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write((byte)Magic0);
                writer.Write((byte)Magic1);
                writer.Write((byte)Magic2);
                writer.Write((byte)Magic3);
                writer.Write(BinaryVersion);
                writer.Write(jsonBytes.Length);
                writer.Write(jsonBytes);
                writer.Write(0);
            }
        }

        public static bool TryRead(string path, out MapDocumentJson json, out string error)
        {
            json = null;
            error = null;
            if (!TryReadParts(path, out json, out error))
                return false;

            if (json.formatVersion > MapPersistenceConfig.CurrentFormatVersion)
            {
                error = MapEditorLocale.Pick(
                    "This save uses a newer format than this editor",
                    "版本过新，当前编辑器无法读取");
                json = null;
                return false;
            }

            return true;
        }

        static bool TryReadHeader(string path, out MapDocumentJson json, out string error)
        {
            json = null;
            return TryReadParts(path, out json, out error);
        }

        static bool TryReadParts(string path, out MapDocumentJson json, out string error)
        {
            json = null;
            error = null;
            try
            {
                using (var stream = File.OpenRead(path))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (reader.ReadByte() != Magic0 ||
                        reader.ReadByte() != Magic1 ||
                        reader.ReadByte() != Magic2 ||
                        reader.ReadByte() != Magic3)
                    {
                        error = MapEditorLocale.Pick("Not a valid map file", "不是有效的地图文件");
                        return false;
                    }

                    var binaryVersion = reader.ReadInt32();
                    if (binaryVersion != BinaryVersion)
                    {
                        error = MapEditorLocale.Pick("Unsupported file format version", "文件格式版本不支持");
                        return false;
                    }

                    var jsonLength = reader.ReadInt32();
                    if (jsonLength < 2 || jsonLength > 16 * 1024 * 1024)
                    {
                        error = MapEditorLocale.Pick("File header is damaged", "文件头损坏");
                        return false;
                    }

                    var jsonBytes = reader.ReadBytes(jsonLength);
                    if (jsonBytes.Length != jsonLength)
                    {
                        error = MapEditorLocale.Pick("File is incomplete", "文件不完整");
                        return false;
                    }

                    json = JsonUtility.FromJson<MapDocumentJson>(Encoding.UTF8.GetString(jsonBytes));
                    if (json == null)
                    {
                        error = MapEditorLocale.Pick("Could not parse map info", "无法解析地图信息");
                        return false;
                    }

                    SkipLegacyTerrainBytes(stream, reader);
                    return true;
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
                json = null;
                return false;
            }
        }

        static void SkipLegacyTerrainBytes(FileStream stream, BinaryReader reader)
        {
            if (stream.Position >= stream.Length)
                return;

            try
            {
                var terrainLength = reader.ReadInt32();
                if (terrainLength <= 0 || terrainLength > 128 * 1024 * 1024)
                    return;
                if (stream.Position + terrainLength > stream.Length)
                    return;
                reader.ReadBytes(terrainLength);
            }
            catch
            {
            }
        }
    }
}
