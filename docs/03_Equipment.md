# Module 3: Equipment & Maintenance Management

**Actors:** Admin (full), Receptionist (báo hỏng)
**Tables:** `Equipments`, `MaintenanceHistory`

---

## 1. Inventory Tracking

- CRUD danh sách thiết bị: `EquipmentName`, `EquipmentType`, `Location`, `PurchaseDate`
- `EquipmentCode`: mã định danh duy nhất (VARCHAR 30, UNIQUE)
- Lọc theo `Status`, `EquipmentType`, `Location`

## 2. Issue Report (Báo hỏng)

- Nhân viên chọn thiết bị → nhấn "Báo hỏng"
- Hệ thống tự động:
  1. Tạo record `MaintenanceHistory` với `LogType = 'IssueReport'`
  2. Cập nhật `Equipments.Status = 'Broken'`
- Input cần: `Description` (mô tả vấn đề)

## 3. Repair Lifecycle (Vòng đời sửa chữa)

**Quy trình:**
1. Admin đặt máy sang `UnderMaintenance` (bắt đầu sửa)
   - Tạo record `LogType = 'RoutineMaintenance'` nếu cần
2. Khi sửa xong, Admin cập nhật:
   - Tạo record `LogType = 'Repair'` với `Cost`, `PerformedBy`, `Notes`
   - Hệ thống tự chuyển `Equipments.Status = 'Operational'`

## Status Enum

| Status | Ý nghĩa |
|--------|---------|
| `Operational` | Hoạt động bình thường |
| `Broken` | Đã hỏng, chờ xử lý |
| `UnderMaintenance` | Đang trong quá trình sửa |
| `Disposed` | Đã thanh lý |

## MaintenanceHistory LogType Enum

`IssueReport | RoutineMaintenance | Repair`
