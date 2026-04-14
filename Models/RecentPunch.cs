using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Bootleg___Taxes.Models
{
    public class RecentPunch
    {
        public Player Puncher { get; set; }
        public PropertyModel DeedTarget { get; set; }
        public Coroutine Controller { get; set; }
        public RecentPunch()
        {

        }
        public RecentPunch(Player puncher, PropertyModel deedTarget)
        {
            Puncher = puncher;
            DeedTarget = deedTarget;
            Controller = Main.Instance.StartCoroutine(Remover());
        }
        public void Dispose()
        {
            if (Controller != null)
                Main.Instance.StopCoroutine(Controller);
            Main.Instance.Recents.Remove(this);
        }
        public IEnumerator Remover()
        {
            yield return new WaitForSeconds(5);
            Dispose();
        }
    }
}
