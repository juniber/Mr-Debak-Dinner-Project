using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SBreakTimePopup : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField minuteInput; // "30" 입력

    [Header("Buttons")]
    [SerializeField] private Button confirmBtn;
    [SerializeField] private Button cancelBtn;

    // 몇 분인지(int) 전달하는 콜백
    private Action<int> onConfirmCallback;

    private void Awake()
    {
        confirmBtn.onClick.AddListener(OnConfirmClicked);
        cancelBtn.onClick.AddListener(Close);
        gameObject.SetActive(false);
    }

    public void Open(Action<int> onConfirm)
    {
        onConfirmCallback = onConfirm;
        minuteInput.text = ""; // 초기화
        gameObject.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        if (int.TryParse(minuteInput.text, out int minutes))
        {
            if (minutes > 0)
            {
                onConfirmCallback?.Invoke(minutes);
                Close();
            }
            else
            {
                Debug.LogWarning("0보다 큰 시간을 입력해주세요.");
            }
        }
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}