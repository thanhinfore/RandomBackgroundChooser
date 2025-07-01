using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ImageClusterSelector
{
    public class Config
    {
        public double ColorWeight { get; set; } = 0.6;
        public double ContentWeight { get; set; } = 0.4;
        public string CachePath { get; set; } = "features.db";
        public int HistogramBins { get; set; } = 16;
        public int ContentSize { get; set; } = 8;

        public static Config Load(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            using var reader = new StreamReader(path);
            return deserializer.Deserialize<Config>(reader);
        }
    }
}
