using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json.Serialization;

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
        public MenuItemForOrder Item { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }

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
    public class Order
    {
        private static int _nextOrderId = 1;
        public int OrderId { get; set; }
        public int TableNumber { get; set; }
        public OrderStatus Status { get; set; }
        public List<OrderItem> Items { get; set; }
        public decimal TotalCost { get; set; }

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
            CalculateTotalCost();
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
                _orders.Add(newOrder);
                SaveOrders();
                DisplayOrders();
            }
        }

        private void DisplayOrders()
        {
            OrdersStackPanel.Children.Clear();

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
                    Background = Brushes.White
                };

                Grid orderGrid = new Grid();
                orderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                orderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                orderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock statusTextBlock = new TextBlock
                {
                    Text = $"Замовлення №{order.OrderId} - {GetOrderStatusDisplayName(order.Status)}",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 10, 5)
                };
                Grid.SetColumn(statusTextBlock, 0);
                headerGrid.Children.Add(statusTextBlock);

                Button threeDotsButton = new Button
                {
                    Content = "...",
                    Style = (Style)FindResource("ThreeDotsButtonStyle"),
                    Tag = order,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(threeDotsButton, 1);
                threeDotsButton.Click += ThreeDotsButton_Click;
                headerGrid.Children.Add(threeDotsButton);

                Grid.SetRow(headerGrid, 0);
                orderGrid.Children.Add(headerGrid);

                TextBlock totalCostTextBlock = new TextBlock
                {
                    Text = $"Загальна вартість: {order.TotalCost:C}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                Grid.SetRow(totalCostTextBlock, 1);
                orderGrid.Children.Add(totalCostTextBlock);

                StackPanel itemsStackPanel = new StackPanel();
                foreach (var item in order.Items)
                {
                    StackPanel itemPanel = new StackPanel();
                    
                    TextBlock itemTextBlock = new TextBlock
                    {
                        Text = $"- {item.Item.Name} x{item.Quantity} ({item.TotalPrice:C})",
                        FontSize = 14,
                        Margin = new Thickness(10, 0, 0, 2)
                    };
                    itemPanel.Children.Add(itemTextBlock);

                    if (!string.IsNullOrWhiteSpace(item.Notes))
                    {
                        TextBlock notesTextBlock = new TextBlock
                        {
                            Text = $"({item.Notes})",
                            FontSize = 12,
                            Foreground = Brushes.DarkGray,
                            FontStyle = FontStyles.Normal,
                            Margin = new Thickness(2, 0, 0, 2),
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 750
                        };
                        itemPanel.Children.Add(notesTextBlock);
                    }

                    itemsStackPanel.Children.Add(itemPanel);
                }
                Grid.SetRow(itemsStackPanel, 2);
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

                MenuItem editOrderMenuItem = new MenuItem { Header = "Редагувати замовлення" };
                editOrderMenuItem.Click += (s, ev) => EditOrder(clickedOrder);
                contextMenu.Items.Add(editOrderMenuItem);

                MenuItem deleteOrderMenuItem = new MenuItem { Header = "Видалити замовлення" };
                deleteOrderMenuItem.Click += (s, ev) => DeleteOrder(clickedOrder);
                contextMenu.Items.Add(deleteOrderMenuItem);

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
            statusComboBox.ItemsSource = System.Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>();
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
