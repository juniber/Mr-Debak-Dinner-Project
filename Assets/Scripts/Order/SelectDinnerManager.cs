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
    public TMP_Text statusText;     // "�޴� ǰ��"�� ǥ���� �ؽ�Ʈ

    private DatabaseReference dbReference;

    private void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // �� ��ư Ŭ�� �� CheckMenuValidationAsync �Լ��� ������ �ڽ� Ÿ�԰� �Բ� ȣ��
        valentineButton.onClick.AddListener(() => OnDinnerButtonClicked(CourseType.ValentineDinner));
        frenchButton.onClick.AddListener(() => OnDinnerButtonClicked(CourseType.FrenchDinner));
        englishButton.onClick.AddListener(() => OnDinnerButtonClicked(CourseType.EnglishDinner));
        champagneButton.onClick.AddListener(() => OnDinnerButtonClicked(CourseType.ChampagneFeastDinner));
    }

    // ��� �ڽ� ��ư�� Ŭ���Ǿ��� �� ȣ��
    private void OnDinnerButtonClicked(CourseType courseType)
    {
        statusText.text = "�޴� ��� Ȯ�� ���Դϴ�...";
        // �񵿱� �Լ��� ȣ���ϰ� ����� ��ٸ��� ���� (Fire-and-forget)
        _ = CheckMenuValidationAsync(courseType);
    }

    // Firebase DB�� �ش� �ڽ��� ��ȿ����(ǰ���� �ƴ���) �񵿱������� Ȯ��
    private async Task CheckMenuValidationAsync(CourseType courseType)
    {
        // CourseType enum�� �̸��� ���ڿ��� ��ȯ (��: "ValentineDinner")
        string courseKey = courseType.ToString();

        try
        {
            // Firebase DB�� "menuStatus/{�ڽ��̸�}/isValidation" ��ο��� �����͸� ������
            DataSnapshot snapshot = await dbReference.Child("menuStatus").Child(courseKey).Child("isValidation").GetValueAsync();

            // isValidation�� true�̸� ���� �ܰ�� ����
            if (snapshot.Exists && (bool)snapshot.Value == true)
            {
                // ���� �����忡�� UI �� OrderManager �۾� ����
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    statusText.text = "";
                    // OrderManager�� ���� �ڽ��� �߰��ϵ��� ��û
                    OrderManager.Instance.AddCourseToOrder(courseType);
                    // �޴� �� ���� ȭ������ �̵�
                    UIManager.Instance.ShowPanel("DinnerDetailPanel");
                });
            }
            else
            {
                // isValidation�� false�̰ų� �����Ͱ� ����(ǰ��)
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    StartCoroutine(ShowTemporaryStatusMessage($"[ {courseKey} ] �޴��� ǰ���Ǿ����ϴ�.", 2f));
                });
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Firebase Validation Error: {ex.Message}");
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                StartCoroutine(ShowTemporaryStatusMessage("��� Ȯ�� �� ������ �߻��߽��ϴ�.", 2f));
            });
        }
    }

    // ������ �ð� �� �޽����� �ڵ����� ����� �ڷ�ƾ
    private IEnumerator ShowTemporaryStatusMessage(string message, float delay)
    {
        statusText.text = message;
        yield return new WaitForSeconds(delay);
        statusText.text = "";
    }
}
