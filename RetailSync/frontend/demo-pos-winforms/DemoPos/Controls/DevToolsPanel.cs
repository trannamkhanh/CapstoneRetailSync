using DemoPos.Models;

namespace DemoPos.Controls;

/// <summary>Các thao tác mô phỏng lỗi từ Dev Tools.</summary>
public enum DevAction
{
    ResetMock,
    SendDuplicate,
    SendConflict,
    SendInsufficientStock,
    SendInvalidToken,
    SendUnknownTerminal,
    SendUnavailable
}

/// <summary>Bảng Dev Tools: chọn terminal gửi thử và kích hoạt các tình huống lỗi.
/// Mặc định ẩn; bật/tắt bằng nút trên thanh công cụ hoặc phím F12.
/// Không chứa điều khiển BaseUrl/mock — các điều khiển đó nằm trên thanh công cụ.</summary>
public sealed class DevToolsPanel : UserControl
{
    private readonly ComboBox _terminal = new();

    public DevToolsPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 251, 248);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 4,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        for (var i = 0; i < 3; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        }

        _terminal.Dock = DockStyle.Fill;
        _terminal.Margin = new Padding(3, 8, 3, 8);
        _terminal.DropDownStyle = ComboBoxStyle.DropDownList;
        _terminal.DisplayMember = nameof(TerminalConfig.DisplayName);

        layout.Controls.Add(Caption("Terminal gửi thử"), 0, 0);
        layout.Controls.Add(_terminal, 1, 0);

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true
        };

        buttonRow.Controls.Add(DevButton("Reset mock", DevAction.ResetMock));
        buttonRow.Controls.Add(DevButton("Gửi lại giống hệt", DevAction.SendDuplicate));
        buttonRow.Controls.Add(DevButton("Gửi khác nội dung", DevAction.SendConflict));
        buttonRow.Controls.Add(DevButton("Vượt tồn kho", DevAction.SendInsufficientStock));
        buttonRow.Controls.Add(DevButton("Token sai (401)", DevAction.SendInvalidToken));
        buttonRow.Controls.Add(DevButton("Terminal lạ (403)", DevAction.SendUnknownTerminal));
        buttonRow.Controls.Add(DevButton("503 rồi gửi lại", DevAction.SendUnavailable));

        layout.Controls.Add(Caption("Kịch bản"), 0, 1);
        layout.SetColumnSpan(buttonRow, 3);
        layout.Controls.Add(buttonRow, 1, 1);

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Dev Tools dùng để kiểm tra idempotency (DUPLICATE/CONFLICT), lỗi token/terminal, "
                   + "thiếu tồn kho và giả lập 503 — giống hệt các kịch bản của bản web.",
            ForeColor = Color.FromArgb(90, 105, 95),
            Font = new Font("Segoe UI", 9.5f)
        };
        layout.Controls.Add(hint, 0, 2);
        layout.SetColumnSpan(hint, 4);

        Controls.Add(layout);
    }

    public event EventHandler<DevAction>? ActionRequested;

    public TerminalConfig? SelectedTerminal => _terminal.SelectedItem as TerminalConfig;

    public void SetTerminals(IEnumerable<TerminalConfig> terminals)
    {
        _terminal.Items.Clear();
        foreach (var terminal in terminals)
        {
            _terminal.Items.Add(terminal);
        }

        if (_terminal.Items.Count > 0)
        {
            _terminal.SelectedIndex = 0;
        }
    }

    private Button DevButton(string text, DevAction action)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 36,
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 62, 45),
            Font = new Font("Segoe UI", 9.5f)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(200, 215, 204);
        button.Click += (_, _) => ActionRequested?.Invoke(this, action);
        return button;
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
