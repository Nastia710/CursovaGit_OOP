using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cursova
{
    public class MenuManager
    {
        private readonly JsonDataManager _jsonDataManager;
        public List<MenuItemForOrder> AllMenuItems { get; private set; }

        public MenuManager()
        {
            _jsonDataManager = new JsonDataManager();
            LoadMenuData();
        }

        private void LoadMenuData()
        {
            AllMenuItems = _jsonDataManager.LoadMenu();
            SaveMenuData();
        }

        private void SaveMenuData()
        {
            _jsonDataManager.SaveMenu(AllMenuItems);
        }

        public List<MenuItemForOrder> GetItemsByCategory(string category)
        {
            return AllMenuItems.Where(item => item.GetCategory() == category).ToList();
        }
    }
}
