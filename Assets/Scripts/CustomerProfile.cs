using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CustomerProfile
{
    public List<string> cupons;
    public List<string> orderHistory;

    public CustomerProfile()
    {
        this.cupons = new List<string>();
        this.orderHistory = new List<string>();
    }
}
