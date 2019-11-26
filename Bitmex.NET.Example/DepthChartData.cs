using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitmex.NET.Example
{
    public class DepthChartData
    {
        public string Price { get; set; }
        public decimal Volumn { get; set; }
        public decimal Size { get; set; }

        public DepthChartData()
        { }
        public DepthChartData(string price,decimal size, decimal volumn)
        {
            Price = price;
            Volumn = volumn;
            Size = size;
        }
    }
}
