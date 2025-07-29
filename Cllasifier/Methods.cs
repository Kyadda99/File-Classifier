using MaIN.Core.Hub;
using System.Text.RegularExpressions;

namespace Cllasifier
{
    public static class Methods
    {

        public static string SortFiles(string sourcePath, bool includeSubfolders = false)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                Console.WriteLine("Please select a valid folder.");
                return "";
            }

            string sortedFolder = Path.Combine(sourcePath, "sortedFiles");

            if (!Directory.Exists(sortedFolder))
            {
                Directory.CreateDirectory(sortedFolder);
            }

            // Get files from base folder and optionally subfolders
            var files = Directory.GetFiles(
                sourcePath,
                "*.*",
                includeSubfolders? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
            //chkIncludeSubfolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
            );

            foreach (var file in files)
            {
                if (Path.GetFullPath(file).StartsWith(Path.GetFullPath(sortedFolder) + Path.DirectorySeparatorChar))
                    continue;

                string ext = Path.GetExtension(file).TrimStart('.').ToLower();
                if (string.IsNullOrEmpty(ext)) ext = "no_extension";

                string destDir = Path.Combine(sortedFolder, ext);

                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                string destFile = Path.Combine(destDir, Path.GetFileName(file));

                // Move file, handle duplicates by renaming
                int counter = 1;
                while (File.Exists(destFile))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string extension = Path.GetExtension(file);
                    destFile = Path.Combine(destDir, $"{fileName}_{counter}{extension}");
                    counter++;
                }

                File.Move(file, destFile);
            }
            return sortedFolder;
        }

        public static bool Delete(string sourcePath, string sortedFolder)
        {
            var allDirs = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)
                    .Where(d =>
    !Path.GetFullPath(d).Equals(Path.GetFullPath(sortedFolder), StringComparison.OrdinalIgnoreCase) &&
    !Path.GetFullPath(d).StartsWith(Path.GetFullPath(sortedFolder) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(d => d.Length); // Delete deepest folders first

            foreach (var dir in allDirs)
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir, true);
                }
            }
            return true;
        }

        public static async Task<Dictionary<string, string>> ClassifyWithAi(string path, bool local = false)
        {

            var systemPrompt =
            """
            You are a folder classification assistant.

            I will provide you a list of full folder paths, including subfolders.

            Your task is to analyze the **folder name only** (not its contents), and assign each folder a **one-word category**. Choose from general-purpose categories like:

            study, game, work, mods, music, photos, code, school, personal, movie, financial, system, archive, backup, other

            Your output must be a **JSON object** where:
            - The keys are the full folder paths
            - The values are the assigned category labels

            Example output format:

            {
              "C:\\Users\\me\\Documents\\University\\MathNotes.docx": "study",
              "C:\\Users\\me\\Games\\Steam\\Skyrim.exe": "game",
              "C:\\Users\\me\\Downloads\\Mods\\Skyrim_Armor_Pack.zip": "mods"
            }

            Return only the JSON. No explanations or comments.

            Here is the folder list:
            
            
            """;



            var prompt = $" List of dirs:{GetRelativeFilePaths(path,true)} ";
            MaIN.Services.Services.Models.ChatResult result1;
            if (local)
            {
                var model = new GeminiModel();
                model.prompt = prompt;
                model.systemPrompt = systemPrompt;

                await model.Start();

                result1 = model.result;
            }
            else
            {
                var model = new ModelLocal();
                model.prompt = prompt;
                model.systemPrompt = systemPrompt;

                await model.Start();

                result1 = model.result;
            }

            var result = result1;

            var match = Regex.Match(result.Message.Content, @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))*\}(?(open)(?!))");
            string json;

            if (match.Success) json = match.Value;
            else return new Dictionary<string, string> { };

            Dictionary<string, string> folderCategoryMap = new Dictionary<string, string> { };
            
            folderCategoryMap = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);


            return folderCategoryMap;



        }

        public static void SortFilesByCategory(Dictionary<string, string> classificationMap,string path)
        {
            foreach (var kvp in classificationMap)
            {
                string filePath = path +"\\" + kvp.Key;
                string category = kvp.Value;

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    continue;
                }

                // Get the current file directory
                string currentDir = Path.GetDirectoryName(filePath);

                // Create category folder inside the current directory
                string categoryFolder = Path.Combine(currentDir, category);
                Directory.CreateDirectory(categoryFolder);

                // Destination path is inside the category folder
                string destFile = Path.Combine(categoryFolder, Path.GetFileName(filePath));

                // Avoid overwriting existing file
                int count = 1;
                while (File.Exists(destFile))
                {
                    string filenameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    string ext = Path.GetExtension(filePath);
                    destFile = Path.Combine(categoryFolder, $"{filenameWithoutExt}_{count}{ext}");
                    count++;
                }

                File.Move(filePath, destFile);
            }
        }

        public static string GetRelativeFilePaths(string baseFolderPath,bool full = false)
        {
            string relativeFilePaths = "";

            // Ensure the provided base folder path exists
            if (!Directory.Exists(baseFolderPath))
            {
                Console.WriteLine($"Error: The specified folder '{baseFolderPath}' does not exist.");
                return relativeFilePaths; // Return an empty list
            }

            try
            {
                // Get all files (including those in subdirectories)
                string[] allFiles = Directory.GetFiles(baseFolderPath, "*.*", SearchOption.AllDirectories);

                foreach (string filePath in allFiles)
                {
                    // Make the path relative to the base folder
                    // Example: If baseFolderPath is "C:\MyFolder" and filePath is "C:\MyFolder\SubFolder\file.txt"
                    // Then relativePath will be "SubFolder\file.txt"
                    if (full)
                    {
                        string relativePath = filePath;
                        relativeFilePaths = relativeFilePaths + relativePath + "\n";
                    }
                    else
                    {
                        string relativePath = Path.GetRelativePath(baseFolderPath, filePath);
                        relativeFilePaths = relativeFilePaths + relativePath + "\n";
                    }



                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Error: Access to a path is denied. Please check permissions for '{baseFolderPath}' or its subfolders. Details: {ex.Message}");
            }
            catch (PathTooLongException ex)
            {
                Console.WriteLine($"Error: The specified path, file name, or both are too long. Details: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error: An I/O error occurred while accessing files. Details: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }

            return relativeFilePaths;
        }
    }
}
