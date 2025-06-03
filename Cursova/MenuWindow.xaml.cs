using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cursova
{
    public partial class MenuWindow : Window
    {
        private MenuManager _menuManager = new MenuManager();
        private Order _currentOrderReference;
        private EditOrderWindow _parentEditOrderWindow;
        private bool _isNewOrderMode;
        
        public List<OrderItem> SelectedOrderItems { get; private set; } = new List<OrderItem>();

        public MenuWindow()
        {
            InitializeComponent();
            _isNewOrderMode = true;
            _currentOrderReference = new Order(0);
            LoadMenuToUI();
            UpdateOrderSummary();
        }

        public MenuWindow(Order existingOrder, EditOrderWindow parentWindow) : this()
        {
            _isNewOrderMode = false;
            _currentOrderReference = existingOrder;
            _parentEditOrderWindow = parentWindow;

            LoadMenuToUI();
            UpdateOrderSummary();
        }

        private void LoadMenuToUI()
        {
            DishesStackPanel.Children.Clear();
            DrinksStackPanel.Children.Clear();
            DessertsStackPanel.Children.Clear();

            foreach (var dish in _menuManager.GetItemsByCategory("Блюда власної кухні"))
            {
                DishesStackPanel.Children.Add(CreateMenuItemUI(dish));
            }

            foreach (var drink in _menuManager.GetItemsByCategory("Напої"))
            {
                DrinksStackPanel.Children.Add(CreateMenuItemUI(drink));
            }

            foreach (var dessert in _menuManager.GetItemsByCategory("Десерти"))
            {
                DessertsStackPanel.Children.Add(CreateMenuItemUI(dessert));
            }
        }

        private UIElement CreateMenuItemUI(MenuItemForOrder item)
        {
            Border itemBorder = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 10, 0, 10),
                Margin = new Thickness(0, 0, 0, 5),
                Background = Brushes.White
            };

            Grid itemGrid = new Grid();
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel itemDetailsPanel = new StackPanel();

            TextBlock namePriceTextBlock = new TextBlock
            {
                Text = $"{item.Name} - {item.Price:C}",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold
            };
            itemDetailsPanel.Children.Add(namePriceTextBlock);

            TextBlock descriptionWeightTextBlock = new TextBlock
            {
                Text = $"{item.Description} ({item.WeightGrams}г)",
                FontSize = 14,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 2, 0, 0)
            };
            itemDetailsPanel.Children.Add(descriptionWeightTextBlock);

            if (item.Allergens != null && item.Allergens.Any())
            {
                TextBlock allergensTextBlock = new TextBlock
                {
                    Text = $"Алергени: {string.Join(", ", item.Allergens)}",
                    FontSize = 12,
                    Foreground = Brushes.Red,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                itemDetailsPanel.Children.Add(allergensTextBlock);
            }

            Grid.SetColumn(itemDetailsPanel, 0);
            itemGrid.Children.Add(itemDetailsPanel);

            StackPanel controlPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            OrderItem existingOrderItem = _currentOrderReference.Items.FirstOrDefault(oi => oi.Item.Name == item.Name);

            if (existingOrderItem == null)
            {
                Button addButton = new Button
                {
                    Content = "Додати",
                    Style = (Style)FindResource("AddButtonMenu"),
                    Tag = item,
                    Foreground= Brushes.Black,
                };
                addButton.Click += AddToOrderButton_Click;
                controlPanel.Children.Add(addButton);
            }
            else
            {
                Button minusButton = new Button
                {
                    Content = "-",
                    Style = (Style)FindResource("QuantityButtonStyle"),
                    Tag = existingOrderItem
                };
                minusButton.Click += QuantityMinusButton_Click;
                controlPanel.Children.Add(minusButton);

                TextBox quantityTextBox = new TextBox
                {
                    Text = existingOrderItem.Quantity.ToString(),
                    Style = (Style)FindResource("NumericUpDownTextBoxStyle"),
                    Tag = existingOrderItem
                };
                quantityTextBox.PreviewTextInput += NumericTextBox_PreviewTextInput;
                quantityTextBox.LostFocus += QuantityTextBox_LostFocus;
                controlPanel.Children.Add(quantityTextBox);

                Button plusButton = new Button
                {
                    Content = "+",
                    Style = (Style)FindResource("QuantityButtonStyle"),
                    Tag = existingOrderItem
                };
                plusButton.Click += QuantityPlusButton_Click;
                controlPanel.Children.Add(plusButton);

                Button removeButton = new Button
                {
                    Content = "Видалити",
                    Style = (Style)FindResource("AddButtonMenu"),
                    Background = Brushes.Red,
                    Tag = existingOrderItem,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                removeButton.Click += RemoveItemFromOrderButton_Click;
                controlPanel.Children.Add(removeButton);
            }

            Grid.SetColumn(controlPanel, 1);
            itemGrid.Children.Add(controlPanel);

            itemBorder.Child = itemGrid;
            return itemBorder;
        }

        private void AddToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            MenuItemForOrder selectedMenuItem = button.Tag as MenuItemForOrder;

            if (selectedMenuItem != null)
            {
                OrderItem existingItem = _currentOrderReference.Items.FirstOrDefault(oi => oi.Item.Name == selectedMenuItem.Name);
                if (existingItem != null)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    OrderItem newOrderItem = new OrderItem(selectedMenuItem, 1);
                    _currentOrderReference.Items.Add(newOrderItem);
                }

                LoadMenuToUI();
                UpdateOrderSummary();
                _parentEditOrderWindow?.DisplayOrderItems();
            }
        }

        private void QuantityPlusButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OrderItem item = button.Tag as OrderItem;
            if (item != null)
            {
                item.Quantity++;
                UpdateOrderSummary();
                _parentEditOrderWindow?.DisplayOrderItems();

                if (button.Parent is StackPanel parentPanel)
                {
                    TextBox quantityTextBox = parentPanel.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Tag == item);
                    if (quantityTextBox != null)
                    {
                        quantityTextBox.Text = item.Quantity.ToString();
                    }
                }
            }
        }

        private void QuantityMinusButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OrderItem item = button.Tag as OrderItem;
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                    UpdateOrderSummary();
                    _parentEditOrderWindow?.DisplayOrderItems();
                    if (button.Parent is StackPanel parentPanel)
                    {
                        TextBox quantityTextBox = parentPanel.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Tag == item);
                        if (quantityTextBox != null)
                        {
                            quantityTextBox.Text = item.Quantity.ToString();
                        }
                    }
                }
                else if (item.Quantity == 1)
                {
                    MessageBoxResult result = MessageBox.Show($"Видалити \"{item.Item.Name}\" з замовлення?",
                                                              "Видалити позицію",
                                                              MessageBoxButton.YesNo,
                                                              MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        RemoveItemFromOrder(item);
                    }
                }
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void QuantityTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            OrderItem item = textBox.Tag as OrderItem;
            if (item != null)
            {
                if (int.TryParse(textBox.Text, out int newQuantity) && newQuantity > 0)
                {
                    item.Quantity = newQuantity;
                }
                else
                {
                    textBox.Text = item.Quantity.ToString();
                    MessageBox.Show("Кількість повинна бути цілим числом більше нуля.", "Некоректне значення", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                UpdateOrderSummary();
                _parentEditOrderWindow?.DisplayOrderItems();
            }
        }

        private void RemoveItemFromOrderButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OrderItem itemToRemove = button.Tag as OrderItem;
            if (itemToRemove != null)
            {
                RemoveItemFromOrder(itemToRemove);
            }
        }

        private void RemoveItemFromOrder(OrderItem itemToRemove)
        {
            _currentOrderReference.Items.Remove(itemToRemove);
            LoadMenuToUI();
            UpdateOrderSummary();
            _parentEditOrderWindow?.DisplayOrderItems();
        }

        private void UpdateOrderSummary()
        {
            int totalItems = _currentOrderReference.Items.Sum(oi => oi.Quantity);
            decimal totalCost = _currentOrderReference.Items.Sum(oi => oi.TotalPrice);
            OrderSummaryTextBlock.Text = $"В замовленні {totalItems} позицій: {totalCost:C}";
        }

        private void ConfirmSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isNewOrderMode)
            {
                SelectedOrderItems = _currentOrderReference.Items.ToList();
            }
            DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}