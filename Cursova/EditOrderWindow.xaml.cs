using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cursova
{
    public partial class EditOrderWindow : Window
    {
        private Order _currentOrder;

        private Order _originalOrderCopy;

        public EditOrderWindow(Order orderToEdit)
        {
            InitializeComponent();
            _currentOrder = orderToEdit;
            _originalOrderCopy = DeepCopyOrder(orderToEdit);

            OrderIdTextBlock.Text = $"Замовлення №{_currentOrder.OrderId}";

            Grid dateTimeGrid = new Grid();
            dateTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            dateTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            dateTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            DatePicker datePicker = new DatePicker
            {
                SelectedDate = _currentOrder.OrderDateTime.Date,
                Width = 150,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                DisplayDateStart = DateTime.Now.Date,
                DisplayDateEnd = DateTime.Now.AddYears(1)
            };
            datePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            Grid.SetColumn(datePicker, 0);
            dateTimeGrid.Children.Add(datePicker);

            TextBox timeTextBox = new TextBox
            {
                Text = _currentOrder.OrderDateTime.ToString("HH:mm"),
                Width = 70,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                MaxLength = 5
            };
            timeTextBox.TextChanged += TimeTextBox_TextChanged;
            timeTextBox.PreviewTextInput += (s, e) =>
            {
                if (!char.IsDigit(e.Text[0]) && e.Text[0] != ':')
                {
                    e.Handled = true;
                    return;
                }

                var textBox = s as TextBox;
                var futureText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

                if (e.Text == ":" && textBox.Text.Count(c => c == ':') >= 1)
                {
                    e.Handled = true;
                    return;
                }

                if (futureText.Length > 5)
                {
                    e.Handled = true;
                    return;
                }
            };

            timeTextBox.LostFocus += (s, e) =>
            {
                if (s is TextBox tb)
                {
                    if (tb.Text.Length < 5 || !tb.Text.Contains(":"))
                    {
                        MessageBox.Show("Некоректний ввід даних", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        tb.Text = DateTime.Now.ToString("HH:mm");
                    }
                }
            };

            Grid.SetColumn(timeTextBox, 2);
            dateTimeGrid.Children.Add(timeTextBox);

            var headerPanel = (Grid)this.FindName("HeaderPanel");
            if (headerPanel != null)
            {
                headerPanel.Children.Add(dateTimeGrid);
                Grid.SetColumn(dateTimeGrid, 1);
            }

            DisplayOrderItems();
            UpdateTotalCost();
        }

        private Order DeepCopyOrder(Order original)
        {
            Order copy = new Order(original.TableNumber)
            {
                OrderId = original.OrderId,
                Status = original.Status,
            };

            foreach (var item in original.Items)
            {
                copy.Items.Add(new OrderItem(item.Item, item.Quantity, item.Notes));
            }
            copy.CalculateTotalCost();
            return copy;
        }

        public void DisplayOrderItems()
        {
            OrderItemsStackPanel.Children.Clear();

            if (_currentOrder.Items.Count == 0)
            {
                OrderItemsStackPanel.Children.Add(new TextBlock
                {
                    Text = "Замовлення порожнє. Додайте позиції.",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                });
                UpdateTotalCost();
                return;
            }

            foreach (var orderItem in _currentOrder.Items)
            {
                Border itemBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(0, 10, 0, 10),
                    Margin = new Thickness(0, 0, 0, 5)
                };

                StackPanel mainPanel = new StackPanel();

                Grid itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel itemDetailsPanel = new StackPanel();

                TextBlock namePriceTextBlock = new TextBlock
                {
                    Text = $"{orderItem.Item.Name} - {orderItem.Item.Price:C}",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold
                };
                itemDetailsPanel.Children.Add(namePriceTextBlock);

                TextBlock descriptionWeightTextBlock = new TextBlock
                {
                    Text = $"{orderItem.Item.Description} ({orderItem.Item.WeightGrams}г)",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                itemDetailsPanel.Children.Add(descriptionWeightTextBlock);

                if (orderItem.Item.Allergens != null && orderItem.Item.Allergens.Any())
                {
                    TextBlock allergensTextBlock = new TextBlock
                    {
                        Text = $"Алергени: {string.Join(", ", orderItem.Item.Allergens)}",
                        FontSize = 12,
                        Foreground = Brushes.Red,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    itemDetailsPanel.Children.Add(allergensTextBlock);
                }

                Grid.SetColumn(itemDetailsPanel, 0);
                itemGrid.Children.Add(itemDetailsPanel);

                StackPanel quantityControlPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };

                Button minusButton = new Button
                {
                    Content = "-",
                    Style = (Style)FindResource("QuantityButtonStyle"),
                    Tag = orderItem
                };
                minusButton.Click += QuantityMinusButton_Click;
                quantityControlPanel.Children.Add(minusButton);

                TextBox quantityTextBox = new TextBox
                {
                    Text = orderItem.Quantity.ToString(),
                    Style = (Style)FindResource("NumericUpDownTextBoxStyle"),
                    Tag = orderItem
                };

                quantityTextBox.PreviewTextInput += NumericTextBox_PreviewTextInput;
                quantityTextBox.LostFocus += QuantityTextBox_LostFocus;
                quantityControlPanel.Children.Add(quantityTextBox);

                Button plusButton = new Button
                {
                    Content = "+",
                    Style = (Style)FindResource("QuantityButtonStyle"),
                    Tag = orderItem
                };
                plusButton.Click += QuantityPlusButton_Click;
                quantityControlPanel.Children.Add(plusButton);

                Grid.SetColumn(quantityControlPanel, 1);
                itemGrid.Children.Add(quantityControlPanel);

                TextBlock itemTotalPriceTextBlock = new TextBlock
                {
                    Text = $"{orderItem.TotalPrice:C}",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                Grid.SetColumn(itemTotalPriceTextBlock, 2);
                itemGrid.Children.Add(itemTotalPriceTextBlock);

                Button removeButton = new Button
                {
                    Content = "Видалити",
                    Style = (Style)FindResource("RemoveItemButtonStyle"),
                    Tag = orderItem,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5),
                    Foreground = Brushes.White,
                    Height = 30,
                };
                removeButton.Click += RemoveItemButton_Click;
                Grid.SetColumn(removeButton, 3);
                itemGrid.Children.Add(removeButton);

                mainPanel.Children.Add(itemGrid);

                StackPanel notesPanel = new StackPanel
                {
                    Margin = new Thickness(0, 5, 0, 0)
                };

                TextBlock notesLabel = new TextBlock
                {
                    Text = "Примітки:",
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 2)
                };

                TextBox notesTextBox = new TextBox
                {
                    Text = orderItem.Notes ?? "",
                    Height = 50,
                    FontStyle = FontStyles.Normal,
                    Foreground = Brushes.Black,
                    VerticalContentAlignment = VerticalAlignment.Top,
                    Tag = orderItem,
                    Padding = new Thickness(5),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                    AcceptsReturn = true
                };
                notesTextBox.TextChanged += NotesTextBox_TextChanged;

                notesPanel.Children.Add(notesLabel);
                notesPanel.Children.Add(notesTextBox);
                mainPanel.Children.Add(notesPanel);

                itemBorder.Child = mainPanel;
                OrderItemsStackPanel.Children.Add(itemBorder);
            }
            UpdateTotalCost();
        }

        private void UpdateTotalCost()
        {
            _currentOrder.CalculateTotalCost();
            TotalOrderCostTextBlock.Text = $"Загальна вартість: {_currentOrder.TotalCost:C}";
        }

        private void QuantityPlusButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OrderItem item = button.Tag as OrderItem;
            if (item != null)
            {
                item.Quantity++;
                DisplayOrderItems();
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
                    DisplayOrderItems();
                }
                else if (item.Quantity == 1)
                {
                    MessageBoxResult result = MessageBox.Show($"Видалити \"{item.Item.Name}\" з замовлення?",
                                                              "Видалити позицію",
                                                              MessageBoxButton.YesNo,
                                                              MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        _currentOrder.RemoveItem(item);
                        DisplayOrderItems();
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
                DisplayOrderItems();
            }
        }

        private void NotesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox notesTextBox = sender as TextBox;
            OrderItem orderItem = notesTextBox.Tag as OrderItem;
            if (orderItem != null)
            {
                orderItem.Notes = notesTextBox.Text;
            }
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OrderItem itemToRemove = button.Tag as OrderItem;

            if (itemToRemove != null)
            {
                MessageBoxResult result = MessageBox.Show($"Ви впевнені, що хочете видалити \"{itemToRemove.Item.Name}\" з замовлення?",
                                                          "Видалити позицію",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _currentOrder.RemoveItem(itemToRemove);
                    DisplayOrderItems();
                }
            }
        }

        private void AddPositionButton_Click(object sender, RoutedEventArgs e)
        {
            MenuWindow menuWindow = new MenuWindow(_currentOrder, this);
            menuWindow.ShowDialog();
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentOrder.IsValidOrderDateTime())
            {
                return;
            }

            DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _currentOrder.Items.Clear();
            foreach (var item in _originalOrderCopy.Items)
            {
                _currentOrder.Items.Add(new OrderItem(item.Item, item.Quantity, item.Notes));
            }
            _currentOrder.Status = _originalOrderCopy.Status;
            _currentOrder.CalculateTotalCost();

            DialogResult = false;
            this.Close();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DatePicker datePicker && datePicker.SelectedDate.HasValue)
            {
                var selectedDate = datePicker.SelectedDate.Value;
                var maxDate = DateTime.Now.AddYears(1);
                
                if (selectedDate > maxDate)
                {
                    MessageBox.Show("Не можна створити замовлення більше, ніж рік вперед", 
                        "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    datePicker.SelectedDate = DateTime.Now;
                    return;
                }

                var newDateTime = selectedDate.Date + _currentOrder.OrderDateTime.TimeOfDay;
                _currentOrder.OrderDateTime = newDateTime;
            }
        }

        private void TimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox timeTextBox)
            {
                if (timeTextBox.Text.Length < 5)
                    return;

                string timeText = timeTextBox.Text.Trim();

                if (!timeText.Contains(":"))
                {
                    MessageBox.Show("Некоректний ввід даних", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    timeTextBox.Text = DateTime.Now.ToString("HH:mm");
                    return;
                }

                string[] timeParts = timeText.Split(':');
                if (timeParts.Length != 2)
                {
                    MessageBox.Show("Некоректний ввід даних", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    timeTextBox.Text = DateTime.Now.ToString("HH:mm");
                    return;
                }

                if (!int.TryParse(timeParts[0], out int hours) || !int.TryParse(timeParts[1], out int minutes))
                {
                    MessageBox.Show("Некоректний ввід даних", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    timeTextBox.Text = DateTime.Now.ToString("HH:mm");
                    return;
                }

                if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
                {
                    MessageBox.Show("Некоректний ввід даних", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    timeTextBox.Text = DateTime.Now.ToString("HH:mm");
                    return;
                }

                var timeOfDay = new TimeSpan(hours, minutes, 0);
                var newDateTime = _currentOrder.OrderDateTime.Date + timeOfDay;
                _currentOrder.OrderDateTime = newDateTime;
            }
        }
    }
}
