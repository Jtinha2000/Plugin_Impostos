using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bootleg___Taxes.Models
{
    public class PropertyConfiguration
    {
        public ushort DeedID { get; set; }
        public int TimeInterval { get; set; }
        public uint TaxValue { get; set; }
        public PropertyConfiguration()
        {
            
        }
        public PropertyConfiguration(ushort deedID, int timeInterval, uint taxValue)
        {
            DeedID = deedID;
            TimeInterval = timeInterval;
            TaxValue = taxValue;
        }
    }
}
