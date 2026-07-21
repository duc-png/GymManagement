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
| HomeView — TopBar + Hero + PT Cards + Reviews + Footer | ✅ Done | `Views/HomeView.xaml` |
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
| Account menu | ✅ Done | `Views/HomeView.xaml` |
| Personal profile (email read-only) | ✅ Done | `Views/ProfileWindow.xaml` |
| Change password (BCrypt) | ✅ Done | `Views/ChangePasswordWindow.xaml` |
| Purchase history | ✅ Done | `Views/PurchaseHistoryWindow.xaml` |
| Cart | ⬜ Pending | Requires persistent cart design |
| My packages | ✅ Done | Filter all/active/expired; price and PT sessions |
| My PT schedule | ✅ Done | Filter by `Members.UserId` |

## Module 2 — PT Booking System

| Feature | Status | Ghi chú |
|---------|--------|---------|
| PT Portfolio UI | ✅ Done | `Services/PtService.cs`, `Views/PtPortfolioView.xaml` |
| Smart Booking Calendar | ✅ Done | `Views/BookingView.xaml`, `Services/BookingService.cs`; filters PT by Specialty |
| Overlap Conflict Check | ✅ Done | `BookingService.CreateAsync` |
| Session Deduction khi Completed | ✅ Done | `BookingService.UpdateStatusAsync` |

## Module 3 — Equipment & Maintenance

| Feature | Status | Ghi chú |
|---------|--------|---------|
| Inventory Tracking CRUD | ⬜ Pending | |
| Issue Report → auto Broken | ⬜ Pending | |
| Repair Lifecycle → auto Operational | ⬜ Pending | |

## Module 4 — POS & Invoicing

| Feature | Status | Ghi chú |
|---------|--------|---------|
| Retail Cart + kiểm tra tồn kho | ⬜ Pending | |
| Invoice Generator + DiscountPercent | ⬜ Pending | |
| PDF Export Preview | ⬜ Pending | |

## Module 5 — Analytics & Access Control

| Feature | Status | Ghi chú |
|---------|--------|---------|
| RBAC Login (BCrypt) + Route by Role | ✅ Done | `Services/UserSession.cs`, `Services/UserService.cs`, `MainWindow.xaml.cs`; role guards and role-based view routing |
| Dual-Type Feedback | ⬜ Pending | |
| Dashboard — Revenue Bar Chart | ⬜ Pending | |
| Dashboard — Package Pie Chart | ⬜ Pending | |
| Dashboard — Avg Training Hours | ⬜ Pending | |

---

## Gợi ý thứ tự thực hiện

1. **M5: RBAC Login** — setup đăng nhập + phân quyền trước vì toàn bộ app cần
2. **M1: Membership** — core data, các module khác phụ thuộc vào Members
3. **M4: POS** — dòng tiền, gắn với PackageTemplates
4. **M2: PT Booking** — logic lịch, phụ thuộc Members + Users(PT)
5. **M3: Equipment** — độc lập, làm sau
6. **M5: Dashboard + Feedback** — làm cuối sau khi có đủ data
