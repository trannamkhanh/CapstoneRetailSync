# DemoPos — WinForms POS Terminal

Ứng dụng WinForms (.NET 8) mô phỏng terminal POS bán lẻ với 3 tab **Bán hàng / Trả hàng / Hủy đơn**,
Dev Tools mô phỏng lỗi và lịch sử phiên. UI chỉ nói chuyện với `IPosBackend` nên có thể chạy ở 3 chế độ:

| Chế độ | Backend | Lưu dữ liệu |
|---|---|---|
| `Mock` (mặc định) | `MockPosBackend` | RAM, mất khi tắt app |
| `SQLite` | `SqlitePosBackend` | file `pos.db` (EF Core 8 + SQLite) |
| `Api` | `PosApiClient` | FastAPI Middleware qua HTTP |

> Chi tiết schema database: xem [DATABASE.md](DATABASE.md).

---

## 1. Cấu trúc thư mục

```
DemoPos/
├── DemoPos.csproj                  # .NET 8 WinForms + EF Core Sqlite
├── appsettings.json                # BaseUrl, BackendMode, SqliteConnectionString, 6 terminals
├── pos.db                          # ⭐ File database seed sẵn (schema + 10 SKU + 6 terminal)
├── Program.cs                      # Entry point, chọn backend, -- selfcheck / -- initdb
├── MainForm.cs / MainForm.Designer.cs
├── Models/
│   ├── AppConfig.cs                # AppConfig + BackendMode + TerminalConfig
│   ├── CartItem.cs                 # Dòng giỏ hàng
│   ├── PosEvent.cs                 # Sự kiện gửi backend (SALE/RETURN/CANCELLATION)
│   ├── PosResult.cs                # OK / DUPLICATE / REJECTED / ERROR
│   ├── Product.cs                  # Sản phẩm + tồn kho
│   └── TxnRecord.cs                # Lịch sử giao dịch trong phiên
├── Services/
│   ├── IPosBackend.cs              # Hợp đồng chung cho MỌI backend
│   ├── MockPosBackend.cs           # Mock in-memory (đối chiếu luật với middleware)
│   ├── SqlitePosBackend.cs         # Persist xuống SQLite
│   └── PosApiClient.cs             # HTTP client + Bearer token
├── Data/
│   ├── AppDbContext.cs             # DbContext + EnsureCreatedAndSeedAsync
│   ├── ProductCatalog.cs           # 10 SKU seed (dùng cho Mock và SQLite)
│   └── Entities/
│       ├── ProductEntity.cs        # ProductEntity + TerminalEntity
│       └── TransactionEntity.cs    # Transaction/Sold/Returned/Cancelled/EventLog/AppSetting
├── Controls/
│   ├── CartGrid.cs                 # DataGridView giỏ hàng (sửa số lượng)
│   ├── DevToolsPanel.cs            # Panel Dev Tools (F12)
│   ├── HistoryGrid.cs              # Lịch sử giao dịch
│   └── ResultPanel.cs              # Kết quả + nút "Gửi lại"
├── SelfCheck/
│   └── SqliteBackendSelfCheck.cs   # 17 kiểm tra backend SQLite (không cần UI)
├── README.md
└── DATABASE.md
```

---

## 2. Cấu hình (`appsettings.json`)

```json
{
  "BaseUrl": "http://localhost:8000/api/v1",
  "BackendMode": "Mock",
  "SqliteConnectionString": "Data Source=pos.db",
  "Terminals": [
    { "StoreId": "S-01", "StoreName": "Cửa hàng Nguyễn Văn A", "PosId": "POS-01", "Token": "demo-pos-jwt-store-01-pos-01" },
    { "StoreId": "S-01", "StoreName": "Cửa hàng Nguyễn Văn A", "PosId": "POS-02", "Token": "demo-pos-jwt-store-01-pos-02" },
    { "StoreId": "S-02", "StoreName": "Cửa hàng Lê Lợi",        "PosId": "POS-01", "Token": "demo-pos-jwt-store-02-pos-01" },
    { "StoreId": "S-02", "StoreName": "Cửa hàng Lê Lợi",        "PosId": "POS-02", "Token": "demo-pos-jwt-store-02-pos-02" },
    { "StoreId": "S-03", "StoreName": "Cửa hàng Nguyễn Huệ",    "PosId": "POS-01", "Token": "demo-pos-jwt-store-03-pos-01" },
    { "StoreId": "S-03", "StoreName": "Cửa hàng Nguyễn Huệ",    "PosId": "POS-02", "Token": "demo-pos-jwt-store-03-pos-02" }
  ]
}
```

