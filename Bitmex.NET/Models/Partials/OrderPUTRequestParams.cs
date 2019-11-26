using System;
using System.Collections.Generic;
using System.Text;

namespace Bitmex.NET.Models
{
    public partial class OrderPUTRequestParams
    {
        public static OrderPUTRequestParams ModifyOrderQtyByID(string orderID, decimal? orderQty)
        {
            return new OrderPUTRequestParams
            {
                OrderID = orderID,
                OrderQty = orderQty
            };
        }

        public static OrderPUTRequestParams ModifyOrderQtyByUserID(string clOrdID, decimal? orderQty)
        {
            return new OrderPUTRequestParams
            {
                ClOrdID = clOrdID,
                OrderQty = orderQty
            };
        }

        public static OrderPUTRequestParams ModifyOrderPriceByUserID(string clOrdID, decimal? price)
        {
            return new OrderPUTRequestParams
            {
                ClOrdID = clOrdID,
                Price = price
            };
        }

        public static OrderPUTRequestParams ModifyOrderPriceByID(string orderID, decimal? price)
        {
            return new OrderPUTRequestParams
            {
                OrderID = orderID,
                Price = price
            };
        }

        public static OrderPUTRequestParams ModifyOrderByUserID(string clOrdID, decimal? price, decimal? orderQty)
        {
            return new OrderPUTRequestParams
            {
                ClOrdID = clOrdID,
                Price = price,
                OrderQty = orderQty
            };
        }

        public static OrderPUTRequestParams ModifyOrderByID(string orderID, decimal? price, decimal? orderQty)
        {
            return new OrderPUTRequestParams
            {
                OrderID = orderID,
                Price = price,
                OrderQty = orderQty
            };
        }
    }
}
