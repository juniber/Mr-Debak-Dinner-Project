using UnityEngine;

[System.Serializable]
public class UserProfile
{
    public string role;
    public string name;
    public string phone;
    public string address;

    // ���Һ� ���� ������ (�ش��ϴ� ������ �����ʸ� �����Ͱ� ä����)
    public CustomerProfile customerProfile;
    public StaffProfile staffProfile;

    // ������
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
