using DemoPos.Models;

namespace DemoPos.Controls;

/// <summary>Hiển thị kết quả giao dịch: outcome, code, message, độ trễ và nút "Gửi lại".</summary>
public sealed class ResultPanel : UserControl
{
    /// <summary>Chiều cao tối thiểu của panel khi không có nút "Gửi lại" (4 hàng nội dung + padding).</summary>
    private const int BaseHeight = 144;

    /// <summary>Chiều cao của hàng chứa nút "Gửi lại".</summary>
    private const int RetryRowHeight = 52;

    private readonly Label _outcomeLabel = new();
    private readonly Label _codeLabel = new();
    private readonly Label _messageLabel = new();
    private readonly Label _metaLabel = new();
    private readonly Button _retryButton = new();
    private readonly TableLayoutPanel _layout = new();

    public ResultPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 251, 248);

        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(12);
        _layout.ColumnCount = 2;
        _layout.RowCount = 5;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 5; i++)
        {
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 4 ? RetryRowHeight : 30));
        }

        var outcomeCaption = Caption("Kết quả");
        _outcomeLabel.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        _outcomeLabel.Text = "—";

        _codeLabel.Font = new Font("Consolas", 10f);
        _codeLabel.Text = "—";
        _codeLabel.AutoEllipsis = true;

        _messageLabel.Font = new Font("Segoe UI", 10f);
        _messageLabel.AutoEllipsis = true;

        _metaLabel.Font = new Font("Segoe UI", 9.5f);
        _metaLabel.ForeColor = Color.FromArgb(90, 105, 95);
        _metaLabel.Text = "Độ trễ: —";

        _retryButton.Text = "Gửi lại (F10)";
        _retryButton.Dock = DockStyle.Fill;
        _retryButton.Margin = new Padding(0, 8, 0, 0);
        _retryButton.Height = 40;
        _retryButton.FlatStyle = FlatStyle.Flat;
        _retryButton.FlatAppearance.BorderSize = 0;
        _retryButton.UseVisualStyleBackColor = false;
        _retryButton.BackColor = Color.FromArgb(31, 122, 72);
        _retryButton.ForeColor = Color.White;
        _retryButton.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _retryButton.Enabled = false;
        _retryButton.Visible = false;
        _retryButton.Click += (_, _) => RetryRequested?.Invoke(this, EventArgs.Empty);

        _layout.Controls.Add(outcomeCaption, 0, 0);
        _layout.Controls.Add(_outcomeLabel, 1, 0);
        _layout.Controls.Add(Caption("Mã"), 0, 1);
        _layout.Controls.Add(_codeLabel, 1, 1);
        _layout.Controls.Add(Caption("Thông báo"), 0, 2);
        _layout.Controls.Add(_messageLabel, 1, 2);
        _layout.Controls.Add(Caption("Chi tiết"), 0, 3);
        _layout.Controls.Add(_metaLabel, 1, 3);
        _layout.Controls.Add(new Label(), 0, 4);
        _layout.Controls.Add(_retryButton, 1, 4);

        SetRetryVisible(false);

        Controls.Add(_layout);
    }

    /// <summary>Người dùng yêu cầu gửi lại giao dịch (chỉ bật khi lỗi tạm thời).</summary>
    public event EventHandler? RetryRequested;

    // Điểm móc cho kiểm thử tự động (internal + InternalsVisibleTo).
    internal string OutcomeText => _outcomeLabel.Text;

    internal string CodeText => _codeLabel.Text;

    internal string MessageText => _messageLabel.Text;

    internal bool RetryEnabled => _retryButton.Enabled;

    /// <summary>Nút "Gửi lại" chỉ hiện khi giao dịch thực sự gửi lại được.</summary>
    internal bool RetryVisible => _retryButton.Visible;

    public void Clear()
    {
        _outcomeLabel.Text = "—";
        _outcomeLabel.ForeColor = Color.FromArgb(60, 70, 62);
        _codeLabel.Text = "—";
        _messageLabel.Text = "—";
        _metaLabel.Text = "Độ trễ: —";
        _retryButton.Enabled = false;
        SetRetryVisible(false);
    }

    public void Show(PosResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _outcomeLabel.Text = result.Outcome switch
        {
            PosOutcome.OK => "✔ THÀNH CÔNG",
            PosOutcome.DUPLICATE => "↺ TRÙNG LẶP (DUPLICATE)",
            PosOutcome.REJECTED => "✖ BỊ TỪ CHỐI",
            _ => "⚠ LỖI DỊCH VỤ"
        };

        _outcomeLabel.ForeColor = result.Outcome switch
        {
            PosOutcome.OK => Color.FromArgb(21, 101, 60),
            PosOutcome.DUPLICATE => Color.FromArgb(178, 106, 12),
            PosOutcome.REJECTED => Color.FromArgb(168, 42, 42),
            _ => Color.FromArgb(190, 74, 12)
        };

        _codeLabel.Text = result.Code ?? "—";
        _messageLabel.Text = result.Message ?? string.Empty;
        _metaLabel.Text = $"Độ trễ: {result.LatencyMs:0} ms · HTTP {result.HttpStatus}";
        _retryButton.Enabled = result.CanRetry;
        SetRetryVisible(result.CanRetry);
    }

    /// <summary>Hiện/ẩn nút "Gửi lại" và thu gọn hàng chứa nút cho gọn layout.</summary>
    private void SetRetryVisible(bool visible)
    {
        _retryButton.Visible = visible;
        _layout.RowStyles[4].Height = visible ? RetryRowHeight : 0;

        // Panel được dock ở đỉnh cột phải: co giãn chiều cao theo nội dung để không bị cắt.
        Height = visible ? BaseHeight + RetryRowHeight : BaseHeight;

        _layout.PerformLayout();
    }

    private static Label Caption(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(90, 105, 95),
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
    };
}
