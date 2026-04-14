using Bootleg___Taxes.Models;
using Rocket.API;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace Bootleg___Taxes.Commands
{
    public class ForgiveCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "Forgive";

        public string Help => "ForgiveCommand";

        public string Syntax => "/Forgive";

        public List<string> Aliases => new List<string> { "ForgiveDebts", "Perdoar", "RemoveDeed", "DeedRemove", "RaidDeed", "DeedRaid" };

        public List<string> Permissions => new List<string> { "ForgiveCommand" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            Player Caller = PlayerTool.getPlayer(new Steamworks.CSteamID(ulong.Parse(caller.Id)));
            if (Caller == null)
                return;

            RaycastInfo Hit = DamageTool.raycast(new Ray(Caller.look.aim.position, Caller.look.aim.forward), 4f, RayMasks.BARRICADE, Caller);
            if (Hit.transform == null)
                return;

            BarricadeDrop Drop = BarricadeManager.FindBarricadeByRootTransform(Hit.transform);
            if (Drop == null || !BarricadeManager.tryGetRegion(Hit.transform, out byte x, out byte Y, out ushort Plant, out BarricadeRegion Region) || !Main.Instance.Configuration.Instance.PropertiesTypes.Any(X => X.DeedID == Drop.asset.id))
                return;

            PropertyModel Prop = Main.Instance.Properties.FirstOrDefault(X => X.SyncedBarricade != null && X.SyncedBarricade == Drop);
            if (Prop == null)
                return;

            ItemManager.dropItem(new Item(Drop.asset.id, true), Drop.model.position, true, true, true);
            TriggerEffectParameters EffectParams = new TriggerEffectParameters(Assets.find(EAssetType.EFFECT, 58) as EffectAsset);
            EffectParams.position = Drop.model.position;
            EffectParams.relevantDistance = 15;
            EffectManager.triggerEffect(EffectParams);
            BarricadeManager.destroyBarricade(Drop, x, Y, Plant);
            if (Prop.StackedTaxes > 0)
                ChatManager.serverSendMessage(Main.Instance.Translate("ForgivedDebt", Drop.asset.FriendlyName, Prop.LastOwnerCapturedName, Prop.StackedTaxes), Color.white, null, Caller.channel.owner, EChatMode.SAY, null, true);
            else
                ChatManager.serverSendMessage(Main.Instance.Translate("RemovedDeed", Drop.asset.FriendlyName, Prop.LastOwnerCapturedName), Color.white, null, Caller.channel.owner, EChatMode.SAY, null, true);
        }
    }
}
