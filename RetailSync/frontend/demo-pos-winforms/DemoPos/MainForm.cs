using DemoPos.Controls;
using DemoPos.Data;
using DemoPos.Models;
using DemoPos.Services;

namespace DemoPos;

/// <summary>
/// Màn hình POS: 3 tab Bán hàng / Trả hàng / Hủy đơn, kết quả giao dịch, lịch sử phiên và Dev Tools.
/// UI chỉ nói chuyện với IPosBackend nên không quan tâm đang dùng mock hay Middleware thật.
/// </summary>
public partial class MainForm : Form
{
    /// <summary>Chiều cao (px) của bảng Dev Tools khi mở.</summary>
    private const int DevToolsHeight = 190;

    private readonly AppConfig _config;
    private IPosBackend _backend;
    private MockPosBackend? _mockBackend;
    private readonly List<CartItem> _saleCart = new();
    private readonly List<CartItem> _returnCart = new();
    private readonly Dictionary<string, SaleState> _sales = new(StringComparer.OrdinalIgnoreCase);
    private string _saleTransactionId = Guid.NewGuid().ToString();
    private PosEvent? _lastEvent;
    private bool _busy;
    private bool _devToolsOpen;

    public MainForm(AppConfig config, IPosBackend backend)
    {
        _config = config;
        _backend = backend;
        _mockBackend = backend as MockPosBackend;

        InitializeComponent();

        _cmbTerminal.DisplayMember = nameof(TerminalConfig.DisplayName);
        _cmbTerminal.Items.AddRange(config.Terminals.Cast<object>().ToArray());
        if (_cmbTerminal.Items.Count > 0)
        {
            _cmbTerminal.SelectedIndex = 0;
        }

        _devTools.SetTerminals(config.Terminals);
        _txtBaseUrl.Text = config.BaseUrl;
        _chkMock.Checked = config.UseMock;

        WireEvents();
        RefreshProducts();
        UpdateBackendMode();
        UpdateSaleTransactionLabel();
    }

    /// <summary>Trạng thái một đơn bán thành công (dùng cho tab Trả hàng / Hủy đơn).</summary>
    private sealed class SaleState
    {
        public string TransactionId { get; set; } = string.Empty;

        public List<PosEventItem> Items { get; set; } = new();

        public Dictionary<string, int> Returned { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public bool Cancelled { get; set; }

        public string Label => Cancelled
            ? $"{Short(TransactionId)} — Đã hủy"
            : $"{Short(TransactionId)} — {Items.Count} dòng";
    }

    private TerminalConfig? CurrentTerminal => _cmbTerminal.SelectedItem as TerminalConfig;

    // ---------------------------------------------------------------------
    // Điểm móc cho kiểm thử tự động (internal + InternalsVisibleTo).
    // UI vẫn chạy bình thường; các thành phần này chỉ để test lái form
    // mà không cần thao tác chuột/bàn phím.
    // ---------------------------------------------------------------------
    internal TabControl Tabs => _tabs;

    internal ComboBox TerminalCombo => _cmbTerminal;

    internal DataGridView ProductGrid => _dgvProducts;

    internal Button AddProductButton => _btnAddProduct;

    internal CartGrid SaleCartGrid => _cartGrid;

    internal CartGrid ReturnCartGrid => _returnGrid;

    internal Button SubmitSaleButton => _btnSubmitSale;

    internal Button SubmitReturnButton => _btnSubmitReturn;

    internal Button SubmitCancelButton => _btnSubmitCancel;

    internal ComboBox ReturnOriginalCombo => _cmbReturnOriginal;

    internal ComboBox CancelOriginalCombo => _cmbCancelOriginal;

    internal HistoryGrid History => _historyGrid;

    internal ResultPanel Result => _resultPanel;

    internal Label StatusLabel => _lblStatus;

