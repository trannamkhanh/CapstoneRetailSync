#nullable enable

using DemoPos.Controls;
using DemoPos.Models;

namespace DemoPos;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel _rootLayout = null!;
    private TableLayoutPanel _headerLayout = null!;
    private TableLayoutPanel _toolbarLayout = null!;
    private FlowLayoutPanel _headerFlow = null!;
    private FlowLayoutPanel _optionsFlow = null!;
    private Label _lblTitle = null!;
    private Label _lblTerminal = null!;
    private ComboBox _cmbTerminal = null!;
    private Label _lblBaseUrl = null!;
    private TextBox _txtBaseUrl = null!;
    private Button _btnApplyBaseUrl = null!;
    private CheckBox _chkMock = null!;
    private Button _btnDevTools = null!;
    private Label _lblBackendMode = null!;
    private Label _lblStatus = null!;

    private TableLayoutPanel _contentLayout = null!;

    private TableLayoutPanel _mainLayout = null!;
    private TabControl _tabs = null!;
    private TabPage _tabSale = null!;
    private TabPage _tabReturn = null!;
    private TabPage _tabCancel = null!;

    private TableLayoutPanel _saleLayout = null!;
    private TableLayoutPanel _productLayout = null!;
    private TextBox _txtSearch = null!;
    private DataGridView _dgvProducts = null!;
    private Button _btnAddProduct = null!;
    private CartGrid _cartGrid = null!;
    private FlowLayoutPanel _saleActions = null!;
    private Button _btnClearCart = null!;
    private Button _btnSubmitSale = null!;

    private TableLayoutPanel _returnLayout = null!;
    private TableLayoutPanel _returnSide = null!;
    private ComboBox _cmbReturnOriginal = null!;
    private CartGrid _returnGrid = null!;
    private FlowLayoutPanel _returnActions = null!;
    private Button _btnSubmitReturn = null!;

    private TableLayoutPanel _cancelLayout = null!;
    private ComboBox _cmbCancelOriginal = null!;
    private Button _btnSubmitCancel = null!;

    private Panel _sidePanel = null!;
    private ResultPanel _resultPanel = null!;
    private HistoryGrid _historyGrid = null!;

    private DevToolsPanel _devTools = null!;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _rootLayout = new TableLayoutPanel();
        _headerLayout = new TableLayoutPanel();
        _toolbarLayout = new TableLayoutPanel();
        _headerFlow = new FlowLayoutPanel();
        _optionsFlow = new FlowLayoutPanel();
        _lblTitle = new Label();
        _lblTerminal = new Label();
        _cmbTerminal = new ComboBox();
        _lblBaseUrl = new Label();
        _txtBaseUrl = new TextBox();
        _btnApplyBaseUrl = new Button();
        _chkMock = new CheckBox();
        _btnDevTools = new Button();
        _lblBackendMode = new Label();
        _lblStatus = new Label();
        _contentLayout = new TableLayoutPanel();
        _mainLayout = new TableLayoutPanel();
        _tabs = new TabControl();
        _tabSale = new TabPage();
        _tabReturn = new TabPage();
        _tabCancel = new TabPage();
        _saleLayout = new TableLayoutPanel();
        _productLayout = new TableLayoutPanel();
        _txtSearch = new TextBox();
        _dgvProducts = new DataGridView();
        _btnAddProduct = new Button();
        _cartGrid = new CartGrid();
        _saleActions = new FlowLayoutPanel();
        _btnClearCart = new Button();
        _btnSubmitSale = new Button();
        _returnLayout = new TableLayoutPanel();
        _returnSide = new TableLayoutPanel();
        _cmbReturnOriginal = new ComboBox();
        _returnGrid = new CartGrid();
        _returnActions = new FlowLayoutPanel();
        _btnSubmitReturn = new Button();
        _cancelLayout = new TableLayoutPanel();
        _cmbCancelOriginal = new ComboBox();
        _btnSubmitCancel = new Button();
        _sidePanel = new Panel();
        _resultPanel = new ResultPanel();
        _historyGrid = new HistoryGrid();
        _devTools = new DevToolsPanel();

        _tabs.SuspendLayout();
        _tabSale.SuspendLayout();
        _tabReturn.SuspendLayout();
        _tabCancel.SuspendLayout();
        _sidePanel.SuspendLayout();
        SuspendLayout();

        // ---------- Root ----------
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.ColumnCount = 1;
        _rootLayout.RowCount = 2;
        _rootLayout.BackColor = Color.FromArgb(240, 246, 241);
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // ---------- Header ----------
        _headerLayout.Dock = DockStyle.Fill;
        _headerLayout.ColumnCount = 1;
        _headerLayout.RowCount = 2;
        _headerLayout.BackColor = Color.White;
        _headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        _headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

        _toolbarLayout.Dock = DockStyle.Fill;
        _toolbarLayout.ColumnCount = 2;
        _toolbarLayout.RowCount = 1;
        _toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        _toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

        _headerFlow.Dock = DockStyle.Fill;
        _headerFlow.FlowDirection = FlowDirection.LeftToRight;
        _headerFlow.WrapContents = false;
        _headerFlow.Padding = new Padding(12, 8, 12, 8);
        _headerFlow.BackColor = Color.White;

        _optionsFlow.Dock = DockStyle.Fill;
        _optionsFlow.FlowDirection = FlowDirection.LeftToRight;
        _optionsFlow.WrapContents = false;
        _optionsFlow.Padding = new Padding(12, 2, 12, 2);
        _optionsFlow.BackColor = Color.White;

        _lblTitle.Text = "Demo POS Terminal · RetailSync";
        _lblTitle.AutoSize = true;
        _lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _lblTitle.ForeColor = Color.FromArgb(31, 62, 45);
        _lblTitle.Margin = new Padding(0, 6, 16, 0);

        _lblTerminal.Text = "Terminal";
        _lblTerminal.AutoSize = true;
        _lblTerminal.Margin = new Padding(0, 7, 6, 0);
        _lblTerminal.ForeColor = Color.FromArgb(90, 105, 95);

        _cmbTerminal.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbTerminal.Width = 300;
        _cmbTerminal.Margin = new Padding(0, 2, 16, 0);
        _cmbTerminal.Font = new Font("Segoe UI", 10F);

        _lblBaseUrl.Text = "BaseUrl";
        _lblBaseUrl.AutoSize = true;
        _lblBaseUrl.Margin = new Padding(0, 7, 6, 0);
        _lblBaseUrl.ForeColor = Color.FromArgb(90, 105, 95);

        _txtBaseUrl.Width = 210;
        _txtBaseUrl.Margin = new Padding(0, 2, 10, 0);
        _txtBaseUrl.Font = new Font("Consolas", 9.5F);

        _chkMock.Text = "Mock";
        _chkMock.AutoSize = true;
        _chkMock.Margin = new Padding(0, 6, 14, 0);
        _chkMock.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _chkMock.ForeColor = Color.FromArgb(31, 62, 45);

        _btnApplyBaseUrl.Text = "Áp dụng";
        _btnApplyBaseUrl.Width = 88;
        _btnApplyBaseUrl.Height = 30;
        _btnApplyBaseUrl.Margin = new Padding(0, 3, 0, 0);
        _btnApplyBaseUrl.FlatStyle = FlatStyle.Flat;
        _btnApplyBaseUrl.FlatAppearance.BorderSize = 0;
        _btnApplyBaseUrl.BackColor = Color.FromArgb(31, 122, 72);
        _btnApplyBaseUrl.ForeColor = Color.White;
        _btnApplyBaseUrl.Cursor = Cursors.Hand;

        _btnDevTools.Text = "Dev Tools";
        _btnDevTools.Dock = DockStyle.Fill;
        _btnDevTools.Margin = new Padding(4, 6, 12, 6);
        _btnDevTools.FlatStyle = FlatStyle.Flat;
        _btnDevTools.FlatAppearance.BorderSize = 0;
        _btnDevTools.UseVisualStyleBackColor = false;
        _btnDevTools.BackColor = Color.FromArgb(31, 122, 72);
        _btnDevTools.ForeColor = Color.White;
        _btnDevTools.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _btnDevTools.Cursor = Cursors.Hand;

        _lblBackendMode.AutoSize = true;
        _lblBackendMode.Margin = new Padding(0, 4, 16, 0);
        _lblBackendMode.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _lblBackendMode.ForeColor = Color.FromArgb(90, 105, 95);

        _lblStatus.AutoSize = true;
        _lblStatus.Margin = new Padding(0, 4, 0, 0);
        _lblStatus.Font = new Font("Segoe UI", 9.5F);
        _lblStatus.ForeColor = Color.FromArgb(90, 105, 95);
        _lblStatus.Text = "Sẵn sàng · F2 tìm kiếm · F9 gửi · F10 gửi lại · F12 Dev Tools · Esc xóa kết quả";

        _headerFlow.Controls.Add(_lblTitle);
        _headerFlow.Controls.Add(_lblTerminal);
        _headerFlow.Controls.Add(_cmbTerminal);
        _headerFlow.Controls.Add(_lblBaseUrl);
        _headerFlow.Controls.Add(_txtBaseUrl);
        _headerFlow.Controls.Add(_chkMock);
        _headerFlow.Controls.Add(_btnApplyBaseUrl);

        _optionsFlow.Controls.Add(_lblBackendMode);
        _optionsFlow.Controls.Add(_lblStatus);

        _toolbarLayout.Controls.Add(_headerFlow, 0, 0);
        _toolbarLayout.Controls.Add(_btnDevTools, 1, 0);

        _headerLayout.Controls.Add(_toolbarLayout, 0, 0);
        _headerLayout.Controls.Add(_optionsFlow, 0, 1);

        // ---------- Main layout ----------
        _mainLayout.Dock = DockStyle.Fill;
        _mainLayout.ColumnCount = 2;
        _mainLayout.RowCount = 1;
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 500F));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _mainLayout.Padding = new Padding(10, 10, 10, 4);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        _tabs.Padding = new Point(16, 6);

        _tabSale.Text = "Bán hàng";
        _tabReturn.Text = "Trả hàng";
        _tabCancel.Text = "Hủy đơn";
        _tabSale.BackColor = Color.White;
        _tabReturn.BackColor = Color.White;
        _tabCancel.BackColor = Color.White;
        _tabSale.Padding = new Padding(10);
        _tabReturn.Padding = new Padding(10);
        _tabCancel.Padding = new Padding(10);

        _tabs.Controls.Add(_tabSale);
        _tabs.Controls.Add(_tabReturn);
        _tabs.Controls.Add(_tabCancel);
        _mainLayout.Controls.Add(_tabs, 0, 0);

        // ---------- Side panel (kết quả + lịch sử) ----------
        _sidePanel.Dock = DockStyle.Fill;
        _sidePanel.BackColor = Color.White;
        _sidePanel.Padding = new Padding(10, 10, 0, 4);

        _resultPanel.Dock = DockStyle.Top;
        _resultPanel.Height = 144;

        _historyGrid.Dock = DockStyle.Fill;

        _sidePanel.Controls.Add(_historyGrid);
        _sidePanel.Controls.Add(_resultPanel);
        _mainLayout.Controls.Add(_sidePanel, 1, 0);

        // ---------- Dev tools ----------
        _devTools.Dock = DockStyle.Fill;
        _devTools.Visible = false;

        // ---------- Content (giao diện chính + Dev Tools ở đáy) ----------
        _contentLayout.Dock = DockStyle.Fill;
        _contentLayout.ColumnCount = 1;
        _contentLayout.RowCount = 2;
        _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));

        _rootLayout.Controls.Add(_headerLayout, 0, 0);
        _rootLayout.Controls.Add(_contentLayout, 0, 1);
        _contentLayout.Controls.Add(_mainLayout, 0, 0);
        _contentLayout.Controls.Add(_devTools, 0, 1);

        // ---------- Tab Bán hàng ----------
        _saleLayout.Dock = DockStyle.Fill;
        _saleLayout.ColumnCount = 2;
        _saleLayout.RowCount = 1;
        _saleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        _saleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        _saleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _productLayout.Dock = DockStyle.Fill;
        _productLayout.ColumnCount = 1;
        _productLayout.RowCount = 3;
        _productLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _productLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        _productLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _productLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

        _txtSearch.Dock = DockStyle.Fill;
        _txtSearch.Margin = new Padding(0, 4, 0, 6);
        _txtSearch.Font = new Font("Segoe UI", 10F);
        _txtSearch.PlaceholderText = "Tìm theo tên / SKU / barcode (F2)";

        _dgvProducts.Dock = DockStyle.Fill;
        _dgvProducts.AutoGenerateColumns = false;
        _dgvProducts.AllowUserToAddRows = false;
        _dgvProducts.AllowUserToDeleteRows = false;
        _dgvProducts.RowHeadersVisible = false;
        _dgvProducts.ReadOnly = true;
        _dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _dgvProducts.MultiSelect = false;
        _dgvProducts.BackgroundColor = Color.White;
        _dgvProducts.BorderStyle = BorderStyle.None;
        _dgvProducts.EnableHeadersVisualStyles = false;
        _dgvProducts.ShowCellToolTips = true;
        _dgvProducts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _dgvProducts.ColumnHeadersHeight = 32;
        _dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        _dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 62, 45);
        _dgvProducts.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        _dgvProducts.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 62, 45);
        _dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _dgvProducts.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
        _dgvProducts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 234, 220);
        _dgvProducts.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 40, 28);
        _dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "SKU", DataPropertyName = nameof(Product.Sku), Width = 85
        });
        _dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tên", DataPropertyName = nameof(Product.Name),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 60
        });
        _dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Giá", DataPropertyName = nameof(Product.UnitPrice), Width = 70,
            DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tồn", DataPropertyName = nameof(Product.CurrentStock), Width = 50,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
        });

        _btnAddProduct.Text = "Thêm vào giỏ (Enter)";
        _btnAddProduct.Dock = DockStyle.Fill;
        _btnAddProduct.Margin = new Padding(0, 8, 0, 0);
        _btnAddProduct.FlatStyle = FlatStyle.Flat;
        _btnAddProduct.FlatAppearance.BorderSize = 0;
        _btnAddProduct.BackColor = Color.FromArgb(31, 122, 72);
        _btnAddProduct.ForeColor = Color.White;
        _btnAddProduct.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        _btnAddProduct.Cursor = Cursors.Hand;

        _productLayout.Controls.Add(_txtSearch, 0, 0);
        _productLayout.Controls.Add(_dgvProducts, 0, 1);
        _productLayout.Controls.Add(_btnAddProduct, 0, 2);

        _cartGrid.Dock = DockStyle.Fill;

        _saleActions.Dock = DockStyle.Bottom;
        _saleActions.Height = 62;
        _saleActions.FlowDirection = FlowDirection.RightToLeft;
        _saleActions.Padding = new Padding(0, 10, 0, 0);

        _btnSubmitSale.Text = "Thanh toán · Gửi SALE (F9)";
        _btnSubmitSale.Width = 280;
        _btnSubmitSale.Height = 44;
        _btnSubmitSale.Margin = new Padding(8, 0, 0, 0);
        _btnSubmitSale.FlatStyle = FlatStyle.Flat;
        _btnSubmitSale.FlatAppearance.BorderSize = 0;
        _btnSubmitSale.BackColor = Color.FromArgb(31, 122, 72);
        _btnSubmitSale.ForeColor = Color.White;
        _btnSubmitSale.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _btnSubmitSale.Cursor = Cursors.Hand;

        _btnClearCart.Text = "Xóa giỏ";
        _btnClearCart.Width = 110;
        _btnClearCart.Height = 44;
        _btnClearCart.Margin = new Padding(8, 0, 0, 0);
        _btnClearCart.FlatStyle = FlatStyle.Flat;
        _btnClearCart.FlatAppearance.BorderColor = Color.FromArgb(200, 215, 204);
        _btnClearCart.BackColor = Color.White;
        _btnClearCart.ForeColor = Color.FromArgb(31, 62, 45);
        _btnClearCart.Font = new Font("Segoe UI", 10.5F);

        _saleActions.Controls.Add(_btnSubmitSale);
        _saleActions.Controls.Add(_btnClearCart);

        _saleLayout.Controls.Add(_productLayout, 0, 0);
        _saleLayout.Controls.Add(_cartGrid, 1, 0);
        _tabSale.Controls.Add(_saleLayout);
        _tabSale.Controls.Add(_saleActions);

        // ---------- Tab Trả hàng ----------
        _returnLayout.Dock = DockStyle.Fill;
        _returnLayout.ColumnCount = 2;
        _returnLayout.RowCount = 1;
        _returnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
        _returnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _returnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _returnSide.Dock = DockStyle.Fill;
        _returnSide.ColumnCount = 1;
        _returnSide.RowCount = 3;
        _returnSide.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _returnSide.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        _returnSide.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        _returnSide.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _cmbReturnOriginal.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbReturnOriginal.Dock = DockStyle.Fill;
        _cmbReturnOriginal.Margin = new Padding(0, 2, 0, 8);
        _cmbReturnOriginal.Font = new Font("Segoe UI", 10F);

        _returnGrid.Dock = DockStyle.Fill;

        _returnActions.Dock = DockStyle.Bottom;
        _returnActions.Height = 62;
        _returnActions.FlowDirection = FlowDirection.RightToLeft;
        _returnActions.Padding = new Padding(0, 10, 0, 0);

        _btnSubmitReturn.Text = "Xác nhận trả · Gửi RETURN (F9)";
        _btnSubmitReturn.Width = 280;
        _btnSubmitReturn.Height = 44;
        _btnSubmitReturn.Margin = new Padding(8, 0, 0, 0);
        _btnSubmitReturn.FlatStyle = FlatStyle.Flat;
        _btnSubmitReturn.FlatAppearance.BorderSize = 0;
        _btnSubmitReturn.BackColor = Color.FromArgb(31, 122, 72);
        _btnSubmitReturn.ForeColor = Color.White;
        _btnSubmitReturn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _btnSubmitReturn.Cursor = Cursors.Hand;

        _returnActions.Controls.Add(_btnSubmitReturn);

        var returnCaption = new Label
        {
            Text = "Đơn bán gốc (chỉ hiện đơn SALE thành công)",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(90, 105, 95),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        };
        var returnHint = new Label
        {
            Text = "Nhập số lượng còn trả ở cột SL. Tối đa = đã bán − đã trả.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(90, 105, 95),
            Font = new Font("Segoe UI", 9.5F)
        };

        _returnSide.Controls.Add(returnCaption, 0, 0);
        _returnSide.Controls.Add(_cmbReturnOriginal, 0, 1);
        _returnSide.Controls.Add(returnHint, 0, 2);

        _returnLayout.Controls.Add(_returnSide, 0, 0);
        _returnLayout.Controls.Add(_returnGrid, 1, 0);
        _tabReturn.Controls.Add(_returnLayout);
        _tabReturn.Controls.Add(_returnActions);

        // ---------- Tab Hủy đơn ----------
        _cancelLayout.Dock = DockStyle.Fill;
        _cancelLayout.ColumnCount = 1;
        _cancelLayout.RowCount = 4;
        _cancelLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _cancelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        _cancelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        _cancelLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _cancelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));

        _cmbCancelOriginal.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbCancelOriginal.Dock = DockStyle.Fill;
        _cmbCancelOriginal.Margin = new Padding(0, 2, 0, 8);
        _cmbCancelOriginal.Font = new Font("Segoe UI", 10F);

        _btnSubmitCancel.Text = "× Hủy toàn bộ đơn hàng (F9)";
        _btnSubmitCancel.Dock = DockStyle.None;
        _btnSubmitCancel.Size = new Size(300, 44);
        _btnSubmitCancel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _btnSubmitCancel.Margin = new Padding(0, 10, 0, 0);
        _btnSubmitCancel.FlatStyle = FlatStyle.Flat;
        _btnSubmitCancel.FlatAppearance.BorderSize = 0;
        _btnSubmitCancel.BackColor = Color.FromArgb(190, 60, 60);
        _btnSubmitCancel.ForeColor = Color.White;
        _btnSubmitCancel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _btnSubmitCancel.Cursor = Cursors.Hand;

        var cancelCaption = new Label
        {
            Text = "Đơn bán cần hủy (đơn đã hủy sẽ bị ghi nhãn và không chọn được)",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(90, 105, 95),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        };
        var cancelHint = new Label
        {
            Text = "Hủy đơn sẽ hoàn tồn kho toàn bộ sản phẩm của đơn gốc. Không thể hủy hai lần.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(90, 105, 95),
            Font = new Font("Segoe UI", 9.5F)
        };

        _cancelLayout.Controls.Add(cancelCaption, 0, 0);
        _cancelLayout.Controls.Add(_cmbCancelOriginal, 0, 1);
        _cancelLayout.Controls.Add(cancelHint, 0, 2);
        _cancelLayout.Controls.Add(_btnSubmitCancel, 0, 3);
        _tabCancel.Controls.Add(_cancelLayout);

        // ---------- Form ----------
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1440, 900);
        MinimumSize = new Size(1200, 700);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(240, 246, 241);
        Text = "Demo POS Terminal · RetailSync (PB-45)";
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        Controls.Add(_rootLayout);

        _tabs.ResumeLayout(false);
        _tabSale.ResumeLayout(false);
        _tabReturn.ResumeLayout(false);
        _tabCancel.ResumeLayout(false);
        _saleLayout.ResumeLayout(false);
        _productLayout.ResumeLayout(false);
        _returnLayout.ResumeLayout(false);
        _returnSide.ResumeLayout(false);
        _cancelLayout.ResumeLayout(false);
        _sidePanel.ResumeLayout(false);
        _rootLayout.ResumeLayout(false);
        _headerFlow.ResumeLayout(false);
        ResumeLayout(false);
    }
}
