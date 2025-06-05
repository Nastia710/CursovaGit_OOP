using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Windows;

namespace Cursova
{
    public class JsonDataManager
    {
        private readonly string _menuFilePath = "menu.json";
        private readonly string _ordersFilePath = "orders.json";
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonDataManager()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public void SaveMenu(List<MenuItemForOrder> menuItems)
        {
            var json = JsonSerializer.Serialize(menuItems, _jsonOptions);
            File.WriteAllText(_menuFilePath, json);
        }

        public List<MenuItemForOrder> LoadMenu()
        {
            if (!File.Exists(_menuFilePath))
            {
                return new List<MenuItemForOrder>();
            }

            try
            {
                var json = File.ReadAllText(_menuFilePath);
                return JsonSerializer.Deserialize<List<MenuItemForOrder>>(json, _jsonOptions);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Помилка при завантаженні меню: {ex.Message}",
                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<MenuItemForOrder>();
            }
        }

        public void SaveOrders(Dictionary<int, List<Order>> tableOrders)
        {
            var json = JsonSerializer.Serialize(tableOrders, _jsonOptions);
            File.WriteAllText(_ordersFilePath, json);
        }

        public Dictionary<int, List<Order>> LoadOrders()
        {
            if (!File.Exists(_ordersFilePath))
            {
                return new Dictionary<int, List<Order>>();
            }

            try
            {
                var json = File.ReadAllText(_ordersFilePath);
                var orders = JsonSerializer.Deserialize<Dictionary<int, List<Order>>>(json, _jsonOptions);

                foreach (var tableOrders in orders.Values)
                {
                    foreach (var order in tableOrders)
                    {
                        if (order.OrderDateTime == default)
                        {
                            order.OrderDateTime = DateTime.Now;
                        }
                    }
                }

                return orders;
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Помилка при завантаженні замовлень: {ex.Message}",
                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new Dictionary<int, List<Order>>();
            }
        }
    }
}
