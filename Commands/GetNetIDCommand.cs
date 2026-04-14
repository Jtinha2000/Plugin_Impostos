using Rocket.API;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Bootleg___Taxes.Commands
{
    public class GetNetIDCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "GetID";

        public string Help => "IDCommand";

        public string Syntax => "/GetID";

        public List<string> Aliases => new List<string> { };

        public List<string> Permissions => new List<string> { "IDCommand" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            Player Caller = PlayerTool.getPlayer(new Steamworks.CSteamID(ulong.Parse(caller.Id)));
            if (Caller == null)
                return;

            RaycastInfo Hit = DamageTool.raycast(new Ray(Caller.look.aim.position, Caller.look.aim.forward), 4f, RayMasks.BARRICADE, Caller);
            if (Hit.transform == null)
                return;

            BarricadeDrop Drop = BarricadeManager.FindBarricadeByRootTransform(Hit.transform);
            if (Drop == null)
                return;

            UnturnedChat.Say($"{Drop.asset.FriendlyName} ID is {Drop.instanceID}.");
        }
    }
}
