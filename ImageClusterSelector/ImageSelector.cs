using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageClusterSelector
{
    public class ImageSelector
    {
        private readonly Config _config;

        public ImageSelector(Config config)
        {
            _config = config;
        }

        public List<(string Path, double Score)> Select(List<ImageFeatures> features, int count)
        {
            if (features.Count == 0) return new List<(string, double)>();

            var rand = new Random();
            var seed = features[rand.Next(features.Count)];
            var scores = new List<(string Path, double Score)>();

            foreach (var item in features)
            {
                double colorDist = HistogramDistance(seed.ColorHistogram, item.ColorHistogram);
                double contentDist = ContentDistance(seed.ContentVector, item.ContentVector);
                double score = _config.ColorWeight * colorDist + _config.ContentWeight * contentDist;
                scores.Add((item.Path, score));
            }

            var result = new List<(string Path, double Score)>();
            foreach (var s in scores.OrderBy(s => s.Score))
            {
                if (s.Path == seed.Path) continue;
                result.Add(s);
                if (result.Count == count) break;
            }

            return result;
        }

        private static double HistogramDistance(double[] h1, double[] h2)
        {
            double sum = 0;
            for (int i = 0; i < h1.Length; i++)
            {
                double diff = h1[i] - h2[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        private static double ContentDistance(float[] v1, float[] v2)
        {
            // Placeholder for content vector distance
            if (v1.Length == 0 || v2.Length == 0) return 1.0; // unknown distance
            double sum = 0;
            int len = Math.Min(v1.Length, v2.Length);
            for (int i = 0; i < len; i++)
            {
                double diff = v1[i] - v2[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }
    }
}
