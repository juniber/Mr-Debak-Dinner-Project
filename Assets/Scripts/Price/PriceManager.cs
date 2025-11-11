using UnityEngine;
using Firebase.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

// Firebase DB에서 가격표(priceList)를 로드하고,
// 주문 가격 계산을 전담하는 싱글톤 매니저
public class PriceManager : MonoBehaviour
{
    public static PriceManager Instance { get; private set; }
    public bool IsPriceDataReady { get; private set; } = false;

    private DatabaseReference dbReference;

    // DB에서 불러온 가격 데이터를 저장할 딕셔너리
    private Dictionary<string, long> coursePrices;
    private Dictionary<string, long> stylePrices;
    private Dictionary<string, long> addonPrices;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 딕셔너리 초기화
            coursePrices = new Dictionary<string, long>();
            stylePrices = new Dictionary<string, long>();
            addonPrices = new Dictionary<string, long>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // AppInitializer가 호출할 공용 초기화 함수.
    // DB에서 가격표를 비동기식으로 가져온다.
    public async Task FetchPriceDataAsync()
    {
        if (IsPriceDataReady) return; // 이미 로드됨

        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        try
        {
            DataSnapshot snapshot = await dbReference.Child("priceList").GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.LogError("Firebase에 'priceList' 데이터가 없습니다!");
                return;
            }

            // 1. 코스 가격 로드
            foreach (var courseData in snapshot.Child("courses").Children)
            {
                coursePrices[courseData.Key] = Convert.ToInt64(courseData.Value);
            }
            // 2. 스타일 가격 로드
            foreach (var styleData in snapshot.Child("styles").Children)
            {
                stylePrices[styleData.Key] = Convert.ToInt64(styleData.Value);
            }
            // 3. 추가 항목 가격 로드
            foreach (var addonData in snapshot.Child("addons").Children)
            {
                addonPrices[addonData.Key] = Convert.ToInt64(addonData.Value);
            }

            IsPriceDataReady = true;
            Debug.Log("Firebase 가격표 로드 성공!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"가격표 로드 실패: {ex.Message}");
        }
    }

    // Public Getter 함수들 (이제 DB가 아닌 로컬 딕셔너리에서 즉시 반환)

    public long GetCoursePrice(CourseType type)
    {
        return coursePrices.TryGetValue(type.ToString(), out long price) ? price : 0;
    }

    public long GetStylePrice(StyleType style)
    {
        return stylePrices.TryGetValue(style.ToString(), out long price) ? price : 0;
    }

    public long GetAddonPrice(string addonKey)
    {
        return addonPrices.TryGetValue(addonKey, out long price) ? price : 0;
    }

    // Order 객체를 받아 총 가격을 계산하고, order.totalPrice에 값을 설정
    public void CalculateTotalPrice(Order order)
    {
        if (order == null || order.courseGroups == null) return;

        long newTotal = 0;
        foreach (var group in order.courseGroups)
        {
            CourseType type = (CourseType)Enum.Parse(typeof(CourseType), group.courseType);
            // 1. 코스 기본 가격
            long baseCoursePrice = GetCoursePrice(type);

            foreach (var detail in group.details)
            {
                // 코스당 가격 추가
                newTotal += baseCoursePrice;
                // 2. 스타일 추가 가격
                newTotal += GetStylePrice(detail.style);
                // 3. '추가' 항목 가격
                foreach (string addonKey in detail.addedItems)
                {
                    newTotal += GetAddonPrice(addonKey);
                }
            }
        }
        order.totalPrice = newTotal; // Order 객체의 totalPrice 변수를 직접 업데이트
    }
}
