using UnityEngine;
using System;

[Serializable]
public class StaffData
{
    public string id;           // UID
    public string name;         // 이름
    public string status;       // 근무 상태
    public string role;         // 역할
    // 프로필 이미지는 추후 추가
}

[Serializable]
public class UserDB
{
    public string name;
    public string phone;
    public string role;
    public string address;
    public string status = "근무중"; // DB에 없을 경우 기본값
}

// [가게 정보 - Firebase 'store_info' 노드]
[Serializable]
public class StoreStatusData
{
    public bool isOpen;
    public string openTime;
    public string closeTime;
    public float completionRate;
    public int totalSales;
}

