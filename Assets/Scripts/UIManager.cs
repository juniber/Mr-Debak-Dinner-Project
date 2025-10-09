using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Firebase;

public class UIManager : MonoBehaviour
{
    // 1. 싱글톤 인스턴스 : UIManager를 어디서든 UIManager.Instance로 접근할 수 있게 해준다.
    public static UIManager Instance { get; private set; }

    // 2. 패널 리스트 : Inspector 창에서 관리할 모든 패널을 이 리스트에 등록
    [SerializeField]
    private List<GameObject> panels;

    // 3. 패널 딕셔너리 : 빠른 이름 기반 검색을 위해 사용
    private Dictionary<string, GameObject> panelDictionary = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // 싱글톤 패널 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바귀어도 UIManager가 파괴되지 않도록 설정
        }
        else
        {
            Destroy(gameObject);  // 이미 인스턴스가 있다면 새로 생긴 것은 파괴
            return;
        }

        // 딕셔너리 초기화 : 리스트에 있는 패널들을 딕셔너리에 이름(key)과 게임오브젝트(value)로 저장
        foreach (var panel in panels)
        {
            panelDictionary.Add(panel.name, panel);
        }
    }

    private void Start()
    {
        // 맵 시작 시 모든 패널을 끄고 로그인 패널만 켭니다.
        // "LoginPanel"은 Hierarchy에 있는 패널의 이름과 정확히 일치해야 한다.
        ShowPanel("LoginPanel");
    }

    // 4. 범용 패널 표시 함수
    public void ShowPanel(string panelName)
    {
        // 먼저 모든 패널을 비활성화합니다.
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }

        // 요청받은 이름의 패널을 찾아 활성화
        if (panelDictionary.TryGetValue(panelName, out GameObject panelToShow))
        {
            // 보여주기 전에 해당 패널에 있는 모든 InputField의 텍스트를 초기화한다.
            var inputFields = panelToShow.GetComponentsInChildren<TMP_InputField>();
            foreach (var field in inputFields)
            {
                field.text = "";
            }

            panelToShow.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"패널을 찾을 수 없습니다: {panelName}");
        }
    }
}
