using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using System;

// 'ConfirmOrderPanel'의 UI를 전담 관리하는 스크립트
// - 장바구니 내용 표시
// - 배달 옵션 설정
// - 쿠폰 적용
// - 최종 결제 버튼
public class ConfirmOrderManager : MonoBehaviour
{
    [Header("User Info")]
    public TMP_Text AddressAndPhoneText;

    [Header("Order List")]
    public Transform OrderItemContainer;
    public GameObject OrderItemPrefab;
    public Button DeleteAllButton;
    public Button AddCourseButton;

    [Header("Delivery Settings")]
    public Toggle ImmediateToggle;          // "바로 배달"
    public Toggle ScheduledToggle;          // "예약 배달"
    public GameObject DateSelectionContainer;
    public TMP_Text SelectedDateText;
    public Button OpenCalendarButton;

    [Header("Request")]
    public TMP_InputField RequestInput;

    [Header("Payment")]
    public TMP_Text DiscountAmountText;     // "할인 금액"
    public TMP_Text TotalAmountText;        // "최종 결제 금액"
    public Button ApplyCouponButton;        // "쿠폰 적용" 버튼

    [Header("Navigation")]
    public Button WithdrawOrderButton;
    public Button PaymentButton;

    private Order currentOrder;
    private DatabaseReference dbReference;
    private FirebaseAuth auth;
    private UserProfile currentUserProfile;

    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        DeleteAllButton.onClick.AddListener(OnDeleteAllClicked);
        AddCourseButton.onClick.AddListener(OnAddCourseClicked);
        WithdrawOrderButton.onClick.AddListener(OnWithdrawOrderClicked);
        PaymentButton.onClick.AddListener(OnPaymentClicked);

        // ✅ 쿠폰 패널 열기
        if (ApplyCouponButton != null)
            ApplyCouponButton.onClick.AddListener(OnApplyCouponClicked);

        // 배달 타입 토글
        ImmediateToggle.onValueChanged.AddListener(OnDeliveryTypeChanged);
        ScheduledToggle.onValueChanged.AddListener(OnDeliveryTypeChanged);
        OpenCalendarButton.onClick.AddListener(OnOpenCalendarClicked);

