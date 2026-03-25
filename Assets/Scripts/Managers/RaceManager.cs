using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
   public static RaceManager Instance { get; private set; }

   private readonly List<PlayerRaceController> players = new();
   private readonly List<PlayerRaceController> finishedPlayers = new();

   private void Awake()
   {
      if (Instance != null)
      {
         Destroy(gameObject);
         return;
      }

      Instance = this;
   }

   public void RegisterPlayer(PlayerRaceController player)
   {
      if (!players.Contains(player))
         players.Add(player);
   }

   public void UnregisterPlayer(PlayerRaceController player)
   {
      players.Remove(player);
   }

   public int GetPlayerPosition(PlayerRaceController player)
   {
      UpdateRaceOrder();
      return players.IndexOf(player) + 1;
   }

   public int PlayerCount => players.Count;

   private void UpdateRaceOrder()
   {
      players.Sort((a, b) =>
      {
         // Finished players ALWAYS stay ahead
         if (a.HasFinished && !b.HasFinished) return -1;
         if (!a.HasFinished && b.HasFinished) return 1;

         if (a.CurrentLap != b.CurrentLap)
            return b.CurrentLap.CompareTo(a.CurrentLap);

         if (a.LastCheckpointIndex != b.LastCheckpointIndex)
            return b.LastCheckpointIndex.CompareTo(a.LastCheckpointIndex);

         return a.DistanceToNextCheckpoint.CompareTo(b.DistanceToNextCheckpoint);
      });

   }
   public void NotifyPlayerFinished(PlayerRaceController player)
   {
      if (finishedPlayers.Contains(player))
         return;

      finishedPlayers.Add(player);

      int place = finishedPlayers.Count;

      Debug.Log($"{place}{GetSuffix(place)} car finished: {player.name}");
   }
   private string GetSuffix(int pos)
   {
      return pos switch
      {
         1 => "st",
         2 => "nd",
         3 => "rd",
         _ => "th"
      };
   }


}

