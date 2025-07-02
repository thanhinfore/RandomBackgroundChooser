# Image Clustering & Selection

Ứng dụng này được viết bằng **Python 3** nhằm trích xuất và so khớp đặc trưng ảnh.
Chương trình hỗ trợ phân tích màu sắc, vector nội dung đơn giản và lưu cache đặc
trưng vào SQLite để tăng tốc xử lý.

## Sử dụng

```
python image_cluster_selector.py config.yml --images /path/to/images --count 5
```

Nếu không chỉ định `--images`, chương trình sẽ tìm ảnh trong cùng thư mục với tập tin Python.

Đầu ra là JSON liệt kê các ảnh được chọn cùng điểm số tương đồng.

### Cấu hình mẫu

```yaml
colorWeight: 0.6
contentWeight: 0.4
cachePath: features.db
histogramBins: 16
contentSize: 8
```

## Ghi chú

Mã nguồn mới chỉ là khung cơ bản, chưa tích hợp các mô hình deep learning. Người dùng có thể bổ sung thư viện YOLO/ResNet tuỳ ý.
