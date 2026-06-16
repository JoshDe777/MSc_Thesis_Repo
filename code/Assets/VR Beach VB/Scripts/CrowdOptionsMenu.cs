using UnityEngine;
using XRMultiplayer;

public class CrowdOptionsMenu : MonoBehaviour
{
    [SerializeField]
    GameObject menuPanel;

    public void CloseMenu()
    {
        // unpause the game
        Time.timeScale = 1.0f;
        menuPanel.SetActive(false);
    }

    public void OpenMenu()
    {
        // pause the game
        Time.timeScale = 0.0f;
        menuPanel.SetActive(true);
    }
}
