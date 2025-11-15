using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using System;

public class ConfirmOrderManager : MonoBehaviour
{
    [Header("User Info")]
    public TMP_Text AddressAndPhoneText;

    [Header("Order List")]
    public Transform OrderItemContainer; // 스크롤 뷰의 'Content' 오브젝트
    public GameObject OrderItemPrefab;   // 'OrderItemPrefab' 프리팹
    public Button DeleteAllButton;
    public Button AddCourseButton;

    [Header("Request")]
    public TMP_InputField RequestInput;

    [Header("Payment")]
    public TMP_Text DiscountAmountText;
    public TMP_Text TotalAmountText;
    public Button ApplyCouponButton;

    [Header("Navigation")]
    public Button WithdrawOrderButton;
    public Button GoToPaymentButton;

    private Order currentOrder;
    private DatabaseReference dbReference;
    private FirebaseAuth auth;
    private UserProfile currentUserProfile;  // 사용자 주소/전화번호 캐시

    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        DeleteAllButton.onClick.AddListener(OnDeleteAllClicked);
        AddCourseButton.onClick.AddListener(OnAddCourseClicked);
        WithdrawOrderButton.onClick.AddListener(OnWithdrawOrderClicked);
        GoToPaymentButton.onClick.AddListener(OnGoToPaymentClicked);

        // 요청사항은 입력이 끝났을 때(수정 완료) 저장
        RequestInput.onEndEdit.AddListener(OnRequestInputChanged);
    }

    private void OnEnable()
    {
        currentOrder = OrderManager.Instance.CurrentOrder;

        // 장바구니 상태 확인
        bool isCartEmpty = (currentOrder == null || currentOrder.GetTotalCourseCount() == 0);

        // 장바구니가 비어있으면 결제 버튼 비활성화
        GoToPaymentButton.interactable = !isCartEmpty;
        DeleteAllButton.interactable = !isCartEmpty;

        // 1. 사용자 주소/전화번호 불러오기
        _ = LoadUserProfileAsync();

        // 2. 주문 목록 UI 생성
        PopulateOrderList();

        // 3. 결제 요약 정보 업데이트
        UpdatePaymentSummary();

        // 4. 저장된 요청사항 불러오기
        LoadRequestInput();
    }

    // Firebase에서 현재 로그인한 유저의 프로필(주소, 전화번호)을 가져온다,
    private async Task LoadUserProfileAsync()
    {
        if (auth.CurrentUser == null) return;

        // 이미 유저 정보가 있다면 다시 불러오지 않음
        if (currentUserProfile != null)
        {
            UpdateAddressUI();
            return;
        }

        try
        {
            DataSnapshot snapshot = await dbReference.Child("users").Child(auth.CurrentUser.UserId).GetValueAsync();
            if (snapshot.Exists)
            {
                currentUserProfile = JsonUtility.FromJson<UserProfile>(snapshot.GetRawJsonValue());
                UnityMainThreadDispatcher.Instance().Enqueue(UpdateAddressUI);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"사용자 프로필 로드 실패: {ex.Message}");
        }
    }

    private void UpdateAddressUI()
    {
        if (currentUserProfile != null)
        {
            AddressAndPhoneText.text = $"주소: {currentUserProfile.address}\n전화번호: {currentUserProfile.phone}";
        }
    }

    // OrderManager의 CurrentOrder를 기반으로 주문 목록 UI를 생성
    private void PopulateOrderList()
    {
        // 1. 기존에 생성된 아이템 UI들을 모두 삭제
        foreach (Transform child in OrderItemContainer)
        {
            Destroy(child.gameObject);
        }

        if (currentOrder == null || currentOrder.GetTotalCourseCount() == 0)
        {
            Debug.Log("장바구니가 비어있습니다.");
            return;
        }

        // 2. Order 객체를 순회하며 UI 아이템 생성
        foreach (var group in currentOrder.courseGroups)
        {
            string groupTypeKey = group.courseType; // "ValentineDinner"

            foreach (var detail in group.details)
            {
                // 3. Prefab을 Content 자식으로 생성
                GameObject itemGO = Instantiate(OrderItemPrefab, OrderItemContainer);
                OrderItemUI itemUI = itemGO.GetComponent<OrderItemUI>();

                // 4. 이 아이템이 어떤 CourseDetail을 참조하는지 저장
                CourseDetail targetDetail = detail;

                // 5. UI에 데이터 채우기
                itemUI.Populate(groupTypeKey, targetDetail);

                // 6. "옵션 변경" 버튼에 리스너 동적 연결
                itemUI.ChangeOptionButton.onClick.AddListener(() => OnChangeOptionClicked(targetDetail));
            }
        }
    }

    // 총 결제 금액, 할인 금액 등 UI를 업데이트
    private void UpdatePaymentSummary()
    {
        if (currentOrder == null)
        {
            TotalAmountText.text = "총 결제 금액: 0원";
            DiscountAmountText.text = "할인 금액: 0원";
            return;
        }

        // 1. PriceManager에 총 가격 계산 요청
        PriceManager.Instance.CalculateTotalPrice(currentOrder);

        // 2. UI에 반영
        TotalAmountText.text = $"총 결제 금액: {currentOrder.totalPrice:N0}원";
        DiscountAmountText.text = "할인 금액: 0원"; // (쿠폰 기능은 나중에)
    }

    // Order 객체에 저장된 요청사항을 InputField에 불러온다, 
    private void LoadRequestInput()
    {
        if (currentOrder != null)
        {
            RequestInput.text = currentOrder.globalRequests;
        }
        else
        {
            RequestInput.text = "";
        }
    }

    // "옵션 변경" 버튼 클릭 시
    private void OnChangeOptionClicked(CourseDetail targetDetail)
    {
        // 1. OrderManager에 지금 수정할 CourseDetail이 무엇인지 알려줍니다. (핵심)
        OrderManager.Instance.SetCourseDetailForEditing(targetDetail);

        // 2. DinnerDetailPanel로 이동
        UIManager.Instance.ShowPanel("DinnerDetailPanel");
    }

    // "전체 삭제" 버튼 클릭 시
    private void OnDeleteAllClicked()
    {
        OrderManager.Instance.ClearOrder();
        currentOrder = null;

        // UI 즉시 갱신
        PopulateOrderList();
        UpdatePaymentSummary();
        LoadRequestInput();

        // 장바구니가 비었으므로 버튼들 비활성화
        GoToPaymentButton.interactable = false;
        DeleteAllButton.interactable = false;
    }

    // "코스 추가하기" 버튼 클릭 시
    private void OnAddCourseClicked()
    {
        UIManager.Instance.ShowPanel("SelectDinnerPanel");
    }

    // "주문 취소하기" 버튼 클릭 시
    private void OnWithdrawOrderClicked()
    {
        OrderManager.Instance.ClearOrder();
        UIManager.Instance.ShowPanel("CustomerMainPanel");
    }

    // "결제하기" 버튼 클릭 시
    private void OnGoToPaymentClicked()
    {
        // TODO: 결제 전 유효성 검사 (주문이 비어있는지 등)

        UIManager.Instance.ShowPanel("PaymentPanel"); // (PaymentPanel은 나중에 만듭니다)
    }

    // "가게 요청사항" InputField 입력 완료 시
    private void OnRequestInputChanged(string text)
    {
        if (currentOrder != null)
        {
            currentOrder.globalRequests = text;
            Debug.Log("요청사항이 Order 객체에 저장되었습니다.");
        }
    }
}
