using TMPro;
using UnityEngine;

public enum LayoutOrdering
{
    MAIN = 0,
    SETTINGS = 1,
    MATCH_INFO = 2,
    ONE_HAND_HITS = 3,
    SET = 4,
    DIG = 5,
    OTHER = 6
}

public class HowToPlayUINavigator : MonoBehaviour
{
    [Header("GameObject Assignments")]
    [SerializeField] private GameObject UIParent;
    [SerializeField] private TMP_Text title;

    [Header("Layouting")]
    // order: main-match-1hand-set-dig-other
    [SerializeField] private GameObject[] layoutGameObjects;
    [SerializeField] private string[] layoutHeaders;

    private GameObject activeLayout = null;

    private void Start()
    {
        if (layoutGameObjects.Length != ((int)LayoutOrdering.OTHER + 1))
            Debug.LogError("Invalid amount of Layout Game Objects assigned!");

        OpenPanel(LayoutOrdering.MAIN);
    }

    private void CloseActivePanel()
    {
        activeLayout?.SetActive(false);
    }

    private void OpenPanel(LayoutOrdering id)
    {
        CloseActivePanel();
        title.text = layoutHeaders[(int)id];
        activeLayout = layoutGameObjects[(int) id];
        activeLayout.SetActive(true);
    }

    public void CloseMenu()
    {
        UIParent.SetActive(false);
    }

    public void OpenLayoutMain() => OpenPanel(LayoutOrdering.MAIN);
    public void OpenLayoutSettings() => OpenPanel(LayoutOrdering.SETTINGS);
    public void OpenLayoutMatch() => OpenPanel(LayoutOrdering.MATCH_INFO);
    public void OpenLayout1Hand() => OpenPanel(LayoutOrdering.ONE_HAND_HITS);
    public void OpenLayoutSet() => OpenPanel(LayoutOrdering.SET);
    public void OpenLayoutDig() => OpenPanel(LayoutOrdering.DIG);
    public void OpenLayoutOther() => OpenPanel(LayoutOrdering.OTHER);
}