* `BackendMode`: `Mock` | `SQLite` | `Api`. **Mặc định `SQLite`** — dùng ngay file `pos.db` có sẵn.
* `SqliteConnectionString` để trống → dùng `pos.db` cạnh file exe.
* Checkbox **Mock** trên thanh công cụ chỉ chuyển giữa `Mock` ⇄ `Api` (bị khóa khi đang ở chế độ `SQLite`).

> **File `pos.db`** nằm trong thư mục project và được `DemoPos.csproj` copy tự động
> (`PreserveNewest`) ra `bin/...` và thư mục publish. Xem chi tiết tại [DATABASE.md](DATABASE.md).

---

## 3. Tính năng

### 3 tab nghiệp vụ
| Tab | Luồng |
|---|---|
| **Bán hàng** | Tìm SKU/barcode → thêm giỏ → sửa SL → gửi `SALE` |
| **Trả hàng** | Chọn đơn bán gốc → chọn dòng + SL trả → gửi `RETURN` |
| **Hủy đơn** | Chọn đơn bán gốc → xác nhận → gửi `CANCELLATION` (hoàn tồn kho) |

### Luật nghiệp vụ dùng chung cho cả 3 backend
* Idempotency: cùng `transaction_id` + nội dung → `DUPLICATE`; khác nội dung → `CONFLICT`.
* `INSUFFICIENT_STOCK` khi bán vượt tồn.
* `RETURN` không vượt quá `đã bán − đã trả`.
* `CANCELLATION` hoàn phần chưa trả; hủy 2 lần → `ALREADY_CANCELLED`.
* `UNAUTHORIZED` (sai token) và `TERMINAL_NOT_REGISTERED` (Store/POS lạ).

### Dev Tools (F12)
| Nút | Kịch bản | Kết quả mong đợi |
|---|---|---|
| Reset | Xóa giao dịch, khôi phục tồn kho gốc | (mock/SQLite) |
| Gửi lại giống hệt | 2 lần cùng `transaction_id` | lần 2 → `DUPLICATE` |
| Gửi khác nội dung | cùng id, khác items | lần 2 → `CONFLICT` |
| Vượt tồn kho | SL = 999 | `INSUFFICIENT_STOCK` |
| Token sai | token giả | `UNAUTHORIZED` / 401 |
| Terminal lạ | `S-99` / `POS-99` | `TERMINAL_NOT_REGISTERED` / 403 |
| 503 rồi gửi lại | mock trả 503 lần đầu | nút **Gửi lại (F10)** hiển thị |

### Phím tắt
| Phím | Hành động |
|---|---|
| `F2` | Focus ô tìm kiếm sản phẩm |
| `F9` | Gửi giao dịch của tab hiện tại |
| `F10` | Gửi lại giao dịch lỗi tạm thời |
| `F12` | Bật/tắt Dev Tools |

---

## 4. Dữ liệu seed (10 SKU — dùng chung với bản web `frontend/demo-pos`)

