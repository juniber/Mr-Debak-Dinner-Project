using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;

// �ֹ� ������ �����ϴ� �̱���
// ����ڰ� ���� �����ϴ� ���� ���� ���� 'Order' ��ü�� ����
public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    // ���� ����ڰ� ���� ���� �ֹ� (��ٱ���)
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

    // �� �ڽ��� ���� �ֹ��� �߰�
    public void AddCourseToOrder(CourseType type)
    {
        // ���� �ֹ��� ���ٸ� ���� ����
        if (CurrentOrder == null)
        {
            // �α����� ����� ID�� �� �ֹ� ����
            FirebaseUser user = auth.CurrentUser;
            if (user == null)
            {
                Debug.LogError("�α����� ����� ������ �����ϴ�.");
                return;
            }
            CurrentOrder = new Order(user.UserId);
        }

        CurrentOrder.AddCourse(type);
        Debug.Log($"[{type}] �ڽ� �߰���. ���� �� {CurrentOrder.GetTotalCourseCount()}�� �ڽ�.");
    }

    // (���� ����) ���� �ֹ��� DB�� �����ϰ� ������ �����մϴ�.
    public async Task FinalizeAndSubmitOrder()
    {
        if (CurrentOrder == null) return;

        CurrentOrder.status = OrderStatus.Confirmed;
        CurrentOrder.orderTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string json = JsonUtility.ToJson(CurrentOrder);

        Debug.Log("�ֹ��� DB�� ���۵Ǿ����ϴ�.");

        // TODO: "orders" ��ο� �ֹ� ����
        // await dbReference.Child("orders").Child(CurrentOrder.orderId).SetRawJsonValueAsync(json);

        // ���� �ֹ� ���� ����
        CurrentOrder = null;
    }
}
