using DemoPos.Controls;
using DemoPos.Models;
using DemoPos.Services;
using Xunit;

namespace DemoPos.Tests;

/// <summary>
/// Kiểm thử mức UI: chạy MainForm thật trên luồng STA và điều khiển bằng code
/// (không cần chuột/bàn phím) để xác nhận luồng Bán / Trả / Hủy và Dev Tools hoạt động.
/// </summary>
public sealed class MainFormUiTests
{
    private const string StoreId = "S-01";
    private const string PosId = "POS-01";
    private const string Token = "demo-pos-jwt-store-01-pos-01";

    private static AppConfig BuildConfig() => new()
    {
        UseMock = true,
        BaseUrl = "http://localhost:8000/api/v1",
        Terminals = new List<TerminalConfig>
        {
            new() { StoreId = StoreId, StoreName = "Cửa hàng A", PosId = PosId, Token = Token }
        }
    };

    /// <summary>Chạy một tác vụ async trên luồng STA có message pump (WinForms cần vậy).</summary>
    private static Task RunOnStaAsync(Func<Task> action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                var task = action();

                while (!task.IsCompleted)
                {
                    Application.DoEvents();
                    Thread.Sleep(5);
                }

                task.GetAwaiter().GetResult();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "DemoPos UI test"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static int Stock(MockPosBackend backend, string sku) =>
        backend.GetProducts().First(p => p.Sku == sku).CurrentStock;

    /// <summary>
    /// Kích hoạt sự kiện Click của một Button mà không cần form phải hiển thị
    /// (Button.PerformClick bị bỏ qua khi control chưa visible/selectable).
    /// </summary>
    private static void ClickButton(Button button)
    {
        var onClick = typeof(Button).GetMethod(
            "OnClick",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        onClick.Invoke(button, new object[] { EventArgs.Empty });
    }

    [Fact]
    public async Task Luong_ban_hang_Thanh_cong_va_xoa_gio()
    {
        await RunOnStaAsync(async () =>
        {
            var backend = new MockPosBackend(BuildConfig());
            using var form = new MainForm(BuildConfig(), backend);
            form.ConfirmCancelOverride = _ => DialogResult.Yes;

            var product = form.FirstProduct()!;
            var before = Stock(backend, product.Sku);

            form.AddProduct(product, 2);
            Assert.Single(form.SaleCartItems);

            await form.SubmitSaleAsync();

            Assert.Equal("SALE_ACCEPTED", form.Result.CodeText);
            Assert.Empty(form.SaleCartItems);
            Assert.Equal(before - 2, Stock(backend, product.Sku));
            Assert.Single(form.History.Records);
            Assert.Equal("SALE", form.History.Records[0].EventType);
        });
    }

    [Fact]
    public async Task Luong_trả_hàng_Hoan_ton_kho()
    {
        await RunOnStaAsync(async () =>
        {
            var config = BuildConfig();
            var backend = new MockPosBackend(config);
            using var form = new MainForm(config, backend);
            form.ConfirmCancelOverride = _ => DialogResult.Yes;

            var product = form.FirstProduct()!;
            var initialStock = Stock(backend, product.Sku);

            form.AddProduct(product, 2);
            await form.SubmitSaleAsync();
            Assert.Equal(initialStock - 2, Stock(backend, product.Sku));

            // Tab Trả hàng: đơn gốc được chọn tự động sau khi bán thành công.
            form.Tabs.SelectedIndex = 1;
            Assert.NotNull(form.ReturnOriginalCombo.SelectedItem);
            Assert.Single(form.ReturnCartItems);

            form.SetReturnQuantity(0, 1);
            await form.SubmitReturnAsync();

            Assert.Equal("RETURN_ACCEPTED", form.Result.CodeText);
            Assert.Equal(initialStock - 1, Stock(backend, product.Sku));
            Assert.Equal(2, form.History.Records.Count);
        });
    }

    [Fact]
    public async Task Luong_huy_don_Hoan_ton_kho_va_khong_huy_lan_hai()
    {
        await RunOnStaAsync(async () =>
        {
            var config = BuildConfig();
            var backend = new MockPosBackend(config);
            using var form = new MainForm(config, backend);
            form.ConfirmCancelOverride = _ => DialogResult.Yes;

            var product = form.FirstProduct()!;
            var initialStock = Stock(backend, product.Sku);

            form.AddProduct(product, 2);
            await form.SubmitSaleAsync();

            form.Tabs.SelectedIndex = 2;
            Assert.NotNull(form.CancelOriginalCombo.SelectedItem);

            await form.SubmitCancellationAsync();

            Assert.Equal("CANCELLATION_ACCEPTED", form.Result.CodeText);
            Assert.Equal(initialStock, Stock(backend, product.Sku));
            Assert.False(form.SubmitCancelButton.Enabled);

            // Hủy lần hai phải bị chặn ngay ở UI.
            await form.SubmitCancellationAsync();
            Assert.Equal(initialStock, Stock(backend, product.Sku));
        });
    }

    [Fact]
    public async Task Dev_Tools_Duplicate_va_Conflict()
    {
        await RunOnStaAsync(async () =>
        {
            var config = BuildConfig();
            var backend = new MockPosBackend(config);
            using var form = new MainForm(config, backend);

            await form.RunDevActionAsync(DevAction.SendDuplicate);
            Assert.Equal(2, form.History.Records.Count);
            Assert.Equal("DUPLICATE", form.History.Records[0].Code);
            Assert.Equal(PosOutcome.DUPLICATE, form.History.Records[0].Outcome);

            await form.RunDevActionAsync(DevAction.SendConflict);
            Assert.Equal(4, form.History.Records.Count);
            Assert.Equal("CONFLICT", form.History.Records[0].Code);
            Assert.Equal(PosOutcome.REJECTED, form.History.Records[0].Outcome);
        });
    }

    [Fact]
    public async Task Dev_Tools_503_Cho_phep_gui_lai_cung_transaction_id()
    {
        await RunOnStaAsync(async () =>
        {
            var config = BuildConfig();
            var backend = new MockPosBackend(config);
            using var form = new MainForm(config, backend);

            await form.RunDevActionAsync(DevAction.SendUnavailable);

            Assert.Equal("SERVICE_UNAVAILABLE", form.Result.CodeText);
            Assert.True(form.Result.RetryEnabled);

            var failedRecord = form.History.Records[0];
            await form.RetryLastAsync();

            Assert.Equal(2, form.History.Records.Count);
            Assert.Equal(failedRecord.TransactionId, form.History.Records[0].TransactionId);
            Assert.Equal(PosOutcome.OK, form.History.Records[0].Outcome);
        });
    }

    [Fact]
    public async Task Dev_Tools_Loi_401_403_va_thieu_ton_kho()
    {
        await RunOnStaAsync(async () =>
        {
            var config = BuildConfig();
            var backend = new MockPosBackend(config);
            using var form = new MainForm(config, backend);

            await form.RunDevActionAsync(DevAction.SendInvalidToken);
            Assert.Equal("UNAUTHORIZED", form.Result.CodeText);

            await form.RunDevActionAsync(DevAction.SendUnknownTerminal);
            Assert.Equal("TERMINAL_NOT_REGISTERED", form.Result.CodeText);

            await form.RunDevActionAsync(DevAction.SendInsufficientStock);
            Assert.Equal("INSUFFICIENT_STOCK", form.Result.CodeText);
        });
    }

    [Fact]
    public async Task Dev_Tools_An_mac_dinh_va_bat_tat_bang_F12_hoac_nut()
    {
        await RunOnStaAsync(async () =>
        {
            var config = BuildConfig();
            var backend = new MockPosBackend(config);
            using var form = new MainForm(config, backend);

            // Hiện form thật để kiểm tra đúng trạng thái Visible của WinForms.
            form.Show();
            Application.DoEvents();

            // Mặc định phải ẩn.
            Assert.False(form.DevToolsOpen);
            Assert.Equal(0, form.DevToolsRowHeight);
            Assert.False(form.DevTools.Visible);

            // Phím F12 mở panel.
            Assert.True(form.HandleShortcut(Keys.F12));
            Application.DoEvents();
            Assert.True(form.DevToolsOpen);
            Assert.True(form.DevToolsRowHeight > 0);
            Assert.True(form.DevTools.Visible);

            // F12 lần nữa thì ẩn lại.
            Assert.True(form.HandleShortcut(Keys.F12));
            Application.DoEvents();
            Assert.False(form.DevToolsOpen);
            Assert.Equal(0, form.DevToolsRowHeight);
            Assert.False(form.DevTools.Visible);

            // Nút trên thanh công cụ cũng bật/tắt được.
            ClickButton(form.DevToolsButton);
            Application.DoEvents();
            Assert.True(form.DevToolsOpen);
            Assert.True(form.DevToolsRowHeight > 0);
            Assert.True(form.DevTools.Visible);

            ClickButton(form.DevToolsButton);
            Application.DoEvents();
            Assert.False(form.DevToolsOpen);
            Assert.Equal(0, form.DevToolsRowHeight);
            Assert.False(form.DevTools.Visible);

            form.Hide();
        });
    }

    [Fact]
    public async Task Thanh_cong_cu_dieu_khien_BaseUrl_va_Mock_khong_co_ban_trung_lap()
    {
        await RunOnStaAsync(async () =>
        {
            var config = BuildConfig();
            var backend = new MockPosBackend(config);
            using var form = new MainForm(config, backend);

            // BaseUrl + Apply nằm trên thanh công cụ.
            Assert.Equal(config.BaseUrl, form.BaseUrlTextBox.Text);
            Assert.True(form.MockCheckBox.Checked);

            form.BaseUrlTextBox.Text = "http://localhost:9999/api/v1/";
            // Nút Apply trên thanh công cụ áp dụng BaseUrl.
            ClickButton(form.ApplyBaseUrlButton);

            // BaseUrl được chuẩn hoá (bỏ dấu / cuối) và đồng bộ vào config.
            Assert.Equal("http://localhost:9999/api/v1", config.BaseUrl);
            Assert.Equal("http://localhost:9999/api/v1", form.BaseUrlTextBox.Text);
            Assert.Contains("MOCK", form.BackendModeLabel.Text);

            // Bỏ chọn Mock => chuyển sang Middleware và nhãn trạng thái đổi theo.
            form.MockCheckBox.Checked = false;
            Assert.False(config.UseMock);
            Assert.Contains("MIDDLEWARE", form.BackendModeLabel.Text);
            Assert.Contains("http://localhost:9999/api/v1", form.BackendModeLabel.Text);

            // Bật lại Mock => quay về mock in-memory.
            form.MockCheckBox.Checked = true;
            Assert.True(config.UseMock);
            Assert.Contains("MOCK", form.BackendModeLabel.Text);
        });
    }

    [Fact]
    public async Task Chuyen_mock_sang_middleware_va_quay_lai_giu_nguyen_trang_thai_mock()
    {
        await RunOnStaAsync(async () =>
        {
            var config = BuildConfig();
            var backend = new MockPosBackend(config);
            using var form = new MainForm(config, backend);

            var product = form.FirstProduct()!;
            var initialStock = Stock(backend, product.Sku);

            // Bán 2 sản phẩm trong mock => tồn kho giảm.
            form.AddProduct(product, 2);
            await form.SubmitSaleAsync();
            Assert.Equal(initialStock - 2, Stock(backend, product.Sku));

            // Chuyển sang middleware rồi quay lại mock.
            form.MockCheckBox.Checked = false;
            Assert.False(config.UseMock);
            form.MockCheckBox.Checked = true;
            Assert.True(config.UseMock);

            // Instance mock được giữ lại nên tồn kho không bị reset về gốc.
            Assert.Equal(initialStock - 2, Stock(backend, product.Sku));

            // Lịch sử giao dịch của phiên vẫn còn.
            Assert.Single(form.History.Records);
            Assert.Equal("SALE_ACCEPTED", form.History.Records[0].Code);
        });
    }

    [Fact]
    public async Task Combo_Terminal_hien_thi_Ten_cua_hang_POS()
    {
        await RunOnStaAsync(async () =>
        {
            var config = BuildConfig();
            var backend = new MockPosBackend(config);
            using var form = new MainForm(config, backend);

            Assert.Equal(nameof(TerminalConfig.DisplayName), form.TerminalCombo.DisplayMember);
            Assert.Equal("Cửa hàng A · POS-01", form.TerminalCombo.GetItemText(form.TerminalCombo.Items[0]));
            Assert.Equal("Cửa hàng A · POS-01", form.TerminalCombo.Text);
        });
    }
}
