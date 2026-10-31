# Database (SQLite) — DemoPos

> Database local dạng file (`pos.db`) dùng **Entity Framework Core 8 + SQLite**.
> File DB **có sẵn trong repo** (`DemoPos/pos.db`) với schema + seed 10 SKU + 6 terminal,
> được copy tự động ra thư mục build/publish → đóng gói chỉ cần ship kèm 1 file.
> Code: `Data/AppDbContext.cs`, `Data/Entities/*.cs`, `Services/SqlitePosBackend.cs`.

---

## 0. File `pos.db` trong repo

| Mục | Giá trị |
|---|---|
| Vị trí | `DemoPos/pos.db` (cùng cấp `DemoPos.csproj`) |
| Kích thước | ~104 KB |
| Nội dung | 8 bảng + 10 sản phẩm + 6 terminal, **không có giao dịch nào** |
| Journal mode | `DELETE` (không WAL) → **1 file duy nhất**, copy đi đâu cũng chạy |

### Cơ chế copy ra output
`DemoPos.csproj`:
```xml
<None Update="pos.db">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
</None>
```
`PreserveNewest` → chỉ copy khi file nguồn mới hơn, **không ghi đè dữ liệu phát sinh** ở output.

### Tạo lại file seed (khi cần reset về trạng thái ban đầu)
```bash
cd G:\CapstoneRetailSync\RetailSync\frontend\demo-pos-winforms\DemoPos
dotnet run -- initdb          # ghi schema + seed vào bin/.../pos.db
copy bin\Debug\net8.0-windows\pos.db pos.db
```
Hoặc đơn giản: xóa `pos.db`, chạy `dotnet run` (app tự tạo + seed lại).

---

## 1. Vì sao chọn SQLite

| Tiêu chí | SQLite | SQL Server LocalDB | JSON file |
|---|---|---|---|
| Không cần cài đặt thêm trên máy POS | ✅ | ❌ | ✅ |
| Đóng gói 1 file, copy là chạy | ✅ | ❌ | ✅ |
| Transaction / rollback | ✅ | ✅ | ❌ |
| Truy vấn tổng hợp (SUM/GROUP BY) | ✅ | ✅ | ❌ |
| Kích thước 1 file exe + db | ~vài MB | ~100 MB | — |
| Khóa file khi đang chạy (single-writer) | ⚠️ (dùng 1 connection + SemaphoreSlim) | ✅ | ❌ |

→ SQLite là lựa chọn cân bằng nhất cho terminal POS đóng gói 1 máy.

---

## 2. Cấu hình

`appsettings.json`:

```json
{
  "BaseUrl": "http://localhost:8000/api/v1",
  "BackendMode": "SQLite",
  "SqliteConnectionString": "Data Source=pos.db",
  "Terminals": [ ... ]
}
```

| Giá trị `BackendMode` | Backend được tạo |
|---|---|
| `Mock` (mặc định) | `MockPosBackend` — in-memory, mất khi tắt app |
| `SQLite` | `SqlitePosBackend` — lưu xuống `pos.db` |
| `Api` | `PosApiClient` — gọi Middleware FastAPI |

* `SqliteConnectionString` để trống → dùng `pos.db` cạnh file exe (`AppContext.BaseDirectory`).
* DB được tạo tự động + seed lần đầu chạy (`InitializeAsync` trong `Program.Main`).
* Seed chỉ chạy khi bảng rỗng → gọi nhiều lần vẫn an toàn.

---

## 3. Schema

### `Products` — danh mục sản phẩm & tồn kho
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `Sku` | TEXT(50) **PK** | vd `VIN-MLK-1L` |
| `Barcode` | TEXT(100) UNIQUE | vd `890000000001` |
| `Name` | TEXT(200) NOT NULL | |
| `UnitPrice` | decimal(18,2) | giá bán |
| `InitialStock` | INTEGER | tồn gốc, dùng cho Reset |
| `CurrentStock` | INTEGER | tồn hiện tại (giảm khi SALE, tăng khi RETURN/CANCELLATION) |
| `CreatedAt`, `UpdatedAt` | TEXT (ISO UTC) | |

### `Terminals` — terminal POS được phép gửi
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `StoreId` + `PosId` | TEXT(50) **PK ghép** | |
| `StoreName` | TEXT(200) | hiển thị trong combobox |
| `Token` | TEXT(500) | JWT giả lập, so khớp tuyệt đối |
| `IsActive` | INTEGER (bool) | tắt = trả 403 `TERMINAL_NOT_REGISTERED` |
| `CreatedAt`, `UpdatedAt` | TEXT | |

