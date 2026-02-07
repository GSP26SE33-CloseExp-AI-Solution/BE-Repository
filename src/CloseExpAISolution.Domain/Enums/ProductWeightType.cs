namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// Loại sản phẩm theo cách định lượng
/// </summary>
public enum ProductWeightType
{
    /// <summary>
    /// Định lượng cố định (có khối lượng/trọng lượng cố định trong bao bì)
    /// VD: Chai nước 500ml, Gói bánh 200g
    /// </summary>
    Fixed = 1,

    /// <summary>
    /// Không định lượng cố định (bán theo cân - giá tính theo kg)
    /// VD: Rau, củ, quả, thịt cá tươi sống
    /// Mặc định là tươi
    /// </summary>
    Variable = 2
}
