using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DemoPos.Controls;
using DemoPos.Data;
using DemoPos.Models;
using DemoPos.Services;

namespace DemoPos;

public partial class MainForm : Form
{
	private sealed class SaleState
	{
		public string TransactionId { get; set; } = string.Empty;

		public List<PosEventItem> Items { get; set; } = new List<PosEventItem>();

		public Dictionary<string, int> Returned { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		public bool Cancelled { get; set; }

		public string Label => Cancelled ? (Short(TransactionId) + " — Đã hủy") : $"{Short(TransactionId)} — {Items.Count} dòng";
	}

	private const int DevToolsHeight = 190;

	private readonly AppConfig _config;

	private IPosBackend _backend;

	private MockPosBackend? _mockBackend;

	private readonly List<CartItem> _saleCart = new List<CartItem>();

	private readonly List<CartItem> _returnCart = new List<CartItem>();

	private readonly Dictionary<string, SaleState> _sales = new Dictionary<string, SaleState>(StringComparer.OrdinalIgnoreCase);

	private string _saleTransactionId = Guid.NewGuid().ToString();

	private PosEvent? _lastEvent;

	private bool _busy;

	private bool _devToolsOpen;

	private IContainer? components = null;

	private TableLayoutPanel _rootLayout = null;

	private TableLayoutPanel _headerLayout = null;

	private TableLayoutPanel _toolbarLayout = null;

	private FlowLayoutPanel _headerFlow = null;

	private FlowLayoutPanel _optionsFlow = null;

	private Label _lblTitle = null;

	private Label _lblTerminal = null;

	private ComboBox _cmbTerminal = null;

	private Label _lblBaseUrl = null;

	private TextBox _txtBaseUrl = null;

	private Button _btnApplyBaseUrl = null;

	private CheckBox _chkMock = null;

	private Button _btnDevTools = null;

	private Label _lblBackendMode = null;

	private Label _lblStatus = null;

	private TableLayoutPanel _contentLayout = null;

	private TableLayoutPanel _mainLayout = null;

	private TabControl _tabs = null;

	private TabPage _tabSale = null;

	private TabPage _tabReturn = null;

	private TabPage _tabCancel = null;

	private TableLayoutPanel _saleLayout = null;

	private TableLayoutPanel _productLayout = null;

	private TextBox _txtSearch = null;

	private DataGridView _dgvProducts = null;

	private Button _btnAddProduct = null;

	private CartGrid _cartGrid = null;

	private FlowLayoutPanel _saleActions = null;

	private Button _btnClearCart = null;

	private Button _btnSubmitSale = null;

	private TableLayoutPanel _returnLayout = null;

	private TableLayoutPanel _returnSide = null;

	private ComboBox _cmbReturnOriginal = null;

	private CartGrid _returnGrid = null;

	private FlowLayoutPanel _returnActions = null;

	private Button _btnSubmitReturn = null;

	private TableLayoutPanel _cancelLayout = null;

	private ComboBox _cmbCancelOriginal = null;

	private Button _btnSubmitCancel = null;

	private Panel _sidePanel = null;

	private ResultPanel _resultPanel = null;

	private HistoryGrid _historyGrid = null;

	private DevToolsPanel _devTools = null;

	private TerminalConfig? CurrentTerminal => _cmbTerminal.SelectedItem as TerminalConfig;

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

	internal DevToolsPanel DevTools => _devTools;

	internal bool DevToolsOpen => _devToolsOpen;

	internal int DevToolsRowHeight => (int)_contentLayout.RowStyles[1].Height;

	internal Button DevToolsButton => _btnDevTools;

	internal CheckBox MockCheckBox => _chkMock;

	internal TextBox BaseUrlTextBox => _txtBaseUrl;

	internal Button ApplyBaseUrlButton => _btnApplyBaseUrl;

	internal Label BackendModeLabel => _lblBackendMode;

	internal Func<string, DialogResult>? ConfirmCancelOverride { get; set; }

	internal IReadOnlyList<CartItem> SaleCartItems => _saleCart;

	internal IReadOnlyList<CartItem> ReturnCartItems => _returnCart;

	internal string SaleTransactionId => _saleTransactionId;

