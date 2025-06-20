﻿using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json.Serialization;
using System.Data;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace Cursova
{
    public enum OrderStatus
    {
        AwaitingConfirmation,
        Confirmed,
        NotConfirmed,
        Preparing,
        Ready,
        Completed
    }
    public class OrderItem
    {
        private int quantity;
        private MenuItemForOrder item;
        private string notes;

        public MenuItemForOrder Item { get => item; set => item = value; }
        public int Quantity { get => quantity; set => quantity = value; }
        public string Notes { get => notes; set => notes = value; }

        [JsonConstructor]
        /*public OrderItem()
        {
        }*/
        public OrderItem(MenuItemForOrder item, int quantity, string notes = "")
        {
            Item = item;
            Quantity = quantity;
            Notes = notes;
        }
        public decimal TotalPrice => Item.Price * Quantity;
    }
    public abstract class OrderStatusImage
    {
        public abstract string GetImagePath();
    }

    public class AwaitingConfirmationImage : OrderStatusImage
    {
        public override string GetImagePath() => "pack://application:,,,/Cursova;component/Resources/AwaitingConfirmationOrder.png";
    }

    public class ConfirmedImage : OrderStatusImage
    {
        public override string GetImagePath() => "pack://application:,,,/Cursova;component/Resources/ConfirmedOrder.png";
    }

    public class NotConfirmedImage : OrderStatusImage
    {
        public override string GetImagePath() => "pack://application:,,,/Cursova;component/Resources/NotConfirmedOrder.png";
    }

    public class PreparingImage : OrderStatusImage
    {
        public override string GetImagePath() => "pack://application:,,,/Cursova;component/Resources/PreparingOrder.png";
    }

    public class ReadyImage : OrderStatusImage
    {
        public override string GetImagePath() => "pack://application:,,,/Cursova;component/Resources/ReadyOrder.png";
    }

    public class CompletedImage : OrderStatusImage
    {
        public override string GetImagePath() => "pack://application:,,,/Cursova;component/Resources/CompletedOrder.png";
    }

    public class Order
    {
        private static int _nextOrderId = 1;
        private int orderId;
        private int tableNumber;
        private OrderStatus status;
        private List<OrderItem> items;
        private decimal totalCost;
        private DateTime orderDateTime;

        public int OrderId { get => orderId; set => orderId = value; }
        public int TableNumber { get => tableNumber; set => tableNumber = value; }
        public OrderStatus Status { get => status; set => status = value; }
        public List<OrderItem> Items { get => items; set => items = value; }
        public decimal TotalCost { get => totalCost; set => totalCost = value; }
        public DateTime OrderDateTime { get => orderDateTime; set => orderDateTime = value; }

        [JsonConstructor]
        /*public Order()
        {
            Items = new List<OrderItem>();
        }*/
        public Order(int tableNumber)
        {
            OrderId = _nextOrderId++;
            TableNumber = tableNumber;
            Status = OrderStatus.AwaitingConfirmation;
            Items = new List<OrderItem>();
            OrderDateTime = DateTime.Now;
            CalculateTotalCost();
        }

        public OrderType GetOrderType()
        {
            var currentDateTime = DateTime.Now;
            var timeDifference = OrderDateTime - currentDateTime;

            if (timeDifference.TotalDays <= -1)
            {
                return OrderType.Past;
            }
            else if (timeDifference.TotalMinutes >= 30)
            {
                return OrderType.Future;
            }
            else
            {
                return OrderType.Current;
            }
        }

        public bool IsValidOrderDateTime()
        {
            var currentDateTime = DateTime.Now;

            if (OrderDateTime.Date < currentDateTime.Date)
            {
                MessageBox.Show("Неможливо створити замовлення на цю дату, бо вона вже пройшла",
                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            else if (OrderDateTime.Date == currentDateTime.Date &&
                     OrderDateTime.TimeOfDay < currentDateTime.TimeOfDay)
            {
                MessageBox.Show("Неможливо створити замовлення на цей час, бо він вже пройшов",
                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        public void AddItem(OrderItem item)
        {
            Items.Add(item);
            CalculateTotalCost();
        }

        public void RemoveItem(OrderItem item)
        {
            Items.Remove(item);
            CalculateTotalCost();
        }

        public void CalculateTotalCost()
        {
            TotalCost = Items.Sum(item => item.TotalPrice);
        }

        public OrderStatusImage GetStatusImage()
        {
            return Status switch
            {
                OrderStatus.AwaitingConfirmation => new AwaitingConfirmationImage(),
                OrderStatus.Confirmed => new ConfirmedImage(),
                OrderStatus.NotConfirmed => new NotConfirmedImage(),
                OrderStatus.Preparing => new PreparingImage(),
                OrderStatus.Ready => new ReadyImage(),
                OrderStatus.Completed => new CompletedImage(),
                _ => new AwaitingConfirmationImage()
            };
        }
    }

    public enum OrderType
    {
        Current,
        Future,
        Past
    }

    public partial class TableOrdersWindow : Window
    {
        private int _tableNumber;
        private List<Order> _orders = new List<Order>();
        private readonly JsonDataManager _jsonDataManager;
        private static Dictionary<int, List<Order>> _allTableOrders;

        public TableOrdersWindow(int tableNumber)
        {
            InitializeComponent();
            _tableNumber = tableNumber;
            _jsonDataManager = new JsonDataManager();
            TableNumberTextBlock.Text = $"Замовлення для столика #{_tableNumber}";

            if (_allTableOrders == null)
            {
                _allTableOrders = _jsonDataManager.LoadOrders();
            }

            if (_allTableOrders.ContainsKey(_tableNumber))
            {
                _orders = _allTableOrders[_tableNumber];
            }
            else
            {
                _allTableOrders[_tableNumber] = _orders;
            }

            DisplayOrders();
        }

        private void SaveOrders()
        {
            _allTableOrders[_tableNumber] = _orders;
            _jsonDataManager.SaveOrders(_allTableOrders);
        }

        private void CreateNewOrderButton_Click(object sender, RoutedEventArgs e)
        {
            MenuWindow menuWindow = new MenuWindow();
            if (menuWindow.ShowDialog() == true)
            {
                Order newOrder = new Order(_tableNumber);
                foreach (var item in menuWindow.SelectedOrderItems)
                {
                    newOrder.AddItem(item);
                }
                _orders.Insert(0, newOrder);
                SaveOrders();
                DisplayOrders();
            }
        }

        private void DisplayOrders()
        {
            OrdersStackPanel.Children.Clear();

            var unclosedPastOrders = _orders.Where(order =>
                order.GetOrderType() == OrderType.Past &&
                order.Status != OrderStatus.NotConfirmed &&
                order.Status != OrderStatus.Completed).ToList();

            if (unclosedPastOrders.Any())
            {
                foreach (var order in unclosedPastOrders)
                {
                    MessageBox.Show($"Замовлення з номером {order.OrderId} не закрите",
                        "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            if (_orders.Count == 0)
            {
                TextBlock noOrdersText = new TextBlock
                {
                    Text = "Немає замовлень для цього столика",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                OrdersStackPanel.Children.Add(noOrdersText);
                return;
            }

            foreach (var order in _orders)
            {
                Border orderBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 5, 0, 10),
                    Padding = new Thickness(10),
                    Background = order.Status == OrderStatus.Completed ? new SolidColorBrush(Color.FromRgb(245, 245, 245)) : Brushes.White
                };

                Grid orderGrid = new Grid();
                orderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                orderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                orderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                orderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Image statusImage = new Image
                {
                    Source = new BitmapImage(new Uri(order.GetStatusImage().GetImagePath())),
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(0, 0, 10, 0),
                    Opacity = order.Status == OrderStatus.Completed ? 0.6 : 1.0
                };
                Grid.SetColumn(statusImage, 0);
                headerGrid.Children.Add(statusImage);

                TextBlock statusTextBlock = new TextBlock
                {
                    Text = $"Замовлення №{order.OrderId} - {GetOrderStatusDisplayName(order.Status)}",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 10, 5),
                    Opacity = order.Status == OrderStatus.Completed ? 0.6 : 1.0
                };
                Grid.SetColumn(statusTextBlock, 1);
                headerGrid.Children.Add(statusTextBlock);

                if (order.Status != OrderStatus.Completed)
                {
                    Button threeDotsButton = new Button
                    {
                        Content = "...",
                        Style = (Style)FindResource("ThreeDotsButtonStyle"),
                        Tag = order,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    Grid.SetColumn(threeDotsButton, 2);
                    threeDotsButton.Click += ThreeDotsButton_Click;
                    headerGrid.Children.Add(threeDotsButton);
                }

                Grid.SetRow(headerGrid, 0);
                orderGrid.Children.Add(headerGrid);

                TextBlock dateTimeTextBlock = new TextBlock
                {
                    Text = $"Дата та час: {order.OrderDateTime:dd.MM.yyyy HH:mm}",
                    FontSize = 14,
                    Foreground = order.Status == OrderStatus.Completed ? Brushes.Gray : Brushes.Black,
                    Margin = new Thickness(0, 0, 0, 5),
                    Opacity = order.Status == OrderStatus.Completed ? 0.6 : 1.0
                };
                Grid.SetRow(dateTimeTextBlock, 1);
                orderGrid.Children.Add(dateTimeTextBlock);

                TextBlock totalCostTextBlock = new TextBlock
                {
                    Text = $"Загальна вартість: {order.TotalCost:C}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5),
                    Opacity = order.Status == OrderStatus.Completed ? 0.6 : 1.0
                };
                Grid.SetRow(totalCostTextBlock, 2);
                orderGrid.Children.Add(totalCostTextBlock);

                StackPanel itemsStackPanel = new StackPanel();
                foreach (var item in order.Items)
                {
                    StackPanel itemPanel = new StackPanel();

                    TextBlock itemTextBlock = new TextBlock
                    {
                        Text = $"- {item.Item.Name} x{item.Quantity} ({item.TotalPrice:C})",
                        FontSize = 14,
                        Margin = new Thickness(10, 0, 0, 2),
                        Opacity = order.Status == OrderStatus.Completed ? 0.6 : 1.0
                    };
                    itemPanel.Children.Add(itemTextBlock);

                    if (!string.IsNullOrWhiteSpace(item.Notes))
                    {
                        TextBlock notesTextBlock = new TextBlock
                        {
                            Text = $"({item.Notes})",
                            FontSize = 12,
                            Foreground = order.Status == OrderStatus.Completed ? Brushes.Gray : Brushes.DarkGray,
                            FontStyle = FontStyles.Normal,
                            Margin = new Thickness(2, 0, 0, 2),
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 750,
                            Opacity = order.Status == OrderStatus.Completed ? 0.6 : 1.0
                        };
                        itemPanel.Children.Add(notesTextBlock);
                    }

                    itemsStackPanel.Children.Add(itemPanel);
                }
                Grid.SetRow(itemsStackPanel, 3);
                orderGrid.Children.Add(itemsStackPanel);

                orderBorder.Child = orderGrid;
                OrdersStackPanel.Children.Add(orderBorder);
            }
        }
        private string GetOrderStatusDisplayName(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.AwaitingConfirmation: return "Очікує підтвердження";
                case OrderStatus.Confirmed: return "Підтверджено";
                case OrderStatus.NotConfirmed: return "Не підтверджено";
                case OrderStatus.Preparing: return "Готується";
                case OrderStatus.Ready: return "Готове";
                case OrderStatus.Completed: return "Закрито";
                default: return status.ToString();
            }
        }

        private void ThreeDotsButton_Click(object sender, RoutedEventArgs e)
        {
            Button threeDotsButton = sender as Button;
            Order clickedOrder = threeDotsButton.Tag as Order;

            if (clickedOrder != null)
            {
                ContextMenu contextMenu = new ContextMenu();
                contextMenu.PlacementTarget = threeDotsButton;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;

                MenuItem changeStatusMenuItem = new MenuItem { Header = "Змінити статус замовлення" };
                changeStatusMenuItem.Click += (s, ev) => ShowChangeStatusDialog(clickedOrder);
                contextMenu.Items.Add(changeStatusMenuItem);

                if (clickedOrder.Status != OrderStatus.Preparing &&
                    clickedOrder.Status != OrderStatus.Ready &&
                    clickedOrder.Status != OrderStatus.Completed)
                {
                    MenuItem editOrderMenuItem = new MenuItem { Header = "Редагувати замовлення" };
                    editOrderMenuItem.Click += (s, ev) => EditOrder(clickedOrder);
                    contextMenu.Items.Add(editOrderMenuItem);
                }

                /*MenuItem deleteOrderMenuItem = new MenuItem { Header = "Видалити замовлення" };
                deleteOrderMenuItem.Click += (s, ev) => DeleteOrder(clickedOrder);
                contextMenu.Items.Add(deleteOrderMenuItem);*/

                contextMenu.IsOpen = true;
            }
        }

        private void DeleteOrder(Order orderToDelete)
        {
            MessageBoxResult result = MessageBox.Show($"Ви впевнені, що хочете видалити замовлення №{orderToDelete.OrderId}?",
                                                      "Підтвердження видалення",
                                                      MessageBoxButton.YesNo,
                                                      MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _orders.Remove(orderToDelete);
                SaveOrders();
                DisplayOrders();
                MessageBox.Show($"Замовлення №{orderToDelete.OrderId} видалено.");
            }
        }

        private void ShowChangeStatusDialog(Order order)
        {
            Window changeStatusDialog = new Window
            {
                Title = "Змінити статус",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            TextBlock promptText = new TextBlock
            {
                Text = $"Оберіть новий статус для замовлення №{order.OrderId}:",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(promptText);

            ComboBox statusComboBox = new ComboBox();
            var orderType = order.GetOrderType();

            if (orderType == OrderType.Future)
            {
                statusComboBox.ItemsSource = new[] { OrderStatus.AwaitingConfirmation, OrderStatus.Confirmed, OrderStatus.NotConfirmed };
            }
            else if (orderType == OrderType.Past)
            {
                statusComboBox.ItemsSource = new[] { OrderStatus.NotConfirmed, OrderStatus.Completed };
            }
            else
            {
                statusComboBox.ItemsSource = System.Enum.GetValues(typeof(OrderStatus));
            }

            statusComboBox.SelectedItem = order.Status;
            panel.Children.Add(statusComboBox);

            StackPanel buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            Button confirmButton = new Button
            {
                Content = "Підтвердити",
                Margin = new Thickness(0, 0, 10, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 78,
                Height = 25
            };
            confirmButton.Click += (s, e) =>
            {
                OrderStatus newStatus = (OrderStatus)statusComboBox.SelectedItem;
                MessageBoxResult result = MessageBox.Show($"Точно змінити статус замовлення №{order.OrderId} на '{GetOrderStatusDisplayName(newStatus)}'?",
                                                          "Підтвердження зміни статусу",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    order.Status = newStatus;
                    SaveOrders();
                    DisplayOrders();
                    changeStatusDialog.Close();
                }
            };
            buttonsPanel.Children.Add(confirmButton);

            Button cancelButton = new Button
            {
                Content = "Скасувати",
                Margin = new Thickness(0, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 78,
                Height = 25
            };
            cancelButton.Click += (s, e) => changeStatusDialog.Close();
            buttonsPanel.Children.Add(cancelButton);

            panel.Children.Add(buttonsPanel);
            changeStatusDialog.Content = panel;
            changeStatusDialog.ShowDialog();
        }

        private void EditOrder(Order order)
        {
            EditOrderWindow editWindow = new EditOrderWindow(order);
            bool? dialogResult = editWindow.ShowDialog();

            if (dialogResult == true)
            {
                SaveOrders();
                DisplayOrders();
            }
        }
    }
}
