using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Presentation.Release030
{
    [Serializable]
    public sealed class ReleaseManifestContract030
    {
        public string resourcePath;
        public int minimumSaveVersion;
    }

    [Serializable]
    public sealed class ActiveReleaseContract030
    {
        public string contentVersion;
        public string title;
        public string baseContentVersion;
        public int saveFormatVersion;
        public string activeBuildLabel;
        public string activePipeline;
        public string[] requiredPackages;
        public string[] forbiddenPackages;
        public ReleaseManifestContract030[] releaseManifests;
        public string[] migrationIds;
        public string[] hardLaws;
    }

    [Serializable]
    internal sealed class SaveVersionOnly030
    {
        public int saveFormatVersion;
    }

    public sealed class ReleaseSupersessionSnapshot030
    {
        public bool IsReady;
        public string Error;
        public int SaveFormatVersion;
        public int CompatibleReleaseManifests;
        public string ActivePipeline;
        public IReadOnlyList<string> SummaryLines;
    }

    public static class ReleaseSupersessionRegistry030
    {
        private const string ContractResource = "SecondDimension/Release030/Data/ActiveReleaseContract030";
        private static ActiveReleaseContract030 _contract;

        public static ActiveReleaseContract030 Contract
        {
            get
            {
                if (_contract != null) return _contract;
                var asset = Resources.Load<TextAsset>(ContractResource);
                if (asset == null)
                    throw new InvalidOperationException("Missing active Release 030 contract.");
                _contract = JsonConvert.DeserializeObject<ActiveReleaseContract030>(asset.text);
                if (_contract == null)
                    throw new InvalidOperationException("Invalid active Release 030 contract.");
                return _contract;
            }
        }

        public static void ClearCacheForTests() => _contract = null;
    }

    public static class ReleaseSupersessionReadinessService030
    {
        public static ReleaseSupersessionSnapshot030 BuildSnapshot()
        {
            try
            {
                var contract = ReleaseSupersessionRegistry030.Contract;
                var issues = new List<string>();
                if (contract.saveFormatVersion != SaveEnvelopeV1.CurrentFormatVersion)
                    issues.Add("Active Release 030 contract does not match the current save format.");
                var compatible = 0;
                foreach (var entry in contract.releaseManifests ?? Array.Empty<ReleaseManifestContract030>())
                {
                    var asset = Resources.Load<TextAsset>(entry.resourcePath);
                    if (asset == null)
                    {
                        issues.Add("Missing retained release manifest: " + entry.resourcePath);
                        continue;
                    }

                    var parsed = JsonConvert.DeserializeObject<SaveVersionOnly030>(asset.text);
                    var version = parsed == null ? 0 : parsed.saveFormatVersion;
                    if (version < entry.minimumSaveVersion ||
                        version > SaveEnvelopeV1.CurrentFormatVersion)
                        issues.Add("Retained release manifest has an invalid supersession version: " +
                                   entry.resourcePath);
                    else
                        compatible++;
                }

                return new ReleaseSupersessionSnapshot030
                {
                    IsReady = issues.Count == 0,
                    Error = issues.Count == 0 ? string.Empty : string.Join("\n", issues),
                    SaveFormatVersion = SaveEnvelopeV1.CurrentFormatVersion,
                    CompatibleReleaseManifests = compatible,
                    ActivePipeline = contract.activePipeline,
                    SummaryLines = new[]
                    {
                        "ACTIVE RELEASE 030 • SAVE v" + SaveEnvelopeV1.CurrentFormatVersion,
                        compatible + " RETAINED RELEASE MANIFESTS SUPERSESSION-COMPATIBLE",
                        "PIPELINE: " + contract.activePipeline
                    }
                };
            }
            catch (Exception exception)
            {
                return new ReleaseSupersessionSnapshot030
                {
                    IsReady = false,
                    Error = exception.ToString(),
                    SummaryLines = new[] { "RELEASE 030 SUPERSESSION CHECK FAILED" }
                };
            }
        }
    }
}
