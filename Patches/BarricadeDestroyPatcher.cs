using Bootleg___Taxes.Models;
using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bootleg___Taxes.Patches
{
    [HarmonyPatch(typeof(BarricadeManager), nameof(BarricadeManager.destroyBarricade), new Type[] { typeof(BarricadeDrop), typeof(byte), typeof(byte), typeof(ushort) })]
    public class BarricadeDestroyPatcher
    {
        [HarmonyPrefix]
        public static void BarricadeDestroy(BarricadeDrop barricade, byte x, byte y, ushort plant)
        {
            if (!Main.Instance.Properties.Any(X => X.SyncedBarricade != null && X.SyncedBarricade == barricade))
                return;

            PropertyModel Data = Main.Instance.Properties.First(X => X.SyncedBarricade != null && X.SyncedBarricade == barricade);
            Data.RemoveProperty();
        }
    }
}
