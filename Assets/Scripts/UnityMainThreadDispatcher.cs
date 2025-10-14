using System;
using System.Collections.Concurrent;
using UnityEngine;

/// Unity�� ���� �����尡 �ƴ� �ٸ� �����忡�� �߻��� �۾��� ���� �����忡�� ������ �� �ֵ��� ���� �̱��� Ŭ����
public class UnityMainThreadDispatcher : MonoBehaviour
{
    // ������κ��� ������ ť(Queue)�� ����Ͽ� ������ �׼�(Action)���� ����
    private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

    // �̱��� �ν��Ͻ�
    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            // �ν��Ͻ��� ���� ���, ������ ã�ų� ���� ����
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
        // �̱��� ������ �����ϰ�, ���� �ٲ� �ı����� �ʵ��� ����
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
        // �� �����Ӹ��� ť�� �۾��� �ִ��� Ȯ��
        while (_executionQueue.TryDequeue(out var action))
        {
            // ť�� �ִ� �۾��� ������ ���� �����忡�� ����
            action.Invoke();
        }
    }

    /// �ٸ� �����忡�� ���� �����忡�� ������ �۾��� ���
    public void Enqueue(Action action)
    {
        _executionQueue.Enqueue(action);
    }
}
