# Image Clustering & Selection

Repository chứa ứng dụng **C# console** thực hiện trích xuất và so khớp đặc trưng ảnh.
Phiên bản này đã cải tiến thuật toán và khả năng xử lý:

- Phân tích màu sắc bằng histogram RGB với số lượng bin tuỳ chỉnh.
- Phân tích nội dung bằng cách thu nhỏ ảnh thành lưới xám để tạo vector đặc trưng.
- Thuật toán tính điểm tương đồng có trọng số (60% màu sắc, 40% nội dung).
- Lưu trữ đặc trưng vào SQLite để tăng tốc xử lý.
- Đọc cấu hình từ file YAML.
- Tiền xử lý ảnh song song để tận dụng CPU đa nhân.

## Sử dụng

```
dotnet run --project ImageClusterSelector/ImageClusterSelector.csproj config.yml /path/to/images
```

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