    /// <summary>Bảng Dev Tools (mặc định ẩn, bật/tắt bằng nút hoặc F12).</summary>
    internal DevToolsPanel DevTools => _devTools;

    /// <summary>Trạng thái mở của Dev Tools (true = đang hiện).</summary>
    internal bool DevToolsOpen => _devToolsOpen;

    /// <summary>Chiều cao dành cho hàng Dev Tools trong layout (0 = đang thu gọn).</summary>
    internal int DevToolsRowHeight => (int)_contentLayout.RowStyles[1].Height;

    /// <summary>Nút bật/tắt Dev Tools trên thanh công cụ.</summary>
    internal Button DevToolsButton => _btnDevTools;

    /// <summary>Checkbox chọn mock backend trên thanh công cụ.</summary>
    internal CheckBox MockCheckBox => _chkMock;

    /// <summary>Ô nhập BaseUrl trên thanh công cụ.</summary>
    internal TextBox BaseUrlTextBox => _txtBaseUrl;

    /// <summary>Nút "Áp dụng" BaseUrl trên thanh công cụ.</summary>
    internal Button ApplyBaseUrlButton => _btnApplyBaseUrl;

    /// <summary>Nhãn hiển thị chế độ backend hiện tại.</summary>
    internal Label BackendModeLabel => _lblBackendMode;

    /// <summary>Ghi đè hộp thoại xác nhận hủy đơn (test tự động trả về Yes).</summary>
    internal Func<string, DialogResult>? ConfirmCancelOverride { get; set; }

    private void WireEvents()
    {
        _cmbTerminal.SelectedIndexChanged += (_, _) => OnTerminalChanged();
        _btnApplyBaseUrl.Click += (_, _) => ApplyBaseUrl();
        _txtSearch.TextChanged += (_, _) => RefreshProducts();
        _btnAddProduct.Click += (_, _) => AddSelectedProduct();
        _dgvProducts.CellDoubleClick += (_, _) => AddSelectedProduct();
        _btnClearCart.Click += (_, _) => ClearSaleCart();
        _btnSubmitSale.Click += async (_, _) => await SubmitSaleAsync();
        _btnSubmitReturn.Click += async (_, _) => await SubmitReturnAsync();
        _btnSubmitCancel.Click += async (_, _) => await SubmitCancellationAsync();
        _cmbReturnOriginal.SelectedIndexChanged += (_, _) => LoadReturnCart();
        _cmbCancelOriginal.SelectedIndexChanged += (_, _) => UpdateCancelState();
        _resultPanel.RetryRequested += async (_, _) => await RetryLastAsync();
        _chkMock.CheckedChanged += (_, _) => ApplyMockSelection();
        _btnDevTools.Click += (_, _) => ToggleDevTools();
        _devTools.ActionRequested += async (_, action) => await RunDevActionAsync(action);
        _historyGrid.DoubleClick += (_, _) => FocusHistorySelection();
        KeyDown += OnFormKeyDown;
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (HandleShortcut(e.KeyCode))
        {
            e.Handled = true;
        }
    }

    /// <summary>Xử lý phím tắt của form; trả về true nếu phím đã được dùng.</summary>
    internal bool HandleShortcut(Keys keyCode)
    {
        switch (keyCode)
        {
            case Keys.F2:
                _tabs.SelectedIndex = 0;
                _txtSearch.Focus();
                _txtSearch.SelectAll();
                return true;
            case Keys.F9:
                SubmitCurrentTab();
                return true;
            case Keys.F10:
                RetryLast();
                return true;
            case Keys.F12:
                ToggleDevTools();
                return true;
            case Keys.Escape:
                _resultPanel.Clear();
                _txtSearch.Focus();
                return true;
            default:
                return false;
        }
    }

    private async void SubmitCurrentTab()
    {
        switch (_tabs.SelectedIndex)
        {
            case 0:
                await SubmitSaleAsync();
                break;
            case 1:
                await SubmitReturnAsync();
                break;
            case 2:
                await SubmitCancellationAsync();
                break;
        }
    }

