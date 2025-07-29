using MaIN.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Cllasifier
{
    public class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        /// 



        
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddMaIN(configuration);
            services.AddTransient<ModelLocal>();


            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.UseMaIN();

            serviceProvider.GetRequiredService<ModelLocal>();


            Console.WriteLine("--- File Sorter Console Application ---");

            string selectedPath = null;
            var thread = new Thread(() =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        selectedPath = dialog.SelectedPath;
                    }
                }
            });

            thread.SetApartmentState(ApartmentState.STA); // required for dialogs
            thread.Start();
            thread.Join(); // wait for dialog thread to finish



            //string sourcePath = GetFolderPath();
            string sourcePath = selectedPath;
            //string sourcePath = @"Your Path";
            if (string.IsNullOrEmpty(sourcePath))
            {
                Console.WriteLine("No folder selected. Exiting.");
                return;
            }

            bool includeSubfolders = AskForBoolean("Do you want to include subfolders? (yes/no): ");
            bool classifyWithAI = AskForBoolean("Do you want to classify with AI? (yes/no): ");

            string aiModel = string.Empty;
            if (classifyWithAI)
            {
                //aiModel = SelectAIModel();
                aiModel = "Gemini (Online)";
            }

            Console.WriteLine($"\n--- Starting File Sort ---");
            Console.WriteLine($"Source Path: {sourcePath}");
            Console.WriteLine($"Include Subfolders: {includeSubfolders}");
            Console.WriteLine($"Classify with AI: {classifyWithAI}");
            if (classifyWithAI)
            {
                Console.WriteLine($"AI Model: {aiModel}");
            }

            // Simulate btnSort_Click logic
            Console.WriteLine("\nProcessing files...");

            // Assuming Methods.SortFiles, Methods.Delete, Methods.ClassifyWithAi, Methods.SortFilesByCategory exist and are adapted for console use.
            // You'll need to provide the actual implementation of these methods.

            try
            {
                var sortedFolder = Methods.SortFiles(sourcePath, includeSubfolders);
                if (sortedFolder == null)
                {
                    Console.WriteLine("File sorting process was interrupted or failed.");
                    return;
                }

                if (includeSubfolders)
                {
                    Console.WriteLine("Attempting to delete original files from source after sorting...");
                    var deleteSuccess = Methods.Delete(sourcePath, sortedFolder);
                    if (deleteSuccess)
                    {
                        Console.WriteLine("Original files deleted successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to delete original files. Manual cleanup might be required.");
                    }
                }

                if (classifyWithAI)
                {
                    Console.WriteLine("Classifying files with AI...");
                    // Assuming ClassifyWithAi is an async method based on your original code's .Result

                    bool localYes = false;

                    if(aiModel == "Local Model") localYes = true;
                    
                    var categoriesDir = await Methods.ClassifyWithAi(sourcePath,localYes); // Pass appropriate second parameter
                    if (categoriesDir != null)
                    {
                        Console.WriteLine("Sorting files by AI categories...");
                        Methods.SortFilesByCategory(categoriesDir,sourcePath);
                        Console.WriteLine("AI classification and sorting complete.");
                    }
                    else
                    {
                        Console.WriteLine("AI classification failed or returned no categories.");
                    }
                }

                Console.WriteLine("\nFiles sorted successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Optionally log the full exception: Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Prompts the user to enter a folder path.
        /// </summary>
        private static string GetFolderPath()
        {
            while (true)
            {
                Console.Write("Please enter the source folder path: ");
                string path = Console.ReadLine();

                if (Directory.Exists(path))
                {
                    return path;
                }
                else
                {
                    Console.WriteLine("Error: The specified path does not exist. Please try again.");
                }
            }
        }

        /// <summary>
        /// Prompts the user for a yes/no answer and returns a boolean.
        /// </summary>
        private static bool AskForBoolean(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine().Trim().ToLower();
                if (input == "yes" || input == "y")
                {
                    return true;
                }
                else if (input == "no" || input == "n")
                {
                    return false;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter 'yes' or 'no'.");
                }
            }
        }

        /// <summary>
        /// Prompts the user to select an AI model.
        /// </summary>
        private static string SelectAIModel()
        {
            Console.WriteLine("Select an AI Model:");
            Console.WriteLine("1. Local Model");
            Console.WriteLine("2. Gemini (Online)");

            while (true)
            {
                Console.Write("Enter your choice (1 or 2): ");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        return "Local Model";
                    case "2":
                        return "Gemini (Online)";
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1 or 2.");
                        break;
                }
            }
        }
    }


}
