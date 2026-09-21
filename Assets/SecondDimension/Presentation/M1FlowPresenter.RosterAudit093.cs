using System;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        // These explicit verification seams invoke shipping builders and the
        // shipping RuntimeUi canvas. No hand-built fake recruitment/battle UI.
        public void ShowRosterAudit093(M1Screen screen, string applicantId = null)
        {
            if (!Application.isEditor && !Environment.GetCommandLineArgs().Contains("--sd-roster-audit-093"))
                throw new InvalidOperationException("Roster verification requires its explicit command-line flag.");
            CloseOuterGateworks066();
            CloseVersion69Experiences069();
            _screen = screen;
            if (applicantId != null)
            {
                _guildCityTab017D = "APPLICANTS";
                _guildCityMoreOpen060 = false;
                _guildApplicantSelectedId066 = applicantId;
            }
            BuildCurrentScreen();
        }
    }
}
