using UnityEngine;

namespace SecondDimension.Presentation.Battle.ArtProduction013
{
    [DisallowMultipleComponent]
    public sealed class BattleArtCharacterTag013 : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private string displayName;
        [SerializeField] private string side;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public string Side => side;

        public void Configure(string id, string name, string combatSide)
        {
            stableId = id ?? string.Empty;
            displayName = name ?? string.Empty;
            side = combatSide ?? string.Empty;
        }
    }
}
