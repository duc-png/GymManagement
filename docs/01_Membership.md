# Module 1: Membership Management

**Actors:** Admin, Receptionist
**Tables:** `Members`, `MemberPackages`, `PackageTemplates`, `CheckInHistory`

---

## 1. Profile CRUD

- Tìm kiếm theo: `FullName`, `PhoneNumber`, `MemberCode`
- CRUD đầy đủ: thêm, sửa, xóa hội viên
- `MemberCode` là mã định danh duy nhất (VARCHAR 20, UNIQUE)

## 2. Membership Assignment

Package templates should state the price, duration, PT entitlement and the length of each PT session. A sample catalog is available in `Data/20260721_SeedPackageTemplates.sql`.

- Chọn member + chọn `PackageTemplate` → gắn gói tập
- Tự động tính:
  - `StartDate` = ngày hôm nay
  - `EndDate` = StartDate + `PackageTemplate.DurationMonths` (tháng)
  - `RemainingPTSessions` = lấy từ PackageTemplate nếu `HasPT = 1`
- `Status` mặc định = `'Active'`

## 3. Real-time Check-in / Check-out (QR Simulation)

**Luồng xử lý:**
1. Nhân viên nhập `MemberCode` (giả lập quét QR)
2. Kiểm tra `MemberPackages` của member có `Status = 'Active'` không
3. **Nếu Active:**
   - Phát âm thanh beep
   - UI chuyển sang màu **xanh lá**
   - Kiểm tra member đã có record `CheckInHistory` hôm nay chưa có `CheckOutTime`
     - Chưa có → tạo record mới với `CheckInTime = GETDATE()`
     - Đã có (đang trong gym) → cập nhật `CheckOutTime = GETDATE()`
4. **Nếu không Active / hết hạn:**
   - UI chuyển sang màu **đỏ**
   - Hiển thị thông báo từ chối

## Business Rules

- Một member có thể có nhiều `MemberPackages` nhưng chỉ 1 cái `Active` tại một thời điểm
- `Status` enum: `Active | Expiring | Expired`
- `Expiring`: còn ≤ 7 ngày trước `EndDate`
