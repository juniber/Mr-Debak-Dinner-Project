using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// �ֹ� ������ �����ϴ� �̱���
// ����ڰ� ���� �����ϴ� ���� ���� ���� 'Order' ��ü�� ����
public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    // ���� ����ڰ� ���� ���� �ֹ� (��ٱ���)
    public Order CurrentOrder { get; private set; }

    // ���� DinnerDetailPanel���� ���� ���� Ư�� CourseDetail
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
            // �α����� ����� ID�� �� Order ��ü ����
            CurrentOrder = new Order(user.UserId);
        }

        // Order ��ü�� �� �ڽ� �߰� (�� CourseDetail ��ü�� ������)
        CurrentOrder.AddCourse(type);
        // ��� �߰��� �� CourseDetail�� '���� ���'���� �ڵ� ����
        _editingCourseDetail = CurrentOrder.GetLastAddedCourseDetail();
    }

    // "�ɼ� ����" ��, ������ ����� ���������� ����
    public void SetCourseDetailForEditing(CourseDetail detail)
    {
        _editingCourseDetail = detail;
    }

    // DinnerDetailManager�� ���� �����ؾ� �� CourseDetail�� ��ȯ
    public CourseDetail GetCurrentCourseDetailForEditing()
    {
        if (CurrentOrder == null)
        {
            Debug.LogError("CurrentOrder�� null�Դϴ�. AddCourseToOrder�� ���� ȣ��Ǿ�� �մϴ�.");
            _editingCourseDetail = CurrentOrder?.GetLastAddedCourseDetail();
        }
        // Order ��ü ���� ���� �Լ��� ȣ��
        return _editingCourseDetail;
    }

    // ��ٱ���(CurrentOrder)�� ����. 
    public void ClearOrder()
    {
        CurrentOrder = null;
        _editingCourseDetail = null;
    }

    // ���� �ֹ��� DB�� �����ϰ� ������ ����
    // 'isReservation' �÷��׸� �޾� ��� �ֹ��� ���� �ֹ��� ����
    public async Task FinalizeAndSubmitOrder(bool isReservation)
    {
        if (CurrentOrder == null)
        {
            Debug.LogWarning("������ �ֹ��� �����ϴ�.");
            return;
        }

        // 1. �ֹ� ��ü ���� Ȯ��
        PriceManager.Instance.CalculateTotalPrice(CurrentOrder);

        // ���� ���ο� ���� ���¸� �ٸ��� ����
        if (isReservation)
        {
            CurrentOrder.status = OrderStatus.Reserved; // (Enum �� 1)
        }
        else
        {
            CurrentOrder.status = OrderStatus.Confirmed; // (Enum �� 2)
        }
        CurrentOrder.orderTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CurrentOrder.isReservation = isReservation;

        if (auth.CurrentUser != null)
        {
            string address = await FetchUserAddress(auth.CurrentUser.UserId);
            CurrentOrder.deliveryAddress = address;
            Debug.Log($"[OrderManager] 배달 주소 설정됨: {address}");
        }
        else
        {
            CurrentOrder.deliveryAddress = "주소 정보 없음 (비로그인)";
        }

        // 2. ��� ���� (��� �ֹ��� ��쿡��)
        try
        {
            // (����) ��� ���� ������ ���� ���� �Լ��� ����
            await SubtractInventory(CurrentOrder);
        }
        catch (Exception ex)
        {
            // ��� ���� Ʈ����� ���� (��: ���� �������� ��� ����)
            Debug.LogError($"��� ���� ����: {ex}");
            // ����ڿ��� ���� �޽����� ǥ���ϰ� �ֹ� ������ �ߴ�
            UIManager.Instance.ShowTemporaryStatus("�˼��մϴ�. �ֹ� �� ����� �����Ǿ����ϴ�.", 3f, 1);
            throw; // ���ܸ� �ٽ� ������ ConfirmOrderManager�� catch ������ ����ǵ��� ��
        }

        // 3. Order ��ü�� JSON���� ��ȯ
        string json = JsonUtility.ToJson(CurrentOrder, true);
        Debug.Log("--- ���� �ֹ� DB ���� ---");
        Debug.Log(json);

        // 4. (����) �ֹ� ��� �и�
        string orderPath = isReservation ? "scheduledOrders" : "orders";

        await dbReference.Child(orderPath).Child(CurrentOrder.orderId).SetRawJsonValueAsync(json);

        Debug.Log($"�ֹ��� [{orderPath}] ��η� ���۵Ǿ����ϴ�.");

        // 5. �ֹ� �Ϸ� �� ��ٱ���(CurrentOrder) ����
        ClearOrder();
    }

    // �ֹ��� ���� ��� ��Ḧ ����Ͽ� DB���� ����
    private async Task SubtractInventory(Order order)
    {
        Debug.Log("�ֹ� Ȯ��: ��� ������ �����մϴ�...");

        // 0. ���� ���� Ȯ�� (���� �ǽɵǴ� �κ�)
        if (auth.CurrentUser == null)
        {
            Debug.LogError("[ġ���� ����] ��� ���� ������ �α����� �Ǿ����� �ʽ��ϴ�!");
            throw new Exception("User not authenticated");
        }
        else
        {
            Debug.Log($"[���� Ȯ��] ����� ID: {auth.CurrentUser.UserId}");
        }

        var totalCost = new Dictionary<string, long>();
        var addonCosts = MenuData.GetAddonCosts(); // �߾� ������ ��������

        // 1. ��ٱ����� ��� ��� �Ҹ� ���
        foreach (var group in order.courseGroups)
        {
            if (string.IsNullOrEmpty(group.courseType)) continue; // ���� ��ġ

            // Enum �Ľ� �õ�
            if (!System.Enum.TryParse(group.courseType, out CourseType type))
            {
                Debug.LogWarning($"�� �� ���� �ڽ� Ÿ��: {group.courseType}");
                continue;
            }
            var baseReqs = MenuData.GetCourseBaseRequirements(type);

            foreach (var detail in group.details)
            {
                // �⺻ ���
                foreach (var req in baseReqs)
                {
                    if (!totalCost.ContainsKey(req.Key)) totalCost[req.Key] = 0;
                    totalCost[req.Key] += req.Value;
                }

                // �߰� ���
                foreach (string addonKey in detail.addedItems)
                {
                    if (addonCosts.TryGetValue(addonKey, out var costInfo))
                    {
                        if (!totalCost.ContainsKey(costInfo.InventoryKey)) totalCost[costInfo.InventoryKey] = 0;
                        totalCost[costInfo.InventoryKey] += costInfo.Amount;
                    }
                    else
                    {
                        Debug.LogWarning($"�� �� ���� �߰� �ɼ� Ű: {addonKey}");
                    }
                }

                // ���� ��� (ȯ��)
                foreach (string removedKey in detail.removedItems)
                {
                    var refund = MenuData.GetRefundInfo(type, removedKey);
                    if (refund != null)
                    {
                        if (!totalCost.ContainsKey(refund.InventoryKey)) totalCost[refund.InventoryKey] = 0;
                        totalCost[refund.InventoryKey] -= refund.Amount;
                    }
                }
            }
        }

        // 2. ���� �� �Ҹ��� ������� DB Ʈ����� ����
        List<Task> transactionTasks = new List<Task>();
        foreach (var item in totalCost)
        {
            string itemKey = item.Key;
            long amountToSubtract = item.Value;

            if (amountToSubtract <= 0) continue; // 0 ���ϸ� ������ �ʿ� ����

            Debug.Log($"[Transaction] {itemKey} ��� {amountToSubtract} ���� �õ�...");

            DatabaseReference itemRef = dbReference.Child("inventory").Child(itemKey);

            // �� ��� �׸� ���� ���� Ʈ����� ����
            Task transactionTask = itemRef.RunTransaction(data =>
            {
                if (data.Value == null)
                {
                    // DB�� �ش� ��� �׸��� ����. ������ �ߴ�.
                    Debug.LogWarning($"��� ���� ����: {itemKey} �׸��� DB�� �����ϴ�.");
                    return TransactionResult.Success(data); // ������ �ʰ� �Ѿ
                }

                long currentStock = Convert.ToInt64(data.Value);
                try
                {
                    currentStock = Convert.ToInt64(data.Value);
                }
                catch
                {
                    Debug.LogError($"[����ȯ ����] {itemKey} ��: {data.Value}");
                    return TransactionResult.Abort();
                }

                if (currentStock < amountToSubtract)
                {
                    Debug.LogWarning($"[��� ����] {itemKey} (����: {currentStock}, �ʿ�: {amountToSubtract})");
                    return TransactionResult.Abort();
                }

                // ��� ����
                data.Value = currentStock - amountToSubtract;
                return TransactionResult.Success(data);
            });

            transactionTasks.Add(transactionTask);
        }

        // 3. ��� ��� ���� Ʈ������� �Ϸ�� ������ ��ٸ�
        // ���� �ϳ��� Abort()�Ǹ�, Task.WhenAll�� ����(DatabaseException)�� �����ϴ�.
        try
        {
            await Task.WhenAll(transactionTasks);
            Debug.Log("��� ��� ���� �Ϸ�.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Ʈ����� ����] �� ����: {ex.ToString()}");
            throw;
        }
    }
    
    private async Task<string> FetchUserAddress(string userId)
    {
        try
        {
            var snapshot = await dbReference.Child("users").Child(userId).Child("address").GetValueAsync();

            if (snapshot.Exists && snapshot.Value != null)
            {
                string addr = snapshot.Value.ToString();
                return string.IsNullOrEmpty(addr) ? "주소 미입력 (현장 수령)" : addr;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"주소 가져오기 실패: {e.Message}");
        }
        return "주소 정보 없음";
    }
}