    /// <summary>Gửi lại giao dịch gần nhất (phím F10); chỉ có tác dụng khi giao dịch cho phép gửi lại.</summary>
    private async void RetryLast()
    {
        if (!_resultPanel.RetryEnabled)
        {
            ShowInfo("Không có giao dịch nào để gửi lại.");
            return;
        }

        await RetryLastAsync();
    }

    private void OnTerminalChanged()
    {
        // Đổi terminal => xóa giỏ và sinh transaction_id mới (giống web).
        _saleCart.Clear();
        _cartGrid.Clear();
        _returnCart.Clear();
        _returnGrid.Clear();
        _saleTransactionId = Guid.NewGuid().ToString();
        _lastEvent = null;
        _resultPanel.Clear();
        UpdateSaleTransactionLabel();
    }

    private void ApplyBaseUrl()
    {
        var value = _txtBaseUrl.Text.Trim().TrimEnd('/');
        _txtBaseUrl.Text = value;
        _config.BaseUrl = value;

        if (_backend is PosApiClient client)
        {
            client.BaseUrl = value;
        }

        UpdateBackendMode();
    }

    /// <summary>Áp dụng trạng thái mock/real của checkbox trên thanh công cụ.</summary>
    private void ApplyMockSelection()
    {
        var useMock = _chkMock.Checked;
        if (useMock == _config.UseMock)
        {
            return;
        }

        // Đồng bộ BaseUrl trước khi chuyển sang middleware để client dùng đúng URL.
        ApplyBaseUrl();

        _config.UseMock = useMock;
        if (useMock)
        {
            // Giữ lại instance mock để không mất tồn kho/đơn đã gửi trong phiên.
            _mockBackend ??= new MockPosBackend(_config);
            (_backend as IDisposable)?.Dispose();
            _backend = _mockBackend;
        }
        else
        {
            (_backend as IDisposable)?.Dispose();
            _backend = new PosApiClient(_config, _config.BaseUrl);
        }

        UpdateBackendMode();
        RefreshProducts();
    }

    /// <summary>Bật/tắt Dev Tools (nút trên thanh công cụ hoặc phím F12).</summary>
    internal void ToggleDevTools()
    {
        var visible = !_devToolsOpen;
        _devToolsOpen = visible;
        _devTools.Visible = visible;
        _contentLayout.RowStyles[1].Height = visible ? DevToolsHeight : 0;
        _btnDevTools.Text = visible ? "Dev Tools ▲" : "Dev Tools";
        _contentLayout.PerformLayout();
    }

    private void UpdateBackendMode()
    {
        _lblBackendMode.Text = _config.UseMock
            ? "● MOCK (in-memory)"
            : $"● MIDDLEWARE ({_config.BaseUrl})";
        _lblBackendMode.ForeColor = _config.UseMock
            ? Color.FromArgb(21, 101, 60)
            : Color.FromArgb(31, 122, 72);
    }

    private void UpdateSaleTransactionLabel()
    {
        _btnSubmitSale.Text = "Thanh toán · Gửi SALE (F9)";
    }

