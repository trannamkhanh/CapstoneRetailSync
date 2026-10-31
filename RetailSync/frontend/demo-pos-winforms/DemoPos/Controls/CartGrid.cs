using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using DemoPos.Models;

namespace DemoPos.Controls;

public sealed class CartGrid : UserControl
{
	private readonly DataGridView _grid = new DataGridView();

	private readonly BindingList<CartItem> _items = new BindingList<CartItem>();

	private readonly Label _totalLabel = new Label();

	public IReadOnlyList<CartItem> Items => _items.ToList();

	public CartItem? SelectedItem => _grid.CurrentRow?.DataBoundItem as CartItem;

	public decimal Total => _items.Sum((CartItem item) => item.LineTotal);

	public event EventHandler? ItemChanged;

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
			Name = "Sku",
			HeaderText = "SKU",
			DataPropertyName = "Sku",
			Width = 110,
			ReadOnly = true
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "Name",
			HeaderText = "Tên sản phẩm",
			DataPropertyName = "Name",
			AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
			ReadOnly = true
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "UnitPrice",
			HeaderText = "Đơn giá",
			DataPropertyName = "UnitPrice",
			Width = 100,
			ReadOnly = true,
			DefaultCellStyle = 
			{
				Format = "N0",
				Alignment = DataGridViewContentAlignment.MiddleRight
			}
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "Quantity",
			HeaderText = "SL",
			DataPropertyName = "Quantity",
			Width = 70,
			DefaultCellStyle = 
			{
				Alignment = DataGridViewContentAlignment.MiddleRight
			}
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "LineTotal",
			HeaderText = "Thành tiền",
			DataPropertyName = "LineTotal",
			Width = 110,
			ReadOnly = true,
			DefaultCellStyle = 
			{
				Format = "N0",
				Alignment = DataGridViewContentAlignment.MiddleRight
			}
		});
		_grid.DataSource = _items;
		_grid.CurrentCellDirtyStateChanged += (object? _, EventArgs _) =>
		{
			if (_grid.IsCurrentCellDirty)
			{
				_grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
			}
		};
		_grid.CellValueChanged += (object? _, DataGridViewCellEventArgs e) =>
		{
			if (e.RowIndex >= 0)
			{
				UpdateTotal();
				ItemChanged?.Invoke(this, EventArgs.Empty);
			}
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

	public void SetItems(IEnumerable<CartItem> items)
	{
		_items.RaiseListChangedEvents = false;
		_items.Clear();
		foreach (CartItem item in items)
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
		_totalLabel.Text = "Tổng cộng: " + Total.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
	}
}
