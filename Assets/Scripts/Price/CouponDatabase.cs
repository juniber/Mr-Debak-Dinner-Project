using System.Collections.Generic;
using UnityEngine;

// 쿠폰 ID 문자열을 실제 Coupon 객체로 변환해주는 헬퍼 (임시 구현)
public static class CouponDatabase
{
    // 예시: 쿠폰 ID → 할인율(%) 매핑
    private static Dictionary<string, long> couponDiscountTable = new Dictionary<string, long>
    {
        { "WELCOME10", 10 },
        { "VIP20", 20 },
        { "XMAS30", 30 },
        // 필요하면 계속 추가
    };

    public static Coupon GetCouponById(string couponId)
    {
        if (string.IsNullOrEmpty(couponId)) return null;

        if (couponDiscountTable.TryGetValue(couponId, out long discountPercent))
        {
            return new Coupon(couponId, discountPercent,false);
        }

        Debug.LogWarning($"CouponDatabase: 알 수 없는 쿠폰 ID ({couponId})");
        return null;
    }
}
