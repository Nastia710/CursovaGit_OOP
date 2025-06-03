using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Text.Unicode;

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

            var json = File.ReadAllText(_menuFilePath);
            return JsonSerializer.Deserialize<List<MenuItemForOrder>>(json, _jsonOptions);
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

            var json = File.ReadAllText(_ordersFilePath);
            return JsonSerializer.Deserialize<Dictionary<int, List<Order>>>(json, _jsonOptions);
        }
    }
}