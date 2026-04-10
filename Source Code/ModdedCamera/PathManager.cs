using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace ModdedCamera
{
    /// <summary>
    /// Manages saving, loading, and deleting camera paths.
    /// FIXED: Migrated from XML to JSON serialization to avoid
    /// dynamic assembly generation and improve readability.
    /// Old XML files are automatically migrated on first access.
    /// </summary>
    public static class PathManager
    {
        private static readonly string PathsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "paths");
        private const string JsonExtension = ".json";
        private const string XmlExtension = ".xml";

        // Keep XmlSerializer for migration of old saves
        private static readonly XmlSerializer PathXmlSerializer = new XmlSerializer(typeof(CameraPath));

        // JSON settings for human-readable formatted output
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new Vector3JsonConverter() }
        };

        static PathManager()
        {
            if (!Directory.Exists(PathsFolder))
            {
                Directory.CreateDirectory(PathsFolder);
            }

            // Migrate old XML files to JSON on first access
            MigrateXmlToJson();
        }

        /// <summary>
        /// Automatically migrates old XML path files to JSON format.
        /// Runs once during static initialization.
        /// </summary>
        private static void MigrateXmlToJson()
        {
            try
            {
                string[] xmlFiles = Directory.GetFiles(PathsFolder, "*" + XmlExtension);
                int migrated = 0;

                foreach (string xmlFile in xmlFiles)
                {
                    try
                    {
                        CameraPath path;
                        using (var reader = new StreamReader(xmlFile))
                        {
                            path = (CameraPath)PathXmlSerializer.Deserialize(reader);
                        }

                        string jsonFile = Path.ChangeExtension(xmlFile, JsonExtension);
                        string json = JsonConvert.SerializeObject(path, JsonSettings);
                        File.WriteAllText(jsonFile, json);

                        // Delete old XML file after successful migration
                        File.Delete(xmlFile);
                        migrated++;
                        Logger.Info("Migrated: " + Path.GetFileName(xmlFile) + " → " + Path.GetFileName(jsonFile));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to migrate XML file: " + xmlFile);
                    }
                }

                if (migrated > 0)
                {
                    Logger.Info("XML→JSON migration complete. " + migrated + " file(s) migrated.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during XML→JSON migration");
            }
        }

        public static string SavePath(CameraPath path)
        {
            try
            {
                if (path == null)
                {
                    Logger.Error("SavePath: path is null");
                    return null;
                }

                if (string.IsNullOrEmpty(path.Name))
                {
                    Logger.Error("SavePath: path name is empty");
                    return null;
                }

                string fileName = SanitizeFileName(path.Name) + JsonExtension;
                string filePath = Path.Combine(PathsFolder, fileName);

                Logger.Info("SavePath: Saving to " + filePath + " with " + 
                           (path.Positions?.Count.ToString() ?? "null") + " positions");

                string json = JsonConvert.SerializeObject(path, JsonSettings);
                File.WriteAllText(filePath, json);
                
                Logger.Info("SavePath: Successfully saved " + fileName + " (" + new FileInfo(filePath).Length + " bytes)");
                return filePath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save path: " + (path?.Name ?? "null"));
                return null;
            }
        }

        public static CameraPath LoadPath(string pathName)
        {
            string fileName = SanitizeFileName(pathName) + JsonExtension;
            string filePath = Path.Combine(PathsFolder, fileName);

            if (!File.Exists(filePath))
            {
                // Fallback: try XML extension if JSON not found (for safety)
                string xmlFile = Path.ChangeExtension(filePath, XmlExtension);
                if (File.Exists(xmlFile))
                {
                    try
                    {
                        using (var reader = new StreamReader(xmlFile))
                        {
                            CameraPath path = (CameraPath)PathXmlSerializer.Deserialize(reader);
                            return ApplyBackwardCompatibility(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to load fallback XML path: " + pathName);
                        return null;
                    }
                }
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                CameraPath path = JsonConvert.DeserializeObject<CameraPath>(json);
                return ApplyBackwardCompatibility(path);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load path: " + pathName);
                return null;
            }
        }

        public static bool PathExists(string pathName)
        {
            string fileName = SanitizeFileName(pathName);
            string jsonPath = Path.Combine(PathsFolder, fileName + JsonExtension);
            string xmlPath = Path.Combine(PathsFolder, fileName + XmlExtension);
            return File.Exists(jsonPath) || File.Exists(xmlPath);
        }

        public static List<string> GetAllSavedPaths()
        {
            var paths = new List<string>();

            if (!Directory.Exists(PathsFolder))
            {
                Logger.Info("GetAllSavedPaths: Paths folder does not exist: " + PathsFolder);
                return paths;
            }

            // Check JSON files first (new format)
            string[] jsonFiles = Directory.GetFiles(PathsFolder, "*" + JsonExtension);
            Logger.Info("GetAllSavedPaths: Found " + jsonFiles.Length + " JSON files");
            foreach (string file in jsonFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                paths.Add(name);
                Logger.Info("GetAllSavedPaths: Found path: " + name);
            }

            // Also check XML files (old format, for backward compatibility)
            string[] xmlFiles = Directory.GetFiles(PathsFolder, "*" + XmlExtension);
            Logger.Info("GetAllSavedPaths: Found " + xmlFiles.Length + " XML files");
            foreach (string file in xmlFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!paths.Contains(name))
                {
                    paths.Add(name);
                    Logger.Info("GetAllSavedPaths: Found XML path: " + name);
                }
            }

            Logger.Info("GetAllSavedPaths: Returning " + paths.Count + " total paths");
            return paths;
        }

        public static bool DeletePath(string pathName)
        {
            string fileName = SanitizeFileName(pathName);
            string jsonPath = Path.Combine(PathsFolder, fileName + JsonExtension);
            string xmlPath = Path.Combine(PathsFolder, fileName + XmlExtension);

            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
                Logger.Info("Deleted path: " + fileName);
                return true;
            }

            if (File.Exists(xmlPath))
            {
                File.Delete(xmlPath);
                Logger.Info("Deleted path (XML): " + fileName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Apply backward compatibility defaults to loaded paths.
        /// Old saves may not have Fov/Speed/InterpolationMode/DefaultDuration.
        /// </summary>
        private static CameraPath ApplyBackwardCompatibility(CameraPath path)
        {
            if (path == null) return null;

            if (path.Fov <= 0) path.Fov = 50;
            if (path.Speed <= 0) path.Speed = 3;
            if (path.DefaultDuration <= 0) path.DefaultDuration = 5000;

            // Convert old int-based interpolation mode to enum range
            if (path.InterpolationMode != (int)InterpolationMode.Linear &&
                path.InterpolationMode != (int)InterpolationMode.Smooth)
            {
                path.InterpolationMode = (int)InterpolationMode.Smooth;
            }

            if (path.Durations == null || path.Durations.Count == 0)
            {
                path.Durations = new List<int>();
                int nodeCount = (path.Positions != null) ? path.Positions.Count : 0;
                for (int i = 0; i < nodeCount; i++)
                {
                    path.Durations.Add(path.DefaultDuration);
                }
            }

            return path;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "unnamed_path";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return name.Trim();
        }
    }
}
