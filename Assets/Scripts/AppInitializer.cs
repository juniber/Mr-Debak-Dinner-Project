using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Firebase;

public class AppInitializer : MonoBehaviour
{
    async void Start()
    {
        Debug.Log("Firebase 초기화를 시작합니다...");

        // await를 사용하여 Firebase 초기화가 완료될 때까지 기다린다. 
        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("Firebase 초기화 성공!");
            }
            else
            {
                Debug.Log($"Firebase 종속성 해결 실패 : {dependencyStatus}");
                // 초기화 실패 시 앱을 종료 
                QuitApplication("Firebase 초기화에 실패했습니다. 앱을 종료합니다.");
            }
        });

        Debug.Log("모든 초기화 완료. MainScene으로 이동");
        SceneManager.LoadScene("Main");
    }

    private void QuitApplication(string message)
    {
        Debug.LogError(message);

#if UNITY_EDITOR
        // 유니티 에디터에서 실행 중일 경우, Play 모드를 중지
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 기기(안드로이드)에서 실행 중일 경우, 앱을 종료
        Application.Quit();
#endif
    }
}
