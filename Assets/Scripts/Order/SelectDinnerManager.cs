using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using System.Threading.Tasks;
using System.Collections;
using System;

public class SelectDinnerManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button valentineButton;
    public Button frenchButton;
    public Button englishButton;
    public Button champagneButton;

    private DatabaseReference dbReference;

    // 비동기 확인 중 중복 클릭을 방지하기 위한 플래그
    private bool isCheckingValidation = false;

    private void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // 각 버튼 클릭 시 CheckMenuValidationAsync 함수를 적절한 코스 타입과 함께 호출
        valentineButton.onClick.AddListener(() => OnDinnerButtonClicked(CourseType.ValentineDinner));
        frenchButton.onClick.AddListener(() => OnDinnerButtonClicked(CourseType.FrenchDinner));
        englishButton.onClick.AddListener(() => OnDinnerButtonClicked(CourseType.EnglishDinner));
        champagneButton.onClick.AddListener(() => OnDinnerButtonClicked(CourseType.ChampagneFeastDinner));
    }

    // 디너 코스 버튼이 클릭되었을 때 호출
    private void OnDinnerButtonClicked(CourseType courseType)
    {
        // 이미 재고 확인 중이라면 아무것도 하지 않고 함수 종료
        if (isCheckingValidation)
        {
            return;
        }
        // 확인 시작, 플래그를 true로 설정
        isCheckingValidation = true;

        // 상태 메시지를 UIManager를 통해 표시 (5초간 유지)
        UIManager.Instance.ShowTemporaryStatus("메뉴 재고를 확인 중입니다...", 5f);
        // 비동기 함수를 호출하고 결과는 기다리지 않음 (Fire-and-forget)
        _ = CheckMenuValidationAsync(courseType);
    }

    // Firebase DB에 해당 코스가 유효한지(품절이 아닌지) 비동기적으로 확인
    private async Task CheckMenuValidationAsync(CourseType courseType)
    {
        // CourseType enum의 이름을 문자열로 변환 (예: "ValentineDinner")
        string courseKey = courseType.ToString();

        try
        {
            // Firebase DB의 "menuStatus/{코스이름}/isValidation" 경로에서 데이터를 가져옴
            DataSnapshot snapshot = await dbReference.Child("menuStatus").Child(courseKey).Child("isValidation").GetValueAsync();

            bool isAvailable = false;
            if (snapshot.Exists && snapshot.Value != null)
            {
                // (bool)snapshot.Value 대신 Convert.ToBoolean()을 사용
                isAvailable = Convert.ToBoolean(snapshot.Value);
            }

            if (isAvailable) // isAvailable이 true일 때 (주문 가능)
            {
                // 메인 스레드에서 UI 및 OrderManager 작업 수행
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    // OrderManager에 현재 코스를 추가하도록 요청
                    OrderManager.Instance.AddCourseToOrder(courseType);
                    // 메뉴 상세 설정 화면으로 이동
                    UIManager.Instance.ShowPanel("DinnerDetailPanel");
                    isCheckingValidation = false; // 확인 완료, 플래그 해제

                });
            }
            else
            {
                // isValidation이 false이거나 데이터가 없음(품절)
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    UIManager.Instance.ShowTemporaryStatus($"[ {courseKey} ] 메뉴는 품절되었습니다.", 2f);
                    isCheckingValidation = false; // 확인 완료, 플래그 해제
                });
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Firebase Validation Error: {ex.Message}");
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                UIManager.Instance.ShowTemporaryStatus("재고 확인 중 오류가 발생했습니다.", 2f);
                isCheckingValidation = false; // 확인 완료, 플래그 해제
            });
        }
    }
}
