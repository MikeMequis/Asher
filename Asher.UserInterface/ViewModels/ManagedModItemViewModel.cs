using Asher.Core.Models;

namespace Asher.UserInterface.ViewModels
{
    /// <summary>
    /// Presentation wrapper for <see cref="ManagedModInfo"/> toggle binding.
    /// </summary>
    public class ManagedModItemViewModel : BaseViewModel
    {
        public string FileName { get; }
        public string Name { get; }
        public string Description { get; }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private ManagedModItemViewModel(ManagedModInfo source)
        {
            FileName = source.FileName;
            Name = source.Name;
            Description = source.Description;
            _isEnabled = source.IsEnabled;
        }

        public static ManagedModItemViewModel From(ManagedModInfo source) => new(source);

        public override Task InitAsync() => Task.CompletedTask;
    }
}
