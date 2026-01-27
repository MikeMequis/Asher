using Asher.Core.Models;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Asher.Services.Interfaces
{
    public interface INavigationItemsManager
    {
        void CreateOptions(
            ObservableCollection<NavigationItem> target,
            params ITuple[] options
        );

        void ActivateStep(
            ObservableCollection<NavigationItem> items,
            NavigationItem current
        );
    }

}
