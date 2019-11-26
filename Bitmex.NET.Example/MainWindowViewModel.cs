using AutoMapper;
using Bitmex.NET.Dtos;
using Bitmex.NET.Dtos.Socket;
using Bitmex.NET.Example.Annotations;
using Bitmex.NET.Example.Models;
using Bitmex.NET.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Threading;

namespace Bitmex.NET.Example
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IBitmexAuthorization _bitmexAuthorization;
        private readonly IBitmexApiSocketService _bitmexApiSocketService;
        private readonly object _syncObj = new object();
        private readonly object _syncObjOrders = new object();
        private readonly object _syncObjOrderBookL2_25 = new object();
        private readonly object _syncBidWall = new object();
        private readonly object _syncBidDepthChart = new object();
        private readonly object _syncAskWall = new object();
        private readonly object _syncAskDepthChart = new object();

        private int _size;
        private int _price;
        private string _key;
        private string _secret;
        private string _pair = "XBTUSD";
        private bool _isConnected;
        private bool _isTest;
        private string _listOrderID;

        public ObservableCollection<InstrumentModel> Instruments { get; }
        public ObservableCollection<OrderUpdateModel> OrderUpdates { get; }
        public ObservableCollection<OrderBookModel> OrderBookL2_25 { get; }

        public List<DepthChartData> BidDepthChart { get; set; }
        public List<DepthChartData> AskDepthChart { get; set; }

        public List<OrderBookModel> BidWall { get; set; }
        public List<OrderBookModel> AskWall { get; set; }

        public string Secret
        {
            get { return _secret; }
            set
            {
                _secret = value;
                OnPropertyChanged();
                _bitmexAuthorization.Secret = _secret;
                StartLoadSymbolsCmd.RaiseCanExecuteChanged();
            }
        }

        public string Key
        {
            get { return _key; }
            set
            {
                _key = value;
                OnPropertyChanged();
                _bitmexAuthorization.Key = _key;
                StartLoadSymbolsCmd.RaiseCanExecuteChanged();
            }
        }

        public int Size
        {
            get { return _size; }
            set
            {
                _size = value;
                OnPropertyChanged();
            }
        }
        public int Price
        {
            get { return _price; }
            set
            {
                _price = value;
                OnPropertyChanged();
            }
        }
        public string Pair
        {
            get { return _pair; }
            set
            {
                _pair = value;
                OnPropertyChanged();
                using (var semafore = new SemaphoreSlim(0, 1))
                {
                    UnsubscribeAllIndicator();
                    semafore.Release(1);
                    StartLoad();
                }
            }
        }
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotConnected));
            }
        }

        public bool IsNotConnected => !IsConnected;

        public ICommand BuyCmd { get; }
        public ICommand SellCmd { get; }
        public ICommand CancleCmd { get; }
        public ICommand BuyLimitCmd { get; }
        public ICommand SellLimitCmd { get; }

        public ICommand ChaseCmd { get; }

        public string SelectedOrder { get; set; }

        public decimal? NewOrderQty { get; set; }
        public decimal? NewOrderPrice { get; set; }
        public string ListOrderID
        {
            get { return _listOrderID; }
            set {
                _listOrderID = value;
            }
        }

        public bool IsTest
        {
            get { return Properties.Settings.Default.IsTest; }
            set { _isTest = value; }
        }

        public DelegateCommand StartLoadSymbolsCmd { get; }

        public DelegateCommand ChangePair { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {
            if (IsTest)
                _bitmexAuthorization = new BitmexAuthorization { BitmexEnvironment = BitmexEnvironment.Test };
            else
                _bitmexAuthorization = new BitmexAuthorization { BitmexEnvironment = BitmexEnvironment.Prod };

            _bitmexApiSocketService = BitmexApiSocketService.CreateDefaultApi(_bitmexAuthorization);
            BuyCmd = new DelegateCommand(Buy);
            SellCmd = new DelegateCommand(Sell);
            BuyLimitCmd = new DelegateCommand(BuyLimit);
            SellLimitCmd = new DelegateCommand(SellLimit);
            CancleCmd = new DelegateCommand(CancleOrder);
            ChaseCmd = new DelegateCommand(ChaseOrder);
            StartLoadSymbolsCmd = new DelegateCommand(StartLoad, CanStart);
            Size = 1;
            Instruments = new ObservableCollection<InstrumentModel>();
            OrderUpdates = new ObservableCollection<OrderUpdateModel>();
            OrderBookL2_25 = new ObservableCollection<OrderBookModel>();
            BidWall = new List<OrderBookModel>();
            BidDepthChart = new List<DepthChartData>();
            AskWall = new List<OrderBookModel>();
            AskDepthChart = new List<DepthChartData>();

            BindingOperations.EnableCollectionSynchronization(Instruments, _syncObj);
            BindingOperations.EnableCollectionSynchronization(OrderUpdates, _syncObjOrders);
            BindingOperations.EnableCollectionSynchronization(OrderBookL2_25, _syncObjOrderBookL2_25);
            /*
            BindingOperations.EnableCollectionSynchronization(BidWall, _syncBidWall);
            BindingOperations.EnableCollectionSynchronization(BidDepthChart, _syncBidDepthChart);
            BindingOperations.EnableCollectionSynchronization(AskWall, _syncAskWall);
            BindingOperations.EnableCollectionSynchronization(AskDepthChart, _syncAskDepthChart);*/
        }

        private bool CanStart()
        {
            return !string.IsNullOrWhiteSpace(Key) && !string.IsNullOrWhiteSpace(Secret);
        }

        private void StartLoad()
        {
            try
            {
                IsConnected = _bitmexApiSocketService.Connect();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            if (IsConnected)
            {/*
                _bitmexApiSocketService.Subscribe(BitmetSocketSubscriptions.CreateInstrumentSubsription(
                    message =>
                    {
                        foreach (var instrumentDto in message.Data)
                        {
                            lock (_syncObj)
                            {
                                var existing = Instruments.FirstOrDefault(a => a.Symbol == instrumentDto.Symbol);
                                if (existing != null && message.Action == BitmexActions.Update)
                                {
                                    Mapper.Map<InstrumentDto, InstrumentModel>(instrumentDto, existing);
                                }
                                else if (message.Action != BitmexActions.Partial && message.Action != BitmexActions.Delete)
                                {
                                    Instruments.Add(Mapper.Map<InstrumentDto, InstrumentModel>(instrumentDto));
                                }
                            }
                        }
                    }));
                */
                _bitmexApiSocketService.Subscribe(BitmetSocketSubscriptions.CreateOrderSubsription(
                    message =>
                    {
                        lock (_syncObjOrders)
                        {
                            if (message.Action == BitmexActions.Partial)
                                OrderUpdates.Clear();

                            foreach (var order in message.Data)
                            {
                                var existing = OrderUpdates.FirstOrDefault(a => a.OrderId == order.OrderId);

                                if (existing != null && message.Action == BitmexActions.Update)
                                {
                                    if (order.LeavesQty != null && order.OrderQty != null)
                                        Mapper.Map<OrderDto, OrderUpdateModel>(order, existing);

                                    if(order.OrdStatus=="Canceled")
                                        OrderUpdates.Remove(existing);
                                }
                                else if (message.Action == BitmexActions.Partial || message.Action == BitmexActions.Insert)
                                {
                                    OrderUpdates.Add(Mapper.Map<OrderDto, OrderUpdateModel>(order));
                                }
                                
                            }
                            OnPropertyChanged(nameof(OrderUpdates));
                            _listOrderID = "";
                            foreach (var order in OrderUpdates)
                            {
                                _listOrderID += order.OrderId.ToString()+"\n";
                            }
                            OnPropertyChanged(nameof(ListOrderID));
                        }
                    }, null));
                /*
                _bitmexApiSocketService.Subscribe(BitmetSocketSubscriptions.CreateOrderBook10Subsription(
                    message =>
                    {
                        foreach (var dto in message.Data)
                        {
                            if (dto.Symbol != "XBTUSD")
                            {
                                continue;
                            }

                            lock (_syncObjOrderBook10)
                            {
                                OrderBook10 = dto.Asks.Select(a =>
                                    new OrderBookModel { Direction = "Sell", Price = a[0], Size = a[1] })
                                    .Union(dto.Bids.Select(a =>
                                        new OrderBookModel { Direction = "Buy", Price = a[0], Size = a[1] })).ToList();
                            }

                            OnPropertyChanged(nameof(OrderBook10));
                        }
                    }));*/
                
                _bitmexApiSocketService.Subscribe(BitmetSocketSubscriptions.CreateOrderBookL2_25Subsription(
                    message =>
                    {
                        List<OrderBookModel>  tempBidWall = new List<OrderBookModel>();
                        List<OrderBookModel> tempAskWall = new List<OrderBookModel>();
                        lock (_syncObjOrderBookL2_25)
                        {
                            if (message.Action == BitmexActions.Partial)
                                OrderBookL2_25.Clear();
                            foreach (var dto in message.Data)
                            {
                                if (dto.Symbol != _pair)
                                {
                                    continue;
                                }

                                if (message.Action == BitmexActions.Insert || message.Action == BitmexActions.Partial)
                                {
                                    OrderBookL2_25.Add(Mapper.Map<OrderBookDto, OrderBookModel>(dto));
                                }
                                if (message.Action == BitmexActions.Delete)
                                {
                                    var existing = OrderBookL2_25.FirstOrDefault(a => a.Id == dto.Id);
                                    if (existing != null)
                                    {
                                        OrderBookL2_25.Remove(existing);
                                    }
                                }

                                if (message.Action == BitmexActions.Update)
                                {
                                    var existing = OrderBookL2_25.FirstOrDefault(a => a.Id == dto.Id);
                                    if (existing == null)
                                    {
                                        //OrderBookL2.Add(Mapper.Map<OrderBookDto, OrderBookModel>(dto));
                                    }
                                    else
                                    {
                                        existing.Size = dto.Size;
                                    }

                                }


                                OnPropertyChanged(nameof(OrderBookL2_25));
                            }

                            MakeBuyBookAndSellBook(tempBidWall, tempAskWall);
                            UpdateBidDepthChart(tempBidWall);
                            UpdateAskDepthChart(tempAskWall);
                        }

                    }, _pair));
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void Sell()
        {
            var posOrderParams = OrderPOSTRequestParams.CreateSimpleMarket(_pair, Size, OrderSide.Sell);
            var bitmexApiService = BitmexApiService.CreateDefaultApi(_bitmexAuthorization);
            await bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, posOrderParams).ContinueWith(ProcessPostOrderResult);
        }

        private async void Buy()
        {
            var posOrderParams = OrderPOSTRequestParams.CreateSimpleMarket(_pair, Size, OrderSide.Buy);
            var bitmexApiService = BitmexApiService.CreateDefaultApi(_bitmexAuthorization);
            await bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, posOrderParams).ContinueWith(ProcessPostOrderResult);
        }
        private async void SellLimit()
        {
            var posOrderParams = OrderPOSTRequestParams.CreateSimpleLimit(_pair, Size,Price, OrderSide.Sell);
            var bitmexApiService = BitmexApiService.CreateDefaultApi(_bitmexAuthorization);
            await bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, posOrderParams).ContinueWith(ProcessPostOrderResult);
        }

        private async void BuyLimit()
        {
            var posOrderParams = OrderPOSTRequestParams.CreateSimpleLimit(_pair, Size,Price, OrderSide.Buy);
            var bitmexApiService = BitmexApiService.CreateDefaultApi(_bitmexAuthorization);
            await bitmexApiService.Execute(BitmexApiUrls.Order.PostOrder, posOrderParams).ContinueWith(ProcessPostOrderResult);
        }

        private async void CancleOrder()
        {

            var deleteOrderParams = OrderDELETERequestParams.CancleOrderByID(SelectedOrder);
            var bitmexApiService = BitmexApiService.CreateDefaultApi(_bitmexAuthorization);
            await bitmexApiService.Execute(BitmexApiUrls.Order.DeleteOrder, deleteOrderParams);
            
        }

        private async void ChaseOrder()
        {

            var putOrderParams = OrderPUTRequestParams.ModifyOrderByID(SelectedOrder, NewOrderPrice, NewOrderQty);
            var bitmexApiService = BitmexApiService.CreateDefaultApi(_bitmexAuthorization);
            await bitmexApiService.Execute(BitmexApiUrls.Order.PutOrder, putOrderParams);

        }

        private void ProcessPostOrderResult(Task<BitmexApiResult<OrderDto>> task)
        {
            if (task.Exception != null)
            {
                MessageBox.Show((task.Exception.InnerException ?? task.Exception).Message);
            }
            else
            {
                MessageBox.Show($"order has been placed with Id {task.Result.Result.OrderId}");
            }
        }

        private void UnsubscribeAllIndicator()
        {
            UnsubscribeOrderBookL2_25();
        }

        private void UnsubscribeOrderBookL2_25()
        {
            _bitmexApiSocketService.UnsubscribeAsync(BitmetSocketSubscriptions.CreateOrderBookL2_25Subsription(message =>
            { },"XBTUSD"));
        }

        private void MakeBuyBookAndSellBook(List<OrderBookModel> tempBidWall, List<OrderBookModel> tempAskWall)
        {
            foreach (OrderBookModel item in OrderBookL2_25)
            {

                if (item.Direction == "Buy")
                {
                    bool bidAdded = false;
                    if (tempBidWall.Count == 0)
                    {
                        tempBidWall.Add(item);
                        continue;
                    }
                    foreach (OrderBookModel itemBidWall in tempBidWall)
                    {
                        if (item.Price > itemBidWall.Price)
                        {
                            tempBidWall.Insert(tempBidWall.IndexOf(itemBidWall), item);
                            bidAdded = true;
                            break;
                        }
                    }
                    if (!bidAdded)
                        tempBidWall.Add(item);
                }

                bool askAdded = false;
                if (item.Direction == "Sell")
                {
                    if (tempAskWall.Count == 0)
                    {
                        tempAskWall.Add(item);
                        continue;
                    }
                    foreach (OrderBookModel itemAskdWall in tempAskWall)
                    {
                        if (item.Price < itemAskdWall.Price)
                        {
                            tempAskWall.Insert(tempAskWall.IndexOf(itemAskdWall), item);
                            askAdded = true;
                            break;
                        }
                    }
                    if (!askAdded)
                        tempAskWall.Add(item);
                }
            }
        }

        private void UpdateBidDepthChart(List<OrderBookModel> tempBidWall)
        {
            BidDepthChart = new List<DepthChartData>();
            foreach (OrderBookModel itemBidWall in tempBidWall)
            {
                BidDepthChart.Add(new DepthChartData(itemBidWall.Price.ToString(), itemBidWall.Size, itemBidWall.Size));
            }

            if (BidDepthChart.Count > 0)
            {
                for (int i = 1; i < BidDepthChart.Count; i++)
                {
                    BidDepthChart[i].Volumn += BidDepthChart[i - 1].Volumn;
                    //valueList[i].Value += valueList[i - 1].Value;
                }
            }
            OnPropertyChanged(nameof(BidDepthChart));
        }

        private void UpdateAskDepthChart(List<OrderBookModel> tempAskWall)
        {
            AskDepthChart = new List<DepthChartData>();
            foreach (OrderBookModel itemAskWall in tempAskWall)
            {
                AskDepthChart.Add(new DepthChartData(itemAskWall.Price.ToString(), itemAskWall.Size, itemAskWall.Size));
            }

            if (AskDepthChart.Count > 0)
            {
                for (int i = 1; i < AskDepthChart.Count; i++)
                {
                    AskDepthChart[i].Volumn += AskDepthChart[i - 1].Volumn;
                    //valueList[i].Value += valueList[i - 1].Value;
                }
            } 
            OnPropertyChanged(nameof(AskDepthChart));
        }
    }
}