| SKU | Barcode | Tên | Giá | Tồn |
|---|---|---|---|---|
| `VIN-MLK-1L` | 890000000001 | Sữa tươi Vinamilk Hộp 1L | 18.500 | 24 |
| `HH-TOM-01` | 890000000002 | Mì gói Hảo Hảo Tôm | 5.200 | **1** |
| `AQU-5L` | 890000000003 | Nước suối Aquafina 5L | 27.000 | 18 |
| `SND-ICH-01` | 890000000004 | Bánh mì sandwich đông lạnh | 32.000 | 12 |
| `VIT-SY-03` | 890000000005 | Sữa chua uống Vitasoy | 7.500 | 30 |
| `EGG-TA-10` | 890000000006 | Trứng gà ta (lọ 10 quả) | 38.000 | 15 |
| `COC-250-01` | 890000000007 | Nước Coca-Cola 250ml | 6.500 | 48 |
| `TSN-BG-01` | 890000000008 | Trà xanh Trí Việt 500ml | 10.500 | 26 |
| `CFD-MX-01` | 890000000009 | Cà phê lon Phin Đậm Đá | 14.500 | 20 |
| `PST-LAP-01` | 890000000010 | Bánh tráng nước Lạnh | 4.500 | 60 |

**Lưu ý:** `HH-TOM-01` tồn = 1 để dễ demo `INSUFFICIENT_STOCK`.

---

## 5. Kiến trúc

```
               ┌──────────────────────────────────────────┐
               │              MainForm (UI)               │
               │  Bán hàng │ Trả hàng │ Hủy đơn │ DevTools│
               └───────────────────┬──────────────────────┘
                                   │  chỉ biết interface
                                   ▼
                        ┌────────────────────┐
                        │    IPosBackend     │
                        └─────────┬──────────┘
              ┌───────────────────┼───────────────────┐
              ▼                   ▼                   ▼
   ┌──────────────────┐ ┌──────────────────┐ ┌────────────────┐
   │ MockPosBackend   │ │ SqlitePosBackend │ │ PosApiClient   │
   │ (RAM)            │ │ AppDbContext     │ │ HttpClient     │
   └──────────────────┘ │   └─ pos.db      │ └────────────────┘
                        └──────────────────┘
```

* Backend được chọn duy nhất tại `Program.CreateBackend(config)`.
* `SqlitePosBackend` dùng 1 `AppDbContext` + `SemaphoreSlim(1,1)` để tuần tự hóa ghi (tránh `database is locked`).
* `MainForm` expose các property `internal` (`Tabs`, `SaleCartGrid`, …) cho test qua `InternalsVisibleTo DemoPos.Tests`.

---

## 6. Chạy & kiểm thử

```bash
cd G:\CapstoneRetailSync\RetailSync\frontend\demo-pos-winforms\DemoPos

dotnet run                   # mở UI, dùng file pos.db có sẵn (BackendMode = SQLite)
dotnet run -- selfcheck      # 17 kiểm tra backend SQLite, exit code 0 = pass
dotnet run -- initdb         # tạo lại schema + seed vào pos.db trong bin/ (không mở UI)
```

Đổi chế độ: sửa `"BackendMode"` trong `appsettings.json` thành `Mock` hoặc `Api`.

Đổi sang Middleware thật: `"BackendMode": "Api"` và đảm bảo Middleware chạy ở `BaseUrl`.

---

## 7. Đóng gói

```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Phát hành gồm `DemoPos.exe` + `appsettings.json` + `pos.db` (đã có schema + seed).
Không cần cài .NET runtime hay SQLite trên máy POS.

---

## 8. Dependencies

| Thành phần | Nguồn |
|---|---|
| .NET 8 + WinForms | SDK |
| `Microsoft.EntityFrameworkCore.Sqlite` 8.0.0 | NuGet |
| `System.Text.Json`, `System.Net.Http` | BCL |

---

## 9. Ghi chú

* Chỉ chạy trên **Windows** (WinForms).
* `Reset` ở chế độ `Api` chỉ khôi phục cache tồn kho trên UI, không xóa dữ liệu Middleware.
* Chế độ `Mock` không lưu đĩa — lịch sử chỉ tồn tại trong phiên.
* Project nội bộ — Capstone RetailSync.
| `Esc` | Xóa kết quả / focus tìm kiếm |