	public MainForm(AppConfig config, IPosBackend backend)
	{
		_config = config;
		_backend = backend;
		_mockBackend = backend as MockPosBackend;
		InitializeComponent();
		_cmbTerminal.DisplayMember = "DisplayName";
		_cmbTerminal.Items.AddRange(config.Terminals.Cast<object>().ToArray());
		if (_cmbTerminal.Items.Count > 0)
		{
			_cmbTerminal.SelectedIndex = 0;
		}
		_devTools.SetTerminals(config.Terminals);
		_txtBaseUrl.Text = config.BaseUrl;
		_chkMock.Checked = config.BackendMode != BackendMode.Api;
		_chkMock.Enabled = config.BackendMode != BackendMode.SQLite;
		WireEvents();
		RefreshProducts();
		UpdateBackendMode();
		UpdateSaleTransactionLabel();
	}

	private void WireEvents()
	{
		_cmbTerminal.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			OnTerminalChanged();
		};
		_btnApplyBaseUrl.Click += (object? _, EventArgs _) =>
		{
			ApplyBaseUrl();
		};
		_txtSearch.TextChanged += (object? _, EventArgs _) =>
		{
			RefreshProducts();
		};
		_btnAddProduct.Click += (object? _, EventArgs _) =>
		{
			AddSelectedProduct();
		};
		_dgvProducts.CellDoubleClick += (object? _, DataGridViewCellEventArgs _) =>
		{
			AddSelectedProduct();
		};
		_btnClearCart.Click += (object? _, EventArgs _) =>
		{
			ClearSaleCart();
		};
		_btnSubmitSale.Click += async (object? _, EventArgs _) =>
		{
			await SubmitSaleAsync();
		};
		_btnSubmitReturn.Click += async (object? _, EventArgs _) =>
		{
			await SubmitReturnAsync();
		};
		_btnSubmitCancel.Click += async (object? _, EventArgs _) =>
		{
			await SubmitCancellationAsync();
		};
		_cmbReturnOriginal.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			LoadReturnCart();
		};
		_cmbCancelOriginal.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			UpdateCancelState();
		};
		_resultPanel.RetryRequested += async (object? _, EventArgs _) =>
		{
			await RetryLastAsync();
		};
		_chkMock.CheckedChanged += (object? _, EventArgs _) =>
		{
			ApplyMockSelection();
		};
		_btnDevTools.Click += (object? _, EventArgs _) =>
		{
			ToggleDevTools();
		};
		_devTools.ActionRequested += async (object? _, DevAction action) =>
		{
			await RunDevActionAsync(action);
		};
		_historyGrid.DoubleClick += (object? _, EventArgs _) =>
		{
			FocusHistorySelection();
		};
		KeyDown += OnFormKeyDown;
	}

	private void OnFormKeyDown(object? sender, KeyEventArgs e)
	{
		if (HandleShortcut(e.KeyCode))
		{
			e.Handled = true;
		}
	}

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

	private async void RetryLast()
	{
		if (!_resultPanel.RetryEnabled)
		{
			ShowInfo("Không có giao dịch nào để gửi lại.");
		}
		else
		{
			await RetryLastAsync();
		}
	}

	private void OnTerminalChanged()
	{
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
		string text = _txtBaseUrl.Text.Trim().TrimEnd('/');
		_txtBaseUrl.Text = text;
		_config.BaseUrl = text;
		if (_backend is PosApiClient posApiClient)
		{
			posApiClient.BaseUrl = text;
		}
		UpdateBackendMode();
	}

	private void ApplyMockSelection()
	{
		BackendMode backendMode = ((!_chkMock.Checked) ? BackendMode.Api : BackendMode.Mock);
		if (backendMode == _config.BackendMode)
		{
			return;
		}
		ApplyBaseUrl();
		_config.BackendMode = backendMode;
		(_backend as IDisposable)?.Dispose();
		if (backendMode == BackendMode.Mock)
		{
			if (_mockBackend == null)
			{
				_mockBackend = new MockPosBackend(_config);
			}
			_backend = _mockBackend;
		}
		else
		{
			_backend = new PosApiClient(_config, _config.BaseUrl);
		}
		UpdateBackendMode();
		RefreshProducts();
	}

	internal void ToggleDevTools()
	{
		bool flag = (_devToolsOpen = !_devToolsOpen);
		_devTools.Visible = flag;
		_contentLayout.RowStyles[1].Height = (flag ? 190 : 0);
		_btnDevTools.Text = (flag ? "Dev Tools ▲" : "Dev Tools");
		_contentLayout.PerformLayout();
	}

	private void UpdateBackendMode()
	{
		Label lblBackendMode = _lblBackendMode;
		BackendMode backendMode = _config.BackendMode;
		if (1 == 0)
		{
		}
		string text = backendMode switch
		{
			BackendMode.SQLite => "● SQLITE (pos.db)", 
			BackendMode.Api => "● MIDDLEWARE (" + _config.BaseUrl + ")", 
			_ => "● MOCK (in-memory)", 
		};
		if (1 == 0)
		{
		}
		lblBackendMode.Text = text;
		_lblBackendMode.ForeColor = ((_config.BackendMode == BackendMode.Mock) ? Color.FromArgb(21, 101, 60) : Color.FromArgb(31, 122, 72));
	}

	private void UpdateSaleTransactionLabel()
	{
		_btnSubmitSale.Text = "Thanh toán · Gửi SALE (F9)";
	}

	private void RefreshProducts()
	{
		string keyword = _txtSearch.Text.Trim();
		IReadOnlyList<Product> source = _backend.GetProducts();
		if (keyword.Length > 0)
		{
			source = source.Where((Product p) => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) || p.Sku.Contains(keyword, StringComparison.OrdinalIgnoreCase) || p.Barcode.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		_dgvProducts.DataSource = source.ToList();
	}

	private void AddSelectedProduct()
	{
		if (_dgvProducts.CurrentRow?.DataBoundItem is Product product)
		{
			AddProduct(product);
		}
	}

	internal void AddProduct(Product product, int quantity = 1)
	{
		CartItem cartItem = _saleCart.FirstOrDefault((CartItem item) => string.Equals(item.Sku, product.Sku, StringComparison.OrdinalIgnoreCase));
		if (cartItem != null)
		{
			cartItem.Quantity += quantity;
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

	internal Product? FirstProduct()
	{
		return _backend.GetProducts().FirstOrDefault();
	}

	internal void SetReturnQuantity(int index, int quantity)
	{
		if (index >= 0 && index < _returnCart.Count)
		{
			_returnCart[index].Quantity = quantity;
			_returnGrid.SetItems(_returnCart);
		}
	}

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
		TerminalConfig terminal = CurrentTerminal;
		if (terminal == null)
		{
			ShowInfo("Chưa chọn terminal.");
			return;
		}
		PosEvent posEvent = new PosEvent
		{
			EventType = "SALE",
			TransactionId = _saleTransactionId,
			StoreId = terminal.StoreId,
			PosId = terminal.PosId,
			Token = terminal.Token,
			Timestamp = DateTime.UtcNow,
			Items = _saleCart.Select((CartItem item) => new PosEventItem
			{
				Sku = item.Sku,
				Quantity = item.Quantity,
				UnitPrice = item.UnitPrice
			}).ToList()
		};
		PosResult result = await SendAsync(posEvent, addHistory: true);
		if (result.Outcome == PosOutcome.OK)
		{
			RegisterSale(posEvent);
			ClearSaleCartAfterSuccess();
			ShowInfo("Đã ghi nhận giao dịch bán.");
		}
		else if (result.Outcome == PosOutcome.DUPLICATE)
		{
			ClearSaleCartAfterSuccess();
			ShowInfo("Giao dịch đã được ghi nhận trước đó (DUPLICATE).");
		}
	}

	private void RegisterSale(PosEvent posEvent)
	{
		_sales[posEvent.TransactionId] = new SaleState
		{
			TransactionId = posEvent.TransactionId,
			Items = posEvent.Items.Select((PosEventItem item) => new PosEventItem
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
		string currentReturn = (_cmbReturnOriginal.SelectedItem as SaleState)?.TransactionId;
		string currentCancel = (_cmbCancelOriginal.SelectedItem as SaleState)?.TransactionId;
		_cmbReturnOriginal.Items.Clear();
		_cmbCancelOriginal.Items.Clear();
		foreach (SaleState value in _sales.Values)
		{
			_cmbReturnOriginal.Items.Add(value);
			_cmbCancelOriginal.Items.Add(value);
		}
		_cmbReturnOriginal.DisplayMember = "Label";
		_cmbCancelOriginal.DisplayMember = "Label";
		if (currentReturn != null)
		{
			_cmbReturnOriginal.SelectedItem = _sales.Values.FirstOrDefault((SaleState s) => s.TransactionId == currentReturn);
		}
		else if (_cmbReturnOriginal.Items.Count > 0)
		{
			_cmbReturnOriginal.SelectedIndex = 0;
		}
		if (currentCancel != null)
		{
			_cmbCancelOriginal.SelectedItem = _sales.Values.FirstOrDefault((SaleState s) => s.TransactionId == currentCancel);
		}
		else if (_cmbCancelOriginal.Items.Count > 0)
		{
			_cmbCancelOriginal.SelectedIndex = 0;
		}
	}

	private void LoadReturnCart()
	{
		_returnCart.Clear();
		if (_cmbReturnOriginal.SelectedItem is SaleState saleState)
		{
			IReadOnlyList<Product> products = _backend.GetProducts();
			foreach (PosEventItem item in saleState.Items)
			{
				int num = (saleState.Returned.TryGetValue(item.Sku, out var value) ? value : 0);
				int value2 = Math.Max(0, item.Quantity - num);
				_returnCart.Add(new CartItem
				{
					Sku = item.Sku,
					Name = (products.FirstOrDefault((Product p) => p.Sku == item.Sku)?.Name ?? item.Sku),
					UnitPrice = item.UnitPrice,
					Quantity = 0,
					SoldQuantity = item.Quantity,
					ReturnedQuantity = num,
					MaxReturnQuantity = value2
				});
			}
		}
		_returnGrid.SetItems(_returnCart);
	}

	private void UpdateCancelState()
	{
		bool flag = _cmbCancelOriginal.SelectedItem is SaleState saleState && saleState.Cancelled;
		_btnSubmitCancel.Enabled = !flag;
		_btnSubmitCancel.Text = (flag ? "× Đơn này đã được hủy" : "× Hủy toàn bộ đơn hàng (F9)");
	}

	internal async Task SubmitReturnAsync()
	{
		object selectedItem = _cmbReturnOriginal.SelectedItem;
		if (!(selectedItem is SaleState sale))
		{
			ShowInfo("Chọn đơn bán gốc trước khi trả hàng.");
			return;
		}
		List<PosEventItem> items = (from cartItem in _returnCart
			where cartItem.Quantity > 0
			select new PosEventItem
			{
				Sku = cartItem.Sku,
				Quantity = Math.Min(cartItem.Quantity, cartItem.MaxReturnQuantity ?? cartItem.Quantity),
				UnitPrice = cartItem.UnitPrice
			}).ToList();
		if (items.Count == 0)
		{
			ShowInfo("Chọn đơn gốc và ít nhất một sản phẩm cần trả.");
			return;
		}
		TerminalConfig terminal = CurrentTerminal;
		if (terminal == null)
		{
			ShowInfo("Chưa chọn terminal.");
			return;
		}
		PosEvent posEvent = new PosEvent
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
		if ((await SendAsync(posEvent, addHistory: true)).Outcome != PosOutcome.OK)
		{
			return;
		}
		foreach (PosEventItem item in items)
		{
			int returned = (sale.Returned.TryGetValue(item.Sku, out var value) ? value : 0);
			sale.Returned[item.Sku] = returned + item.Quantity;
		}
		LoadReturnCart();
		ShowInfo("Đã ghi nhận giao dịch trả.");
	}

	internal async Task SubmitCancellationAsync()
	{
		object selectedItem = _cmbCancelOriginal.SelectedItem;
		if (!(selectedItem is SaleState sale))
		{
			ShowInfo("Chọn đơn bán cần hủy.");
			return;
		}
		if (sale.Cancelled)
		{
			ShowInfo("Đơn này đã được hủy trước đó.");
			return;
		}
		TerminalConfig terminal = CurrentTerminal;
		if (terminal == null)
		{
			ShowInfo("Chưa chọn terminal.");
			return;
		}
		DialogResult confirmation = ConfirmCancelOverride?.Invoke("Hủy toàn bộ đơn " + Short(sale.TransactionId) + "? Tồn kho sẽ được hoàn lại.") ?? MessageBox.Show("Hủy toàn bộ đơn " + Short(sale.TransactionId) + "? Tồn kho sẽ được hoàn lại.", "Xác nhận hủy đơn", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
		if (confirmation == DialogResult.Yes)
		{
			PosEvent posEvent = new PosEvent
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
			if ((await SendAsync(posEvent, addHistory: true)).Outcome == PosOutcome.OK)
			{
				sale.Cancelled = true;
				RefreshOriginalSelectors();
				LoadReturnCart();
				ShowInfo("Đã hủy toàn bộ đơn.");
			}
		}
	}

	private async Task<PosResult> SendAsync(PosEvent posEvent, bool addHistory)
	{
		if (_busy)
		{
			return new PosResult
			{
				Outcome = PosOutcome.ERROR,
				Message = "Đang xử lý giao dịch trước."
			};
		}
		_busy = true;
		_resultPanel.Show(new PosResult
		{
			Outcome = PosOutcome.ERROR,
			Message = "Đang gửi..."
		});
		try
		{
			PosResult result = await _backend.SendAsync(posEvent);
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
					Items = posEvent.Items.Select((PosEventItem item) => new PosEventItem
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
		if (_lastEvent == null)
		{
			ShowInfo("Không có giao dịch nào để gửi lại.");
		}
		else
		{
			await SendAsync(_lastEvent, addHistory: true);
		}
	}

	private void FocusHistorySelection()
	{
		TxnRecord selectedRecord = _historyGrid.SelectedRecord;
		if (selectedRecord != null && selectedRecord.EventType == "SALE" && _sales.ContainsKey(selectedRecord.TransactionId))
		{
			_tabs.SelectedIndex = 1;
			_cmbReturnOriginal.SelectedItem = _sales[selectedRecord.TransactionId];
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
			PosEvent posEvent3 = BuildDevSale(1);
			posEvent3.Token = "invalid-token";
			await SendDevEventAsync(posEvent3);
			break;
		}
		case DevAction.SendUnknownTerminal:
		{
			PosEvent posEvent2 = BuildDevSale(1);
			posEvent2.StoreId = "S-99";
			posEvent2.PosId = "POS-99";
			await SendDevEventAsync(posEvent2);
			break;
		}
		case DevAction.SendUnavailable:
		{
			PosEvent posEvent = BuildDevSale(1);
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
		ShowInfo((_config.BackendMode == BackendMode.SQLite) ? "Đã reset database SQLite (pos.db)." : "Đã reset trạng thái mock.");
	}

	private PosEvent BuildDevSale(int quantity)
	{
		TerminalConfig terminalConfig = _devTools.SelectedTerminal ?? CurrentTerminal;
		Product product = _backend.GetProducts().FirstOrDefault() ?? ProductCatalog.CreateProducts().First();
		return new PosEvent
		{
			EventType = "SALE",
			TransactionId = Guid.NewGuid().ToString(),
			StoreId = (terminalConfig?.StoreId ?? string.Empty),
			PosId = (terminalConfig?.PosId ?? string.Empty),
			Token = (terminalConfig?.Token ?? string.Empty),
			Timestamp = DateTime.UtcNow,
			Items = new List<PosEventItem>
			{
				new PosEventItem
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
		PosEvent posEvent = BuildDevSale(1);
		await SendAsync(posEvent, addHistory: true);
		await SendAsync(posEvent, addHistory: true);
	}

	private async Task SendConflictAsync()
	{
		PosEvent posEvent = BuildDevSale(1);
		await SendAsync(posEvent, addHistory: true);
		PosEvent changed = CloneEvent(posEvent);
		changed.Items[0].Quantity++;
		await SendAsync(changed, addHistory: true);
	}

	private async Task SendDevEventAsync(PosEvent posEvent)
	{
		await SendAsync(posEvent, addHistory: true);
	}

	private static PosEvent CloneEvent(PosEvent source)
	{
		return new PosEvent
		{
			EventType = source.EventType,
			TransactionId = source.TransactionId,
			OriginalTransactionId = source.OriginalTransactionId,
			StoreId = source.StoreId,
			PosId = source.PosId,
			Token = source.Token,
			Timestamp = source.Timestamp,
			ForceUnavailable = source.ForceUnavailable,
			Items = source.Items.Select((PosEventItem item) => new PosEventItem
			{
				Sku = item.Sku,
				Quantity = item.Quantity,
				UnitPrice = item.UnitPrice
			}).ToList()
		};
	}

	private void ShowInfo(string message)
	{
		_lblStatus.Text = message;
	}

	private static string Short(string id)
	{
		string result;
		if (string.IsNullOrEmpty(id))
		{
			result = string.Empty;
		}
		else
		{
			result = ((id.Length <= 8) ? id : id.Substring(0, 8));
		}
		return result;
	}

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

