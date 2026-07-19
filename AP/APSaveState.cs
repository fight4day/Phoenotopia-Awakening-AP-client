using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace PhoA_AP_client.AP;

public static class APSaveState
{
    public static List<long> CollectedItems { get; private set; } = [];
    public static List<string> UsedSavepoints { get; private set; } = [];

    public static void LoadFoundApItemsFromSaveString([CanBeNull] string apitemsString)
    {
        if (string.IsNullOrEmpty(apitemsString))
        {
            CollectedItems.Clear();
            return;
        }

        CollectedItems = apitemsString
            .Replace("APITEMS|", "")
            .Split('|')
            .Where(x => long.TryParse(x, out _))
            .Select(long.Parse)
            .ToList();
    }

    public static void LoadVisitedSavepointsFromSaveString([CanBeNull] string visitedSavesString)
    {
        if (string.IsNullOrEmpty(visitedSavesString))
        {
            UsedSavepoints.Clear();
            return;
        }

        UsedSavepoints = visitedSavesString
            .Replace("SAVES|", "")
            .Split('|')
            .ToList();
    }
}