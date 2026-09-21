using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        private CampaignState _presentationHashCampaign110;
        private string _presentationHash110;

        private string PresentationHash110()
        {
            // CampaignState and its authority graph are immutable. A command or
            // reload replaces that snapshot. This single-entry display cache is
            // never used to authorize a command or to create/write a save.
            if (!ReferenceEquals(_presentationHashCampaign110, _campaign))
            {
                _presentationHash110 = CanonicalJson.Sha256Hex(_campaign);
                _presentationHashCampaign110 = _campaign;
            }
            return _presentationHash110;
        }
    }
}
