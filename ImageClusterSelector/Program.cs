using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace ImageClusterSelector
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ImageClusterSelector <config.yml> <images_folder>");
                return;
            }

            string configPath = args[0];
            string imagesFolder = args[1];

            var config = Config.Load(configPath);
            var extractor = new FeatureExtractor(config);
            var cache = new FeatureCache(config.CachePath);

            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

            var files = Directory
                .EnumerateFiles(imagesFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f)));

            // Pre-process step: extract & cache features in parallel
            Parallel.ForEach(files, imagePath =>
            {
                if (!cache.HasFeatures(imagePath))
                {
                    var features = extractor.Extract(imagePath);
                    cache.SaveFeatures(imagePath, features);
                }
            });

            var allFeatures = cache.LoadAllFeatures();
            var selector = new ImageSelector(config);

            var selected = selector.Select(allFeatures, 4);

            string json = JsonSerializer.Serialize(selected, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);
        }
    }
}
