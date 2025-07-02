import argparse
import json
import sqlite3
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import yaml
from PIL import Image
import numpy as np


def load_config(path):
    with open(path, 'r', encoding='utf-8') as f:
        return yaml.safe_load(f)


def init_db(db_path):
    conn = sqlite3.connect(db_path)
    conn.execute(
        "CREATE TABLE IF NOT EXISTS features (path TEXT PRIMARY KEY, hist BLOB, content BLOB)"
    )
    conn.commit()
    return conn


def has_features(conn, path):
    cur = conn.execute("SELECT 1 FROM features WHERE path=?", (path,))
    return cur.fetchone() is not None


def save_features(conn, path, hist, content):
    conn.execute(
        "INSERT OR REPLACE INTO features(path, hist, content) VALUES(?,?,?)",
        (
            path,
            hist.astype(np.float64).tobytes(),
            content.astype(np.float32).tobytes(),
        ),
    )
    conn.commit()


def load_all_features(conn):
    result = []
    for path, hist_blob, cont_blob in conn.execute(
        "SELECT path, hist, content FROM features"
    ):
        hist = np.frombuffer(hist_blob, dtype=np.float64)
        content = np.frombuffer(cont_blob, dtype=np.float32)
        result.append({"path": path, "hist": hist, "content": content})
    return result


def compute_color_histogram(img, bins):
    arr = np.array(img)
    hist_r, _ = np.histogram(arr[:, :, 0], bins=bins, range=(0, 256), density=True)
    hist_g, _ = np.histogram(arr[:, :, 1], bins=bins, range=(0, 256), density=True)
    hist_b, _ = np.histogram(arr[:, :, 2], bins=bins, range=(0, 256), density=True)
    return np.concatenate([hist_r, hist_g, hist_b])


def compute_content_vector(img, size):
    gray = img.convert("L").resize((size, size))
    arr = np.array(gray, dtype=np.float32) / 255.0
    return arr.flatten()


def extract_features(path, config):
    with Image.open(path) as img:
        img = img.convert("RGB")
        hist = compute_color_histogram(img, config.get("histogramBins", 16))
        content = compute_content_vector(img, config.get("contentSize", 8))
    return hist, content


def preprocess_images(files, db_path, config):
    """Extract and cache features for image files."""

    def process(image_path):
        conn = sqlite3.connect(db_path)
        try:
            if not has_features(conn, image_path):
                hist, content = extract_features(image_path, config)
                save_features(conn, image_path, hist, content)
        finally:
            conn.close()

    with ThreadPoolExecutor() as executor:
        list(executor.map(process, files))


def select_images(features, config, count=5):
    import random

    if not features:
        return []
    seed = random.choice(features)
    color_w = config.get("colorWeight", 0.6)
    content_w = config.get("contentWeight", 0.4)
    scores = []
    for item in features:
        color_dist = np.linalg.norm(seed["hist"] - item["hist"])
        content_dist = np.linalg.norm(seed["content"] - item["content"])
        score = color_w * color_dist + content_w * content_dist
        scores.append({"path": item["path"], "score": float(score)})
    scores = sorted(scores, key=lambda x: x["score"])
    scores = [s for s in scores if s["path"] != seed["path"]]
    return scores[:count]


def main():
    parser = argparse.ArgumentParser(description="Image clustering and selection")
    parser.add_argument("config", help="YAML configuration file")
    parser.add_argument(
        "--images",
        default=str(Path(__file__).parent),
        help="Folder containing images (default: script directory)",
    )
    parser.add_argument(
        "--count",
        type=int,
        default=5,
        help="Number of images to return",
    )
    args = parser.parse_args()

    config = load_config(args.config)
    db_path = config.get("cachePath", "features.db")
    conn = init_db(db_path)

    exts = {".jpg", ".jpeg", ".png", ".bmp", ".gif"}
    files = [str(p) for p in Path(args.images).rglob("*") if p.suffix.lower() in exts]

    preprocess_images(files, db_path, config)
    features = load_all_features(conn)
    result = select_images(features, config, args.count)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