### `Transactions` — mọi sự kiện đã ghi nhận (idempotency + lịch sử)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `TransactionId` | TEXT(100) **PK** | |
| `StoreId`, `PosId` | TEXT(50) | |
| `EventType` | TEXT(20) | `SALE` / `RETURN` / `CANCELLATION` |
| `OriginalTransactionId` | TEXT(100) NULL | bắt buộc với RETURN/CANCELLATION |
| `Signature` | TEXT NOT NULL | JSON chuẩn hóa (không gồm timestamp) — so khớp idempotency |
| `CurrentStockJson` | TEXT | `{"SKU": số lượng}` tại thời điểm giao dịch |
| `ItemsJson` | TEXT | dòng hàng dạng JSON |
| `Timestamp` | TEXT | ISO 8601 UTC |
| `CreatedAt` | TEXT | thời điểm ghi DB |

Index: `(StoreId, PosId, EventType)`, `OriginalTransactionId`.

### `SoldItems` — dòng đã bán (nguồn cho trả hàng / hủy đơn)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `Id` | INTEGER **PK autoincrement** | |
| `TransactionId` | TEXT(100) | FK logic → `Transactions` |
| `Sku` | TEXT(50) | |
| `Quantity`, `UnitPrice` | INTEGER / decimal | |
| `CreatedAt` | TEXT | |

### `ReturnedItems` — số lượng đã trả theo đơn gốc
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `Id` | INTEGER **PK** | |
| `OriginalTransactionId` | TEXT(100) | đơn bán gốc |
| `Sku` | TEXT(50) | |
| `Quantity`, `UnitPrice` | | |
| `ReturnTransactionId` | TEXT(100) | giao dịch trả tương ứng |
| `CreatedAt` | TEXT | |

### `CancelledTransactions` — đơn đã hủy
| Cột | Kiểu |
|---|---|
| `TransactionId` | TEXT(100) **PK** (đơn gốc) |
| `CancellationTransactionId` | TEXT(100) |
| `CancelledAt` | TEXT |

### `EventLogs` — audit log
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `Id` | INTEGER **PK** | |
| `TransactionId`, `EventType`, `StoreId`, `PosId` | TEXT | |
| `Outcome` | TEXT(50) | `OK` / `DUPLICATE` / `REJECTED` / `ERROR` |
| `Code` | TEXT(50) NULL | `INSUFFICIENT_STOCK`, `CONFLICT`, … |
| `Message` | TEXT NULL | |
| `HttpStatus`, `LatencyMs` | INTEGER / REAL | |
| `RequestJson`, `ResponseJson` | TEXT | payload đầy đủ |
| `CreatedAt` | TEXT | |

### `AppSettings` — key/value mở rộng
| Cột | Kiểu |
|---|---|
| `Key` | TEXT(100) **PK** |
| `Value` | TEXT |
| `UpdatedAt` | TEXT |

---

## 4. Luồng ghi dữ liệu theo sự kiện

### `SALE`
1. Kiểm tra terminal tồn tại + `IsActive` → sai: `403 TERMINAL_NOT_REGISTERED`.
2. So khớp `Token` → sai: `401 UNAUTHORIZED`.
3. Validate items (SKU tồn tại, `quantity > 0`, `unit_price >= 0`).
4. Nếu `TransactionId` đã có:
   * `Signature` trùng → `200 DUPLICATE` (không đổi tồn kho).
   * `Signature` khác → `409 CONFLICT`.
5. **Transaction DB**: trừ `CurrentStock`, thêm `SoldItems`, thêm `Transactions` → commit.

### `RETURN`
1. Bắt buộc `OriginalTransactionId`; đơn gốc phải là `SALE` chưa bị hủy.
2. Mỗi dòng: `quantity` ≤ `đã bán − đã trả` → sai: `422 INVALID_RETURN_QUANTITY`.
3. **Transaction DB**: cộng `CurrentStock`, thêm `ReturnedItems`, thêm `Transactions` → commit.

### `CANCELLATION`
1. Bắt buộc `OriginalTransactionId`; đơn gốc phải là `SALE` chưa bị hủy → hủy lần 2: `409 ALREADY_CANCELLED`.
2. Không kiểm tra `items` (giống `MockPosBackend` / middleware).
3. **Transaction DB**: cộng lại `SoldItems − ReturnedItems` vào `CurrentStock`,
   thêm `CancelledTransactions` + `Transactions` → commit.

Mọi lần gửi (kể cả lỗi) đều ghi 1 dòng `EventLogs`.

---

