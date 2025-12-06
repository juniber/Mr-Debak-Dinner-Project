using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CouponPanelController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform couponListParent;   // 쿠폰 버튼들이 들어갈 부모(스크롤 Content 등)
    [SerializeField] private GameObject couponItemPrefab;  // 쿠폰 한 줄 버튼 프리팹
    [SerializeField] private Button checkButton;           // 하단 Check 버튼

    private FirebaseAuth auth;
    private DatabaseReference dbRef;

    private readonly List<Coupon> userCoupons = new List<Coupon>();
    private Coupon selectedCoupon;

    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // 혹시 Inspector에서 안 넣었으면 자동으로 찾아보기 (안전장치)
        if (couponListParent == null)
        {
            var t = transform.Find("CouponPanel");
            if (t != null) couponListParent = t;
        }

        if (couponItemPrefab == null)
        {
            var t = transform.Find("CouponPanel/Coupon");
            if (t != null) couponItemPrefab = t.gameObject;
        }

        if (checkButton == null)
        {
            var t = transform.Find("Check");
            if (t != null) checkButton = t.GetComponent<Button>();
        }

        if (checkButton != null)
        {
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(OnClickCheck);
        }
        else
        {
            Debug.LogWarning("[CouponPanelController] checkButton 이 비어있습니다.");
        }

        Debug.Log($"[CouponPanelController] Awake - parent={(couponListParent ? couponListParent.name : "NULL")}, " +
                  $"prefab={(couponItemPrefab ? couponItemPrefab.name : "NULL")}");
    }

    private void OnEnable()
    {
        Debug.Log("[CouponPanelController] OnEnable - 쿠폰 로드 시작");
        _ = LoadCouponsAndRenderAsync();
    }

    // DB에서 내 쿠폰 읽어와서 userCoupons에 채운 뒤 버튼 렌더링
    private async Task LoadCouponsAndRenderAsync()
    {
        selectedCoupon = null;
        userCoupons.Clear();

        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogWarning("[CouponPanelController] 로그인 유저가 없습니다.");
            RenderCouponList();
            return;
        }

        try
        {
            var snapshot = await dbRef
                .Child("users")
                .Child(user.UserId)
                .Child("coupons")
                .GetValueAsync();

            Debug.Log($"[CouponPanelController] coupons snapshot.Exists = {snapshot.Exists}");

            if (!snapshot.Exists)
            {
                RenderCouponList();
                return;
            }

            foreach (var child in snapshot.Children)
            {
                string couponId = child.Key;

                // discountRate 읽기
                long discountPercent = 0;
                var discountNode = child.Child("discountRate");
                if (discountNode != null && discountNode.Value != null)
                    long.TryParse(discountNode.Value.ToString(), out discountPercent);

                // used 읽기 (기본값 false)
                bool isUsed = false;
                var usedNode = child.Child("used");
                if (usedNode != null && usedNode.Value != null)
                    bool.TryParse(usedNode.Value.ToString(), out isUsed);

                Debug.Log($"[CouponPanelController] {couponId}: discountRate={discountPercent}, used={isUsed}");

                // 이미 사용한 쿠폰은 목록에서 제외
                if (isUsed) continue;

                userCoupons.Add(new Coupon(couponId, discountPercent, isUsed));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CouponPanelController] 쿠폰 로드 실패: {ex.Message}");
        }

        RenderCouponList();
    }

    // 읽어온 userCoupons를 기반으로 버튼 생성
    private void RenderCouponList()
    {
        if (couponListParent == null || couponItemPrefab == null)
        {
            Debug.LogWarning(
                $"CouponPanelController: couponListParent 또는 couponItemPrefab이 설정되지 않았습니다. " +
                $"parent={(couponListParent ? couponListParent.name : "NULL")}, " +
                $"prefab={(couponItemPrefab ? couponItemPrefab.name : "NULL")}"
            );
            return;
        }

        // 기존 버튼들 삭제
        foreach (Transform child in couponListParent)
        {
            Destroy(child.gameObject);
        }

        if (userCoupons == null || userCoupons.Count == 0)
        {
            Debug.Log("[CouponPanelController] 사용 가능한 쿠폰이 없습니다.");
            return;
        }

        foreach (var c in userCoupons)
        {
            GameObject go = Instantiate(couponItemPrefab, couponListParent);
            TMP_Text label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = $"{c.couponId} ({c.discountAmount}% 할인)";
            }

            Button btn = go.GetComponent<Button>();
            if (btn != null)
            {
                Coupon captured = c;
                btn.onClick.AddListener(() => OnClickCouponButton(captured));
            }
        }
    }

    private void OnClickCouponButton(Coupon coupon)
    {
        selectedCoupon = coupon;
        Debug.Log($"[CouponPanelController] 쿠폰 선택: {coupon.couponId}, {coupon.discountAmount}%");
    }

    private async void OnClickCheck()
    {
        Debug.Log("[CouponPanelController] Check 버튼 클릭됨");

        if (selectedCoupon == null)
        {
            Debug.Log("[CouponPanelController] 선택된 쿠폰이 없음 → 그냥 돌아감");
            UIManager.Instance.ShowPanel("ConfirmOrderPanel");
            return;
        }

        if (OrderManager.Instance == null || OrderManager.Instance.CurrentOrder == null)
        {
            Debug.LogError("[CouponPanelController] OrderManager/CurrentOrder 가 null 입니다.");
            UIManager.Instance.ShowPanel("ConfirmOrderPanel");
            return;
        }

        Order order = OrderManager.Instance.CurrentOrder;

        if (order.coupons == null)
            order.coupons = new List<Coupon>();
        else
            order.coupons.Clear();

        order.coupons.Add(selectedCoupon);

        // 가격 할인 적용
        if (PriceManager.Instance != null)
        {
            PriceManager.Instance.DiscountTotalPrice(order);
        }

        // DB에 used=true 저장
        await MarkCouponAsUsedAsync(selectedCoupon);

        // 주문 확인 패널로 복귀
        UIManager.Instance.ShowPanel("ConfirmOrderPanel");
    }

    private async Task MarkCouponAsUsedAsync(Coupon coupon)
    {
        if (coupon == null) return;
        if (auth.CurrentUser == null) return;

        try
        {
            await dbRef
                .Child("users")
                .Child(auth.CurrentUser.UserId)
                .Child("coupons")
                .Child(coupon.couponId)
                .Child("used")
                .SetValueAsync(true);

            Debug.Log($"[CouponPanelController] 쿠폰 {coupon.couponId} used=true 로 저장 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CouponPanelController] 쿠폰 used 플래그 저장 실패: {ex.Message}");
        }
    }
}
