using UnityEngine;
using XRMultiplayer;

public class CrowdOptionsMenu : MonoBehaviour
{
    [SerializeField]
    GameObject menuPanel;

    public void CloseMenu()
    {
        menuPanel.SetActive(false);
    }

    public void OpenMenu()
    {
        menuPanel.SetActive(true);
    }
}
