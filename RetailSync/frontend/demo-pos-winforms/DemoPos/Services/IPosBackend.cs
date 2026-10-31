using DemoPos.Models;

namespace DemoPos.Services;

/// <summary>
/// Hợp đồng mà UI sử dụng. UI chỉ gọi qua interface này, không biết đang là mock hay Middleware thật.
/// </summary>
public interface IPosBackend
{
    /// <summary>Gửi một sự kiện POS. Không bao giờ ném exception ra UI (lỗi được chuẩn hóa thành PosResult).</summary>
    Task<PosResult> SendAsync(PosEvent posEvent, CancellationToken cancellationToken = default);

    /// <summary>Danh mục sản phẩm kèm tồn kho hiện tại (mock: tồn thật trong RAM; client thật: cache cập nhật theo phản hồi).</summary>
    IReadOnlyList<Product> GetProducts();

    /// <summary>Đặt lại trạng thái (chỉ mock backend có tác dụng).</summary>
    void Reset();
}
