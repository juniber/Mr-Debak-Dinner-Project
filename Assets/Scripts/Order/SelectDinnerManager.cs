using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using System.Threading.Tasks;
using System.Collections;

public class SelectDinnerManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button valentineButton;
    public Button frenchButton;
    public Button englishButton;
    public Button champagneButton;
    public TMP_Text statusText;     // "메뉴 품절"을 표시할 텍스트

    private DatabaseReference dbReference;

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
        statusText.text = "메뉴 재고를 확인 중입니다...";
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

            // isValidation이 true이면 다음 단계로 진행
            if (snapshot.Exists && (bool)snapshot.Value == true)
            {
                // 메인 스레드에서 UI 및 OrderManager 작업 수행
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    statusText.text = "";
                    // OrderManager에 현재 코스를 추가하도록 요청
                    OrderManager.Instance.AddCourseToOrder(courseType);
                    // 메뉴 상세 설정 화면으로 이동
                    UIManager.Instance.ShowPanel("DinnerDetailPanel");
                });
            }
            else
            {
                // isValidation이 false이거나 데이터가 없음(품절)
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    StartCoroutine(ShowTemporaryStatusMessage($"[ {courseKey} ] 메뉴는 품절되었습니다.", 2f));
                });
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Firebase Validation Error: {ex.Message}");
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                StartCoroutine(ShowTemporaryStatusMessage("재고 확인 중 오류가 발생했습니다.", 2f));
            });
        }
    }

    // 지정된 시간 후 메시지를 자동으로 지우는 코루틴
    private IEnumerator ShowTemporaryStatusMessage(string message, float delay)
    {
        statusText.text = message;
        yield return new WaitForSeconds(delay);
        statusText.text = "";
    }
}
