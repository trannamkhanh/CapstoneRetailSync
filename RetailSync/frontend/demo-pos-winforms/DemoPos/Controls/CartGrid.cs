using System.ComponentModel;
using System.Globalization;
using DemoPos.Models;

namespace DemoPos.Controls;

/// <summary>Bảng giỏ hàng: hiển thị và cho phép sửa số lượng từng dòng.</summary>
public sealed class CartGrid : UserControl
{
    private readonly DataGridView _grid = new();
    private readonly BindingList<CartItem> _items = new();
    private readonly Label _totalLabel = new();

    public CartGrid()
    {
        Dock = DockStyle.Fill;

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.ColumnHeadersHeight = 36;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 246, 241);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 62, 45);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10f);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 234, 220);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 40, 28);
        _grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Sku", HeaderText = "SKU", DataPropertyName = nameof(CartItem.Sku), Width = 110, ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name", HeaderText = "Tên sản phẩm", DataPropertyName = nameof(CartItem.Name),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "UnitPrice", HeaderText = "Đơn giá", DataPropertyName = nameof(CartItem.UnitPrice),
            Width = 100, ReadOnly = true, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Quantity", HeaderText = "SL", DataPropertyName = nameof(CartItem.Quantity),
            Width = 70, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "LineTotal", HeaderText = "Thành tiền", DataPropertyName = nameof(CartItem.LineTotal),
            Width = 110, ReadOnly = true, DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
        });

        _grid.DataSource = _items;
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty)
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            UpdateTotal();
            ItemChanged?.Invoke(this, EventArgs.Empty);
        };

        _totalLabel.Dock = DockStyle.Bottom;
        _totalLabel.Height = 34;
        _totalLabel.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        _totalLabel.ForeColor = Color.FromArgb(31, 62, 45);
        _totalLabel.TextAlign = ContentAlignment.MiddleRight;
        _totalLabel.Padding = new Padding(0, 0, 12, 0);

        Controls.Add(_grid);
        Controls.Add(_totalLabel);
        UpdateTotal();
    }

    /// <summary>Phát sinh khi người dùng sửa số lượng trong bảng.</summary>
    public event EventHandler? ItemChanged;

    public IReadOnlyList<CartItem> Items => _items.ToList();

    public CartItem? SelectedItem =>
        _grid.CurrentRow?.DataBoundItem as CartItem;

    public decimal Total => _items.Sum(item => item.LineTotal);

    public void SetItems(IEnumerable<CartItem> items)
    {
        _items.RaiseListChangedEvents = false;
        _items.Clear();
        foreach (var item in items)
        {
            _items.Add(item);
        }

        _items.RaiseListChangedEvents = true;
        _items.ResetBindings();
        UpdateTotal();
    }

    public void Clear()
    {
        _items.Clear();
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        _totalLabel.Text = $"Tổng cộng: {Total.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} đ";
    }
}
