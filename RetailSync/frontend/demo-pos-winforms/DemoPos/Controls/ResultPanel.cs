using System;
using System.Drawing;
using System.Windows.Forms;
using DemoPos.Models;

namespace DemoPos.Controls;

public sealed class ResultPanel : UserControl
{
	private const int BaseHeight = 144;

	private const int RetryRowHeight = 52;

	private readonly Label _outcomeLabel = new Label();

	private readonly Label _codeLabel = new Label();

	private readonly Label _messageLabel = new Label();

	private readonly Label _metaLabel = new Label();

	private readonly Button _retryButton = new Button();

	private readonly TableLayoutPanel _layout = new TableLayoutPanel();

	internal string OutcomeText => _outcomeLabel.Text;

	internal string CodeText => _codeLabel.Text;

	internal string MessageText => _messageLabel.Text;

	internal bool RetryEnabled => _retryButton.Enabled;

	internal bool RetryVisible => _retryButton.Visible;

	public event EventHandler? RetryRequested;

	public ResultPanel()
	{
		Dock = DockStyle.Fill;
		BackColor = Color.FromArgb(248, 251, 248);
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(12);
		_layout.ColumnCount = 2;
		_layout.RowCount = 5;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		for (int i = 0; i < 5; i++)
		{
			_layout.RowStyles.Add(new RowStyle(SizeType.Absolute, (i == 4) ? 52 : 30));
		}
		Label control = Caption("Kết quả");
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
		_retryButton.Click += (object? _, EventArgs _) =>
		{
			RetryRequested?.Invoke(this, EventArgs.Empty);
		};
		_layout.Controls.Add(control, 0, 0);
		_layout.Controls.Add(_outcomeLabel, 1, 0);
		_layout.Controls.Add(Caption("Mã"), 0, 1);
		_layout.Controls.Add(_codeLabel, 1, 1);
		_layout.Controls.Add(Caption("Thông báo"), 0, 2);
		_layout.Controls.Add(_messageLabel, 1, 2);
		_layout.Controls.Add(Caption("Chi tiết"), 0, 3);
		_layout.Controls.Add(_metaLabel, 1, 3);
		_layout.Controls.Add(new Label(), 0, 4);
		_layout.Controls.Add(_retryButton, 1, 4);
		SetRetryVisible(visible: false);
		Controls.Add(_layout);
	}

	public void Clear()
	{
		_outcomeLabel.Text = "—";
		_outcomeLabel.ForeColor = Color.FromArgb(60, 70, 62);
		_codeLabel.Text = "—";
		_messageLabel.Text = "—";
		_metaLabel.Text = "Độ trễ: —";
		_retryButton.Enabled = false;
		SetRetryVisible(visible: false);
	}

	public void Show(PosResult result)
	{
		ArgumentNullException.ThrowIfNull(result, "result");
		Label outcomeLabel = _outcomeLabel;
		PosOutcome outcome = result.Outcome;
		if (1 == 0)
		{
		}
		string text = outcome switch
		{
			PosOutcome.OK => "✔ THÀNH CÔNG", 
			PosOutcome.DUPLICATE => "↺ TRÙNG LẶP (DUPLICATE)", 
			PosOutcome.REJECTED => "✖ BỊ TỪ CHỐI", 
			_ => "⚠ LỖI DỊCH VỤ", 
		};
		if (1 == 0)
		{
		}
		outcomeLabel.Text = text;
		Label outcomeLabel2 = _outcomeLabel;
		PosOutcome outcome2 = result.Outcome;
		if (1 == 0)
		{
		}
		Color foreColor = outcome2 switch
		{
			PosOutcome.OK => Color.FromArgb(21, 101, 60), 
			PosOutcome.DUPLICATE => Color.FromArgb(178, 106, 12), 
			PosOutcome.REJECTED => Color.FromArgb(168, 42, 42), 
			_ => Color.FromArgb(190, 74, 12), 
		};
		if (1 == 0)
		{
		}
		outcomeLabel2.ForeColor = foreColor;
		_codeLabel.Text = result.Code ?? "—";
		_messageLabel.Text = result.Message ?? string.Empty;
		_metaLabel.Text = $"Độ trễ: {result.LatencyMs:0} ms · HTTP {result.HttpStatus}";
		_retryButton.Enabled = result.CanRetry;
		SetRetryVisible(result.CanRetry);
	}

	private void SetRetryVisible(bool visible)
	{
		_retryButton.Visible = visible;
		_layout.RowStyles[4].Height = (visible ? 52 : 0);
		Height = (visible ? 196 : 144);
		_layout.PerformLayout();
	}

	private static Label Caption(string text)
	{
		return new Label
		{
			Text = text,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = Color.FromArgb(90, 105, 95),
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
		};
	}
}
