using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // DateTime 사용
using System.Collections.Generic; // List 사용
using System.Globalization; // "yyyy년 MM월" 형식 사용

// 'CalendarPanel'의 로직을 관리
// C#의 DateTime 기능을 사용해 날짜 그리드를 동적으로 생성
public class CalendarManager : MonoBehaviour
{
    [Header("Calendar UI")]
    public TMP_Text monthYearText;
    public Button prevMonthButton;
    public Button nextMonthButton;
    public Button confirmDateButton;
    public Transform dateGridPanel; // 날짜 버튼이 생성될 Grid Layout Group
    public GameObject dateButtonPrefab; // 이전에 만든 'DateButtonPrefab'

    [Header("External Link")]
    // (중요) 날짜를 선택한 후, ConfirmOrderPanel에 있는 텍스트를 업데이트하기 위해 연결
    public TMP_Text selectedDateDisplay;
    public GameObject scheduledDateContainer; // "예약 날짜:" 컨테이너

    private DateTime currentDate; // 현재 달력에 표시된 월
    private DateTime selectedDate; // 사용자가 선택한 날짜
    private List<GameObject> dateButtons = new List<GameObject>(); // 생성된 날짜 버튼들

    void Start()
    {
        currentDate = DateTime.Today;
        selectedDate = DateTime.Today;

        prevMonthButton.onClick.AddListener(OnPrevMonth);
        nextMonthButton.onClick.AddListener(OnNextMonth);
        confirmDateButton.onClick.AddListener(OnConfirmDate);
    }

    private void OnEnable()
    {
        // 패널이 켜질 때마다 오늘 날짜 기준으로 달력을 다시 그림
        currentDate = DateTime.Today;
        selectedDate = DateTime.Today;
        PopulateCalendar(currentDate);
    }

    // 달력의 날짜 그리드를 생성
    private void PopulateCalendar(DateTime date)
    {
        // 1. 기존 날짜 버튼들 삭제
        foreach (GameObject button in dateButtons)
        {
            Destroy(button);
        }
        dateButtons.Clear();

        // 2. 상단 텍스트 변경
        monthYearText.text = date.ToString("yyyy년 MM월", new CultureInfo("ko-KR"));

        // 3. 이 달의 1일이 무슨 요일인지 계산
        DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
        int startDayOfWeek = (int)firstDayOfMonth.DayOfWeek; // 일요일=0, 월요일=1 ...

        // 4. 이 달이 며칠까지 있는지 계산 (윤년 등 자동 계산)
        int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);

        // 5. 달력 앞의 빈 칸 채우기
        for (int i = 0; i < startDayOfWeek; i++)
        {
            GameObject emptySlot = Instantiate(dateButtonPrefab, dateGridPanel);
            emptySlot.GetComponent<Button>().interactable = false; // 클릭 불가
            emptySlot.GetComponentInChildren<TMP_Text>().text = "";
        }

        // 6. 실제 날짜 버튼 생성
        for (int day = 1; day <= daysInMonth; day++)
        {
            GameObject dateButton = Instantiate(dateButtonPrefab, dateGridPanel);
            dateButton.GetComponentInChildren<TMP_Text>().text = day.ToString();

            DateTime buttonDate = new DateTime(date.Year, date.Month, day);

            // 날짜 버튼에 클릭 이벤트 연결
            int dayCapture = day; // 클로저 문제 방지를 위해 변수 복사
            Button buttonComp = dateButton.GetComponent<Button>();
            buttonComp.onClick.AddListener(() => OnDateButtonClicked(dayCapture, dateButton));

            // 오늘보다 이전 날짜는 선택 불가
            if (buttonDate < DateTime.Today)
            {
                buttonComp.interactable = false;
            }

            // 현재 선택된 날짜 하이라이트
            if (buttonDate == selectedDate)
            {
                HighlightButton(dateButton);
            }
        }
    }

    // 날짜 버튼이 클릭되었을 때 호출
    private void OnDateButtonClicked(int day, GameObject clickedButton)
    {
        selectedDate = new DateTime(currentDate.Year, currentDate.Month, day);
        Debug.Log($"선택된 날짜: {selectedDate.ToString("yyyy-MM-dd")}");

        // 모든 버튼의 하이라이트를 끄고, 선택된 버튼만 켭니다.
        HighlightButton(clickedButton);
    }

    // "이 날짜로 선택" 버튼 클릭 시
    private void OnConfirmDate()
    {
        // 1. OrderManager에 예약 날짜 저장
        if (OrderManager.Instance.CurrentOrder != null)
        {
            OrderManager.Instance.CurrentOrder.deliveryDate = selectedDate.ToString("yyyy-MM-dd");
            OrderManager.Instance.CurrentOrder.isReservation = true; // 예약 주문으로 설정
        }

        // 2. ConfirmOrderPanel의 텍스트 업데이트
        selectedDateDisplay.text = $"예약 날짜: {selectedDate:yyyy년 MM월 dd일}";

        // 3. 날짜 선택 컨테이너 활성화
        scheduledDateContainer.SetActive(true);

        // 4. 달력 패널 닫기
        UIManager.Instance.ShowPanel("ConfirmOrderPanel");
    }

    private void OnPrevMonth()
    {
        currentDate = currentDate.AddMonths(-1);
        PopulateCalendar(currentDate);
    }

    private void OnNextMonth()
    {
        currentDate = currentDate.AddMonths(1);
        PopulateCalendar(currentDate);
    }

    private void HighlightButton(GameObject selectedButton)
    {
        // 1. 모든 버튼의 'SelectedHighlight' 이미지를 끈다.
        foreach (GameObject button in dateButtons)
        {
            Transform highlight = button.transform.Find("SelectedHighlight");
            if (highlight != null) highlight.gameObject.SetActive(false);
        }

        // 2. 선택된 버튼의 'SelectedHighlight' 이미지만 켠다.
        Transform selectedHighlight = selectedButton.transform.Find("SelectedHighlight");
        if (selectedHighlight != null) selectedHighlight.gameObject.SetActive(true);
    }
}
