using Bootleg___Taxes.Models;
using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.UI.GridLayoutGroup;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Bootleg___Taxes.Commands
{
    public class PendenciesCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "Pendencies";

        public string Help => "PendenciesCommand";

        public string Syntax => "/Pendencies <ID>";

        public List<string> Aliases => new List<string> { "Sonegadores", "Pendencias" };

        public List<string> Permissions => new List<string> { "PendenciesCommand" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            Player Caller = PlayerTool.getPlayer(new Steamworks.CSteamID(ulong.Parse(caller.Id)));
            if (Caller == null)
                return;

            List<PropertyModel> PendingDeeds = Main.Instance.Properties.FindAll(X => X.StackedTaxes > 0).OrderByDescending(X => X.StackedTaxes).ToList();
            if (PendingDeeds.Count == 0)
            {
                ChatManager.serverSendMessage(Main.Instance.Translate("NoPendings"), Color.white, null, Caller.channel.owner, EChatMode.SAY, null, true);
                return;
            }

            if (command.Length == 0)
            {
                ChatManager.serverSendMessage(Main.Instance.Translate("PendingsTitle"), Color.white, null, Caller.channel.owner, EChatMode.SAY, null, true);
                for (int ID = 0; ID < PendingDeeds.Count; ID++)
                {
                    PropertyModel Property = PendingDeeds[ID];
                    string NearestNode = LocationDevkitNodeSystem.Get().GetAllNodes().Count == 0 ? "UNDEFINED" : LocationDevkitNodeSystem.Get().GetAllNodes().OrderBy(X => Vector3.Distance(X.transform.position, Property.SyncedBarricade.model.transform.position)).First().locationName;
                    ChatManager.serverSendMessage(Main.Instance.Translate("PendingsDeed", (ID + 1).ToString("D2"), Property.LastOwnerCapturedName.Length > 6 ? Property.LastOwnerCapturedName.Substring(0, 6) : Property.LastOwnerCapturedName, Property.StackedTaxes, NearestNode), Color.white, null, Caller.channel.owner, EChatMode.SAY, null, true);
                }
                return;
            }

            if (!int.TryParse(command[0], out int Index))
            {
                ChatManager.serverSendMessage(Main.Instance.Translate("NotID"), Color.white, null, Caller.channel.owner, EChatMode.SAY, null, true);
                return;
            }
            if (Index > PendingDeeds.Count || Index <= 0)
            {
                ChatManager.serverSendMessage(Main.Instance.Translate("InvalidRangeID", PendingDeeds.Count), Color.white, null, Caller.channel.owner, EChatMode.SAY, null, true);
                return;
            }

            PropertyModel PropertySpecific = PendingDeeds[Index - 1];
            string SpecificNearestNode = LocationDevkitNodeSystem.Get().GetAllNodes().Count == 0 ? "UNDEFINED" : LocationDevkitNodeSystem.Get().GetAllNodes().OrderBy(X => Vector3.Distance(X.transform.position, PropertySpecific.SyncedBarricade.model.transform.position)).First().locationName;
            ChatManager.serverSendMessage(Main.Instance.Translate("SpecificPendingsDeed", (Index).ToString("D2"), PropertySpecific.LastOwnerCapturedName, PropertySpecific.StackedTaxes, SpecificNearestNode), Color.white, null, Caller.channel.owner, EChatMode.SAY, null, true);
            Caller.quests.replicateSetMarker(true, PropertySpecific.SyncedBarricade.model.transform.position, "SONEGADOR");
        }
    }
}
