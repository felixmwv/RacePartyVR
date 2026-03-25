using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
   public static MenuManager Instance;

   [Header("Panels")]
   [SerializeField] private GameObject playerSelectPanel;
   [SerializeField] private GameObject gamemodePanel;
   [SerializeField] private GameObject raceSettingsPanel;
   [SerializeField] private GameObject hotlapSettingsPanel;
   [SerializeField] private GameObject menuCamera;
   
   [Header("Race Settings UI")]
   [SerializeField] private Slider lapSlider;
   [SerializeField] private TextMeshProUGUI lapValueText;
   [SerializeField] private Toggle infiniteLapsToggle;

   private void Awake()
   {
      if (Instance != null)
      {
         Destroy(gameObject);
         return;
      }

      Instance = this;
   }

   private void Start()
   {
      ShowPlayerSelect();
      menuCamera.SetActive(true);
   }

   private void DisableAll()
   {
      playerSelectPanel.SetActive(false);
      gamemodePanel.SetActive(false);
      raceSettingsPanel.SetActive(false);
      hotlapSettingsPanel.SetActive(false);
   }
   
   public void OnLapSliderChanged(float value)
   {
      int laps = Mathf.RoundToInt(value);

      lapValueText.text = $"Laps: {laps}";

      GameModeManager.Instance.SetRaceMode(laps, infiniteLapsToggle.isOn);
   }
   
   public void OnInfiniteLapsToggled(bool isOn)
   {
      lapSlider.interactable = !isOn;

      if (isOn)
      {
         lapValueText.text = "Laps: ∞";
         GameModeManager.Instance.SetRaceMode(0, true);
      }
      else
      {
         int laps = Mathf.RoundToInt(lapSlider.value);
         lapValueText.text = $"Laps: {laps}";
         GameModeManager.Instance.SetRaceMode(laps, false);
      }
   }

   public void ShowPlayerSelect()
   {
      DisableAll();
      playerSelectPanel.SetActive(true);
   }

   public void ContinueFromPlayerSelect()
   {
      DisableAll();
      gamemodePanel.SetActive(true);
   }

   public void SelectRaceMode()
   {
      DisableAll();
      raceSettingsPanel.SetActive(true);

      // init default UI state
      lapSlider.value = 5;
      infiniteLapsToggle.isOn = false;
      OnLapSliderChanged(lapSlider.value);
   }


   public void SelectHotlapMode()
   {
      DisableAll();
      GameModeManager.Instance.SetHotlapMode(-1);
      hotlapSettingsPanel.SetActive(true);
   }

   public void StartGame()
   {
      if (!LobbyManager.Instance.AllPlayersReady())
         return;
        
      LobbyManager.Instance.ResetReadyStates();
      DisableAll();
      menuCamera.SetActive(false);
      // spelers + HUD starten automatisch
   }
}



