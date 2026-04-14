using Bootleg___Taxes.Utils;
using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bootleg___Taxes.Models
{
    public class PropertyModel
    {
        [JsonIgnore]
        public BarricadeDrop SyncedBarricade { get; set; }
        [JsonIgnore]
        public Coroutine Controller { get; set; }

        //CoreData:
        public uint InstanceID { get; set; }
        public int Countdown { get; set; }
        public uint StackedTaxes { get; set; }

        //Estatistisca/PlayerExperience:
        public string LastOwnerCapturedName { get; set; }
        public DateTime LastAddedTax { get; set; }
        public PropertyModel()
        {
            
        }
        public PropertyModel(bool Default)
        {
            LastOwnerCapturedName = "UNDEFINED";
            LastAddedTax = DateTime.Now;
            StackedTaxes = 0;
        }
        public PropertyModel(BarricadeDrop syncedBarricade, int countdown) : this(true)
        {//(fazer)
            InstanceID = syncedBarricade.instanceID;
            IsDrop(syncedBarricade);
            Countdown = countdown;
            Controller = Main.Instance.StartCoroutine(Waiter());
        }

        public void AddTax()
        {
            PropertyConfiguration Config = Main.Instance.Configuration.Instance.PropertiesTypes.FirstOrDefault(X => X.DeedID == SyncedBarricade.asset.id);
            if (Config == null)
                return;

            StackedTaxes += Config.TaxValue;
            LastAddedTax = DateTime.Now;
            Countdown = Config.TimeInterval;
            if (Controller == null)
                Controller = Main.Instance.StartCoroutine(Waiter());
        }
        public void Pay(Player Payer, uint Amount)
        {
            if (!CurrencyUtils.RemoveInventoryValue(Payer, Amount))
                return;

            StackedTaxes -= Amount;
            CurrencyUtils.AddValueInStorage(Main.Instance.GovernamentStorage.interactable as InteractableStorage, Amount);
        }
        public bool HasPermissionOverProperty(Player Questioner) =>
            SyncedBarricade != null && (Questioner.channel.owner.playerID.steamID.m_SteamID == SyncedBarricade.GetServersideData().owner || (SyncedBarricade.GetServersideData().group != 0 && Questioner.quests.groupID.m_SteamID == SyncedBarricade.GetServersideData().group));
        public bool IsDrop(BarricadeDrop Drop)
        {//(fazer)
            if (Drop != null && Drop.instanceID == InstanceID)
            {
                SyncedBarricade = Drop;
                Player Owner = PlayerTool.getPlayer(new Steamworks.CSteamID(SyncedBarricade.GetServersideData().owner));
                if (Owner != null)
                    LastOwnerCapturedName = Owner.channel.owner.playerID.characterName;
                return true;
            }
            return false;
        }
        public void RemoveProperty()
        {
            if (Controller != null)
                Main.Instance.StopCoroutine(Controller);

            Main.Instance.Properties.Remove(this);

            List<RecentPunch> Punches = Main.Instance.Recents.FindAll(X => X.DeedTarget == this);
            Punches.ForEach(X => X.Dispose());
        }

        public IEnumerator Waiter()
        {
            while (Countdown > 0)
            {
                yield return new WaitForSeconds(1);
                Countdown--;
            }

            Controller = null;
            AddTax();
        }
    }
}
