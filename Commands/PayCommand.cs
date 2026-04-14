using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bootleg___Taxes.Commands
{
    public class PayCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "Pay";

        public string Help => "PayCommand";

        public string Syntax => "/Pay";

        public List<string> Aliases => new List<string> { "PayTaxes", "Tax", "Taxes", "Impostos", "Imposto", "Pagar" };

        public List<string> Permissions => new List<string> { "PayCommand"};

        public void Execute(IRocketPlayer caller, string[] command)
        {
            Player Caller = PlayerTool.getPlayer(new Steamworks.CSteamID(ulong.Parse(caller.Id)));
            if (Caller == null)
                return;

            Main.Instance.PlayerEquipment_OnPlayerPunched(Caller.equipment, EPlayerPunch.LEFT);
        }
    }
}
