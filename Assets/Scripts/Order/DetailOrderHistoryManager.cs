using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DetailOrderHistoryManager : MonoBehaviour
{
    [Header("Navigation")]
    public Button backButton;

    [Header("User Info UI")]
    public TMP_Text phoneText;
    public TMP_Text addressText;
    public TMP_Text requestText;

    [Header("Order Menu UI")]
    public TMP_Text courseNameText;
    public TMP_Text styleText;
    public TMP_Text optionsText; // "스테이크 80g 추가 ..."
    public TMP_Text productPriceText;
    public TMP_Text couponText;

    [Header("Payment UI")]
    public TMP_Text totalPaymentText;

    [Header("Order Info UI")]
    public TMP_Text orderIdText;
    public TMP_Text deliveryDateText;

    private DatabaseReference dbReference;

    private void Awake()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                UIManager.Instance.ShowPanel("OrderHistoryPanel");
            });
        }
    }

    // 외부에서 호출: 패널이 열릴 때 데이터를 채움
    public void Setup(Order order)
    {
        // 1. 주문 정보 표시
        orderIdText.text = order.orderId;

        // 배달 날짜 (예약이면 예약일, 아니면 주문 시간)
        if (order.isReservation)
            deliveryDateText.text = order.deliveryDate;
        else
            deliveryDateText.text = DateTimeOffset.FromUnixTimeSeconds(order.orderTimestamp).LocalDateTime.ToString("yyyy-MM-dd");

        // 요청사항
        requestText.text = string.IsNullOrEmpty(order.globalRequests) ? "(요청사항 없음)" : order.globalRequests;

        // 2. 가격 정보
        productPriceText.text = $"{order.totalPrice:N0}원";
        couponText.text = "-0원"; // 쿠폰 기능 미구현 시 0원 처리
        totalPaymentText.text = $"{order.totalPrice:N0}원";

        // 3. 메뉴 상세 정보 (첫 번째 코스 그룹 기준)
        if (order.courseGroups != null && order.courseGroups.Count > 0)
        {
            var firstGroup = order.courseGroups[0];
            string courseKey = firstGroup.courseType;

            // 코스 이름
            courseNameText.text = MenuData.GetMenuName(courseKey);

            if (firstGroup.details.Count > 0)
            {
                var detail = firstGroup.details[0];

                // 스타일
                styleText.text = $"스타일: {detail.style}";

                // 옵션 (추가/제외) - 앞 2개만 표시하고 ... 처리
                optionsText.text = BuildOptionString(detail);
            }
        }

        // 4. 유저 정보 (전화번호, 주소) 비동기 로드
        LoadUserInfo(order.userId);
    }

    // 유저 정보 로드
    private void LoadUserInfo(string userId)
    {
        // 초기화
        phoneText.text = "불러오는 중...";
        addressText.text = "불러오는 중...";

        dbReference.Child("users").Child(userId).GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || !task.Result.Exists)
            {
                phoneText.text = "-";
                addressText.text = "-";
                return;
            }

            var snapshot = task.Result;
            // UserProfile 클래스가 있다고 가정 (ConfirmOrderManager와 동일)
            // 없으면 Dictionary로 파싱
            var userData = snapshot.Value as Dictionary<string, object>;

            if (userData != null)
            {
                phoneText.text = userData.ContainsKey("phone") ? userData["phone"].ToString() : "-";
                addressText.text = userData.ContainsKey("address") ? userData["address"].ToString() : "-";
            }
        });
    }

    // 옵션 문자열 생성 함수
    private string BuildOptionString(CourseDetail detail)
    {
        List<string> optionList = new List<string>();

        // 1. 추가 항목 (addedItems가 null이 아닐 때만 반복)
        if (detail.addedItems != null)
        {
            foreach (string key in detail.addedItems)
            {
                optionList.Add(MenuData.GetAddonName(key) + " 추가");
            }
        }
        // 2. 제외 항목 (removedItems가 null이 아닐 때만 반복)
        if (detail.removedItems != null)
        {
            foreach (string key in detail.removedItems)
            {
                optionList.Add(MenuData.GetAddonName(key) + " 제외");
            }
        }

        if (optionList.Count == 0) return "(옵션 없음)";

        // LINQ Take 사용
        string result = string.Join(", ", optionList.Take(2));
        if (optionList.Count > 2)
        {
            result += " ...";
        }
        return result;
    }
}
