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