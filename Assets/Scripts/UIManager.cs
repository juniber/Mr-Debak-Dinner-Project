using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Firebase;

public class UIManager : MonoBehaviour
{
    // 1. �̱��� �ν��Ͻ� : UIManager�� ��𼭵� UIManager.Instance�� ������ �� �ְ� ���ش�.
    public static UIManager Instance { get; private set; }

    // 2. �г� ����Ʈ : Inspector â���� ������ ��� �г��� �� ����Ʈ�� ���
    [SerializeField]
    private List<GameObject> panels;

    // 3. �г� ��ųʸ� : ���� �̸� ��� �˻��� ���� ���
    private Dictionary<string, GameObject> panelDictionary = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // �̱��� �г� ����
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ���� �ٱ; UIManager�� �ı����� �ʵ��� ����
        }
        else
        {
            Destroy(gameObject);  // �̹� �ν��Ͻ��� �ִٸ� ���� ���� ���� �ı�
            return;
        }

        // ��ųʸ� �ʱ�ȭ : ����Ʈ�� �ִ� �гε��� ��ųʸ��� �̸�(key)�� ���ӿ�����Ʈ(value)�� ����
        foreach (var panel in panels)
        {
            panelDictionary.Add(panel.name, panel);
        }
    }

    private void Start()
    {
        // �� ���� �� ��� �г��� ���� �α��� �гθ� �մϴ�.
        // "LoginPanel"�� Hierarchy�� �ִ� �г��� �̸��� ��Ȯ�� ��ġ�ؾ� �Ѵ�.
        ShowPanel("LoginPanel");
    }

    // 4. ���� �г� ǥ�� �Լ�
    public void ShowPanel(string panelName)
    {
        // ���� ��� �г��� ��Ȱ��ȭ�մϴ�.
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }

        // ��û���� �̸��� �г��� ã�� Ȱ��ȭ
        if (panelDictionary.TryGetValue(panelName, out GameObject panelToShow))
        {
            // �����ֱ� ���� �ش� �гο� �ִ� ��� InputField�� �ؽ�Ʈ�� �ʱ�ȭ�Ѵ�.
            var inputFields = panelToShow.GetComponentsInChildren<TMP_InputField>();
            foreach (var field in inputFields)
            {
                field.text = "";
            }

            panelToShow.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"�г��� ã�� �� �����ϴ�: {panelName}");
        }
    }
}
