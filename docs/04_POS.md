# Module 4: Point of Sale (POS) & Invoicing

**Actors:** Admin, Receptionist
**Tables:** `Invoices`, `InvoiceDetails`, `Products`, `PackageTemplates`

---

## 1. Retail Cart (Giỏ hàng)

- Chọn sản phẩm từ `Products` (Whey, nước uống, phụ kiện)
- Kiểm tra `StockQuantity > 0` trước khi thêm vào giỏ
- Khi thanh toán → trừ `StockQuantity` tương ứng

## 2. Invoice Generator (Tạo hóa đơn)

- Hóa đơn gộp cả gói tập lẫn sản phẩm
- Áp mã giảm giá: `DiscountPercent` (0–100)
- Công thức: `FinalAmount = TotalAmount * (1 - DiscountPercent / 100)`
- `InvoiceCode` tự động sinh (ví dụ: `INV-20260721-001`)

## 3. InvoiceDetails — Polymorphic

Mỗi dòng hàng (`InvoiceDetails`) có `ItemType`:
- `'Package'` → `ItemId` trỏ đến `PackageTemplates.Id`
- `'Product'` → `ItemId` trỏ đến `Products.Id`

Lưu thêm `ItemName` (snapshot tên tại thời điểm mua, không bị ảnh hưởng nếu tên sản phẩm thay đổi sau).

## 4. Payment Method Enum

`Cash | Card | Transfer | MoMo`

## 5. PDF Export Preview

- Giao diện xem trước hóa đơn (tên gym, ngày, member, danh sách sản phẩm, tổng tiền)
- Nút "Xuất PDF" → lưu file PDF định dạng hóa đơn bán hàng chuyên nghiệp

## Business Rules

- Khi Invoice liên kết gói tập → tạo `MemberPackages` record tương ứng cho member
- Một Invoice có thể không có MemberId (khách vãng lai mua sản phẩm lẻ)
