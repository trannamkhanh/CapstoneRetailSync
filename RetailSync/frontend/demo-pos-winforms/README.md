# Demo POS Terminal (WinForms)

Trình mô phỏng POS Terminal trên **.NET 8 WinForms**, dùng để demo luồng **SALE / RETURN / CANCELLATION**
theo hợp đồng API `POST /pos/events` của Middleware (RetailSync — PB-45).

Ứng dụng có sẵn **mock backend** nên chạy độc lập, đồng thời có thể trỏ sang Middleware FastAPI thật
qua **BaseUrl / Mock** trên thanh công cụ. Bảng **Dev Tools** (kịch bản lỗi) mặc định **ẩn**,
bật/tắt bằng nút **Dev Tools** hoặc phím **F12**.

---

## 1. Yêu cầu

| Thành phần | Phiên bản |
|---|---|
| .NET SDK | 8.0 (hoặc mới hơn) |
| OS | Windows (bắt buộc cho WinForms) |
| Middleware (tuỳ chọn) | FastAPI tại `http://localhost:8000/api/v1` |

## 2. Chạy ứng dụng

```bash
cd RetailSync/frontend/demo-pos-winforms
dotnet run --project DemoPos\DemoPos.csproj
```

Mặc định `appsettings.json` bật `UseMock: true` → **không cần backend thật**.

### Cấu hình `DemoPos/appsettings.json`

```jsonc
{
  "BaseUrl": "http://localhost:8000/api/v1",   // Middleware thật
  "UseMock": true,                              // true = mock backend, false = gọi HTTP
  "Terminals": [ /* 6 terminal mẫu: S-01..S-03 × POS-01..POS-02 */ ]
}
```

> `Token` trong cấu hình chỉ dùng để dựng header `Authorization: Bearer <token>`.
> Token chứa chuỗi `invalid` sẽ bị backend trả `401`.

## 3. Bố cục mã nguồn

```
demo-pos-winforms/
├─ DemoPos.slnx
├─ README.md
├─ DemoPos/
│  ├─ Program.cs                 # Entry point, nạp appsettings.json, chọn mock/HTTP
│  ├─ MainForm.cs                # Điều phối 3 tab + Dev Tools
│  ├─ MainForm.Designer.cs       # Layout WinForms
│  ├─ appsettings.json
│  ├─ Models/
│  │  ├─ PosEvent.cs             # Payload gửi lên (event_type, transaction_id, items…)
│  │  ├─ PosResult.cs            # Kết quả chuẩn hóa (outcome, code, current_stock…)
│  │  ├─ CartItem.cs             # Dòng giỏ (bán / trả)
│  │  ├─ TxnRecord.cs            # Dòng lịch sử giao dịch
│  │  └─ AppConfig.cs            # BaseUrl, UseMock, Terminals
│  ├─ Services/
│  │  ├─ IPosBackend.cs          # Interface duy nhất mà UI phụ thuộc
│  │  ├─ MockPosBackend.cs       # Mô phỏng luật nghiệp vụ của Middleware
│  │  └─ PosApiClient.cs         # Gọi POST {BaseUrl}/pos/events
│  ├─ Controls/
│  │  ├─ CartGrid.cs             # Bảng giỏ (sửa được số lượng)
│  │  ├─ HistoryGrid.cs          # Bảng lịch sử
│  │  ├─ ResultPanel.cs          # Khung kết quả + nút "Gửi lại"
│  │  └─ DevToolsPanel.cs        # Kịch bản lỗi (ẩn mặc định, bật/tắt bằng F12)
│  └─ Data/ProductCatalog.cs     # Danh mục 10 SKU (dữ liệu mẫu, dùng làm seed cho backend)
└─ DemoPos.Tests/
   ├─ MockPosBackendTests.cs     # 9 test luật nghiệp vụ của mock backend
   └─ MainFormUiTests.cs         # 10 test mức UI (chạy MainForm thật trên luồng STA)
```

## 4. Luồng nghiệp vụ

### 4.0 Thanh công cụ (Toolbar)

Tất cả điều khiển cấu hình nằm **trên một thanh công cụ duy nhất** ở đầu cửa sổ (không có bản sao trong Dev Tools):

