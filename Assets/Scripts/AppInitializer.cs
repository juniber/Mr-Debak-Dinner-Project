using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Firebase;

public class AppInitializer : MonoBehaviour
{
    async void Start()
    {
        Debug.Log("Firebase �ʱ�ȭ�� �����մϴ�...");

        // await�� ����Ͽ� Firebase �ʱ�ȭ�� �Ϸ�� ������ ��ٸ���. 
        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("Firebase �ʱ�ȭ ����!");
            }
            else
            {
                Debug.Log($"Firebase ���Ӽ� �ذ� ���� : {dependencyStatus}");
                // �ʱ�ȭ ���� �� ���� ���� 
                QuitApplication("Firebase �ʱ�ȭ�� �����߽��ϴ�. ���� �����մϴ�.");
            }
        });

        Debug.Log("��� �ʱ�ȭ �Ϸ�. MainScene���� �̵�");
        SceneManager.LoadScene("Main");
    }

    private void QuitApplication(string message)
    {
        Debug.LogError(message);

#if UNITY_EDITOR
        // ����Ƽ �����Ϳ��� ���� ���� ���, Play ��带 ����
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // ���� ���(�ȵ���̵�)���� ���� ���� ���, ���� ����
        Application.Quit();
#endif
    }
}