        // 요청사항 입력 끝났을 때
        RequestInput.onEndEdit.AddListener(OnRequestInputChanged);
    }

    private void OnEnable()
    {
        currentOrder = OrderManager.Instance.CurrentOrder;

        // 1) 유저 주소/전화번호 로드
        _ = LoadUserProfileAsync();

        // 2) 주문 리스트 UI 갱신
        PopulateOrderList();

        // 3) 결제 요약(금액, 할인) 갱신
        UpdatePaymentSummary();

        // 4) 요청사항 InputField 채우기
        LoadRequestInput();

        // 5) 배달 설정 초기화
        InitializeDeliverySettings();
    }

    // ----- Firebase에서 UserProfile 읽기 -----

    private async Task LoadUserProfileAsync()
    {
        if (auth.CurrentUser == null) return;

        if (currentUserProfile != null)
        {
            UpdateAddressUI();
            return;
        }

        try
        {
            DataSnapshot snapshot = await dbReference
                .Child("users")
                .Child(auth.CurrentUser.UserId)
                .GetValueAsync();

            if (snapshot.Exists)
            {
                currentUserProfile = JsonUtility.FromJson<UserProfile>(snapshot.GetRawJsonValue());
                UnityMainThreadDispatcher.Instance().Enqueue(UpdateAddressUI);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"사용자 프로필 로드 실패: {ex.Message}");
        }
    }

    private void UpdateAddressUI()
    {
        if (currentUserProfile != null)
        {
            AddressAndPhoneText.text =
                $"주소: {currentUserProfile.address}\n전화번호: {currentUserProfile.phone}";
        }
    }

    // ----- 주문 리스트 UI 구성 -----

    private void PopulateOrderList()
    {
        // 기존 항목 지우기
        foreach (Transform child in OrderItemContainer)
        {
            Destroy(child.gameObject);
        }

        if (currentOrder == null || currentOrder.GetTotalCourseCount() == 0)
        {
            Debug.Log("장바구니가 비어 있습니다.");
            return;
        }

        foreach (var group in currentOrder.courseGroups)
        {
            string groupTypeKey = group.courseType;

            foreach (var detail in group.details)
            {
                GameObject itemGO = Instantiate(OrderItemPrefab, OrderItemContainer);
                OrderItemUI itemUI = itemGO.GetComponent<OrderItemUI>();

                CourseDetail targetDetail = detail;

                itemUI.Populate(groupTypeKey, targetDetail);

                itemUI.ChangeOptionButton.onClick.AddListener(
                    () => OnChangeOptionClicked(targetDetail)
                );
            }
        }
    }

    // ----- 결제 요약 UI 갱신 -----

    private void UpdatePaymentSummary()
    {
        bool isCartEmpty = (currentOrder == null || currentOrder.GetTotalCourseCount() == 0);

        if (currentOrder == null)
        {
            TotalAmountText.text = "총 결제 금액: 0원";
            DiscountAmountText.text = "할인 금액: 0원";
            PaymentButton.interactable = false;
            DeleteAllButton.interactable = false;
            WithdrawOrderButton.interactable = false;
            return;
        }

        // 1) 먼저 기본 총액 계산 (할인 전)
        PriceManager.Instance.CalculateTotalPrice(currentOrder);
        long baseTotal = currentOrder.totalPrice;

        // 2) 쿠폰 적용 여부에 따라 최종 금액 계산
        long finalTotal = baseTotal;
        long discountValue = 0;

        if (currentOrder.coupons != null && currentOrder.coupons.Count > 0)
        {
            finalTotal = PriceManager.Instance.DiscountTotalPrice(currentOrder);
            discountValue = baseTotal - finalTotal;
        }
        else
        {
            currentOrder.totalDiscountPrice = baseTotal;
        }

        // 3) UI 반영
        TotalAmountText.text = $"총 결제 금액: {finalTotal:N0}원";
        DiscountAmountText.text = $"할인 금액: {discountValue:N0}원";

        // 버튼 활성화 여부
        PaymentButton.interactable = !isCartEmpty;
        DeleteAllButton.interactable = !isCartEmpty;
        WithdrawOrderButton.interactable = !isCartEmpty;
    }

    // ----- 요청사항 InputField -----

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

    private void InitializeDeliverySettings()
    {
        if (currentOrder != null && currentOrder.isReservation)
        {
            ScheduledToggle.isOn = true;
            DateSelectionContainer.SetActive(true);
            SelectedDateText.text =
                $"배달 날짜: {DateTime.Parse(currentOrder.deliveryDate):yyyy년 MM월 dd일}";
        }
        else
        {
            ImmediateToggle.isOn = true;
            DateSelectionContainer.SetActive(false);
            SelectedDateText.text = "배달 날짜: (선택되지 않음)";
        }
    }

    // ----- 버튼 핸들러 -----

    private void OnChangeOptionClicked(CourseDetail targetDetail)
    {
        OrderManager.Instance.SetCourseDetailForEditing(targetDetail);
        UIManager.Instance.ShowPanel("DinnerDetailPanel");
    }

    private void OnDeleteAllClicked()
    {
        OrderManager.Instance.ClearOrder();
        currentOrder = null;

        PopulateOrderList();
        UpdatePaymentSummary();
        LoadRequestInput();
        InitializeDeliverySettings();

        PaymentButton.interactable = false;
        DeleteAllButton.interactable = false;
    }

    private void OnAddCourseClicked()
    {
        UIManager.Instance.ShowPanel("SelectDinnerPanel");
    }

    private void OnWithdrawOrderClicked()
    {
        OrderManager.Instance.ClearOrder();
        UIManager.Instance.ShowPanel("CustomerMainPanel");
    }

    private async void OnPaymentClicked()
    {
        PaymentButton.interactable = false;
        UIManager.Instance.ShowTemporaryStatus("주문을 처리하는 중입니다...", 10f);

        try
        {
            OnRequestInputChanged(RequestInput.text);

            bool isReservation = ScheduledToggle.isOn;
            await OrderManager.Instance.FinalizeAndSubmitOrder(isReservation);

            UIManager.Instance.ShowPanel("OrderCompletePanel");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"주문 처리 실패: {ex}");
            if (!ex.Message.Contains("취소"))
            {
                UIManager.Instance.ShowTemporaryStatus(
                    "주문 처리 중 오류가 발생했습니다. 다시 시도해 주세요.",
                    3f
                );
            }
            PaymentButton.interactable = true;
        }
    }

    private void OnRequestInputChanged(string text)
    {
        if (currentOrder != null)
        {
            currentOrder.globalRequests = text;
            Debug.Log("요청사항이 Order 객체에 반영되었습니다.");
        }
    }

    private void OnDeliveryTypeChanged(bool isOn)
    {
        if (!isOn) return;

        if (ImmediateToggle.isOn)
        {
            DateSelectionContainer.SetActive(false);
            if (currentOrder != null) currentOrder.isReservation = false;
        }
        else if (ScheduledToggle.isOn)
        {
            DateSelectionContainer.SetActive(true);

            if (currentOrder != null)
            {
                currentOrder.isReservation = true;
                if (DateTime.Parse(currentOrder.deliveryDate) < DateTime.Today)
                {
                    currentOrder.deliveryDate = DateTime.Today.ToString("yyyy-MM-dd");
                }
                SelectedDateText.text =
                    $"배달 날짜: {DateTime.Parse(currentOrder.deliveryDate):yyyy년 MM월 dd일}";
            }
        }
    }

    private void OnOpenCalendarClicked()
    {
        UIManager.Instance.ShowPanel("CalendarPanel");
    }

    // ✅ 쿠폰 적용 버튼 → 쿠폰 선택 패널로 이동
    private void OnApplyCouponClicked()
    {
        UIManager.Instance.ShowPanel("FindCouponPanel"); // 또는 CouponPanel 이름
    }
}
