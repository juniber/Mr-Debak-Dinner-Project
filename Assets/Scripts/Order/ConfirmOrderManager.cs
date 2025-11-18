using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using System;

// 'ConfirmOrderPanel'의 UI와 로직을 관리
// 주문 내역(장바구니)을 요약해서 보여주고, 배달 옵션을 설정
// 수정/삭제/결제 요청을 처리
public class ConfirmOrderManager : MonoBehaviour
{
    [Header("User Info")]
    public TMP_Text AddressAndPhoneText;

    [Header("Order List")]
    public Transform OrderItemContainer; // 스크롤 뷰의 'Content' 오브젝트
    public GameObject OrderItemPrefab;   // 'OrderItemPrefab' 프리팹
    public Button DeleteAllButton;
    public Button AddCourseButton;

    [Header("Delivery Settings")]
    public Toggle ImmediateToggle; // "즉시 배달" 토글
    public Toggle ScheduledToggle; // "예약 배달" 토글
    public GameObject DateSelectionContainer; // "예약 날짜: [..] [날짜 변경]" 버튼의 부모
    public TMP_Text SelectedDateText; // "예약 날짜: [..]" 텍스트
    public Button OpenCalendarButton; // "날짜 변경" 버튼

    [Header("Request")]
    public TMP_InputField RequestInput;

    [Header("Payment")]
    public TMP_Text DiscountAmountText;
    public TMP_Text TotalAmountText;
    public Button ApplyCouponButton;

    [Header("Navigation")]
    public Button WithdrawOrderButton;
    public Button PaymentButton;

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
        PaymentButton.onClick.AddListener(OnPaymentClicked);

        // 배달 설정 리스너 연결
        ImmediateToggle.onValueChanged.AddListener(OnDeliveryTypeChanged);
        ScheduledToggle.onValueChanged.AddListener(OnDeliveryTypeChanged);
        OpenCalendarButton.onClick.AddListener(OnOpenCalendarClicked);

        // 요청사항은 입력이 끝났을 때(수정 완료) 저장
        RequestInput.onEndEdit.AddListener(OnRequestInputChanged);
    }

    private void OnEnable()
    {
        currentOrder = OrderManager.Instance.CurrentOrder;

        // 1. 사용자 주소/전화번호 불러오기
        _ = LoadUserProfileAsync();

        // 2. 주문 목록 UI 생성
        PopulateOrderList();

        // 3. 결제 요약 정보 업데이트
        UpdatePaymentSummary();

        // 4. 저장된 요청사항 불러오기
        LoadRequestInput();

        // 배달 설정 UI 초기화
        InitializeDeliverySettings();
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
        // 장바구니 상태 확인
        bool isCartEmpty = (currentOrder == null || currentOrder.GetTotalCourseCount() == 0);

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

        // 3. 장바구니 상태에 따라 버튼 활성화/비활성화
        PaymentButton.interactable = !isCartEmpty;
        DeleteAllButton.interactable = !isCartEmpty;
        WithdrawOrderButton.interactable = !isCartEmpty;
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

    // Order 객체의 배달 설정값을 UI에 로드
    private void InitializeDeliverySettings()
    {
        if (currentOrder != null && currentOrder.isReservation)
        {
            // 예약 주문 상태로 복원
            ScheduledToggle.isOn = true;
            DateSelectionContainer.SetActive(true);
            SelectedDateText.text = $"예약 날짜: {DateTime.Parse(currentOrder.deliveryDate):yyyy년 MM월 dd일}";
        }
        else
        {
            // 즉시 배달 상태로 초기화
            ImmediateToggle.isOn = true;
            DateSelectionContainer.SetActive(false);
            SelectedDateText.text = "예약 날짜: (선택되지 않음)";
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
        InitializeDeliverySettings(); 

        // 장바구니가 비었으므로 버튼들 비활성화
        PaymentButton.interactable = false;
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
    private async void OnPaymentClicked()
    {
        // 1. 버튼 비활성화 (중복 전송 방지)
        PaymentButton.interactable = false;
        UIManager.Instance.ShowTemporaryStatus("주문을 전송하는 중...", 10f); // 10초간 로딩 메시지

        try
        {
            // 2. (안전 장치) InputField의 현재 텍스트를 Order 객체에 즉시 저장
            OnRequestInputChanged(RequestInput.text);

            // 3. 현재 토글 상태(즉시/예약)를 OrderManager에 전달
            bool isReservation = ScheduledToggle.isOn;
            await OrderManager.Instance.FinalizeAndSubmitOrder(isReservation);

            // 4. 성공 시, 주문 완료 패널로 이동
            UIManager.Instance.ShowPanel("OrderCompletePanel");
        }
        catch (Exception ex)
        {
            // 5. 실패 시, 사용자에게 알리고 버튼 다시 활성화
            Debug.LogError($"주문 전송 실패: {ex}");
            // UIManager가 이미 "재고 부족" 메시지를 띄웠을 수 있음
            if (!ex.Message.Contains("재고")) // 재고 오류가 아닐 경우에만 추가 메시지
            {
                UIManager.Instance.ShowTemporaryStatus("주문 전송에 실패했습니다. 다시 시도해주세요.", 3f);
            }
            PaymentButton.interactable = true;
        }
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

    // 배달 유형 토글이 변경되었을 때 호출
    private void OnDeliveryTypeChanged(bool isOn)
    {
        // isOn이 false일 때는(토글이 꺼질 때) 아무것도 하지 않음
        if (!isOn) return;

        if (ImmediateToggle.isOn)
        {
            // 즉시 배달 선택됨
            DateSelectionContainer.SetActive(false);
            if (currentOrder != null) currentOrder.isReservation = false;
        }
        else if (ScheduledToggle.isOn)
        {
            // 예약 배달 선택됨
            DateSelectionContainer.SetActive(true);

            // 만약 날짜가 아직 선택 안됐으면, 오늘 날짜로 기본 설정
            if (currentOrder != null)
            {
                currentOrder.isReservation = true;
                // 날짜가 이미 설정되었는지 확인, 안됐으면 오늘 날짜로
                if (DateTime.Parse(currentOrder.deliveryDate) < DateTime.Today)
                {
                    currentOrder.deliveryDate = DateTime.Today.ToString("yyyy-MM-dd");
                }
                SelectedDateText.text = $"예약 날짜: {DateTime.Parse(currentOrder.deliveryDate):yyyy년 MM월 dd일}";
            }
        }
    }

    // "날짜 변경" 버튼 클릭 시
    private void OnOpenCalendarClicked()
    {
        // CalendarPanel을 띄움
        UIManager.Instance.ShowPanel("CalendarPanel");
    }
}
