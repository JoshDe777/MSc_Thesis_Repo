using TMPro;
using UnityEngine;
using UnityEngine.Events;
using XRMultiplayer;

public class CrowdOptionsMenu : MonoBehaviour
{
    [SerializeField] GameObject menuPanel;
    [SerializeField] TMP_Dropdown dropdown;

    public readonly UnityEvent OnConfirm = new();
    private CrowdManager m_Manager;

    private void Awake()
    {
        m_Manager = FindAnyObjectByType<CrowdManager>();
        CloseMenu();
    }

    public void CloseMenuAndApplyCrowdState()
    {
        m_Manager.SetDensity((CrowdStates) dropdown.value);
        CloseMenu();
    }
    
    private void CloseMenu()
    {
        // unpause the game & hide the menu panel
        Time.timeScale = 1.0f;
        menuPanel.SetActive(false);
        // OnConfirm listeners set each time the menu is opened -> needs full cleanse after use.
        OnConfirm.Invoke();
        OnConfirm.RemoveAllListeners();
    }

    public void OpenMenu()
    {
        // pause the game
        menuPanel.SetActive(true);
        Time.timeScale = 0.0f;
    }
}
