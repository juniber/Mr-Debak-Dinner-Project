using UnityEngine;

[System.Serializable]
public class UserProfile
{
    public string role;
    public string name;
    public string phone;
    public string address;

    // 역할별 세부 프로필 (해당하는 역할의 프로필만 데이터가 채워짐)
    public CustomerProfile customerProfile;
    public StaffProfile staffProfile;

    // 생성자
    public UserProfile(string role, string name, string phone)
    {
        this.role = role;
        this.name = name;
        this.phone = phone;
        this.address = "";

        if (role == "Customer")
        {
            this.customerProfile = new CustomerProfile();
        }
        else if (role == "Staff")
        {
            this.staffProfile = new StaffProfile();
        }
    }
}
