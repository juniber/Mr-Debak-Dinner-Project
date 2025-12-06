using UnityEngine;
using Firebase.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

// Firebase DB에서 가격표(priceList)를 로드하고,
// 주문 금액 계산을 담당하는 매니저
public class PriceManager : MonoBehaviour
{
    public static PriceManager Instance { get; private set; }
    public bool IsPriceDataReady { get; private set; } = false;

    private DatabaseReference dbReference;

    // DB에서 가져온 가격 데이터를 저장할 캐시
    private Dictionary<string, long> coursePrices;
    private Dictionary<string, long> stylePrices;
    private Dictionary<string, long> addonPrices;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 캐시 초기화
            coursePrices = new Dictionary<string, long>();
            stylePrices = new Dictionary<string, long>();
            addonPrices = new Dictionary<string, long>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ✅ 씬에 PriceManager가 등장하면 바로 priceList를 한 번 로드해 둔다.
    private async void Start()
    {
        await FetchPriceDataAsync();
    }

    // AppInitializer에서 호출할 수도 있고, Start()에서 자동으로 한 번 호출하기도 함.
    public async Task FetchPriceDataAsync()
    {
        if (IsPriceDataReady)
        {
            Debug.Log("[PriceManager] 이미 priceList 로딩이 끝난 상태입니다.");
            return;
        }

        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        try
        {
            Debug.Log("[PriceManager] priceList 로딩 시작");

            DataSnapshot snapshot = await dbReference.Child("priceList").GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.LogError("Firebase에 'priceList' 노드가 없습니다!");
                return;
            }

            coursePrices.Clear();
            stylePrices.Clear();
            addonPrices.Clear();

            // 1. 코스 가격 로드
            var coursesNode = snapshot.Child("courses");
            foreach (var courseData in coursesNode.Children)
            {
                long v = Convert.ToInt64(courseData.Value);
                coursePrices[courseData.Key] = v;
                Debug.Log($"[PriceManager] course 가격 로드: {courseData.Key} = {v}");
            }

            // 2. 스타일 가격 로드
            var stylesNode = snapshot.Child("styles");
            foreach (var styleData in stylesNode.Children)
            {
                long v = Convert.ToInt64(styleData.Value);
                stylePrices[styleData.Key] = v;
                Debug.Log($"[PriceManager] style 가격 로드: {styleData.Key} = {v}");
            }

            // 3. 추가 메뉴(addon) 가격 로드
            var addonsNode = snapshot.Child("addons");
            foreach (var addonData in addonsNode.Children)
            {
                long v = Convert.ToInt64(addonData.Value);
                addonPrices[addonData.Key] = v;
                Debug.Log($"[PriceManager] addon 가격 로드: {addonData.Key} = {v}");
            }

            IsPriceDataReady = true;
            Debug.Log($"[PriceManager] priceList 로딩 완료. " +
                      $"courses={coursePrices.Count}, styles={stylePrices.Count}, addons={addonPrices.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PriceManager] priceList 로딩 실패: {ex.Message}");
        }
    }

    // ====== Public Getter들 ======

    public long GetCoursePrice(CourseType type)
    {
        if (!IsPriceDataReady)
        {
            Debug.LogWarning($"[PriceManager] GetCoursePrice 호출 시점에 priceList가 아직 준비 안 됨. type={type}");
        }

        return coursePrices.TryGetValue(type.ToString(), out long price) ? price : 0;
    }

    public long GetStylePrice(StyleType style)
    {
        if (!IsPriceDataReady)
        {
            Debug.LogWarning($"[PriceManager] GetStylePrice 호출 시점에 priceList가 아직 준비 안 됨. style={style}");
        }

        return stylePrices.TryGetValue(style.ToString(), out long price) ? price : 0;
    }

    public long GetAddonPrice(string addonKey)
    {
        if (!IsPriceDataReady)
        {
            Debug.LogWarning($"[PriceManager] GetAddonPrice 호출 시점에 priceList가 아직 준비 안 됨. addonKey={addonKey}");
        }

        return addonPrices.TryGetValue(addonKey, out long price) ? price : 0;
    }

    // ====== 합계 계산 ======

    // Order 객체를 받아 총 금액(할인 전)을 계산하고 order.totalPrice에 저장
    public void CalculateTotalPrice(Order order)
    {
        if (order == null || order.courseGroups == null)
        {
            Debug.LogWarning("[PriceManager] CalculateTotalPrice: order 또는 courseGroups 가 null 입니다.");
            return;
        }

        if (!IsPriceDataReady)
        {
            Debug.LogWarning("[PriceManager] CalculateTotalPrice 호출됐지만 priceList가 아직 준비 안 됨. " +
                             "모든 금액이 0으로 계산될 수 있습니다.");
        }

        long newTotal = 0;

        foreach (var group in order.courseGroups)
        {
            // group.courseType 은 "ValentineDinner" 이런 문자열이어야 함.
            if (string.IsNullOrEmpty(group.courseType))
                continue;

            if (!System.Enum.TryParse(group.courseType, out CourseType type))
            {
                Debug.LogWarning($"[PriceManager] 알 수 없는 courseType 문자열: {group.courseType}");
                continue;
            }

            long baseCoursePrice = GetCoursePrice(type);

            foreach (var detail in group.details)
            {
                long itemPrice = baseCoursePrice;

                // 스타일 가격
                itemPrice += GetStylePrice(detail.style);

                // 추가 addon 가격
                foreach (string addonKey in detail.addedItems)
                {
                    itemPrice += GetAddonPrice(addonKey);
                }

                newTotal += itemPrice;
            }
        }

        order.totalPrice = newTotal;
        Debug.Log($"[PriceManager] CalculateTotalPrice 완료: totalPrice={newTotal}");
    }

    // (네가 추가한 할인 함수) Order 안의 coupons 리스트를 이용해
    // 최종 할인된 금액을 계산하고 totalPrice/totalDiscountPrice에 반영
    public long DiscountTotalPrice(Order order)
    {
        if (order == null)
        {
            Debug.LogWarning("[PriceManager] DiscountTotalPrice: order가 null 입니다.");
            return 0;
        }

        // 항상 먼저 기본 금액 계산
        CalculateTotalPrice(order);
        long baseTotal = order.totalPrice;

        if (order.coupons == null || order.coupons.Count == 0)
        {
            // 쿠폰 없으면 그냥 기본 금액
            order.totalDiscountPrice = baseTotal;
            return baseTotal;
        }

        long maxDiscountPercent = 0;

        foreach (var coupon in order.coupons)
        {
            if (coupon == null) continue;
            if (coupon.discountAmount < 0) continue;

            if (coupon.discountAmount > maxDiscountPercent)
                maxDiscountPercent = coupon.discountAmount;
        }

        if (maxDiscountPercent > 100)
            maxDiscountPercent = 100;

        long discountValue = baseTotal * maxDiscountPercent / 100;
        long finalTotal = baseTotal - discountValue;
        if (finalTotal < 0) finalTotal = 0;

        order.totalDiscountPrice = finalTotal;
        order.totalPrice = finalTotal;

        Debug.Log($"[PriceManager] 할인 전={baseTotal}, 할인율={maxDiscountPercent}%, 최종={finalTotal}");

        return finalTotal;
    }
}
