using System.Collections.Generic;
using UnityEngine;

namespace ThiefSimulator.NPC
{
    [CreateAssetMenu(fileName = "NewNPCSchedule", menuName = "ThiefSimulator/NPC Schedule")]
    public class NPCSchedule : ScriptableObject
    {
        [Tooltip("A list of 12 target areas for the NPC, one for each 2-hour block of the day.\n" +
                 "Index 0: 00:00-01:59\n" +
                 "Index 1: 02:00-03:59\n" +
                 "... \n" +
                 "Index 11: 22:00-23:59")]
        public List<Vector2Int> hourlyTargetAreas = new List<Vector2Int>(new Vector2Int[12]);

        // No GetScheduleEntry method needed here anymore, NPCManager will directly index.
    }
}
