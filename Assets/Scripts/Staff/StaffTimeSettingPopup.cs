using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class StaffTimeSettingPopup : MonoBehaviour
{
    [Header("Open Time Inputs")]
    [SerializeField] private TMP_InputField openHourInput;
    [SerializeField] private TMP_InputField openMinInput;

    [Header("Close Time Inputs")]
    [SerializeField] private TMP_InputField closeHourInput;
    [SerializeField] private TMP_InputField closeMinInput;

    [Header("Buttons")]
    [SerializeField] private Button confirmBtn;
    [SerializeField] private Button cancelBtn;

    // 데이터 전달을 위한 콜백 함수 (오픈시간, 마감시간)
    private Action<string, string> onConfirmCallback;

    private void Awake()
    {
        confirmBtn.onClick.AddListener(OnConfirmClicked);
        cancelBtn.onClick.AddListener(Close);

        // 처음엔 숨김
        gameObject.SetActive(false);
    }

    // 외부에서 이 팝업을 열 때 호출하는 함수
    public void Open(string currentOpen, string currentClose, Action<string, string> onConfirm)
    {
        this.onConfirmCallback = onConfirm;

        // 기존 시간 쪼개서 입력칸에 넣어주기 (예: "09:00" -> "09", "00")
        SetTimeInput(currentOpen, openHourInput, openMinInput);
        SetTimeInput(currentClose, closeHourInput, closeMinInput);

        gameObject.SetActive(true);
    }

    private void SetTimeInput(string timeStr, TMP_InputField hourField, TMP_InputField minField)
    {
        // "09:00" 형식을 분해
        string[] parts = timeStr.Split(':');
        if (parts.Length == 2)
        {
            hourField.text = parts[0];
            minField.text = parts[1];
        }
    }

    private void OnConfirmClicked()
    {
        // 입력값 검증 및 포맷팅
        string newOpenTime = FormatTime(openHourInput.text, openMinInput.text);
        string newCloseTime = FormatTime(closeHourInput.text, closeMinInput.text);

        // 유효하지 않은 시간이면 (예: 25시 90분) 무시하거나 경고
        if (newOpenTime == null || newCloseTime == null)
        {
            Debug.LogWarning("잘못된 시간 형식입니다.");
            return;
        }

        // 변경된 시간 정보를 메인 패널로 전달
        onConfirmCallback?.Invoke(newOpenTime, newCloseTime);
        Close();
    }

    // 숫자 텍스트를 "HH:mm" 형식으로 변환 (유효성 검사 포함)
    private string FormatTime(string hourStr, string minStr)
    {
        if (int.TryParse(hourStr, out int h) && int.TryParse(minStr, out int m))
        {
            // 시간 범위 체크 (0~23시, 0~59분)
            h = Mathf.Clamp(h, 0, 23);
            m = Mathf.Clamp(m, 0, 59);

            // D2: 한 자리 숫자면 앞에 0을 붙임 (9 -> 09)
            return $"{h:D2}:{m:D2}";
        }
        return null; // 숫자 변환 실패
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}