# Progress Tracker

> Cập nhật file này sau mỗi feature hoàn thành. Format: `✅ Done | 🔄 In Progress | ⬜ Pending`

---

## Trạng thái hiện tại

**Đang làm:** _(chưa bắt đầu)_
**Vừa xong:** Home UI + Login/Signup (BCrypt)
**Chặn bởi:** _(chưa có)_

---

## Foundation (Infrastructure)

| Feature | Status | Ghi chú |
|---------|--------|---------|
| HomeView — TopBar + Hero + PT Cards + Reviews + Footer | ✅ Done | `Views/HomeView.xaml`, `Services/HomeService.cs`; PT, feedback và số liệu tổng quan lấy từ database |
| UserSession singleton | ✅ Done | `Services/UserSession.cs` |
| LoginWindow — Tab Login + Tab Register | ✅ Done | `Views/LoginWindow.xaml` |
| BCrypt password hash/verify | ✅ Done | `BCrypt.Net-Next 4.0.3` |
| Nút Đăng nhập TopBar → mở dialog, cập nhật tên sau login | ✅ Done | `Views/HomeView.xaml.cs` |

---

## Module 1 — Membership Management

| Feature | Status | Ghi chú |
|---------|--------|---------|
| Profile CRUD (Add/Edit/Delete/Search) | ✅ Done | `Views/MemberView.xaml`, `Views/MemberView.xaml.cs` |
| Membership Assignment (gắn gói tập) | ✅ Done | `Views/PackageView.xaml`, `Views/PackageView.xaml.cs`; assign active package and calculate dates |
| Check-in / Check-out QR Simulation | ✅ Done | `Services/CheckInService.cs`, `Views/CheckInView.xaml` |

## Member Self-Service

| Feature | Status | Ghi chú |
|---------|--------|---------|
| Member navigation | ✅ Done | Workspace menu có Gói tập, Lịch PT, Giỏ hàng, Lịch sử mua hàng, Đánh giá, tài khoản và mật khẩu |
| Personal profile (email read-only) | ✅ Done | `Views/ProfileWindow.xaml` |
| Change password (BCrypt) | ✅ Done | `Views/ChangePasswordWindow.xaml` |
| Purchase history | ✅ Done | `Views/PurchaseHistoryWindow.xaml` |
| Cart | ✅ Done | Persistent SQL cart; Member mua sản phẩm, gói tập và thanh toán booking PT mua thêm |
| My packages | ✅ Done | Filter all/active/expired; price and PT sessions |
| My PT schedule | ✅ Done | Filter by `Members.UserId` |

## Module 2 — PT Booking System

| Feature | Status | Ghi chú |
|---------|--------|---------|
| PT Portfolio UI | ✅ Done | `Services/PtService.cs`, `Views/PtPortfolioView.xaml` |
| Smart Booking Calendar | ✅ Done | PT chỉ xem lịch dạy của chính mình; Member chỉ xem lịch đã thuê của chính mình; chọn ngày xem chi tiết |
| Overlap Conflict Check | ✅ Done | `BookingService.CreateAsync` |
| PT Session Reservation | ✅ Done | Đặt lịch trừ buổi ngay; hủy hoàn buổi; chỉ hoàn thành sau giờ kết thúc lịch và không trừ lần hai |
| PT Self-Service Profile | ✅ Done | Update profile, specialty, status, avatar and password; portfolio tự làm mới và hiển thị ảnh |

## Module 3 — Equipment & Maintenance

| Feature | Status | Ghi chú |
|---------|--------|---------|
| Inventory Tracking CRUD | ✅ Done | `Services/EquipmentService.cs`, `Views/EquipmentView.xaml`; Admin only |
| Issue Report → auto Broken | ✅ Done | Admin/Receptionist |
| Repair Lifecycle → auto Operational | ✅ Done | Admin only for lifecycle changes |

## Module 4 — POS & Invoicing

| Feature | Status | Ghi chú |
|---------|--------|---------|
| Product Inventory CRUD | ✅ Done | Admin CRUD; Receptionist read-only |
| Retail Cart + kiểm tra tồn kho | ✅ Done | `Services/PosService.cs`, `Views/PosView.xaml` |
| Invoice Generator + DiscountPercent | ✅ Done | Transactional checkout with package and extra PT booking assignment |
| PDF Export Preview | ✅ Done | `InvoicePreviewWindow`; Windows Print / Microsoft Print to PDF |

## Module 5 — Analytics & Access Control

| Feature | Status | Ghi chú |
|---------|--------|---------|
| RBAC Login (BCrypt) + Route by Role | ✅ Done | Admin vào Dashboard và chỉ hiện Dashboard/Thiết bị/Sản phẩm; các role khác dùng menu riêng |
| Dual-Type Feedback | ✅ Done | Phòng tập: đã mua gói; PT: booking đã thanh toán (nếu mua thêm) và Receptionist xác nhận hoàn thành |
| Feedback visibility | ✅ Done | PT xem đánh giá về mình; mọi người xem được toàn bộ đánh giá PT và phòng tập |
| Dashboard — Revenue Bar Chart | ✅ Done | `DashboardService`, `DashboardView`; grouped by payment method |
| Dashboard — Package Pie Chart | ✅ Done | Package sales summary from invoice details |
| Dashboard — Avg Training Hours | ✅ Done | Average check-in duration |

---

## Gợi ý thứ tự thực hiện

1. **M5: RBAC Login** — setup đăng nhập + phân quyền trước vì toàn bộ app cần
2. **M1: Membership** — core data, các module khác phụ thuộc vào Members
3. **M4: POS** — dòng tiền, gắn với PackageTemplates
4. **M2: PT Booking** — logic lịch, phụ thuộc Members + Users(PT)
5. **M3: Equipment** — độc lập, làm sau
6. **M5: Dashboard + Feedback** — làm cuối sau khi có đủ data
