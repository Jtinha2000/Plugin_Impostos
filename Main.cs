using Bootleg___Taxes.Models;
using Bootleg___Taxes.Utils;
using HarmonyLib;
using Newtonsoft.Json;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core;
using Rocket.Core.Assets;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Extensions;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Rocket.Unturned.Enumerations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using static UnityEngine.UI.GridLayoutGroup;

namespace Bootleg___Taxes
{
    public class Main : RocketPlugin<Configuration>
    {
        public static Main Instance { get; set; }
        public static Harmony HarmonyInstance { get; set; }
        public BarricadeDrop GovernamentStorage { get; set; }
        public List<PropertyModel> Properties { get; set; }
        public List<RecentPunch> Recents { get; set; }
        protected override void Load()
        {
            Instance = this;
            HarmonyInstance = new Harmony("PROTAXULTRAPROMAXTAXERPLUGIN.cachorrocomunista");
            Recents = new List<RecentPunch>();
            if (!File.Exists(Directory + @"\PropertiesDatabase.json"))
                File.Create(Directory + @"\PropertiesDatabase.json");
            else
                Properties = JsonConvert.DeserializeObject<List<PropertyModel>>(File.ReadAllText(Directory + @"\PropertiesDatabase.json"));
            if (Properties is null)
                Properties = new List<PropertyModel>();

            HarmonyInstance.PatchAll();

            UnturnedPlayerEvents.OnPlayerInventoryAdded += UnturnedPlayerEvents_OnPlayerInventoryAdded;
            BarricadeDrop.OnSalvageRequested_Global += BarricadeDrop_OnSalvageRequested_Global;
            BarricadeManager.onBarricadeSpawned += BarricadeManager_DeployedBarricade;
            BarricadeManager.onDamageBarricadeRequested += BarricadeManager_DamageBarricadeRequested;
            U.Events.OnPlayerConnected += Events_OnPlayerConnected;
            UnturnedPlayerEvents.OnPlayerDeath += UnturnedPlayerEvents_OnPlayerDeath;
            U.Events.OnPlayerDisconnected += Events_OnPlayerDisconnected;
            PlayerEquipment.OnPunch_Global += PlayerEquipment_OnPlayerPunched;
            
            if (Level.isLoaded)
                Level_OnLevelLoaded(0);
            else
                Level.onLevelLoaded += Level_OnLevelLoaded;
        }
        protected override void Unload()
        {
            while (Recents.Count > 0)
                Recents.First().Dispose();
            Properties.ForEach(X =>
            {
                if(X.Controller != null)
                    StopCoroutine(X.Controller);
            });
            using (StreamWriter Writer = new StreamWriter(Directory + @"\PropertiesDatabase.json", false))
            {
                Writer.Write(JsonConvert.SerializeObject(Properties));
            }

            HarmonyInstance.UnpatchAll();

            UnturnedPlayerEvents.OnPlayerInventoryAdded -= UnturnedPlayerEvents_OnPlayerInventoryAdded;
            BarricadeDrop.OnSalvageRequested_Global -= BarricadeDrop_OnSalvageRequested_Global;
            BarricadeManager.onBarricadeSpawned -= BarricadeManager_DeployedBarricade;
            BarricadeManager.onDamageBarricadeRequested -= BarricadeManager_DamageBarricadeRequested;
            U.Events.OnPlayerConnected -= Events_OnPlayerConnected;
            Level.onLevelLoaded -= Level_OnLevelLoaded;
            UnturnedPlayerEvents.OnPlayerDeath -= UnturnedPlayerEvents_OnPlayerDeath;
            U.Events.OnPlayerDisconnected -= Events_OnPlayerDisconnected;
            PlayerEquipment.OnPunch_Global -= PlayerEquipment_OnPlayerPunched;
        }
        public void BarricadeManager_DamageBarricadeRequested(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            BarricadeDrop Drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
            ushort WeaponID = PlayerTool.getPlayer(instigatorSteamID) != null ? PlayerTool.getPlayer(instigatorSteamID).equipment.itemID : ushort.MinValue;
            if ((Properties.Any(X => X.SyncedBarricade == Drop) || GovernamentStorage == Drop) && !Configuration.Instance.DamageDeedWhitelist.Contains(WeaponID))
                shouldAllow = false;
        }
        //Att Nome Salvo
        public void Events_OnPlayerConnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            List<PropertyModel> OwnedDeeds = Properties.FindAll(X => X.SyncedBarricade.GetServersideData().owner == player.Player.channel.owner.playerID.steamID.m_SteamID);
            OwnedDeeds.ForEach(X =>
            {
                if (X.LastOwnerCapturedName != player.Player.channel.owner.playerID.characterName)
                    X.LastOwnerCapturedName = player.Player.channel.owner.playerID.characterName;
            });
            if(OwnedDeeds.Count(X => X.StackedTaxes > 0) > 0)
                ChatManager.serverSendMessage(Translate("PendingDeedsConnect", OwnedDeeds.Sum(X => X.StackedTaxes)), Color.white, null, player.Player.channel.owner, EChatMode.SAY, null, true);
        }
        public void BarricadeDrop_OnSalvageRequested_Global(BarricadeDrop barricade, SteamPlayer instigatorClient, ref bool shouldAllow)
        {
            shouldAllow = Properties.FirstOrDefault(X => X.SyncedBarricade != null && X.SyncedBarricade == barricade) != null || barricade.instanceID == Configuration.Instance.RevenueStorageInstanceID;
        }
        public void Level_OnLevelLoaded(int _)
        {
            CurrencyUtils.Currency = Assets.find(new Guid(Main.Instance.Configuration.Instance.CurrencyIdentifier)) as ItemCurrencyAsset;

            List<PropertyModel> RemainingDeeds = new List<PropertyModel>(Properties);
            foreach (BarricadeRegion Region in BarricadeManager.BarricadeRegions)
            {
                if (Region != null)
                {
                    foreach (BarricadeDrop Drop in Region.drops)
                    {
                        if (Configuration.Instance.PropertiesTypes.Any(X => X.DeedID == Drop.asset.id))
                        {
                            PropertyModel Finded = RemainingDeeds.FirstOrDefault(X => X.IsDrop(Drop));
                            if (Finded != null)
                            {
                                RemainingDeeds.Remove(Finded);
                                Finded.Controller = StartCoroutine(Finded.Waiter());
                                if (RemainingDeeds.Count == 0)
                                    return;
                            }
                            else
                                Properties.Add(new PropertyModel(Drop, Configuration.Instance.PropertiesTypes.First(X => X.DeedID == Drop.asset.id).TimeInterval));
                        }
                        else if (Properties.Any(X => X.IsDrop(Drop)))
                            RemainingDeeds.Add(Properties.First(X => X.IsDrop(Drop)));
                        else if (GovernamentStorage == null && Drop.interactable is InteractableStorage && Drop.instanceID == Configuration.Instance.RevenueStorageInstanceID)
                        {
                            GovernamentStorage = Drop;
                            (Drop.interactable as InteractableStorage).items.resize(255, 255);
                        }
                    }
                }
            }
            RemainingDeeds.ForEach(X => Properties.First(Y => Y == X).RemoveProperty());
        }
        public void UnturnedPlayerEvents_OnPlayerInventoryAdded(Rocket.Unturned.Player.UnturnedPlayer Player, Rocket.Unturned.Enumerations.InventoryGroup InventoryGroup, byte InventoryIndex, ItemJar ItemJar)
        {
            if (InventoryGroup != InventoryGroup.Storage || CurrencyUtils.Currency.entries.Any(X => X.item.Find().id == ItemJar.item.id))
                return;

            BarricadeDrop Drop = BarricadeManager.FindBarricadeByRootTransform(Player.Player.inventory.storage.transform);
            if (Drop == null || Drop.instanceID != Main.Instance.Configuration.Instance.RevenueStorageInstanceID)
                return;

            Player.Player.inventory.storage.items.removeItem(InventoryIndex);
            ItemManager.dropItem(ItemJar.item, Drop.model.position, true, true, true);
        }
        public void BarricadeManager_DeployedBarricade(BarricadeRegion region, BarricadeDrop Drop)
        {
            Player Owner = PlayerTool.getPlayer(new Steamworks.CSteamID(Drop.GetServersideData().owner));
            if (!Configuration.Instance.PropertiesTypes.Any(X => X.DeedID == Drop.asset.id) || Owner == null)
                return;

            PropertyConfiguration PropConfig = Configuration.Instance.PropertiesTypes.First(X => X.DeedID == Drop.asset.id);
            Properties.Add(new PropertyModel(Drop, PropConfig.TimeInterval));
            ChatManager.serverSendMessage(Translate("NewDeed", Drop.asset.FriendlyName, new TimeSpan(0,0, PropConfig.TimeInterval).ConvertToUnturnedSpan(), PropConfig.TaxValue), Color.white, null, Owner.channel.owner, EChatMode.SAY, null, true);
        }
        public void PlayerEquipment_OnPlayerPunched(PlayerEquipment Player, EPlayerPunch PunchType)
        {
            RaycastInfo Hit = DamageTool.raycast(new Ray(Player.player.look.aim.position, Player.player.look.aim.forward), 4f, RayMasks.BARRICADE, Player.player);
            if (Hit.transform == null)
                return;

            BarricadeDrop Drop = BarricadeManager.FindBarricadeByRootTransform(Hit.transform);
            if (Drop == null || !Main.Instance.Configuration.Instance.PropertiesTypes.Any(X => X.DeedID == Drop.asset.id))
                return;

            PropertyModel Prop = Properties.FirstOrDefault(X => X.SyncedBarricade != null && X.SyncedBarricade == Drop);
            PropertyConfiguration PropConfig = Configuration.Instance.PropertiesTypes.First(X => X.DeedID == Drop.asset.id);
            if (Prop == null)
                return;

            if (Prop.HasPermissionOverProperty(Player.player))
            {
                if(!Recents.Any(X => X.Puncher == Player.player && X.DeedTarget == Prop))
                {
                    if (Prop.StackedTaxes > 0)
                    {
                        ChatManager.serverSendMessage(Translate("DeedOwnerInfoPending", Drop.asset.FriendlyName, (Prop.StackedTaxes / PropConfig.TaxValue), (DateTime.Now - Prop.LastAddedTax).ConvertToUnturnedSpan(), Prop.StackedTaxes), Color.white, null, Player.player.channel.owner, EChatMode.SAY, null, true);
                        Recents.Add(new RecentPunch(Player.player, Prop));
                    }
                    else
                        ChatManager.serverSendMessage(Translate("DeedOwnerInfoNotPending", Drop.asset.FriendlyName, PropConfig.TaxValue, (DateTime.Now - (Prop.LastAddedTax + new TimeSpan(0, 0, PropConfig.TimeInterval))).ConvertToUnturnedSpan()), Color.white, null, Player.player.channel.owner, EChatMode.SAY, null, true);
                    return;
                }

                Recents.First(X => X.Puncher == Player.player && X.DeedTarget == Prop).Dispose();
                if(Prop.StackedTaxes == 0)
                {
                    ChatManager.serverSendMessage(Translate("DeedOwnerInfoNotPending", Drop.asset.FriendlyName, PropConfig.TaxValue, (DateTime.Now - (Prop.LastAddedTax + new TimeSpan(0, 0, PropConfig.TimeInterval))).ConvertToUnturnedSpan()), Color.white, null, Player.player.channel.owner, EChatMode.SAY, null, true);
                    return;
                }

                uint InventoryValue = CurrencyUtils.PlayerInventoryValue(Player.player);
                if (InventoryValue < Prop.StackedTaxes)
                {
                    if (Configuration.Instance.AllowSlowPayament)
                    {
                        ChatManager.serverSendMessage(Translate("InsufficientFundsAllowed", Drop.asset.FriendlyName, Prop.StackedTaxes, InventoryValue, (Prop.StackedTaxes - InventoryValue)), Color.white, null, Player.player.channel.owner, EChatMode.SAY, null, true);
                        Prop.Pay(Player.player, InventoryValue);
                    }
                    else
                        ChatManager.serverSendMessage(Translate("InsufficientFundsDisallowed", Drop.asset.FriendlyName, Prop.StackedTaxes, InventoryValue, (Prop.StackedTaxes - InventoryValue)), Color.white, null, Player.player.channel.owner, EChatMode.SAY, null, true);
                    return;
                }

                ChatManager.serverSendMessage(Translate("DeedTaxPayed", Drop.asset.FriendlyName, Prop.StackedTaxes, (InventoryValue - Prop.StackedTaxes)), Color.white, null, Player.player.channel.owner, EChatMode.SAY, null, true);
                Prop.Pay(Player.player, Prop.StackedTaxes);
            }
            else if (Player.player.channel.owner.ToUnturnedPlayer().HasPermission(Configuration.Instance.AcessDeedInfoPermission))
            {
                if(Prop.StackedTaxes > 0)
                    ChatManager.serverSendMessage(Translate("DeedInfoPending", Drop.asset.FriendlyName, Prop.LastOwnerCapturedName, Prop.SyncedBarricade.GetServersideData().owner, (Prop.StackedTaxes / PropConfig.TaxValue), (DateTime.Now - Prop.LastAddedTax).ConvertToUnturnedSpan(), Prop.StackedTaxes), Color.white, null, Player.player.channel.owner, EChatMode.SAY, null, true);
                else
                    ChatManager.serverSendMessage(Translate("DeedInfoNotPending", Drop.asset.FriendlyName, Prop.LastOwnerCapturedName, Prop.SyncedBarricade.GetServersideData().owner), Color.white, null, Player.player.channel.owner, EChatMode.SAY, null, true);
            }
            else
                ChatManager.serverSendMessage(Translate("NoDeedPermission"), Color.white, null, Player.player.channel.owner, EChatMode.SAY, null, true);
        }
        //Evitar bugar recentspunchs
        public void UnturnedPlayerEvents_OnPlayerDeath(Rocket.Unturned.Player.UnturnedPlayer player, EDeathCause cause, ELimb limb, Steamworks.CSteamID murderer) =>
            Events_OnPlayerDisconnected(player);
        public void Events_OnPlayerDisconnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            List<RecentPunch> Punches = Recents.FindAll(X => X.Puncher == player.Player);
            Punches.ForEach(X => X.Dispose());
        }

        public override TranslationList DefaultTranslations => new TranslationList 
        {
            { "PendingDeedsConnect", "<color=#78B357>[Tributos Estaduais]</color> Você possuí <color=#345094>{0}</color>$ de impostos em pendência." }, //Total devido
            { "NoDeedPermission", "<color=#C26946>[Secretária de Documentação]</color> Seu nível de acesso não permite o acesso à documentação!" }, //Nada
            { "NewDeed", "<color=#78B357>[Tributos Estaduais]</color> Parabéns! '<color=#345094>{0}</color>' acaba de ser declarada em seu nome." +
                " A cada <color=#4A4A4A>{1}</color> será cobrado <color=#78B357>{2}</color>$ em impostos." }, //Name + TimeSpan + Value
            { "DeedInfoPending", "<color=#C26946>[Secretária de Documentação]</color> O certificado de <color=#315BBD>'{0}'</color> do cidadão <color=#A8A8A8>{1}</color> ({2})" +
                " está atrasada <color=#C26946>{2}x</color> ({3} sem pagar), totalizando em <color=#78B357>{4}</color>$ sonegados!" }, //Name + OwnerName + OwnerID + Vezes Devendo + Tempo sem Pagar + Total Devido
            { "DeedInfoNotPending", "<color=#C26946>[Secretária de Documentação]</color> O certificado de <color=#315BBD>'{0}'</color> do cidadão <color=#A8A8A8>{1}</color> ({2})" +
                " está em conformidade com a receita estadual!" }, //Name + Owner + OwnerID
            { "DeedOwnerInfoPending", "<color=#78B357>[Tributos Estaduais]</color> Sua propriedade <color=#345094>'{0}'</color> está com impostos atrasados <color=#C26946>{1}x</color>" +
                " ({2} sem pagar), totalizando em <color=#78B357>{3}</color>$ sonegados! Para pagar, <b>bata</b> novamente no documento!" },//Name + Vezes Devendo + Tempo sem Pagar + Total Devido
            { "DeedOwnerInfoNotPending", "<color=#78B357>[Tributos Estaduais]</color> Sua propriedade <color=#345094>'{0}'</color> está com os impostos em dia, " +
                "seu próximo pagamento totaliza-se em <color=#A8A8A8>{1}</color>$ e deverá ser pago em <color=#78B357>{2}</color>!" },//Name + ValorProximoImposto + TempoParaProximoImposto
            { "DeedTaxPayed", "<color=#78B357>[Tributos Estaduais]</color> Você acaba de quitar <color=#A8A8A8>{1}</color>$ de impostos pendentes" +
                " da sua propriedade '<color=#345094>{0}</color>', resta em suas mãos <color=#78B357>{2}</color>$!" },//Name + Valor Pago + Quanto Restou No Inventario
            { "InsufficientFundsAllowed", "<color=#78B357>[Tributos Estaduais]</color> Sua propriedade '<color=#345094>{0}</color>'" +
                " está atrelada a impostos pendentes no valor de <color=#A8A8A8>{1}</color>$, você acaba de cobrir <color=#78B357>{2}</color>$ da dívida," +
                " restam <color=#78B357>{3}</color>$..." },//Name + Valor Total Pendente + Valor do Inventario + Quanto Falta
            { "InsufficientFundsDisallowed", "<color=#78B357>[Tributos Estaduais]</color> Sua propriedade '<color=#345094>{0}</color>'" +
                " está atrelada a impostos pendentes no valor de <color=#A8A8A8>{1}</color>$, e você não possuí fundos (<color=#78B357>{2}</color>$) suficientes para cobrir a dívida," +
                " faltam <color=#78B357>{3}</color>$..." },//Name + Valor Total Pendente + Valor do Inventario + Quanto Falta
            { "PendingsTitle", "[ID] <color=#315BBD>Nome</color> | <color=#C26946>Valor</color> | <color=#78B357>Local</color>" },
            { "PendingsDeed", "[{0}] <color=#315BBD>{1}</color> | <color=#C26946>{2}$</color> | <color=#78B357>{3}</color>" }, //Index na lista + Nome do Devedor + Quanto Deve + Node Mais Próximo
            { "NoPendings", "<color=#78B357>[Tributos Estaduais]</color> Todos cidadãos estão com seus impostos pagos em dia." },
            { "NotID", "<color=#FF1717>[CommandUsage]</color> Insira um número inteiro válido, respeitando a formatação: <color=#A8A8A8><b>/Pendencies <ID></b></color>" },
            { "InvalidRangeID", "<color=#FF1717>[CommandUsage]</color> Insira um número inteiro válido, necessariamente entre <color=#A8A8A8>1</color> e <color=#A8A8A8>{0}</color>!" }, //Max Count
            { "SpecificPendingsDeed", "<color=#78B357>[Tributos Estaduais]</color> O documento devedor ID <color=#A8A8A8><b>{0}</b></color> do jogador '{1}', em débito por <color=#345094>{2}</color>$, está próximo de <color=#C26946>{3}</color> e foi marcado em seu mapa!" },//Index na lista + Nome do Devedor + Quanto Deve + Node Mais Próximo (AVISAR QUE MARCOU NO MAPA)
            { "RemovedDeed", "<color=#C26946>[Secretária de Documentação]</color> O certificado de <color=#315BBD>'{0}'</color> do cidadão <color=#A8A8A8>{1}</color>" +
                " acaba de ser revogado por você." },//Name + OwnerName 
            { "ForgivedDebt", "<color=#C26946>[Secretária de Documentação]</color> O certificado de <color=#315BBD>'{0}'</color> do cidadão <color=#A8A8A8>{1}</color>" +
                " acaba de ser revogado, perdoando sua respectiva dívida de <color=#C26946>{2}</color>$ em impostos pendentes!" },//Name + OwnerName + DebtSize
        };
    }
}
