using System.IO;
using UnityEngine;

namespace SecondDimension.Platform
{
    public static class LocalPathProvider
    {
        public static string SavePath(string slotName)
        {
            var safeName = string.IsNullOrWhiteSpace(slotName) ? "campaign_01" : slotName.Trim();
            return Path.Combine(Application.persistentDataPath, "Saves", safeName + ".json");
        }
    }
}

