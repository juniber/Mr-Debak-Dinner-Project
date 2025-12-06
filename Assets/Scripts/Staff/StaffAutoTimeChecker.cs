using UnityEngine;
using Firebase.Database;
using System.Collections;
using System;

public class StaffAutoTimeChecker : MonoBehaviour
{
    private DatabaseReference dbReference;
    private bool isRunning = false;

    private void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        // 앱이 켜지면 감시 시작
        StartCoroutine(CheckBreakTimeRoutine());
    }

    private IEnumerator CheckBreakTimeRoutine()
    {
        isRunning = true;
        while (isRunning)
        {
            // 1. 10초마다 검사 (너무 자주하면 부하 걸림)
            yield return new WaitForSeconds(10f);

            // 2. Firebase에서 현재 상태 확인
            CheckAndOpenStore();
        }
    }

    private async void CheckAndOpenStore()
    {
        // store_info 데이터 가져오기
        var snapshot = await dbReference.Child("store_info").GetValueAsync();

        if (snapshot.Exists)
        {
            // 현재 영업 중이면 체크할 필요 없음
            bool isOpen = false;
            if (snapshot.Child("isOpen").Value != null)
                bool.TryParse(snapshot.Child("isOpen").Value.ToString(), out isOpen);

            if (isOpen) return; // 이미 열려있으면 패스

            // 휴식 종료 시간이 설정되어 있는지 확인
            if (snapshot.HasChild("breakEndTime"))
            {
                string endTimeStr = snapshot.Child("breakEndTime").Value.ToString();

                // 시간 비교 로직
                if (IsTimePassed(endTimeStr))
                {
                    Debug.Log($"휴식 시간({endTimeStr})이 지났습니다! 자동으로 영업을 재개합니다.");
                    OpenStore();
                }
            }
        }
    }

    // "HH:mm" 문자열과 현재 시간을 비교하는 함수
    private bool IsTimePassed(string targetTimeStr)
    {
        try
        {
            DateTime now = DateTime.Now;

            DateTime targetTime = DateTime.ParseExact(targetTimeStr, "HH:mm", null);

            return now >= targetTime;
        }
        catch
        {
            return false;
        }
    }

    // Firebase에 '영업중'으로 변경 요청
    private void OpenStore()
    {
        var updates = new System.Collections.Generic.Dictionary<string, object>();
        updates["store_info/isOpen"] = true;
        updates["store_info/breakEndTime"] = null; // 종료 시간 삭제

        dbReference.UpdateChildrenAsync(updates);
    }
}