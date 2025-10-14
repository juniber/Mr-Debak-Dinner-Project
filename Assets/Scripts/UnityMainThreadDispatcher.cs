using System;
using System.Collections.Concurrent;
using UnityEngine;

/// Unity의 메인 스레드가 아닌 다른 스레드에서 발생한 작업을 메인 스레드에서 실행할 수 있도록 돕는 싱글톤 클래스
public class UnityMainThreadDispatcher : MonoBehaviour
{
    // 스레드로부터 안전한 큐(Queue)를 사용하여 실행할 액션(Action)들을 저장
    private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

    // 싱글톤 인스턴스
    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            // 인스턴스가 없을 경우, 씬에서 찾거나 새로 생성
            _instance = FindFirstObjectByType<UnityMainThreadDispatcher>();
            if (_instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
            }
        }
        return _instance;
    }

    void Awake()
    {
        // 싱글톤 패턴을 유지하고, 씬이 바뀌어도 파괴되지 않도록 설정
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 매 프레임마다 큐에 작업이 있는지 확인
        while (_executionQueue.TryDequeue(out var action))
        {
            // 큐에 있는 작업을 꺼내서 메인 스레드에서 실행
            action.Invoke();
        }
    }

    /// 다른 스레드에서 메인 스레드에서 실행할 작업을 등록
    public void Enqueue(Action action)
    {
        _executionQueue.Enqueue(action);
    }
}
