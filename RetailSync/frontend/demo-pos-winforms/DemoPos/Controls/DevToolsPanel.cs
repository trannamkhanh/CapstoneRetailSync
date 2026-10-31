using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DemoPos.Models;

namespace DemoPos.Controls;

public sealed class DevToolsPanel : UserControl
{
	private readonly ComboBox _terminal = new ComboBox();

	public TerminalConfig? SelectedTerminal => _terminal.SelectedItem as TerminalConfig;

	public event EventHandler<DevAction>? ActionRequested;

	public DevToolsPanel()
	{
		Dock = DockStyle.Fill;
		BackColor = Color.FromArgb(248, 251, 248);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(12),
			ColumnCount = 4,
			RowCount = 3
		};
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320f));
		for (int i = 0; i < 3; i++)
		{
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
		}
		_terminal.Dock = DockStyle.Fill;
		_terminal.Margin = new Padding(3, 8, 3, 8);
		_terminal.DropDownStyle = ComboBoxStyle.DropDownList;
		_terminal.DisplayMember = "DisplayName";
		tableLayoutPanel.Controls.Add(Caption("Terminal gửi thử"), 0, 0);
		tableLayoutPanel.Controls.Add(_terminal, 1, 0);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			AutoScroll = true
		};
		flowLayoutPanel.Controls.Add(DevButton("Reset mock", DevAction.ResetMock));
		flowLayoutPanel.Controls.Add(DevButton("Gửi lại giống hệt", DevAction.SendDuplicate));
		flowLayoutPanel.Controls.Add(DevButton("Gửi khác nội dung", DevAction.SendConflict));
		flowLayoutPanel.Controls.Add(DevButton("Vượt tồn kho", DevAction.SendInsufficientStock));
		flowLayoutPanel.Controls.Add(DevButton("Token sai (401)", DevAction.SendInvalidToken));
		flowLayoutPanel.Controls.Add(DevButton("Terminal lạ (403)", DevAction.SendUnknownTerminal));
		flowLayoutPanel.Controls.Add(DevButton("503 rồi gửi lại", DevAction.SendUnavailable));
		tableLayoutPanel.Controls.Add(Caption("Kịch bản"), 0, 1);
		tableLayoutPanel.SetColumnSpan(flowLayoutPanel, 3);
		tableLayoutPanel.Controls.Add(flowLayoutPanel, 1, 1);
		Label control = new Label
		{
			Dock = DockStyle.Fill,
			Text = "Dev Tools dùng để kiểm tra idempotency (DUPLICATE/CONFLICT), lỗi token/terminal, thiếu tồn kho và giả lập 503 — giống hệt các kịch bản của bản web.",
			ForeColor = Color.FromArgb(90, 105, 95),
			Font = new Font("Segoe UI", 9.5f)
		};
		tableLayoutPanel.Controls.Add(control, 0, 2);
		tableLayoutPanel.SetColumnSpan(control, 4);
		Controls.Add(tableLayoutPanel);
	}

	public void SetTerminals(IEnumerable<TerminalConfig> terminals)
	{
		_terminal.Items.Clear();
		foreach (TerminalConfig terminal in terminals)
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
		Button button = new Button
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
		button.Click += (object? _, EventArgs _) =>
		{
			ActionRequested?.Invoke(this, action);
		};
		return button;
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
