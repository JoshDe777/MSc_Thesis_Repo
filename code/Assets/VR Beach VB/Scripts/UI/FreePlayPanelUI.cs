using UnityEngine;
using Volleyball;

public class FreePlayPanelUI : MonoBehaviour
{
    [Header("GameObject & Script Assignment")]
    [SerializeField] private HowToPlayUINavigator rulesMenu;
    [SerializeField] private VBMatchManager matchManager;
    [SerializeField] private GameObject freeplayPanel;

    public void ExitFreeplay()
    {
        matchManager.ExitFreeplay();
        rulesMenu.CloseMenu();
        freeplayPanel.SetActive(false);
    }
}
