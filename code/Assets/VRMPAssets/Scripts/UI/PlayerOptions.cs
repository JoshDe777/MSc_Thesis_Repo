using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using TMPro;
using System;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.Android;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace XRMultiplayer
{
    [DefaultExecutionOrder(100)]
    public class PlayerOptions : MonoBehaviour
    {
        [SerializeField] InputActionReference m_ToggleMenuAction;
        [SerializeField] AudioMixer m_Mixer;

        [Header("Panels")]
        [SerializeField] GameObject m_HostRoomPanel;
        [SerializeField] GameObject m_ClientRoomPanel;
        [SerializeField] GameObject[] m_OfflineWarningPanels;
        [SerializeField] GameObject[] m_OnlinePanels;
        [SerializeField] GameObject[] m_Panels;
        [SerializeField] Toggle[] m_PanelToggles;

        [Header("Text Components")]
        [SerializeField] TMP_Text m_SnapTurnText;
        [SerializeField] TMP_Text m_RoomCodeText;
        [SerializeField] TMP_Text m_TimeText;
        [SerializeField] TMP_Text[] m_RoomNameText;
        [SerializeField] TMP_InputField m_RoomNameInputField;
        [SerializeField] TMP_Text[] m_PlayerCountText;

        [Header("Voice Chat")]
        [SerializeField] Button m_MicPermsButton;
        [SerializeField] Image m_LocalPlayerAudioVolume;
        [SerializeField] Image m_MutedIcon;
        [SerializeField] Image m_MicOnIcon;
        [SerializeField] TMP_Text m_VoiceChatStatus;

        [Header("Player Options")]
        [SerializeField] bool m_tunnelingVignetteEnabledByDefault = true;
        [SerializeField] Vector2 m_MinMaxMoveSpeed = new Vector2(2.0f, 10.0f);
        [SerializeField] Vector2 m_MinMaxTurnAmount = new Vector2(15.0f, 180.0f);
        [SerializeField] float m_SnapTurnUpdateAmount = 15.0f;

        DynamicMoveProvider m_MoveProvider;
        SnapTurnProvider m_TurnProvider;
        ContinuousTurnProvider m_ContinuousTurnProvider;
        UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController m_TunnelingVignetteController;

        PermissionCallbacks permCallbacks;

        private void Awake()
        {
            m_MoveProvider = FindFirstObjectByType<DynamicMoveProvider>();
            m_TurnProvider = FindFirstObjectByType<SnapTurnProvider>();
            m_ContinuousTurnProvider = FindAnyObjectByType<ContinuousTurnProvider>();
            m_TunnelingVignetteController = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController>();

            ConnectOnline(false);

            if (m_ToggleMenuAction != null)
                m_ToggleMenuAction.action.performed += ctx => ToggleMenu();
            else
                Utils.Log("No toggle menu action assigned to OptionsPanel", 1);

            permCallbacks = new PermissionCallbacks();
            permCallbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
            permCallbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;

            ToggleTunnelingVignette(m_tunnelingVignetteEnabledByDefault);
        }

        private void UpdateHostVisuals(ulong newHostId)
        {
            
        }

        internal void PermissionCallbacks_PermissionGranted(string permissionName)
        {
            Utils.Log($"{permissionName} PermissionCallbacks_PermissionGranted");
            m_MicPermsButton.gameObject.SetActive(false);
        }

        internal void PermissionCallbacks_PermissionDenied(string permissionName)
        {
            Utils.Log($"{permissionName} PermissionCallbacks_PermissionDenied");
        }

        void OnEnable()
        {
            TogglePanel(0);

            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                m_MicPermsButton.gameObject.SetActive(true);
            }
            else
            {
                m_MicPermsButton.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            
        }

        private void Update()
        {
            m_TimeText.text = $"{DateTime.Now:h:mm}<size=4><voffset=1em>{DateTime.Now:tt}</size></voffset>";
        }

        void ConnectOnline(bool connected)
        {
            foreach (var go in m_OfflineWarningPanels)
            {
                go.SetActive(!connected);
            }

            foreach (var go in m_OnlinePanels)
            {
                go.SetActive(connected);
            }

            if (connected)
            {
                m_MutedIcon.enabled = false;
                m_MicOnIcon.enabled = true;
                m_LocalPlayerAudioVolume.enabled = true;
            }
            else
            {
                ToggleMenu(false);
            }
        }

        public void TogglePanel(int panelID)
        {
            for (int i = 0; i < m_Panels.Length; i++)
            {
                m_PanelToggles[i].SetIsOnWithoutNotify(panelID == i);
                m_Panels[i].SetActive(i == panelID);
            }
        }

        /// <summary>
        /// Toggles the menu on or off.
        /// </summary>
        /// <param name="overrideToggle"></param>
        /// <param name="overrideValue"></param>
        public void ToggleMenu(bool overrideToggle = false, bool overrideValue = false)
        {
            if (overrideToggle)
            {
                gameObject.SetActive(overrideValue);
            }
            else
            {
                ToggleMenu();
            }
            TogglePanel(0);
        }

        public void ToggleMenu()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void LogOut()
        {
            XRINetworkGameManager.Instance.Disconnect();
        }

        public void QuickJoin()
        {
            XRINetworkGameManager.Instance.QuickJoinLobby();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void UpdateVoiceChatStatus(string statusMessage)
        {
            m_VoiceChatStatus.text = $"<b>Voice Chat:</b> {statusMessage}";
        }
        public void SetVolumeLevel(float sliderValue)
        {
            m_Mixer.SetFloat("MainVolume", Mathf.Log10(sliderValue) * 20);
        }

        void MutedChanged(bool muted)
        {
            m_MutedIcon.enabled = muted;
            m_MicOnIcon.enabled = !muted;
            m_LocalPlayerAudioVolume.enabled = !muted;
            PlayerHudNotification.Instance.ShowText($"<b>Microphone: {(muted ? "OFF" : "ON")}</b>");
        }

        // Room Options
        public void UpdateRoomPrivacy(bool toggle)
        {
            XRINetworkGameManager.Instance.sessionManager.UpdateRoomPrivacy(toggle);
        }

        public void SubmitNewRoomName(string text)
        {
            XRINetworkGameManager.Instance.sessionManager.UpdateLobbyName(text);
        }

        void UpdateRoomName(string newValue)
        {
            m_RoomCodeText.text = $"Room Code: {XRINetworkGameManager.ConnectedRoomCode}";
            foreach (var t in m_RoomNameText)
            {
                t.text = XRINetworkGameManager.ConnectedRoomName.Value;
            }
            m_RoomNameInputField.text = XRINetworkGameManager.ConnectedRoomName.Value;
        }

        // Player Options
        public void SetHandOrientation(bool toggle)
        {
            if (toggle)
            {
                m_MoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HandRelative;
            }
        }
        public void SetHeadOrientation(bool toggle)
        {
            if (toggle)
            {
                m_MoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
            }
        }
        public void SetMoveSpeed(float speedPercent)
        {
            m_MoveProvider.moveSpeed = Mathf.Lerp(m_MinMaxMoveSpeed.x, m_MinMaxMoveSpeed.y, speedPercent);
        }

        public void UpdateSnapTurn(int dir)
        {
            float newTurnAmount = Mathf.Clamp(m_TurnProvider.turnAmount + (m_SnapTurnUpdateAmount * dir), m_MinMaxTurnAmount.x, m_MinMaxTurnAmount.y);
            m_TurnProvider.turnAmount = newTurnAmount;
            m_SnapTurnText.text = $"{newTurnAmount}°";
        }
        public void UpdateSmoothTurn(int dir)
        {
            float newTurnSpeed = Mathf.Clamp(m_ContinuousTurnProvider.turnSpeed + (m_SnapTurnUpdateAmount * dir), m_MinMaxTurnAmount.x, m_MinMaxTurnAmount.y);
            m_ContinuousTurnProvider.turnSpeed = newTurnSpeed;
            m_SnapTurnText.text = $"{newTurnSpeed}";
        }

        public void ToggleTunnelingVignette(bool toggle)
        {
            m_TunnelingVignetteController.gameObject.SetActive(toggle);
        }

        public void ToggleFlight(bool toggle)
        {
            var gravityProvider = m_MoveProvider.GetComponent<GravityProvider>();
            if (gravityProvider != null)
            {
                gravityProvider.enabled = !toggle;
            }
            m_MoveProvider.enableFly = toggle;
        }
    }
}