    private void RefreshProducts()
    {
        var keyword = _txtSearch.Text.Trim();
        var products = _backend.GetProducts();

        if (keyword.Length > 0)
        {
            products = products
                .Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                            || p.Sku.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                            || p.Barcode.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        _dgvProducts.DataSource = products.ToList();
    }

    private void AddSelectedProduct()
    {
        if (_dgvProducts.CurrentRow?.DataBoundItem is not Product product)
        {
            return;
        }

        AddProduct(product, 1);
    }

    /// <summary>Thêm sản phẩm vào giỏ (dùng chung cho thao tác chuột và kiểm thử).</summary>
    internal void AddProduct(Product product, int quantity = 1)
    {
        var existing = _saleCart.FirstOrDefault(item =>
            string.Equals(item.Sku, product.Sku, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _saleCart.Add(new CartItem
            {
                Sku = product.Sku,
                Name = product.Name,
                UnitPrice = product.UnitPrice,
                Quantity = quantity
            });
        }

        _cartGrid.SetItems(_saleCart);
        _resultPanel.Clear();
    }

    /// <summary>Sản phẩm đầu tiên trong danh mục của backend hiện tại.</summary>
    internal Product? FirstProduct() => _backend.GetProducts().FirstOrDefault();

    /// <summary>Nội dung giỏ bán hiện tại (dùng cho kiểm thử).</summary>
    internal IReadOnlyList<CartItem> SaleCartItems => _saleCart;

    /// <summary>Nội dung giỏ trả hàng hiện tại (dùng cho kiểm thử).</summary>
    internal IReadOnlyList<CartItem> ReturnCartItems => _returnCart;

    /// <summary>Đặt số lượng trả của một dòng trong tab Trả hàng.</summary>
    internal void SetReturnQuantity(int index, int quantity)
    {
        if (index < 0 || index >= _returnCart.Count)
        {
            return;
        }

        _returnCart[index].Quantity = quantity;
        _returnGrid.SetItems(_returnCart);
    }

    /// <summary>Transaction ID hiện tại của tab Bán hàng (dùng cho kiểm thử).</summary>
    internal string SaleTransactionId => _saleTransactionId;

    /// <summary>Gán lại transaction ID của tab Bán hàng (dùng cho kiểm thử idempotency).</summary>
    internal void SetSaleTransactionId(string transactionId)
    {
        _saleTransactionId = transactionId;
        UpdateSaleTransactionLabel();
    }

    private void ClearSaleCart()
    {
        _saleCart.Clear();
        _cartGrid.Clear();
        _saleTransactionId = Guid.NewGuid().ToString();
        _lastEvent = null;
        _resultPanel.Clear();
        UpdateSaleTransactionLabel();
    }

    internal async Task SubmitSaleAsync()
    {
        if (_saleCart.Count == 0)
        {
            ShowInfo("Giỏ hàng đang trống.");
            return;
        }

        var terminal = CurrentTerminal;
        if (terminal is null)
        {
            ShowInfo("Chưa chọn terminal.");
            return;
        }

        var posEvent = new PosEvent
        {
            EventType = "SALE",
            TransactionId = _saleTransactionId,
            StoreId = terminal.StoreId,
            PosId = terminal.PosId,
            Token = terminal.Token,
            Timestamp = DateTime.UtcNow,
            Items = _saleCart.Select(item => new PosEventItem
            {
                Sku = item.Sku,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

        var result = await SendAsync(posEvent, addHistory: true);

        if (result.Outcome == PosOutcome.OK)
        {
            RegisterSale(posEvent);
            ClearSaleCartAfterSuccess();
            ShowInfo("Đã ghi nhận giao dịch bán.");
        }
        else if (result.Outcome == PosOutcome.DUPLICATE)
        {
            // Đơn đã được ghi nhận trước đó: coi như hoàn tất, sinh transaction_id mới.
            ClearSaleCartAfterSuccess();
            ShowInfo("Giao dịch đã được ghi nhận trước đó (DUPLICATE).");
        }
    }

    private void RegisterSale(PosEvent posEvent)
    {
        _sales[posEvent.TransactionId] = new SaleState
        {
            TransactionId = posEvent.TransactionId,
            Items = posEvent.Items
                .Select(item => new PosEventItem
                {
                    Sku = item.Sku,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
        };

        RefreshOriginalSelectors();
    }

    private void RefreshOriginalSelectors()
    {
        var currentReturn = (_cmbReturnOriginal.SelectedItem as SaleState)?.TransactionId;
        var currentCancel = (_cmbCancelOriginal.SelectedItem as SaleState)?.TransactionId;

        _cmbReturnOriginal.Items.Clear();
        _cmbCancelOriginal.Items.Clear();

        foreach (var sale in _sales.Values)
        {
            _cmbReturnOriginal.Items.Add(sale);
            _cmbCancelOriginal.Items.Add(sale);
        }

        _cmbReturnOriginal.DisplayMember = nameof(SaleState.Label);
        _cmbCancelOriginal.DisplayMember = nameof(SaleState.Label);

        if (currentReturn is not null)
        {
            _cmbReturnOriginal.SelectedItem = _sales.Values.FirstOrDefault(s => s.TransactionId == currentReturn);
        }
        else if (_cmbReturnOriginal.Items.Count > 0)
        {
            _cmbReturnOriginal.SelectedIndex = 0;
        }

        if (currentCancel is not null)
        {
            _cmbCancelOriginal.SelectedItem = _sales.Values.FirstOrDefault(s => s.TransactionId == currentCancel);
        }
        else if (_cmbCancelOriginal.Items.Count > 0)
        {
            _cmbCancelOriginal.SelectedIndex = 0;
        }
    }

    private void LoadReturnCart()
    {
        _returnCart.Clear();

        if (_cmbReturnOriginal.SelectedItem is SaleState sale)
        {
            var catalog = _backend.GetProducts();
            foreach (var item in sale.Items)
            {
                var returned = sale.Returned.TryGetValue(item.Sku, out var value) ? value : 0;
                var max = Math.Max(0, item.Quantity - returned);
                _returnCart.Add(new CartItem
                {
                    Sku = item.Sku,
                    Name = catalog.FirstOrDefault(p => p.Sku == item.Sku)?.Name ?? item.Sku,
                    UnitPrice = item.UnitPrice,
                    Quantity = 0,
                    SoldQuantity = item.Quantity,
                    ReturnedQuantity = returned,
                    MaxReturnQuantity = max
                });
            }
        }

        _returnGrid.SetItems(_returnCart);
    }

    private void UpdateCancelState()
    {
        var sale = _cmbCancelOriginal.SelectedItem as SaleState;
        var cancelled = sale?.Cancelled == true;
        _btnSubmitCancel.Enabled = !cancelled;
        _btnSubmitCancel.Text = cancelled
            ? "× Đơn này đã được hủy"
            : "× Hủy toàn bộ đơn hàng (F9)";
    }

    internal async Task SubmitReturnAsync()
    {
        if (_cmbReturnOriginal.SelectedItem is not SaleState sale)
        {
            ShowInfo("Chọn đơn bán gốc trước khi trả hàng.");
            return;
        }

        var items = _returnCart
            .Where(item => item.Quantity > 0)
            .Select(item => new PosEventItem
            {
                Sku = item.Sku,
                Quantity = Math.Min(item.Quantity, item.MaxReturnQuantity ?? item.Quantity),
                UnitPrice = item.UnitPrice
            })
            .ToList();

        if (items.Count == 0)
        {
            ShowInfo("Chọn đơn gốc và ít nhất một sản phẩm cần trả.");
            return;
        }

        var terminal = CurrentTerminal;
        if (terminal is null)
        {
            ShowInfo("Chưa chọn terminal.");
            return;
        }

        var posEvent = new PosEvent
        {
            EventType = "RETURN",
            TransactionId = Guid.NewGuid().ToString(),
            OriginalTransactionId = sale.TransactionId,
            StoreId = terminal.StoreId,
            PosId = terminal.PosId,
            Token = terminal.Token,
            Timestamp = DateTime.UtcNow,
            Items = items
        };

        var result = await SendAsync(posEvent, addHistory: true);

        if (result.Outcome == PosOutcome.OK)
        {
            foreach (var item in items)
            {
                var returned = sale.Returned.TryGetValue(item.Sku, out var value) ? value : 0;
                sale.Returned[item.Sku] = returned + item.Quantity;
            }

            LoadReturnCart();
            ShowInfo("Đã ghi nhận giao dịch trả.");
        }
    }

    internal async Task SubmitCancellationAsync()
    {
        if (_cmbCancelOriginal.SelectedItem is not SaleState sale)
        {
            ShowInfo("Chọn đơn bán cần hủy.");
            return;
        }

        if (sale.Cancelled)
        {
            ShowInfo("Đơn này đã được hủy trước đó.");
            return;
        }

        var terminal = CurrentTerminal;
        if (terminal is null)
        {
            ShowInfo("Chưa chọn terminal.");
            return;
        }

        var confirmation = ConfirmCancelOverride?.Invoke(
                $"Hủy toàn bộ đơn {Short(sale.TransactionId)}? Tồn kho sẽ được hoàn lại.")
            ?? MessageBox.Show(
                $"Hủy toàn bộ đơn {Short(sale.TransactionId)}? Tồn kho sẽ được hoàn lại.",
                "Xác nhận hủy đơn",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        var posEvent = new PosEvent
        {
            EventType = "CANCELLATION",
            TransactionId = Guid.NewGuid().ToString(),
            OriginalTransactionId = sale.TransactionId,
            StoreId = terminal.StoreId,
            PosId = terminal.PosId,
            Token = terminal.Token,
            Timestamp = DateTime.UtcNow,
            Items = new List<PosEventItem>()
        };

        var result = await SendAsync(posEvent, addHistory: true);

        if (result.Outcome == PosOutcome.OK)
        {
            sale.Cancelled = true;
            RefreshOriginalSelectors();
            LoadReturnCart();
            ShowInfo("Đã hủy toàn bộ đơn.");
        }
    }

    private async Task<PosResult> SendAsync(PosEvent posEvent, bool addHistory)
    {
        if (_busy)
        {
            return new PosResult { Outcome = PosOutcome.ERROR, Message = "Đang xử lý giao dịch trước." };
        }

        _busy = true;
        _resultPanel.Show(new PosResult { Outcome = PosOutcome.ERROR, Message = "Đang gửi..." });

        try
        {
            var result = await _backend.SendAsync(posEvent);
            _lastEvent = posEvent;
            _resultPanel.Show(result);

            if (addHistory)
            {
                _historyGrid.Add(new TxnRecord
                {
                    Time = DateTime.Now,
                    EventType = posEvent.EventType,
                    TransactionId = posEvent.TransactionId,
                    OriginalTransactionId = posEvent.OriginalTransactionId,
                    StoreId = posEvent.StoreId,
                    PosId = posEvent.PosId,
                    Outcome = result.Outcome,
                    Code = result.Code,
                    LatencyMs = result.LatencyMs,
                    Items = posEvent.Items
                        .Select(item => new PosEventItem
                        {
                            Sku = item.Sku,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        }).ToList()
                });
            }

            RefreshProducts();
            return result;
        }
        finally
        {
            _busy = false;
        }
    }

    internal async Task RetryLastAsync()
    {
        if (_lastEvent is null)
        {
            ShowInfo("Không có giao dịch nào để gửi lại.");
            return;
        }

        // Gửi lại y hệt (cùng transaction_id, cùng nội dung) để backend trả DUPLICATE nếu đã ghi nhận.
        await SendAsync(_lastEvent, addHistory: true);
    }

    private void FocusHistorySelection()
    {
        var record = _historyGrid.SelectedRecord;
        if (record is null)
        {
            return;
        }

        if (record.EventType == "SALE" && _sales.ContainsKey(record.TransactionId))
        {
            _tabs.SelectedIndex = 1;
            _cmbReturnOriginal.SelectedItem = _sales[record.TransactionId];
            LoadReturnCart();
        }
    }

    private void ClearSaleCartAfterSuccess()
    {
        _saleCart.Clear();
        _cartGrid.Clear();
        _saleTransactionId = Guid.NewGuid().ToString();
        _lastEvent = null;
        UpdateSaleTransactionLabel();
    }

    internal async Task RunDevActionAsync(DevAction action)
    {
        switch (action)
        {
            case DevAction.ResetMock:
                ResetMockState();
                break;
            case DevAction.SendDuplicate:
                await SendDuplicateAsync();
                break;
            case DevAction.SendConflict:
                await SendConflictAsync();
                break;
            case DevAction.SendInsufficientStock:
                await SendDevEventAsync(BuildDevSale(999));
                break;
            case DevAction.SendInvalidToken:
            {
                var posEvent = BuildDevSale(1);
                posEvent.Token = "invalid-token";
                await SendDevEventAsync(posEvent);
                break;
            }
            case DevAction.SendUnknownTerminal:
            {
                var posEvent = BuildDevSale(1);
                posEvent.StoreId = "S-99";
                posEvent.PosId = "POS-99";
                await SendDevEventAsync(posEvent);
                break;
            }
            case DevAction.SendUnavailable:
            {
                var posEvent = BuildDevSale(1);
                posEvent.ForceUnavailable = true;
                await SendDevEventAsync(posEvent);
                break;
            }
        }
    }

    private void ResetMockState()
    {
        _backend.Reset();
        _sales.Clear();
        _saleCart.Clear();
        _returnCart.Clear();
        _cartGrid.Clear();
        _returnGrid.Clear();
        _historyGrid.Clear();
        _lastEvent = null;
        _saleTransactionId = Guid.NewGuid().ToString();
        _resultPanel.Clear();
        RefreshOriginalSelectors();
        RefreshProducts();
        UpdateSaleTransactionLabel();
        ShowInfo("Đã reset trạng thái mock.");
    }

    private PosEvent BuildDevSale(int quantity)
    {
        var terminal = _devTools.SelectedTerminal ?? CurrentTerminal;
        var product = _backend.GetProducts().FirstOrDefault() ?? ProductCatalog.CreateProducts().First();

        return new PosEvent
        {
            EventType = "SALE",
            TransactionId = Guid.NewGuid().ToString(),
            StoreId = terminal?.StoreId ?? string.Empty,
            PosId = terminal?.PosId ?? string.Empty,
            Token = terminal?.Token ?? string.Empty,
            Timestamp = DateTime.UtcNow,
            Items = new List<PosEventItem>
            {
                new()
                {
                    Sku = product.Sku,
                    Quantity = quantity,
                    UnitPrice = product.UnitPrice
                }
            }
        };
    }

    private async Task SendDuplicateAsync()
    {
        var posEvent = BuildDevSale(1);
        await SendAsync(posEvent, addHistory: true);
        await SendAsync(posEvent, addHistory: true);
    }

    private async Task SendConflictAsync()
    {
        var posEvent = BuildDevSale(1);
        await SendAsync(posEvent, addHistory: true);

        var changed = CloneEvent(posEvent);
        changed.Items[0].Quantity += 1;
        await SendAsync(changed, addHistory: true);
    }

    private async Task SendDevEventAsync(PosEvent posEvent)
    {
        await SendAsync(posEvent, addHistory: true);
    }

    private static PosEvent CloneEvent(PosEvent source) => new()
    {
        EventType = source.EventType,
        TransactionId = source.TransactionId,
        OriginalTransactionId = source.OriginalTransactionId,
        StoreId = source.StoreId,
        PosId = source.PosId,
        Token = source.Token,
        Timestamp = source.Timestamp,
        ForceUnavailable = source.ForceUnavailable,
        Items = source.Items
            .Select(item => new PosEventItem
            {
                Sku = item.Sku,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
    };

    private void ShowInfo(string message) =>
        _lblStatus.Text = message;

    private static string Short(string id) =>
        string.IsNullOrEmpty(id) ? string.Empty : id.Length <= 8 ? id : id[..8];

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            (_backend as IDisposable)?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }
}
