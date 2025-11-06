using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;

// 주문 세션을 관리하는 싱글톤
// 사용자가 앱을 실행하는 동안 생성 중인 'Order' 객체를 보관
public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    // 현재 사용자가 생성 중인 주문 (장바구니)
    public Order CurrentOrder { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // 새 코스를 현재 주문에 추가
    public void AddCourseToOrder(CourseType type)
    {
        // 현재 주문이 없다면 새로 생성
        if (CurrentOrder == null)
        {
            // 로그인한 사용자 ID로 새 주문 생성
            FirebaseUser user = auth.CurrentUser;
            if (user == null)
            {
                Debug.LogError("로그인한 사용자 정보가 없습니다.");
                return;
            }
            // 로그인한 사용자 ID로 새 Order 객체 생성
            CurrentOrder = new Order(user.UserId);
        }

        // Order 객체에 새 코스 추가 (새 CourseDetail 객체가 생성됨)
        CurrentOrder.AddCourse(type);
        Debug.Log($"[{type}] 코스 추가됨. 현재 총 {CurrentOrder.GetTotalCourseCount()}개 코스.");
    }

    // 현재 수정 중인(가장 마지막에 추가된) CourseDetail을 가져온다.
    // DinnerDetailManager가 OnEnable될 때 호출하여 어떤 CourseDetail을 수정해야 하는지 알 수 있게 한다.
    public CourseDetail GetCurrentCourseDetailForEditing()
    {
        if (CurrentOrder == null)
        {
            Debug.LogError("CurrentOrder가 null입니다. AddCourseToOrder가 먼저 호출되어야 합니다.");
            return null;
        }
        // Order 객체 내의 헬퍼 함수를 호출
        return CurrentOrder.GetLastAddedCourseDetail();
    }

    // (추후 구현) 현재 주문을 DB에 저장하고 세션을 종료합니다.
    public async Task FinalizeAndSubmitOrder()
    {
        if (CurrentOrder == null) return;

        // 주문 상태를 '확정'으로 변경하고 타임스탬프 기록
        CurrentOrder.status = OrderStatus.Confirmed;
        CurrentOrder.orderTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Order 객체를 JSON으로 변환
        string json = JsonUtility.ToJson(CurrentOrder);

        Debug.Log("--- 최종 주문 DB 전송 ---");
        Debug.Log(json);

        // "orders" 경로에 주문 ID를 키로 하여 저장
        await dbReference.Child("orders").Child(CurrentOrder.orderId).SetRawJsonValueAsync(json);

        Debug.Log("주문이 DB로 전송되었습니다.");

        // 현재 주문 세션 종료 (장바구니 비우기)
        CurrentOrder = null;
    }
}
