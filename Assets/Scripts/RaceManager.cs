using System;
using TMPro;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
   public static RaceManager Instance;
   [SerializeField] private TextMeshProUGUI currentLapTimeText;
   [SerializeField] private TextMeshProUGUI overallRaceTimeText;
   [SerializeField] private TextMeshProUGUI bestLapTimeText;
   [SerializeField] private TextMeshProUGUI lapText;
   
   [SerializeField] private TextMeshProUGUI currentLapTimeText2;
   [SerializeField] private TextMeshProUGUI overallRaceTimeText2;
   [SerializeField] private TextMeshProUGUI bestLapTimeText2;
   [SerializeField] private TextMeshProUGUI lapText2;
   
   [SerializeField] private Checkpoint[] checkpoints;
   [SerializeField] private int lastCheckpointIndex = -1;
   [SerializeField] private bool isCircuit;
   [SerializeField] private int totalLaps = 1;
   
   private int currentLap = 1;
   
   private bool raceStarted;
   private bool raceFinished;

   private float currentLapTime = 0f;
   private float overallRaceTime = 0f;
   private float bestLapTime = Mathf.Infinity;

   private void Awake()
   {
      if (Instance == null)
      {
         Instance = this;
      }
      else
      {
         Destroy(gameObject);
      }
   }

   private void Update()
   {
      if (raceStarted)
      {
         UpdateTimers();
      }
      UpdateUI();
   }

   public void CheckPointReached(int checkpointIndex)
   {
      if (!raceStarted && checkpointIndex != 0 || raceFinished) return;

      if (checkpointIndex == lastCheckpointIndex + 1)
      {
         UpdateCheckpoint(checkpointIndex);
      }
   }

   public void UpdateCheckpoint(int checkpointIndex)
   {
      if (checkpointIndex == 0)
      {
         if (!raceStarted)
         {
            StartRace();
         }
         else if (isCircuit && lastCheckpointIndex == checkpoints.Length - 1)
         { 
            OnLapFinish();
         }
      }
      else if (!isCircuit && checkpointIndex == checkpoints.Length - 1)
      {
         OnLapFinish();
      }
      lastCheckpointIndex = checkpointIndex;
   }

   private void OnLapFinish()
   {
      currentLap++;
      if (currentLapTime < bestLapTime)
      {
         bestLapTime = currentLapTime;
      }
      if (currentLap > totalLaps)
      {
         EndRace();
      }
      else
      {
         currentLapTime = 0f;
         lastCheckpointIndex = isCircuit ? 0 : -1;
      }
     
   }
   private void StartRace()
   {
      raceStarted = true;
      raceFinished = false;
   }

   private void EndRace()
   {
      raceFinished = true;
      raceStarted = false;
   }

   private void UpdateTimers()
   {
      currentLapTime += Time.deltaTime;
      overallRaceTime += Time.deltaTime;
   }

   private void UpdateUI()
   {
      currentLapTimeText.text = FormatTime(currentLapTime);
      overallRaceTimeText.text = FormatTime(overallRaceTime);
      lapText.text = "Lap: " + currentLap + "/" + totalLaps;
      bestLapTimeText.text = FormatTime(bestLapTime);
      
      currentLapTimeText2.text = FormatTime(currentLapTime);
      overallRaceTimeText2.text = FormatTime(overallRaceTime);
      lapText2.text = "Lap: " + currentLap + "/" + totalLaps;
      bestLapTimeText2.text = FormatTime(bestLapTime);
   }

   private string FormatTime(float time)
   {
      if (float.IsInfinity(time) || time < 0) return "--:--";
      int minutes = (int)time / 60;
      float seconds = time % 60;
      return string.Format("{0:00}:{1:00}", minutes, seconds);
   }
}
