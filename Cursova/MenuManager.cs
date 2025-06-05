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

        /*public void AddMenuItem(MenuItemForOrder item)
        {
            AllMenuItems.Add(item);
            // !save to files!
        }

        public void RemoveMenuItem(MenuItemForOrder item)
        {
            AllMenuItems.Remove(item);
            // !save to files!
        }

        public void UpdateMenuItem(MenuItemForOrder oldItem, MenuItemForOrder newItem)
        {
            int index = AllMenuItems.IndexOf(oldItem);
            if (index != -1)
            {
                AllMenuItems[index] = newItem;
                // !save to files!
            }
        }*/
    }
}
