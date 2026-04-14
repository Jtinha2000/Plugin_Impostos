using Bootleg___Taxes.Models;
using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Bootleg___Taxes
{
    public class Configuration : IRocketPluginConfiguration
    {
        public List<PropertyConfiguration> PropertiesTypes { get; set; }
        public string AcessDeedInfoPermission { get; set; }
        public bool AllowSlowPayament { get; set; }
        public uint RevenueStorageInstanceID { get; set; }
        [XmlArrayItem("WeaponID")]
        public List<ushort> DamageDeedWhitelist { get; set; }
        public string CurrencyIdentifier { get; set; }
        public void LoadDefaults()
        {
            CurrencyIdentifier = "9e8ed6721a954ab885458063025f1cb5";
            DamageDeedWhitelist = new List<ushort> { 132 };
            RevenueStorageInstanceID = 65;
            AllowSlowPayament = false;
            AcessDeedInfoPermission = "DeedInfoPermission";
            PropertiesTypes = new List<PropertyConfiguration> 
            {
                new PropertyConfiguration(62252, 604800, 200),
                new PropertyConfiguration(62253, 604800, 320),
                new PropertyConfiguration(62254, 604800, 1800),
                new PropertyConfiguration(62255, 604800, 3150),
                new PropertyConfiguration(62256, 604800, 250),
                new PropertyConfiguration(62257, 604800, 8000),
                new PropertyConfiguration(62258, 604800, 12000),
            };
        }
    }
}
