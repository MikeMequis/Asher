using System.Windows;

namespace Asher.UserInterface.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            if (DataContext is ViewModels.MainWindowViewModel viewModel)
                viewModel.PerformStartupNavigation();
        }
    }
}