## 5. Reset & backup

| Thao tác | Cách làm |
|---|---|
| Reset (Dev Tools → **Reset mock**) | `SqlitePosBackend.Reset()`: xóa 5 bảng giao dịch, đưa `CurrentStock = InitialStock` |
| Backup khi đang chạy | Copy `pos.db` (SQLite ghi bằng file journal, copy an toàn nhất khi app đã đóng) |
| Xóa sạch, tạo mới | Đóng app → xóa `pos.db` → mở lại: DB tự tạo + seed lại 10 SKU & 6 terminal |
| Đổi vị trí DB | Sửa `SqliteConnectionString`, vd `Data Source=D:\RetailSync\pos.db` |

> `pos.db` đã khai báo `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` trong
> `DemoPos.csproj` — nếu có sẵn file seed, nó sẽ được copy ra thư mục build.

---

## 6. Đóng gói (publish)

```bash
# Self-contained, 1 thư mục
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

# Kết quả
publish/
├── DemoPos.exe          # 1 file duy nhất (đã gồm e_sqlite3)
└── appsettings.json     # phải nằm cạnh exe
# pos.db sinh ra tự động ở lần chạy đầu
```

* Không cần cài SQL Server / SQLite runtime trên máy POS (`SQLitePCLRaw.bundle_e_sqlite3` đã native).
* Muốn phân phối kèm dữ liệu mẫu: chạy app 1 lần (hoặc `dotnet run -- selfcheck` không tạo file này),
  đóng app, rồi phát hành kèm `pos.db`.
* Vì `pos.db` nằm cạnh exe, khi cài vào `C:\Program Files` cần quyền ghi → nên cài vào
  `%LOCALAPPDATA%` hoặc ổ Dữ liệu.

---

## 7. Kiểm thử

```bash
dotnet run -- selfcheck
```

`SelfCheck/SqliteBackendSelfCheck.cs` tạo DB tạm, chạy 17 kiểm tra (SALE/DUPLICATE/CONFLICT/
INSUFFICIENT_STOCK/401/403/RETURN/trả vượt/hủy/hủy lần 2/persistence sau khi mở lại/Reset)
rồi xóa file tạm. Exit code `0` = pass toàn bộ.

---

## 8. Lưu ý kỹ thuật

* **Journal mode `DELETE`**: `AppDbContext.Compact()` chạy `PRAGMA wal_checkpoint(TRUNCATE)` +
  `PRAGMA journal_mode=DELETE` → không sinh `pos.db-wal` / `pos.db-shm`, file `.db` tự chứa hoàn toàn.
* **Single writer**: `SqlitePosBackend` dùng 1 `AppDbContext` + `SemaphoreSlim(1,1)` nên mọi lệnh ghi tuần tự,
  tránh lỗi `database is locked`.
* **EF Core `EnsureCreated`**: dùng cho demo. Khi schema cần thay đổi trên máy khách đã có dữ liệu,
  nên chuyển sang `dotnet ef migrations` (`Microsoft.EntityFrameworkCore.Design`).
* `TransactionEntity.CurrentStock` / `.Items` là property `[NotMapped]` — serialize/deserialize JSON
  qua `CurrentStockJson` / `ItemsJson`.
* Xóa dữ liệu bằng `ExecuteDelete()` (bulk delete, không load entity vào memory).


> Số còn có thể trả = `SUM(SoldItems.Quantity) − SUM(ReturnedItems.Quantity)` theo từng SKU.

### `CancelledTransactions` — đơn đã hủy
| Cột | Kiểu |
|---|---|
| `TransactionId` | TEXT(100) **PK** (đơn gốc) |
| `CancellationTransactionId` | TEXT(100) |
| `CancelledAt` | TEXT |

### `EventLogs` — audit log
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `Id` | INTEGER **PK** | |
| `TransactionId`, `EventType`, `StoreId`, `PosId` | TEXT | |
| `Outcome` | TEXT(50) | `OK` / `DUPLICATE` / `REJECTED` / `ERROR` |
| `Code` | TEXT(50) NULL | `INSUFFICIENT_STOCK`, `CONFLICT`, … |
| `Message` | TEXT NULL | |
| `HttpStatus`, `LatencyMs` | INTEGER / REAL | |
| `RequestJson`, `ResponseJson` | TEXT | payload đầy đủ |
| `CreatedAt` | TEXT | |

### `AppSettings` — key/value mở rộng
| Cột | Kiểu |
|---|---|
| `Key` | TEXT(100) **PK** |
| `Value` | TEXT |
| `UpdatedAt` | TEXT |

---
