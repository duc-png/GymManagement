# GymManagement — Tổng quan dự án

**Loại:** C# WPF Desktop App | **Môn:** PRN232 | **DB:** SQL Server `GymManagementDB`

## Mục tiêu
Phần mềm quản lý phòng gym toàn diện cho lễ tân, huấn luyện viên và admin.

## 5 Module chính

| # | Module | Mô tả ngắn | File spec |
|---|--------|------------|-----------|
| 1 | Membership Management | Quản lý hội viên, gắn gói tập, check-in/out QR | [01_Membership.md](01_Membership.md) |
| 2 | PT Booking System | Đặt lịch HLV, chống trùng lịch, trừ buổi | [02_PTBooking.md](02_PTBooking.md) |
| 3 | Equipment & Maintenance | Quản lý thiết bị, báo hỏng, vòng đời sửa chữa | [03_Equipment.md](03_Equipment.md) |
| 4 | POS & Invoicing | Bán hàng, hóa đơn gộp, xuất PDF | [04_POS.md](04_POS.md) |
| 5 | Feedbacks & Analytics | Đánh giá, dashboard doanh thu, phân quyền RBAC | [05_Analytics.md](05_Analytics.md) |

## Roles

| Role | Quyền hạn |
|------|-----------|
| Admin | Toàn quyền |
| Receptionist | Member, POS, Check-in, Booking |
| PT | Chỉ xem lịch dạy của chính mình |

## Tài liệu kỹ thuật
- Stack & DB Schema: [06_Technical.md](06_Technical.md)
- Tiến độ thực hiện: [07_Progress.md](07_Progress.md)
