using System.ComponentModel;
using DemoPos.Models;

namespace DemoPos.Controls;

/// <summary>Lịch sử giao dịch của phiên làm việc (chỉ giữ ở RAM).</summary>
public sealed class HistoryGrid : UserControl
{
    private readonly DataGridView _grid = new();
    private readonly BindingList<TxnRecord> _records = new();

    public HistoryGrid()
    {
        Dock = DockStyle.Fill;

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.ColumnHeadersHeight = 32;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 246, 241);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 62, 45);
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 246, 241);
        _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 62, 45);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;  
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 234, 220);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 40, 28);
        _grid.ShowCellToolTips = true;

        // Tất cả cột dùng chế độ Fill để vừa bề ngang, không phát sinh thanh cuộn ngang.
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Thời gian", DataPropertyName = nameof(TxnRecord.Time),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 13,
            MinimumWidth = 60,
            DefaultCellStyle = { Format = "HH:mm:ss" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Loại", DataPropertyName = nameof(TxnRecord.TypeLabel),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 9,
            MinimumWidth = 45
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Transaction ID", DataPropertyName = nameof(TxnRecord.TransactionId),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 26,
            MinimumWidth = 90
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Kết quả", DataPropertyName = nameof(TxnRecord.Outcome),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 18,
            MinimumWidth = 70
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Mã", DataPropertyName = nameof(TxnRecord.Code),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 20,
            MinimumWidth = 100
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Độ trễ (ms)", DataPropertyName = nameof(TxnRecord.LatencyMs),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 14,
            MinimumWidth = 55,
            DefaultCellStyle = { Format = "0", Alignment = DataGridViewContentAlignment.MiddleRight }
        });

        _grid.DataSource = _records;
        Controls.Add(_grid);
    }

    /// <summary>Bản ghi được chọn trong lịch sử (dùng cho tab Trả hàng / Hủy đơn).</summary>
    public TxnRecord? SelectedRecord => _grid.CurrentRow?.DataBoundItem as TxnRecord;

    public IReadOnlyList<TxnRecord> Records => _records.ToList();

    public void Add(TxnRecord record)
    {
        _records.Insert(0, record);
        if (_grid.Rows.Count > 0)
        {
            _grid.CurrentCell = _grid.Rows[0].Cells[0];
        }
    }

    public void Clear() => _records.Clear();
}
