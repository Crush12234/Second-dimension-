using System;
using System.Collections.Generic;
namespace SecondDimension.SSS.V3
{
    [Serializable] public sealed class SssFamilyRow
    { public string baseFamilyId,displayName,summonId,transformFamilyId; public List<string> aliases=new List<string>(); }
    [Serializable] public sealed class SssFamilyCatalog
    { public int schemaVersion,baseFamilyCount; public List<SssFamilyRow> families=new List<SssFamilyRow>(); }
    public sealed class SssFamilyResolver : IBaseFamilyResolver
    {
        readonly Dictionary<string,string> map=new Dictionary<string,string>(StringComparer.Ordinal);
        public SssFamilyResolver(SssFamilyCatalog catalog)
        {
            if(catalog==null || catalog.schemaVersion!=3 || catalog.families==null || catalog.baseFamilyCount!=catalog.families.Count)throw new ArgumentException("Invalid family catalog.");
            var families=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in catalog.families) {
                if(row==null || row.aliases==null || !families.Add(ExactNumbers.RequireId(row.baseFamilyId)))throw new ArgumentException("Invalid or duplicate family.");
                Add(row.baseFamilyId,row.baseFamilyId);
                foreach(var alias in row.aliases)Add(alias,row.baseFamilyId);
            }
        }
        void Add(string key,string family)
        { ExactNumbers.RequireId(key);string previous;if(map.TryGetValue(key,out previous) && previous!=family)throw new ArgumentException("Conflicting family alias.");map[key]=family; }
        public bool TryResolve(string enemyDefinitionOrVariantId,out string baseFamilyId)
        { if(enemyDefinitionOrVariantId==null){baseFamilyId=null;return false;}return map.TryGetValue(enemyDefinitionOrVariantId,out baseFamilyId); }
    }
}