| Điều khiển | Ý nghĩa |
|---|---|
| Combo **Terminal** | Chọn terminal đang hoạt động, hiển thị theo định dạng `Tên cửa hàng · POS-xx` (ví dụ `Cửa hàng Trung tâm · POS-01`) |
| Ô **BaseUrl** | Địa chỉ Middleware, ví dụ `http://localhost:8000/api/v1` |
| Nút **Áp dụng** | Chuẩn hoá `BaseUrl` (cắt khoảng trắng và dấu `/` cuối), đồng bộ vào `AppConfig` + `PosApiClient`, cập nhật nhãn chế độ backend |
| Checkbox **Mock** | Bật → dùng mock backend trong bộ nhớ; bỏ chọn → gọi HTTP Middleware thật bằng `BaseUrl` hiện tại |
| Nhãn chế độ | Hiển thị `● MOCK (in-memory)` khi đang mock, hoặc `● MIDDLEWARE (<BaseUrl>)` khi gọi HTTP |
| Nút **Dev Tools** | Bật/tắt bảng kịch bản lỗi (tương đương phím `F12`) |

**Hành vi khi chuyển mock ↔ middleware:**

* Instance `MockPosBackend` được **giữ lại** qua các lần chuyển, nên tồn kho và lịch sử giao dịch trong
  phiên làm việc không bị mất khi bỏ/tick lại checkbox `Mock`.
* Khi chuyển sang middleware, `PosApiClient` cũ (nếu có) được `Dispose` để giải phóng `HttpClient`.
* Khi chuyển lại mock, `PosApiClient` đang chạy cũng được `Dispose`.
* `BaseUrl` được áp dụng **trước** khi tạo client mới, nên URL dùng để gọi luôn khớp với ô nhập.

### 4.1 Tab **Bán hàng** (SALE)

1. Chọn terminal ở thanh công cụ, tìm sản phẩm, bấm **Thêm vào giỏ** (hoặc `Enter`).
2. Sửa số lượng trực tiếp trong bảng giỏ.
3. Bấm **Thanh toán · Gửi SALE** (hoặc `F9`).

Payload gửi đi:

```json
{
  "event_type": "SALE",
  "transaction_id": "<uuid>",
  "store_id": "S-01",
  "pos_id": "POS-01",
  "items": [{ "sku": "VIN-MLK-1L", "quantity": 2, "unit_price": 18500 }],
  "timestamp": "2026-10-31T09:00:00Z"
}
```

* Thành công → `outcome = OK`, `code = SALE_ACCEPTED`, giỏ được xoá, tồn kho giảm.
* Gửi lại **cùng** `transaction_id` + **cùng** nội dung → `DUPLICATE` (HTTP 200), tồn kho **không** đổi.
* Gửi lại **cùng** `transaction_id` nhưng **khác** nội dung → `CONFLICT` (HTTP 409).
* Vượt tồn → `INSUFFICIENT_STOCK` (HTTP 409), giỏ được giữ nguyên.

### 4.2 Tab **Trả hàng** (RETURN)

* Danh sách "đơn bán gốc" chỉ hiện các đơn **SALE thành công**.
* Số lượng trả tối đa = `đã bán − đã trả trước đó`; vượt quá → `RETURN_EXCEEDS_SOLD` (409).
* Thành công → `RETURN_ACCEPTED`, tồn kho được hoàn lại.

### 4.3 Tab **Hủy đơn** (CANCELLATION)

* Hủy **toàn bộ** đơn bán gốc, có hộp thoại xác nhận.
* Tồn kho được hoàn lại theo **số lượng còn thực sự trong đơn** (đã trả trước đó thì không hoàn 2 lần).
* Đơn đã hủy không hủy lại được → `ALREADY_CANCELLED` (409); nút hủy bị khoá trên UI.

### 4.4 Dev Tools

Bảng Dev Tools **mặc định ẩn** để giao diện chính chiếm hết không gian.
Bật/tắt bằng nút **Dev Tools** trên thanh công cụ hoặc phím **F12**.

| Nút | Kịch bản | Kết quả mong đợi |
|---|---|---|
| Reset mock | Xoá lịch sử, khôi phục tồn kho gốc | Tồn kho về `initial_stock` |
| Gửi lại giống hệt | Gửi 2 lần cùng `transaction_id` | Lần 2 → `DUPLICATE` |
| Gửi khác nội dung | Gửi 2 lần cùng id, khác items | Lần 2 → `CONFLICT` |
| Vượt tồn kho | Gửi số lượng 999 | `INSUFFICIENT_STOCK` |
| Token sai (401) | Token chứa `invalid` | `UNAUTHORIZED` |
| Terminal lạ (403) | Store/POS không có trong cấu hình | `TERMINAL_NOT_REGISTERED` |
| 503 rồi gửi lại | Lần đầu lỗi, lần sau thành công | `SERVICE_UNAVAILABLE` → bấm **Gửi lại** được |

