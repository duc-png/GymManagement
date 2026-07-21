# Module 5: Feedbacks, Analytics & Access Control

**Actors:** Admin (full), Receptionist (feedback + partial), PT (view own schedule)
**Tables:** `Feedbacks`, `Users`, `CheckInHistory`, `Invoices`

---

## 1. Dual-Type Feedback

### PT Feedback
- `FeedbackType = 'PT'`
- Member đánh giá PT: `RatingStars` (1–5) + `Comment`
- Link `TargetPTId` → `Users.Id` (chỉ PT role)

### Facility Feedback
- `FeedbackType = 'Facility'`
- Member báo lỗi thiết bị: `Comment` mô tả vấn đề
- Link `EquipmentId` → `Equipments.Id`
- `RatingStars` = NULL (không áp dụng)

## 2. Role-Based Access Control (RBAC)

### Đăng nhập
- Mã hóa password bằng BCrypt
- Sau đăng nhập, lưu thông tin user vào session/singleton
- Routing view dựa theo `Role`

### Phân quyền theo Role

| Feature | Admin | Receptionist | PT |
|---------|-------|--------------|-----|
| Member CRUD | ✓ | ✓ | ✗ |
| Check-in/out | ✓ | ✓ | ✗ |
| PT Bookings (all) | ✓ | ✓ | ✗ |
| PT Bookings (own) | ✓ | ✓ | ✓ |
| Equipment CRUD | ✓ | Report only | ✗ |
| POS / Invoices | ✓ | ✓ | ✗ |
| Dashboard Analytics | ✓ | ✗ | ✗ |
| User Management | ✓ | ✗ | ✗ |
| Feedbacks | ✓ | ✓ | ✗ |

## 3. LiveCharts Dashboard (Admin only)

### Biểu đồ doanh thu (Bar Chart)
- Lọc theo: khoảng thời gian (ngày/tháng/năm)
- Phân nhóm theo: `PaymentMethod` (Cash, Card, Transfer, MoMo)
- Nguồn: `Invoices.FinalAmount` + `Invoices.CreatedDate`

### Biểu đồ gói tập (Pie Chart)
- Tỷ lệ % từng `PackageTemplate` được mua nhiều nhất
- Nguồn: `InvoiceDetails` WHERE `ItemType = 'Package'`

### Thống kê giờ tập trung bình
- `AVG(DATEDIFF(MINUTE, CheckInTime, CheckOutTime))` per member
- Chỉ tính record có `CheckOutTime IS NOT NULL`
