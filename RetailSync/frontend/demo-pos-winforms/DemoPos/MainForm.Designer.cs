namespace DemoPos;

partial class MainForm
{
	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this._rootLayout = new System.Windows.Forms.TableLayoutPanel();
		this._headerLayout = new System.Windows.Forms.TableLayoutPanel();
		this._toolbarLayout = new System.Windows.Forms.TableLayoutPanel();
		this._headerFlow = new System.Windows.Forms.FlowLayoutPanel();
		this._optionsFlow = new System.Windows.Forms.FlowLayoutPanel();
		this._lblTitle = new System.Windows.Forms.Label();
		this._lblTerminal = new System.Windows.Forms.Label();
		this._cmbTerminal = new System.Windows.Forms.ComboBox();
		this._lblBaseUrl = new System.Windows.Forms.Label();
		this._txtBaseUrl = new System.Windows.Forms.TextBox();
		this._btnApplyBaseUrl = new System.Windows.Forms.Button();
		this._chkMock = new System.Windows.Forms.CheckBox();
		this._btnDevTools = new System.Windows.Forms.Button();
		this._lblBackendMode = new System.Windows.Forms.Label();
		this._lblStatus = new System.Windows.Forms.Label();
		this._contentLayout = new System.Windows.Forms.TableLayoutPanel();
		this._mainLayout = new System.Windows.Forms.TableLayoutPanel();
		this._tabs = new System.Windows.Forms.TabControl();
		this._tabSale = new System.Windows.Forms.TabPage();
		this._tabReturn = new System.Windows.Forms.TabPage();
		this._tabCancel = new System.Windows.Forms.TabPage();
		this._saleLayout = new System.Windows.Forms.TableLayoutPanel();
		this._productLayout = new System.Windows.Forms.TableLayoutPanel();
		this._txtSearch = new System.Windows.Forms.TextBox();
		this._dgvProducts = new System.Windows.Forms.DataGridView();
		this._btnAddProduct = new System.Windows.Forms.Button();
		this._cartGrid = new DemoPos.Controls.CartGrid();
		this._saleActions = new System.Windows.Forms.FlowLayoutPanel();
		this._btnClearCart = new System.Windows.Forms.Button();
		this._btnSubmitSale = new System.Windows.Forms.Button();
		this._returnLayout = new System.Windows.Forms.TableLayoutPanel();
		this._returnSide = new System.Windows.Forms.TableLayoutPanel();
		this._cmbReturnOriginal = new System.Windows.Forms.ComboBox();
		this._returnGrid = new DemoPos.Controls.CartGrid();
		this._returnActions = new System.Windows.Forms.FlowLayoutPanel();
		this._btnSubmitReturn = new System.Windows.Forms.Button();
		this._cancelLayout = new System.Windows.Forms.TableLayoutPanel();
		this._cmbCancelOriginal = new System.Windows.Forms.ComboBox();
		this._btnSubmitCancel = new System.Windows.Forms.Button();
		this._sidePanel = new System.Windows.Forms.Panel();
		this._resultPanel = new DemoPos.Controls.ResultPanel();
		this._historyGrid = new DemoPos.Controls.HistoryGrid();
		this._devTools = new DemoPos.Controls.DevToolsPanel();
		this._tabs.SuspendLayout();
		this._tabSale.SuspendLayout();
		this._tabReturn.SuspendLayout();
		this._tabCancel.SuspendLayout();
		this._sidePanel.SuspendLayout();
		base.SuspendLayout();
		this._rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this._rootLayout.ColumnCount = 1;
		this._rootLayout.RowCount = 2;
		this._rootLayout.BackColor = System.Drawing.Color.FromArgb(240, 246, 241);
		this._rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80f));
		this._rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._headerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this._headerLayout.ColumnCount = 1;
		this._headerLayout.RowCount = 2;
		this._headerLayout.BackColor = System.Drawing.Color.White;
		this._headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._headerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48f));
		this._headerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32f));
		this._toolbarLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this._toolbarLayout.ColumnCount = 2;
		this._toolbarLayout.RowCount = 1;
		this._toolbarLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._toolbarLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120f));
		this._toolbarLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48f));
		this._headerFlow.Dock = System.Windows.Forms.DockStyle.Fill;
		this._headerFlow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
		this._headerFlow.WrapContents = false;
		this._headerFlow.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
		this._headerFlow.BackColor = System.Drawing.Color.White;
		this._optionsFlow.Dock = System.Windows.Forms.DockStyle.Fill;
		this._optionsFlow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
		this._optionsFlow.WrapContents = false;
		this._optionsFlow.Padding = new System.Windows.Forms.Padding(12, 2, 12, 2);
		this._optionsFlow.BackColor = System.Drawing.Color.White;
		this._lblTitle.Text = "Demo POS Terminal · RetailSync";
		this._lblTitle.AutoSize = true;
		this._lblTitle.Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold);
		this._lblTitle.ForeColor = System.Drawing.Color.FromArgb(31, 62, 45);
		this._lblTitle.Margin = new System.Windows.Forms.Padding(0, 6, 16, 0);
		this._lblTerminal.Text = "Terminal";
		this._lblTerminal.AutoSize = true;
		this._lblTerminal.Margin = new System.Windows.Forms.Padding(0, 7, 6, 0);
		this._lblTerminal.ForeColor = System.Drawing.Color.FromArgb(90, 105, 95);
		this._cmbTerminal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._cmbTerminal.Width = 300;
		this._cmbTerminal.Margin = new System.Windows.Forms.Padding(0, 2, 16, 0);
		this._cmbTerminal.Font = new System.Drawing.Font("Segoe UI", 10f);
		this._lblBaseUrl.Text = "BaseUrl";
		this._lblBaseUrl.AutoSize = true;
		this._lblBaseUrl.Margin = new System.Windows.Forms.Padding(0, 7, 6, 0);
		this._lblBaseUrl.ForeColor = System.Drawing.Color.FromArgb(90, 105, 95);
		this._txtBaseUrl.Width = 210;
		this._txtBaseUrl.Margin = new System.Windows.Forms.Padding(0, 2, 10, 0);
		this._txtBaseUrl.Font = new System.Drawing.Font("Consolas", 9.5f);
		this._chkMock.Text = "Mock";
		this._chkMock.AutoSize = true;
		this._chkMock.Margin = new System.Windows.Forms.Padding(0, 6, 14, 0);
		this._chkMock.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
		this._chkMock.ForeColor = System.Drawing.Color.FromArgb(31, 62, 45);
		this._btnApplyBaseUrl.Text = "Áp dụng";
		this._btnApplyBaseUrl.Width = 88;
		this._btnApplyBaseUrl.Height = 30;
		this._btnApplyBaseUrl.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
		this._btnApplyBaseUrl.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this._btnApplyBaseUrl.FlatAppearance.BorderSize = 0;
		this._btnApplyBaseUrl.BackColor = System.Drawing.Color.FromArgb(31, 122, 72);
		this._btnApplyBaseUrl.ForeColor = System.Drawing.Color.White;
		this._btnApplyBaseUrl.Cursor = System.Windows.Forms.Cursors.Hand;
		this._btnDevTools.Text = "Dev Tools";
		this._btnDevTools.Dock = System.Windows.Forms.DockStyle.Fill;
		this._btnDevTools.Margin = new System.Windows.Forms.Padding(4, 6, 12, 6);
		this._btnDevTools.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this._btnDevTools.FlatAppearance.BorderSize = 0;
		this._btnDevTools.UseVisualStyleBackColor = false;
		this._btnDevTools.BackColor = System.Drawing.Color.FromArgb(31, 122, 72);
		this._btnDevTools.ForeColor = System.Drawing.Color.White;
		this._btnDevTools.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
		this._btnDevTools.Cursor = System.Windows.Forms.Cursors.Hand;
		this._lblBackendMode.AutoSize = true;
		this._lblBackendMode.Margin = new System.Windows.Forms.Padding(0, 4, 16, 0);
		this._lblBackendMode.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
		this._lblBackendMode.ForeColor = System.Drawing.Color.FromArgb(90, 105, 95);
		this._lblStatus.AutoSize = true;
		this._lblStatus.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
		this._lblStatus.Font = new System.Drawing.Font("Segoe UI", 9.5f);
		this._lblStatus.ForeColor = System.Drawing.Color.FromArgb(90, 105, 95);
		this._lblStatus.Text = "Sẵn sàng · F2 tìm kiếm · F9 gửi · F10 gửi lại · F12 Dev Tools · Esc xóa kết quả";
		this._headerFlow.Controls.Add(this._lblTitle);
		this._headerFlow.Controls.Add(this._lblTerminal);
		this._headerFlow.Controls.Add(this._cmbTerminal);
		this._headerFlow.Controls.Add(this._lblBaseUrl);
		this._headerFlow.Controls.Add(this._txtBaseUrl);
		this._headerFlow.Controls.Add(this._chkMock);
		this._headerFlow.Controls.Add(this._btnApplyBaseUrl);
		this._optionsFlow.Controls.Add(this._lblBackendMode);
		this._optionsFlow.Controls.Add(this._lblStatus);
		this._toolbarLayout.Controls.Add(this._headerFlow, 0, 0);
		this._toolbarLayout.Controls.Add(this._btnDevTools, 1, 0);
		this._headerLayout.Controls.Add(this._toolbarLayout, 0, 0);
		this._headerLayout.Controls.Add(this._optionsFlow, 0, 1);
		this._mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this._mainLayout.ColumnCount = 2;
		this._mainLayout.RowCount = 1;
		this._mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 500f));
		this._mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._mainLayout.Padding = new System.Windows.Forms.Padding(10, 10, 10, 4);
		this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
		this._tabs.Font = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold);
		this._tabs.Padding = new System.Drawing.Point(16, 6);
		this._tabSale.Text = "Bán hàng";
		this._tabReturn.Text = "Trả hàng";
		this._tabCancel.Text = "Hủy đơn";
		this._tabSale.BackColor = System.Drawing.Color.White;
		this._tabReturn.BackColor = System.Drawing.Color.White;
		this._tabCancel.BackColor = System.Drawing.Color.White;
		this._tabSale.Padding = new System.Windows.Forms.Padding(10);
		this._tabReturn.Padding = new System.Windows.Forms.Padding(10);
		this._tabCancel.Padding = new System.Windows.Forms.Padding(10);
		this._tabs.Controls.Add(this._tabSale);
		this._tabs.Controls.Add(this._tabReturn);
		this._tabs.Controls.Add(this._tabCancel);
		this._mainLayout.Controls.Add(this._tabs, 0, 0);
		this._sidePanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this._sidePanel.BackColor = System.Drawing.Color.White;
		this._sidePanel.Padding = new System.Windows.Forms.Padding(10, 10, 0, 4);
		this._resultPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this._resultPanel.Height = 144;
		this._historyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
		this._sidePanel.Controls.Add(this._historyGrid);
		this._sidePanel.Controls.Add(this._resultPanel);
		this._mainLayout.Controls.Add(this._sidePanel, 1, 0);
		this._devTools.Dock = System.Windows.Forms.DockStyle.Fill;
		this._devTools.Visible = false;
		this._contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this._contentLayout.ColumnCount = 1;
		this._contentLayout.RowCount = 2;
		this._contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 0f));
		this._rootLayout.Controls.Add(this._headerLayout, 0, 0);
		this._rootLayout.Controls.Add(this._contentLayout, 0, 1);
		this._contentLayout.Controls.Add(this._mainLayout, 0, 0);
		this._contentLayout.Controls.Add(this._devTools, 0, 1);
		this._saleLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this._saleLayout.ColumnCount = 2;
		this._saleLayout.RowCount = 1;
		this._saleLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40f));
		this._saleLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60f));
		this._saleLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._productLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this._productLayout.ColumnCount = 1;
		this._productLayout.RowCount = 3;
		this._productLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._productLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40f));
		this._productLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._productLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52f));
		this._txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
		this._txtSearch.Margin = new System.Windows.Forms.Padding(0, 4, 0, 6);
		this._txtSearch.Font = new System.Drawing.Font("Segoe UI", 10f);
		this._txtSearch.PlaceholderText = "Tìm theo tên / SKU / barcode (F2)";
		this._dgvProducts.Dock = System.Windows.Forms.DockStyle.Fill;
		this._dgvProducts.AutoGenerateColumns = false;
		this._dgvProducts.AllowUserToAddRows = false;
		this._dgvProducts.AllowUserToDeleteRows = false;
		this._dgvProducts.RowHeadersVisible = false;
		this._dgvProducts.ReadOnly = true;
		this._dgvProducts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this._dgvProducts.MultiSelect = false;
		this._dgvProducts.BackgroundColor = System.Drawing.Color.White;
		this._dgvProducts.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this._dgvProducts.EnableHeadersVisualStyles = false;
		this._dgvProducts.ShowCellToolTips = true;
		this._dgvProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
		this._dgvProducts.ColumnHeadersHeight = 32;
		this._dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.White;
		this._dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(31, 62, 45);
		this._dgvProducts.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.White;
		this._dgvProducts.ColumnHeadersDefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(31, 62, 45);
		this._dgvProducts.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
		this._dgvProducts.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9.5f);
		this._dgvProducts.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(214, 234, 220);
		this._dgvProducts.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(20, 40, 28);
		this._dgvProducts.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
		{
			HeaderText = "SKU",
			DataPropertyName = "Sku",
			Width = 85
		});
		this._dgvProducts.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
		{
			HeaderText = "Tên",
			DataPropertyName = "Name",
			AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill,
			MinimumWidth = 60
		});
		this._dgvProducts.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
		{
			HeaderText = "Giá",
			DataPropertyName = "UnitPrice",
			Width = 70,
			DefaultCellStyle = 
			{
				Format = "N0",
				Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight
			}
		});
		this._dgvProducts.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
		{
			HeaderText = "Tồn",
			DataPropertyName = "CurrentStock",
			Width = 50,
			DefaultCellStyle = 
			{
				Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight
			}
		});
		this._btnAddProduct.Text = "Thêm vào giỏ (Enter)";
		this._btnAddProduct.Dock = System.Windows.Forms.DockStyle.Fill;
		this._btnAddProduct.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
		this._btnAddProduct.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this._btnAddProduct.FlatAppearance.BorderSize = 0;
		this._btnAddProduct.BackColor = System.Drawing.Color.FromArgb(31, 122, 72);
		this._btnAddProduct.ForeColor = System.Drawing.Color.White;
		this._btnAddProduct.Font = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold);
		this._btnAddProduct.Cursor = System.Windows.Forms.Cursors.Hand;
		this._productLayout.Controls.Add(this._txtSearch, 0, 0);
		this._productLayout.Controls.Add(this._dgvProducts, 0, 1);
		this._productLayout.Controls.Add(this._btnAddProduct, 0, 2);
		this._cartGrid.Dock = System.Windows.Forms.DockStyle.Fill;
		this._saleActions.Dock = System.Windows.Forms.DockStyle.Bottom;
		this._saleActions.Height = 62;
		this._saleActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
		this._saleActions.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
		this._btnSubmitSale.Text = "Thanh toán · Gửi SALE (F9)";
		this._btnSubmitSale.Width = 280;
		this._btnSubmitSale.Height = 44;
		this._btnSubmitSale.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
		this._btnSubmitSale.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this._btnSubmitSale.FlatAppearance.BorderSize = 0;
		this._btnSubmitSale.BackColor = System.Drawing.Color.FromArgb(31, 122, 72);
		this._btnSubmitSale.ForeColor = System.Drawing.Color.White;
		this._btnSubmitSale.Font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
		this._btnSubmitSale.Cursor = System.Windows.Forms.Cursors.Hand;
		this._btnClearCart.Text = "Xóa giỏ";
		this._btnClearCart.Width = 110;
		this._btnClearCart.Height = 44;
		this._btnClearCart.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
		this._btnClearCart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this._btnClearCart.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 215, 204);
		this._btnClearCart.BackColor = System.Drawing.Color.White;
		this._btnClearCart.ForeColor = System.Drawing.Color.FromArgb(31, 62, 45);
		this._btnClearCart.Font = new System.Drawing.Font("Segoe UI", 10.5f);
		this._saleActions.Controls.Add(this._btnSubmitSale);
		this._saleActions.Controls.Add(this._btnClearCart);
		this._saleLayout.Controls.Add(this._productLayout, 0, 0);
		this._saleLayout.Controls.Add(this._cartGrid, 1, 0);
		this._tabSale.Controls.Add(this._saleLayout);
		this._tabSale.Controls.Add(this._saleActions);
		this._returnLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this._returnLayout.ColumnCount = 2;
		this._returnLayout.RowCount = 1;
		this._returnLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 320f));
		this._returnLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._returnLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._returnSide.Dock = System.Windows.Forms.DockStyle.Fill;
		this._returnSide.ColumnCount = 1;
		this._returnSide.RowCount = 3;
		this._returnSide.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._returnSide.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26f));
		this._returnSide.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44f));
		this._returnSide.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._cmbReturnOriginal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._cmbReturnOriginal.Dock = System.Windows.Forms.DockStyle.Fill;
		this._cmbReturnOriginal.Margin = new System.Windows.Forms.Padding(0, 2, 0, 8);
		this._cmbReturnOriginal.Font = new System.Drawing.Font("Segoe UI", 10f);
		this._returnGrid.Dock = System.Windows.Forms.DockStyle.Fill;
		this._returnActions.Dock = System.Windows.Forms.DockStyle.Bottom;
		this._returnActions.Height = 62;
		this._returnActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
		this._returnActions.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
		this._btnSubmitReturn.Text = "Xác nhận trả · Gửi RETURN (F9)";
		this._btnSubmitReturn.Width = 280;
		this._btnSubmitReturn.Height = 44;
		this._btnSubmitReturn.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
		this._btnSubmitReturn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this._btnSubmitReturn.FlatAppearance.BorderSize = 0;
		this._btnSubmitReturn.BackColor = System.Drawing.Color.FromArgb(31, 122, 72);
		this._btnSubmitReturn.ForeColor = System.Drawing.Color.White;
		this._btnSubmitReturn.Font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
		this._btnSubmitReturn.Cursor = System.Windows.Forms.Cursors.Hand;
		this._returnActions.Controls.Add(this._btnSubmitReturn);
		System.Windows.Forms.Label control = new System.Windows.Forms.Label
		{
			Text = "Đơn bán gốc (chỉ hiện đơn SALE thành công)",
			Dock = System.Windows.Forms.DockStyle.Fill,
			TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
			ForeColor = System.Drawing.Color.FromArgb(90, 105, 95),
			Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold)
		};
		System.Windows.Forms.Label control2 = new System.Windows.Forms.Label
		{
			Text = "Nhập số lượng còn trả ở cột SL. Tối đa = đã bán − đã trả.",
			Dock = System.Windows.Forms.DockStyle.Fill,
			ForeColor = System.Drawing.Color.FromArgb(90, 105, 95),
			Font = new System.Drawing.Font("Segoe UI", 9.5f)
		};
		this._returnSide.Controls.Add(control, 0, 0);
		this._returnSide.Controls.Add(this._cmbReturnOriginal, 0, 1);
		this._returnSide.Controls.Add(control2, 0, 2);
		this._returnLayout.Controls.Add(this._returnSide, 0, 0);
		this._returnLayout.Controls.Add(this._returnGrid, 1, 0);
		this._tabReturn.Controls.Add(this._returnLayout);
		this._tabReturn.Controls.Add(this._returnActions);
		this._cancelLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this._cancelLayout.ColumnCount = 1;
		this._cancelLayout.RowCount = 4;
		this._cancelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._cancelLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26f));
		this._cancelLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44f));
		this._cancelLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this._cancelLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62f));
		this._cmbCancelOriginal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._cmbCancelOriginal.Dock = System.Windows.Forms.DockStyle.Fill;
		this._cmbCancelOriginal.Margin = new System.Windows.Forms.Padding(0, 2, 0, 8);
		this._cmbCancelOriginal.Font = new System.Drawing.Font("Segoe UI", 10f);
		this._btnSubmitCancel.Text = "× Hủy toàn bộ đơn hàng (F9)";
		this._btnSubmitCancel.Dock = System.Windows.Forms.DockStyle.None;
		this._btnSubmitCancel.Size = new System.Drawing.Size(300, 44);
		this._btnSubmitCancel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
		this._btnSubmitCancel.Margin = new System.Windows.Forms.Padding(0, 10, 0, 0);
		this._btnSubmitCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this._btnSubmitCancel.FlatAppearance.BorderSize = 0;
		this._btnSubmitCancel.BackColor = System.Drawing.Color.FromArgb(190, 60, 60);
		this._btnSubmitCancel.ForeColor = System.Drawing.Color.White;
		this._btnSubmitCancel.Font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
		this._btnSubmitCancel.Cursor = System.Windows.Forms.Cursors.Hand;
		System.Windows.Forms.Label control3 = new System.Windows.Forms.Label
		{
			Text = "Đơn bán cần hủy (đơn đã hủy sẽ bị ghi nhãn và không chọn được)",
			Dock = System.Windows.Forms.DockStyle.Fill,
			TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
			ForeColor = System.Drawing.Color.FromArgb(90, 105, 95),
			Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold)
		};
		System.Windows.Forms.Label control4 = new System.Windows.Forms.Label
		{
			Text = "Hủy đơn sẽ hoàn tồn kho toàn bộ sản phẩm của đơn gốc. Không thể hủy hai lần.",
			Dock = System.Windows.Forms.DockStyle.Fill,
			ForeColor = System.Drawing.Color.FromArgb(90, 105, 95),
			Font = new System.Drawing.Font("Segoe UI", 9.5f)
		};
		this._cancelLayout.Controls.Add(control3, 0, 0);
		this._cancelLayout.Controls.Add(this._cmbCancelOriginal, 0, 1);
		this._cancelLayout.Controls.Add(control4, 0, 2);
		this._cancelLayout.Controls.Add(this._btnSubmitCancel, 0, 3);
		this._tabCancel.Controls.Add(this._cancelLayout);
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 17f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(1440, 900);
		this.MinimumSize = new System.Drawing.Size(1200, 700);
		this.Font = new System.Drawing.Font("Segoe UI", 10f);
		this.BackColor = System.Drawing.Color.FromArgb(240, 246, 241);
		this.Text = "Demo POS Terminal · RetailSync (PB-45)";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		base.KeyPreview = true;
		base.Controls.Add(this._rootLayout);
		this._tabs.ResumeLayout(false);
		this._tabSale.ResumeLayout(false);
		this._tabReturn.ResumeLayout(false);
		this._tabCancel.ResumeLayout(false);
		this._saleLayout.ResumeLayout(false);
		this._productLayout.ResumeLayout(false);
		this._returnLayout.ResumeLayout(false);
		this._returnSide.ResumeLayout(false);
		this._cancelLayout.ResumeLayout(false);
		this._sidePanel.ResumeLayout(false);
		this._rootLayout.ResumeLayout(false);
		this._headerFlow.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
