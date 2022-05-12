using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Program
{
    static class Program
    {
        static Dictionary<long, string> NameHashes = new Dictionary<long, string>();

        static void Main(string[] inputPath)
        {
            string path;
            if (inputPath.Length > 0)
            {
                path = inputPath[0];
            }
            else
            {
                Console.WriteLine("Invalid Path!\nPlease drag and drop your replay file into the executable\nPress Enter to close this window...");
                Console.Read();
                return;
            }

            JArray replay = new JArray();
            try
            {
                Console.WriteLine("Loading replay file...\nThis might take a while.");
                replay = JArray.Parse(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Loading Hash Map...");
            LoadHashes();

            while (NameHashes.Count == 0)
            {
                Console.WriteLine("Content folder not found!\nPlease, insert path to Content the folder manually:");
                path = Console.ReadLine();
                Regex rgx = new Regex("[\"]");
                path = rgx.Replace(path, "");
                LoadHashes(path);
            }

            Console.WriteLine("Hash Map Loaded!");

            Console.WriteLine("Unhashing...\nThis might take a while");
            Parallel.For(0, replay.Count, i =>
            {
                var packetInfo = replay[i].SelectToken("Packet");

                Parallel.ForEach(packetInfo.ToArray(), parameter =>
                {
                    try
                    {
                        var key = parameter.First.Value<long>();

                        if (NameHashes.ContainsKey(key))
                        {
                            string name = parameter.ToString();
                            Regex rgx = new Regex("[\\d - : \" ]");
                            name = rgx.Replace(name, "");
                            replay[i]["Packet"][name] = NameHashes[key];
                            Console.WriteLine($"Unhashed {key} to {NameHashes[key]}!");
                        }
                    }
                    catch
                    {

                    }
                });
            });

            string outputPath = $"{Path.GetFullPath(Path.GetDirectoryName(path))}/{Path.GetFileNameWithoutExtension(path)}Unhashed.json";
            File.WriteAllText(outputPath, replay.ToString());
            Console.WriteLine($"Done!\nYour file is in: {outputPath}\nPress Enter to close this window...");
            Console.Read();
        }

        public static void LoadHashes(string manualPath = "")
        {
            string contentPath;
            if (string.IsNullOrEmpty(manualPath))
            {
                contentPath = GetContentPath();
            }
            else
            {
                contentPath = manualPath;
            }

            if(contentPath != null && Directory.Exists(contentPath))
            {
                foreach (var file in Directory.GetFiles(contentPath, "*.json", SearchOption.AllDirectories))
                {
                    var jsonFile = JArray.Parse(File.ReadAllText(file));
                    foreach (JObject hash in jsonFile)
                    {
                        if (!NameHashes.ContainsKey(hash.Value<long>("Hash")))
                        {
                            NameHashes.Add(hash.Value<long>("Hash"), hash.Value<string>("Name"));
                        }
                    }
                }
            }
        }

        private static string GetContentPath()
        {
            string result = null;

            var executionDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if(executionDirectory != null)
            {
                var directories = Directory.GetDirectories(executionDirectory);
                if (directories != null && directories.Contains($"{executionDirectory}\\Content"))
                {
                    return $"{executionDirectory}\\Content";
                }
            }
            var path = new DirectoryInfo(executionDirectory ?? Directory.GetCurrentDirectory());

            while (result == null)
            {
                if (path == null)
                {
                    break;
                }

                var directory = path.GetDirectories().Where(c => c.Name.Equals("Content")).ToArray();

                if (directory.Length == 1)
                {
                    result = directory[0].FullName;
                }
                else
                {
                    path = path.Parent;
                }
            }

            return result;
        }
    }
}