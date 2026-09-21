using System;
using SecondDimension.Presentation.GuildCity017F;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor.GuildCity017F
{
    public static class GuildCityNarrativeValidation017F
    {
        [MenuItem("Second Dimension/Guild City 017F/Validate Narrative & Facility Assets")]
        public static void ValidateFromMenu() { ValidateOrThrow(); Debug.Log("Guild City Narrative & Facility 017F validation passed."); }

        public static void PrepareAndValidateFromCommandLine()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateOrThrow();
        }

        private static void ValidateOrThrow()
        {
            var root=GuildCityNarrativeRegistry017F.Load();
            Require(root.onboardingScenes,12,"onboarding scenes"); Require(root.contractArcs,3,"contract arcs");
            Require(root.nodeNarratives,45,"node narratives"); Require(root.eventNarratives,18,"event narratives");
            Require(root.relationshipScenes,12,"relationship scenes"); Require(root.facilities,18,"facility narratives");
            if (root.hallAmbient == null || root.hallAmbient.Length < 40) throw new InvalidOperationException("Expected at least 40 Hall ambient actions.");
            Require(root.audioCues,15,"audio cues"); Require(root.outcomeEpilogues,12,"outcome epilogues");
            for (var i=0;i<root.facilities.Length;i++)
            {
                var facility=root.facilities[i]; if (facility.states == null || facility.states.Length != 4) throw new InvalidOperationException("Facility state count: "+facility.id);
                for (var j=0;j<facility.states.Length;j++) if (GuildCityNarrativeRegistry017F.Sprite(facility.states[j].resourcePath)==null) throw new InvalidOperationException("Missing facility state sprite: "+facility.states[j].resourcePath);
            }
            for (var i=0;i<root.audioCues.Length;i++) if (GuildCityNarrativeRegistry017F.Audio(root.audioCues[i].id)==null) throw new InvalidOperationException("Missing audio cue: "+root.audioCues[i].id);
            for (var i=0;i<root.eventNarratives.Length;i++) if (root.eventNarratives[i].costsOperation || root.eventNarratives[i].canCauseDeparture) throw new InvalidOperationException("Owner law violation: "+root.eventNarratives[i].id);
            for (var i=0;i<root.relationshipScenes.Length;i++) if (root.relationshipScenes[i].costsOperation || root.relationshipScenes[i].expires || root.relationshipScenes[i].canCauseDeparture) throw new InvalidOperationException("Relationship owner law violation: "+root.relationshipScenes[i].id);
        }

        private static void Require<T>(T[] values,int count,string label)
        { if (values == null || values.Length != count) throw new InvalidOperationException("Expected "+count+" "+label+"."); }
    }
}
