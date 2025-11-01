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
            CurrentOrder = new Order(user.UserId);
        }

        CurrentOrder.AddCourse(type);
        Debug.Log($"[{type}] 코스 추가됨. 현재 총 {CurrentOrder.GetTotalCourseCount()}개 코스.");
    }

    // (추후 구현) 현재 주문을 DB에 저장하고 세션을 종료합니다.
    public async Task FinalizeAndSubmitOrder()
    {
        if (CurrentOrder == null) return;

        CurrentOrder.status = OrderStatus.Confirmed;
        CurrentOrder.orderTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string json = JsonUtility.ToJson(CurrentOrder);

        Debug.Log("주문이 DB로 전송되었습니다.");

        // TODO: "orders" 경로에 주문 저장
        // await dbReference.Child("orders").Child(CurrentOrder.orderId).SetRawJsonValueAsync(json);

        // 현재 주문 세션 종료
        CurrentOrder = null;
    }
}
