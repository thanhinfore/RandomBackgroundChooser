using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageClusterSelector
{
    public class ImageFeatures
    {
        public string Path { get; set; } = string.Empty;
        public double[] ColorHistogram { get; set; } = Array.Empty<double>();
        public float[] ContentVector { get; set; } = Array.Empty<float>();
    }

    public class FeatureExtractor
    {
        private readonly Config _config;

        public FeatureExtractor(Config config)
        {
            _config = config;
        }

        public ImageFeatures Extract(string imagePath)
        {
            var features = new ImageFeatures { Path = imagePath };
            features.ColorHistogram = ComputeColorHistogram(imagePath);
            features.ContentVector = ComputeContentVector(imagePath); // TODO: integrate YOLO/ResNet
            return features;
        }

        private double[] ComputeColorHistogram(string imagePath)
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(imagePath);
            int binsPerChannel = _config.HistogramBins;
            int[] bins = new int[binsPerChannel * 3];
            foreach (var pixel in image.GetPixelSpan())
            {
                bins[pixel.R * binsPerChannel / 256]++;
                bins[binsPerChannel + pixel.G * binsPerChannel / 256]++;
                bins[2 * binsPerChannel + pixel.B * binsPerChannel / 256]++;
            }
            double[] hist = new double[bins.Length];
            double total = image.Width * image.Height;
            for (int i = 0; i < bins.Length; i++)
            {
                hist[i] = bins[i] / total;
            }
            return hist;
        }

        private float[] ComputeContentVector(string imagePath)
        {
            int size = _config.ContentSize;
            using Image<Rgba32> image = Image.Load<Rgba32>(imagePath);
            image.Mutate(ctx => ctx.Resize(size, size));
            float[] vector = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = image[x, y];
                    float gray = (p.R + p.G + p.B) / 3f / 255f;
                    vector[y * size + x] = gray;
                }
            }
            return vector;
        }
    }
}
