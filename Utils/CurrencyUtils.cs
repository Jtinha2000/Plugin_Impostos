using Newtonsoft.Json.Linq;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static SDG.Unturned.ItemCurrencyAsset;

namespace Bootleg___Taxes.Utils
{
    public static class CurrencyUtils
    {
        public static ItemCurrencyAsset Currency { get; set; }
        public static uint PlayerInventoryValue(Player Player)
        {
            if (Player == null || Currency == null)
                return 0;

            return Currency.getInventoryValue(Player);
        }
        public static bool RemoveInventoryValue(Player Player, uint Value)
        {
            if (Player == null || Currency == null || Value > PlayerInventoryValue(Player))
                return false;

            return Currency.spendValue(Player, Value);
        }
        public static void AddValueInStorage(InteractableStorage Storage, uint Value)
        {
            if (Storage == null || Currency == null || Value == 0)
                return;

            for (int EntrieIndex = Currency.entries.Length - 1; EntrieIndex >= 0; EntrieIndex--)
            {
                Entry entry = Currency.entries[EntrieIndex];
                ItemAsset itemAsset = entry.item.Find();
                if (itemAsset != null && Value >= entry.value)
                {
                    uint Repeats = Value / entry.value;
                    for (uint Times = Repeats; Times > 0; Times--)
                    {
                        while (!Storage.items.tryAddItem(new Item(entry.item.Find(), EItemOrigin.ADMIN)))
                        {
                            if (Storage.items.height == 255)
                            {
                                ItemManager.dropItem(new Item(entry.item.Find(), EItemOrigin.ADMIN), Storage.transform.position, true, true, false);
                            }
                            else
                            {
                                Storage.items.resize(byte.MaxValue, (byte)(Storage.items.height + 1));
                            }
                        }
                    }
                    Value -= Repeats * entry.value;
                    if (Value == 0)
                        break;
                }
            }
        }
    }
}
