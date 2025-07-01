Phát triển hệ thống Image Clustering & Selection với các tính năng:

CORE FEATURES:
- Phân tích màu sắc: RGB histogram, dominant colors extraction
- Phân tích nội dung: object detection (YOLO/ResNet), scene classification
- Thuật toán matching: weighted scoring (60% color, 40% content)
- Caching: lưu pre-computed features để tăng tốc

TECHNICAL SPECS:
- Framework: Python 3.9+, FastAPI cho API
- Libraries: OpenCV, scikit-image, torch/tensorflow
- Database: SQLite cho tracking history
- Config: YAML file cho tuning parameters

WORKFLOW:
1. Pre-process: Extract & cache features của toàn bộ ảnh
2. Runtime: Random seed → Calculate distances → Select top-4 → Validate uniqueness
3. Output: JSON với paths + similarity scores + preview grid

CONSTRAINTS:
- Max processing time: 2s cho 1000 ảnh
- Memory efficient: batch processing cho large datasets
- Dùng python hoặc c# (console app trên windows)