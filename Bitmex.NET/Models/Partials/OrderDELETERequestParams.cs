using System;
using System.Collections.Generic;
using System.Text;

namespace Bitmex.NET.Models
{
    public partial class OrderDELETERequestParams
    {
        public static OrderDELETERequestParams CancleOrderByID(string orderID, string comment)
        {
            return new OrderDELETERequestParams
            {
                OrderID = orderID,
                Text = comment
            };
        }

        public static OrderDELETERequestParams CancleOrderByID(string orderID)
        {
            return new OrderDELETERequestParams
            {
                OrderID = orderID
            };
        }

        public static OrderDELETERequestParams CancleOrderByClientID(string clOrdID, string comment)
        {
            return new OrderDELETERequestParams
            {
                ClOrdID = clOrdID,
                Text = comment
            };
        }

        public static OrderDELETERequestParams CancleOrderByClientID(string clOrdID)
        {
            return new OrderDELETERequestParams
            {
                ClOrdID = clOrdID
            };
        }
    }
}