> Bật/tắt **mock** và đổi `BaseUrl` **không** nằm trong Dev Tools nữa mà nằm trên **thanh công cụ**
> (`BaseUrl` + **Áp dụng** + checkbox `Mock`), tránh lặp lại điều khiển.

### 4.5 Phím tắt

| Phím | Chức năng |
|---|---|
| `F2` | Về tab **Bán hàng** và focus ô tìm kiếm |
| `F9` | Gửi giao dịch của tab hiện tại (SALE / RETURN / CANCELLATION) |
| `F12` | Bật/tắt bảng **Dev Tools** |
| `Esc` | Xoá kết quả giao dịch hiện tại |

## 5. Ánh xạ hợp đồng API

| Khái niệm | Hợp đồng API (JSON) | WinForms |
|---|---|---|
| Endpoint | `POST {BaseUrl}/pos/events` | `PosApiClient.PosEventsEndpoint = "/pos/events"` |
| Loại sự kiện | `event_type` = `SALE` / `RETURN` / `CANCELLATION` | `PosEvent.EventType` |
| Idempotency | `transaction_id` | `PosEvent.TransactionId` (UUID tạo 1 lần, giữ nguyên khi retry) |
| Đơn gốc (trả/hủy) | `original_transaction_id` | `PosEvent.OriginalTransactionId` |
| Xác thực | `Authorization: Bearer <token>` | `PosEvent.Token` → header |
| Kết quả | `outcome` = `OK` / `DUPLICATE` / `REJECTED` / `ERROR` | `PosResult.Outcome` |
| Mã lỗi | `code` | `PosResult.Code` |
| Tồn kho | `current_stock` | `PosResult.CurrentStock` |

`PosResult.CanRetry = true` khi HTTP `503`/`0` hoặc code ∈ {`SERVICE_UNAVAILABLE`, `TIMEOUT`, `NETWORK_ERROR`}
→ UI hiện nút **Gửi lại** và giữ nguyên giỏ + `transaction_id`.

## 6. Chạy test

```bash
cd RetailSync/frontend/demo-pos-winforms
dotnet test DemoPos.slnx
```

Kết quả hiện tại: **19/19 passed**.

* `MockPosBackendTests` (9 test) — luật nghiệp vụ của mock backend:
  SALE thành công + trừ tồn, DUPLICATE, CONFLICT, INSUFFICIENT_STOCK,
  RETURN giới hạn theo số đã bán − đã trả và hoàn tồn,
  CANCELLATION hoàn tồn và `ALREADY_CANCELLED`,
  403 terminal lạ, 401 token sai, 503 buộc gửi lại, Reset khôi phục tồn kho.
* `MainFormUiTests` (10 test) — mức UI, chạy `MainForm` thật trên luồng **STA** có message pump:
  bán hàng xoá giỏ, trả hàng hoàn tồn, hủy đơn hoàn tồn + khoá nút hủy,
  Dev Tools sinh `DUPLICATE` / `CONFLICT`, 503 → gửi lại giữ nguyên `transaction_id`,
  các lỗi 401 / 403 / `INSUFFICIENT_STOCK`,
  Dev Tools ẩn mặc định + bật/tắt bằng **F12**/nút (form được `Show()` thật để kiểm tra `Visible`),
  thanh công cụ điều khiển `BaseUrl` + `Mock` (không còn control trùng lặp),
  chuyển mock → middleware → mock vẫn giữ nguyên tồn kho và lịch sử trong phiên,
  và combo Terminal hiển thị `Tên cửa hàng · POS-xx`.

> Các thành phần UI dùng cho test được đánh dấu `internal` kèm `InternalsVisibleTo("DemoPos.Tests")`
> nên **không** làm thay đổi hành vi khi chạy ứng dụng thật.

## 7. Ghi chú giới hạn

* Chỉ hỗ trợ **Windows** (WinForms), không chạy được trên Linux/macOS.
* `PosApiClient` giữ cache tồn kho cục bộ để hiển thị; nút **Reset mock** chỉ khôi phục cache UI,
  không gọi API lên Middleware.
* Ứng dụng không lưu dữ liệu xuống đĩa; lịch sử chỉ tồn tại trong phiên làm việc.

