using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace ImageClusterSelector
{
    public class FeatureCache
    {
        private readonly string _dbPath;

        public FeatureCache(string dbPath)
        {
            _dbPath = dbPath;
            Initialize();
        }

        private void Initialize()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
                using var conn = new SQLiteConnection($"Data Source={_dbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE TABLE features (path TEXT PRIMARY KEY, histogram BLOB, content BLOB)";
                cmd.ExecuteNonQuery();
            }
        }

        public bool HasFeatures(string imagePath)
        {
            using var conn = new SQLiteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM features WHERE path = @path";
            cmd.Parameters.AddWithValue("@path", imagePath);
            using var reader = cmd.ExecuteReader();
            return reader.Read();
        }

        public void SaveFeatures(string imagePath, ImageFeatures features)
        {
            using var conn = new SQLiteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO features(path, histogram, content) VALUES(@p, @h, @c)";
            cmd.Parameters.AddWithValue("@p", imagePath);
            cmd.Parameters.AddWithValue("@h", SerializeArray(features.ColorHistogram));
            cmd.Parameters.AddWithValue("@c", SerializeArray(features.ContentVector));
            cmd.ExecuteNonQuery();
        }

        public List<ImageFeatures> LoadAllFeatures()
        {
            var list = new List<ImageFeatures>();
            using var conn = new SQLiteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT path, histogram, content FROM features";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var item = new ImageFeatures
                {
                    Path = reader.GetString(0),
                    ColorHistogram = DeserializeDoubleArray((byte[])reader[1]),
                    ContentVector = DeserializeFloatArray((byte[])reader[2])
                };
                list.Add(item);
            }
            return list;
        }

        private static byte[] SerializeArray(double[] array)
        {
            var bytes = new byte[array.Length * sizeof(double)];
            System.Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static byte[] SerializeArray(float[] array)
        {
            var bytes = new byte[array.Length * sizeof(float)];
            System.Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static double[] DeserializeDoubleArray(byte[] bytes)
        {
            var array = new double[bytes.Length / sizeof(double)];
            System.Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
            return array;
        }

        private static float[] DeserializeFloatArray(byte[] bytes)
        {
            var array = new float[bytes.Length / sizeof(float)];
            System.Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
            return array;
        }
    }
}
