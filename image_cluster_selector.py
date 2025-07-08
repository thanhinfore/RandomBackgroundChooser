import argparse
import json
import sqlite3
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

try:
    import yaml
except ImportError as e:
    raise SystemExit("PyYAML is required. Install it with 'pip install PyYAML'") from e

try:
    from PIL import Image
except ImportError as e:
    raise SystemExit("Pillow is required. Install it with 'pip install Pillow'") from e

try:
    import numpy as np
except ImportError as e:
    raise SystemExit("NumPy is required. Install it with 'pip install numpy'") from e


def load_config(path):
    try:
        with open(path, 'r', encoding='utf-8') as f:
            return yaml.safe_load(f)
    except FileNotFoundError:
        raise SystemExit(f"Config file '{path}' not found")
    except yaml.YAMLError as e:
        raise SystemExit(f"Failed to parse YAML config: {e}")


def init_db(db_path):
    try:
        conn = sqlite3.connect(db_path)
        conn.execute(
            "CREATE TABLE IF NOT EXISTS features (path TEXT PRIMARY KEY, hist BLOB, content BLOB)"
        )
        conn.commit()
        return conn
    except sqlite3.Error as e:
        raise SystemExit(f"Database error: {e}")


def has_features(conn, path):
    cur = conn.execute("SELECT 1 FROM features WHERE path=?", (path,))
    return cur.fetchone() is not None


def save_features(conn, path, hist, content):
    try:
        conn.execute(
            "INSERT OR REPLACE INTO features(path, hist, content) VALUES(?,?,?)",
            (
                path,
                hist.astype(np.float64).tobytes(),
                content.astype(np.float32).tobytes(),
            ),
        )
        conn.commit()
    except sqlite3.Error as e:
        print(f"Failed to save features for {path}: {e}")


def load_all_features(conn):
    result = []
    try:
        for path, hist_blob, cont_blob in conn.execute(
            "SELECT path, hist, content FROM features"
        ):
            hist = np.frombuffer(hist_blob, dtype=np.float64)
            content = np.frombuffer(cont_blob, dtype=np.float32)
            result.append({"path": path, "hist": hist, "content": content})
    except sqlite3.Error as e:
        print(f"Failed to load features from DB: {e}")
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
    try:
        with Image.open(path) as img:
            img = img.convert("RGB")
            hist = compute_color_histogram(img, config.get("histogramBins", 16))
            content = compute_content_vector(img, config.get("contentSize", 8))
        return hist, content
    except Exception as e:
        raise RuntimeError(f"Failed to process image {path}: {e}")


def preprocess_images(files, conn, config):
    def process(image_path):
        if not has_features(conn, image_path):
            try:
                hist, content = extract_features(image_path, config)
                save_features(conn, image_path, hist, content)
            except Exception as e:
                print(e)

    with ThreadPoolExecutor() as executor:
        executor.map(process, files)


def select_images(features, config, count=4):
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
    parser.add_argument("images", help="Folder containing images")
    args = parser.parse_args()

    config = load_config(args.config)
    if not Path(args.images).exists():
        raise SystemExit(f"Image folder '{args.images}' not found")

    conn = init_db(config.get("cachePath", "features.db"))

    exts = {".jpg", ".jpeg", ".png", ".bmp", ".gif"}
    files = [str(p) for p in Path(args.images).rglob("*") if p.suffix.lower() in exts]

    if not files:
        raise SystemExit("No images found")

    preprocess_images(files, conn, config)
    features = load_all_features(conn)
    result = select_images(features, config)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
