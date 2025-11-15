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

    // 현재 DinnerDetailPanel에서 수정 중인 특정 CourseDetail
    private CourseDetail _editingCourseDetail;

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
        // 방금 추가한 새 CourseDetail을 '수정 대상'으로 자동 설정
        _editingCourseDetail = CurrentOrder.GetLastAddedCourseDetail();
        Debug.Log($"[{type}] 코스 추가됨. 현재 총 {CurrentOrder.GetTotalCourseCount()}개 코스.");
    }

    // "옵션 변경" 시, 수정할 대상을 명시적으로 설정
    public void SetCourseDetailForEditing(CourseDetail detail)
    {
        _editingCourseDetail = detail;
    }

    // DinnerDetailManager가 현재 수정해야 할 CourseDetail을 반환
    public CourseDetail GetCurrentCourseDetailForEditing()
    {
        if (CurrentOrder == null)
        {
            Debug.LogError("CurrentOrder가 null입니다. AddCourseToOrder가 먼저 호출되어야 합니다.");
            _editingCourseDetail = CurrentOrder?.GetLastAddedCourseDetail();
        }
        // Order 객체 내의 헬퍼 함수를 호출
        return _editingCourseDetail;
    }

    // 장바구니(CurrentOrder)를 비운다. 
    public void ClearOrder()
    {
        CurrentOrder = null;
        _editingCourseDetail = null;
    }

    // (추후 구현) 현재 주문을 DB에 저장하고 세션을 종료합니다.
    public async Task FinalizeAndSubmitOrder()
    {
        if (CurrentOrder == null)
        {
            Debug.LogWarning("전송할 주문이 없습니다.");
            return;
        }

        // 최종 가격과 요청사항을 DB에 저장하기 전에 마지막으로 업데이트
        PriceManager.Instance.CalculateTotalPrice(CurrentOrder);
        // (요청사항은 ConfirmOrderManager의 onEndEdit에서 이미 Order 객체에 저장됨)

        // 주문 상태 변경 및 타임스탬프 기록
        CurrentOrder.status = OrderStatus.Confirmed;
        CurrentOrder.orderTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Order 객체를 JSON으로 변환
        string json = JsonUtility.ToJson(CurrentOrder, true);

        Debug.Log("--- 최종 주문 DB 전송 ---");
        Debug.Log(json);

        // "orders" 경로에 주문 ID를 키로 하여 저장
        await dbReference.Child("orders").Child(CurrentOrder.orderId).SetRawJsonValueAsync(json);

        Debug.Log("주문이 DB로 전송되었습니다.");

        // 주문 완료 후 장바구니(CurrentOrder) 비우기
        ClearOrder();
    }
}
