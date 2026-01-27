using Asher.Core.Models;
using Asher.Services.Interfaces;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Asher.Services.Implementations
{
    public class NavigationItemsManager : INavigationItemsManager
    {
        public void CreateOptions(
            ObservableCollection<NavigationItem> target,
            params ITuple[] options)
        {
            target.Clear();

            foreach (var data in options)
                target.Add(CreateOption(data));
        }

        private NavigationItem CreateOption(ITuple data)
        {
            return new NavigationItem
            {
                Name = (string)data[0],
                Label = (string)data[1],
                Icon = (PackIconKind)data[2],
                NavigationPath = (string)data[3],
                IsEnabled = (bool)data[4]
            };
        }

        public void ActivateStep(
            ObservableCollection<NavigationItem> items,
            NavigationItem current)
        {
            bool reachedCurrent = false;

            foreach (var item in items)
            {
                if (item == current)
                {
                    item.IsEnabled = true;
                    item.IsSelected = true;
                    reachedCurrent = true;
                }
                else
                {
                    item.IsSelected = false;
                    item.IsEnabled = !reachedCurrent;
                }
            }
        }
    }
}
