using Firebase.Database;
using System; // Convert.ToBoolean
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DinnerDetailManager : MonoBehaviour
{
    [Header("Style UI")]
    public Toggle simpleToggle;
    public Toggle grandToggle;
    public Toggle deluxeToggle;
    public TMP_Text styleDescriptionText;

    // '추가' 토글들이 들어있는 부모 오브젝트 (예: 'Content')
    [Header("Option Containers")]
    public Transform addonContainer;
    public GameObject valentineRemoveGroup;
    public GameObject frenchRemoveGroup;
    public GameObject englishRemoveGroup;
    public GameObject champagneRemoveGroup;

    [Header("Navigation Buttons")]
    public Button addCourseButton;
    public Button confirmOrderButton;

    private DatabaseReference dbReference;
    private CourseType currentCourseType;
    private CourseDetail currentCourseDetail;

    // 찾은 토글들을 저장할 리스트와 딕셔너리
    private List<AddonToggleLinker> addonToggles = new List<AddonToggleLinker>();
    private Dictionary<CourseType, List<AddonToggleLinker>> removeTogglesMap = new Dictionary<CourseType, List<AddonToggleLinker>>();

    // '추가' 항목의 재고 소모량을 정의하는 딕셔너리 (중앙 관리)
    private Dictionary<string, AddonInventoryInfo> addonInventoryCosts;

    // 재고 소모량 정의를 위한 작은 헬퍼 클래스
    private class AddonInventoryInfo
    {
        public string InventoryKey;
        public long Amount;
        public AddonInventoryInfo(string key, long amount) { InventoryKey = key; Amount = amount; }
    }

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // 1. '추가' 항목 재고 소모량 맵 초기화
        InitializeInventoryCostMap();

        // 2. '추가' 토글들 자동 스캔
        // addonContainer의 모든 자식(비활성화 포함)에서 AddonToggleLinker를 찾아 리스트에 추가
        addonContainer.GetComponentsInChildren<AddonToggleLinker>(true, addonToggles);

        // 3. '제외' 토글들 자동 스캔 및 맵핑
        removeTogglesMap[CourseType.ValentineDinner] = valentineRemoveGroup.GetComponentsInChildren<AddonToggleLinker>(true).ToList();
        removeTogglesMap[CourseType.FrenchDinner] = frenchRemoveGroup.GetComponentsInChildren<AddonToggleLinker>(true).ToList();
        removeTogglesMap[CourseType.EnglishDinner] = englishRemoveGroup.GetComponentsInChildren<AddonToggleLinker>(true).ToList();
        removeTogglesMap[CourseType.ChampagneFeastDinner] = champagneRemoveGroup.GetComponentsInChildren<AddonToggleLinker>(true).ToList();

        // 4. 스타일 토글 리스너 연결
        simpleToggle.onValueChanged.AddListener((isOn) => OnStyleToggleChanged(isOn, StyleType.Simple));
        grandToggle.onValueChanged.AddListener((isOn) => OnStyleToggleChanged(isOn, StyleType.Grand));
        deluxeToggle.onValueChanged.AddListener((isOn) => OnStyleToggleChanged(isOn, StyleType.Deluxe));

        // 5. 하단 버튼 리스너 연결
        addCourseButton.onClick.AddListener(OnAddCourseClicked);
        confirmOrderButton.onClick.AddListener(OnConfirmOrderClicked);
    }

    // '추가' 항목 재고 소모량 맵을 초기화하는 함수
    private void InitializeInventoryCostMap()
    {
        addonInventoryCosts = new Dictionary<string, AddonInventoryInfo>
        {
            { AddonKeys.AddSteak80g, new AddonInventoryInfo(InventoryKeys.SteakMeatG, 80) },
            { AddonKeys.AddSteak160g, new AddonInventoryInfo(InventoryKeys.SteakMeatG, 160) },
            { AddonKeys.AddMiniCorn2P, new AddonInventoryInfo(InventoryKeys.MiniCornPcs, 2) },
            { AddonKeys.AddPotatoSalad180g, new AddonInventoryInfo(InventoryKeys.PotatoSaladG, 180) },
            { AddonKeys.AddSalad70g, new AddonInventoryInfo(InventoryKeys.SaladGreensG, 70) },
            { AddonKeys.AddBacon18g, new AddonInventoryInfo(InventoryKeys.BaconG, 18) },
            { AddonKeys.AddBaguette3P, new AddonInventoryInfo(InventoryKeys.BaguettePcs, 3) },
            { AddonKeys.AddBaguette6P, new AddonInventoryInfo(InventoryKeys.BaguettePcs, 6) },
            { AddonKeys.AddWineGlass, new AddonInventoryInfo(InventoryKeys.WineServings, 1) },
            { AddonKeys.AddWineBottle, new AddonInventoryInfo(InventoryKeys.WineServings, 5) },
            { AddonKeys.AddCoffeeGlass, new AddonInventoryInfo(InventoryKeys.CoffeeServings, 1) },
            { AddonKeys.AddCoffeePot, new AddonInventoryInfo(InventoryKeys.CoffeeServings, 4) },
            { AddonKeys.AddChampagneBottle, new AddonInventoryInfo(InventoryKeys.ChampagneBottles, 1) }
        };
    }

    // 스타일 토글 설명
    private void OnStyleToggleChanged(bool isOn, StyleType style)
    {
        if (!isOn) return; // 켜질 때만 동작

        switch (style)
        {
            case StyleType.Simple:
                styleDescriptionText.text = "플라스틱 접시와 플라스틱 컵, 종이 냅킨이 플라스틱 쟁반에 제공되고, 와인이 포함되면 잔은 플라스틱 잔 제공";
                break;
            case StyleType.Grand:
                styleDescriptionText.text = "도자기 접시와 도자기 컵, 흰색 면 냅킨이 나무 쟁반에 제공되고, 와인이 포함되며 잔은 플라스틱 잔 제공";
                break;
            case StyleType.Deluxe:
                styleDescriptionText.text = "꽃들이 있는 작은 꽃병, 도자기 접시와 도자기 컵, 린넨 냅킨이 나무 쟁반에 제공되고, 와인이 포함되면 잔은 유리 잔 제공";
                break;
        }
    }

    // "코스 추가하기" 버튼 클릭
    private void OnAddCourseClicked()
    {
        // 1. 현재 패널의 선택 사항을 OrderManager의 currentCourseDetail에 저장
        SaveCurrentSelectionsToOrder();

        // 2. 디버그 로그 출력
        Debug.Log("--- [코스 추가] 현재까지의 주문 내역 ---");
        Debug.Log(JsonUtility.ToJson(OrderManager.Instance.CurrentOrder, true)); // JsonUtility로 예쁘게 출력

        // 3. 패널 이동
        UIManager.Instance.ShowPanel("SelectDinnerPanel");
    }

    // "주문 확인하기" 버튼 클릭
    private void OnConfirmOrderClicked()
    {
        // 1. 현재 패널의 선택 사항을 OrderManager의 currentCourseDetail에 저장
        SaveCurrentSelectionsToOrder();

        // 2. 디버그 로그 출력
        Debug.Log("--- [주문 확인] 최종 주문 내역 ---");
        Debug.Log(JsonUtility.ToJson(OrderManager.Instance.CurrentOrder, true)); // JsonUtility로 예쁘게 출력

        // 3. 패널 이동
        UIManager.Instance.ShowPanel("ConfirmOrderPanel");
    }

    // 현재 UI의 모든 선택 사항을 currentCourseDetail 객체에 저장
    private void SaveCurrentSelectionsToOrder()
    {
        if (currentCourseDetail == null) return;

        // 1. 스타일 저장 
        if (simpleToggle.isOn) currentCourseDetail.style = StyleType.Simple;
        else if (grandToggle.isOn) currentCourseDetail.style = StyleType.Grand;
        else if (deluxeToggle.isOn) currentCourseDetail.style = StyleType.Deluxe;

        // 2. '추가' 항목 저장 
        currentCourseDetail.addedItems.Clear();
        foreach (var linker in addonToggles)
        {
            if (linker.toggle.isOn)
            {
                currentCourseDetail.addedItems.Add(linker.addonKey);
            }
        }

        // 3. '제외' 항목 저장 
        currentCourseDetail.removedItems.Clear();
        if (removeTogglesMap.ContainsKey(currentCourseType))
        {
            foreach (var linker in removeTogglesMap[currentCourseType])
            {
                if (linker.toggle.isOn)
                {
                    currentCourseDetail.removedItems.Add(linker.addonKey);
                }
            }
        }
    }

    // 패널이 활성화될 때마다 호출
    private void OnEnable()
    {
        // 1. 현재 수정할 CourseDetail 객체를 OrderManager에서 가져온다.
        currentCourseDetail = OrderManager.Instance.GetCurrentCourseDetailForEditing();
        if (currentCourseDetail == null)
        {
            Debug.LogError("수정할 코스 정보가 없습니다. SelectDinnerPanel에서 문제가 발생했습니다.");
            UIManager.Instance.ShowPanel("SelectDinnerPanel"); // 오류 발생 시 이전 화면으로
            return;
        }

        // 2. 현재 코스 타입을 OrderManager의 Order 객체에서 알아낸다.
        string courseTypeString = OrderManager.Instance.CurrentOrder.courseGroups.Last().courseType;
        // courseTypeString으로 CourseType을 가져올 수 있다. 
        currentCourseType = (CourseType)Enum.Parse(typeof(CourseType), courseTypeString);

        // 3. UI 초기화 및 재고 확인
        SetupPanelForCourse();
        _ = CheckInventoryAndSetInteractable();
    }

    // 패널 초기화
    private void SetupPanelForCourse()
    {
        // '제외' 그룹 활성화/비활성화 (기존과 동일)
        valentineRemoveGroup.SetActive(currentCourseType == CourseType.ValentineDinner);
        frenchRemoveGroup.SetActive(currentCourseType == CourseType.FrenchDinner);
        englishRemoveGroup.SetActive(currentCourseType == CourseType.EnglishDinner);
        champagneRemoveGroup.SetActive(currentCourseType == CourseType.ChampagneFeastDinner);

        // 스타일 토글 초기화 (기존과 동일)
        simpleToggle.isOn = true;
        grandToggle.isOn = false;
        deluxeToggle.isOn = false;

        // '추가' 토글들도 모두 끔 (자동화된 루프)
        foreach (var linker in addonToggles)
        {
            linker.toggle.isOn = false;
        }

        // '제외' 토글들도 모두 끔 (자동화된 루프)
        if (removeTogglesMap.ContainsKey(currentCourseType))
        {
            foreach (var linker in removeTogglesMap[currentCourseType])
            {
                linker.toggle.isOn = false;
            }
        }
    }

    // (비동기) Firebase에서 재고를 가져와 '추가' 토글들의 활성화 상태를 설정
    private async Task CheckInventoryAndSetInteractable()
    {
        try
        {
            DataSnapshot inventorySnapshot = await dbReference.Child("inventory").GetValueAsync();
            if (!inventorySnapshot.Exists)
            {
                Debug.LogError("재고(Inventory) 데이터를 찾을 수 없습니다.");
                return;
            }

            // 1. 현재 코스의 기본 소모량
            var baseRequirements = MenuData.GetCourseBaseRequirements(currentCourseType);

            // 2. '이전 코스들' (현재 편집 중인 코스 제외)의 총 소모량 계산
            var committedCost = GetCommittedCost();

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                // '추가' 토글 전체를 순회하며 재고 확인
                foreach (var linker in addonToggles)
                {
                    if (addonInventoryCosts.TryGetValue(linker.addonKey, out AddonInventoryInfo cost))
                    {
                        // 헬퍼 함수를 호출하여 재고 확인 및 interactable 설정
                        SetToggleInteractable(linker.toggle, inventorySnapshot, 
                            baseRequirements, committedCost, 
                            cost.InventoryKey, cost.Amount);
                    }
                    else
                    {
                        linker.toggle.interactable = true; // 재고 비용이 없는 항목은 항상 활성화
                    }
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"재고 확인 중 오류 발생: {ex.Message}");
        }
    }

    // (신규) 현재 편집 중인 코스를 '제외한' 장바구니의 모든 재료 소모량을 계산
    private Dictionary<string, long> GetCommittedCost()
    {
        var committedCost = new Dictionary<string, long>();
        var order = OrderManager.Instance.CurrentOrder;

        if (order == null || order.courseGroups == null)
        {
            return committedCost;
        }

        // 장바구니의 모든 코스 그룹(Valentine, French...)을 순회
        foreach (var group in order.courseGroups)
        {
            CourseType type = (CourseType)Enum.Parse(typeof(CourseType), group.courseType);
            var baseReqs = MenuData.GetCourseBaseRequirements(type);

            // 각 코스 상세(Valentine 1, Valentine 2...)를 순회
            foreach (var detail in group.details)
            {
                // 현재 편집 중인 코스(currentCourseDetail)는 계산에서 제외 
                if (detail == currentCourseDetail)
                {
                    continue;
                }

                // (이전 코스)의 기본 재료 소모량 추가
                foreach (var req in baseReqs)
                {
                    if (!committedCost.ContainsKey(req.Key)) committedCost[req.Key] = 0;
                    committedCost[req.Key] += req.Value;
                }

                // (이전 코스)의 추가 재료 소모량 추가
                foreach (string addonKey in detail.addedItems)
                {
                    if (addonInventoryCosts.TryGetValue(addonKey, out AddonInventoryInfo costInfo))
                    {
                        if (!committedCost.ContainsKey(costInfo.InventoryKey)) committedCost[costInfo.InventoryKey] = 0;
                        committedCost[costInfo.InventoryKey] += costInfo.Amount;
                    }
                }

                // TODO: '제외' 항목에 대한 재료 차감 로직 (나중에 추가 가능)
                // 예: detail.removedItems을 순회하며 baseReqs에서 해당 재료 차감
            }
        }
        return committedCost;
    }

    // (Helper) 재고 확인 로직을 처리하는 함수
    private void SetToggleInteractable(Toggle toggle, DataSnapshot inventory, 
                                       Dictionary<string, long> baseReq,      // 현재 코스 기본 소모량
                                       Dictionary<string, long> committedReq, // 이전 코스들 총 소모량
                                       string itemKey, long additionalAmount) // 이 토글의 추가 소모량
    {
        // 1. 현재 코스 기본 소모량 (itemKey에 해당하는)
        long baseAmount = 0;
        baseReq.TryGetValue(itemKey, out baseAmount);

        // 2. 이전 코스들 총 소모량 (itemKey에 해당하는)
        long committedAmount = 0;
        committedReq.TryGetValue(itemKey, out committedAmount);

        // 3. DB의 총 재고
        long currentStock = 0;
        if (inventory.Child(itemKey).Exists)
        {
            currentStock = Convert.ToInt64(inventory.Child(itemKey).Value);
        }

        // 4. 최종 수식: DB재고 >= 이전코스총합 + 현재코스기본 + 이토글추가량
        bool isAvailable = (currentStock >= (committedAmount + baseAmount + additionalAmount));
        toggle.interactable = isAvailable;

        if (!isAvailable)
        {
            Debug.LogWarning($"재고 부족으로 [{toggle.name}] 비활성화: " +
                          $"Key: {itemKey}, " +
                          $"DB재고({currentStock}) < " +
                          $"이전 코스({committedAmount}) + " +
                          $"현재 코스({baseAmount}) + " +
                          $"추가 옵션({additionalAmount})");
        }
    }
}
