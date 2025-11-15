using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

// 'OrderItemPrefab'에 붙어, 하위 UI 요소들의 데이터를 채워주는 헬퍼 스크립트
public class OrderItemUI : MonoBehaviour
{
    [Header("Prefab UI Elements")]
    public TMP_Text CourseNameText;
    public TMP_Text StyleText;
    public TMP_Text AddonsText;
    public TMP_Text PriceText;
    public Button ChangeOptionButton;

    // ConfirmOrderManager가 이 함수를 호출하여 UI를 채운다. 
    public void Populate(string courseTypeKey, CourseDetail detail)
    {
        CourseType courseType = (CourseType)System.Enum.Parse(typeof(CourseType), courseTypeKey);

        // 1. 코스 이름
        CourseNameText.text = courseTypeKey; // TODO: "ValentineDinner" 대신 "발렌타인 디너"로 표시하려면 변환 로직 필요

        // 2. 스타일
        StyleText.text = $"스타일: {detail.style.ToString()}";

        // 3. 추가/제외 목록 (요청사항: 2개 + "...")
        var allOptions = new List<string>(detail.addedItems);
        allOptions.AddRange(detail.removedItems);

        if (allOptions.Count == 0)
        {
            AddonsText.text = "기본 설정";
        }
        else if (allOptions.Count <= 2)
        {
            AddonsText.text = string.Join(", ", allOptions);
        }
        else
        {
            // 2개만 표시하고 "..." 추가
            AddonsText.text = $"{allOptions[0]}, {allOptions[1]} ...";
        }

        // 4. 이 코스 1개의 가격 계산
        long itemPrice = PriceManager.Instance.GetCoursePrice(courseType);
        itemPrice += PriceManager.Instance.GetStylePrice(detail.style);
        foreach (string key in detail.addedItems)
        {
            itemPrice += PriceManager.Instance.GetAddonPrice(key);
        }
        PriceText.text = $"{itemPrice:N0}원";
    }
}
