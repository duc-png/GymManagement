# Module 2: PT Booking System

**Actors:** Member (tự đặt lịch của mình), Admin/Receptionist (đặt hộ/quản lý), PT (chỉ xem lịch của mình)
**Tables:** `PTBookings`, `Users` (role=PT), `PTMedia`, `MemberPackages`

---

## 1. PT Portfolio UI

- Hiển thị danh sách user có `Role = 'PT'`
- Mỗi PT hiển thị: `FullName`, `Specialty`, `PTStatus`, `Avatar`
- Media từ bảng `PTMedia`: ảnh chứng chỉ, video tập (`MediaType = 'Image' | 'Video'`)
- `PTStatus` enum: `Available | Busy | OnLeave`

## 2. Smart Booking Calendar

- Giao diện lịch biểu theo ngày/tuần
- Chọn ngày + chọn PT → hiển thị các slot đã đặt và còn trống
- Input để đặt: `MemberId`, `PTId`, `StartTime`, `EndTime`

## 3. Overlap Conflict Check (CRUCIAL LOGIC)

**Bắt buộc kiểm tra trước khi lưu booking mới:**

```sql
-- Kiểm tra có booking nào của cùng PTId bị overlap không
SELECT COUNT(*) FROM PTBookings
WHERE PTId = @NewPTId
  AND Status != 'Cancelled'
  AND NOT (@NewEndTime <= StartTime OR @NewStartTime >= EndTime)
```

- Nếu `COUNT(*) > 0` → **từ chối**, hiển thị lỗi: "PT đã có lịch trong khoảng thời gian này"
- Nếu `COUNT(*) = 0` → cho phép lưu

## 4. Session Deduction

- Khi Admin/Receptionist đổi `Status` của booking → `'Completed'`
- Hệ thống tự trừ: `MemberPackages.RemainingPTSessions -= 1`
- Nếu `RemainingPTSessions = 0` → không cho đặt thêm lịch PT

## Status Booking Enum

`Pending → Completed | Cancelled`
