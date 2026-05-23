using FMVideoManagerApp.ViewModels;
using System.Windows;

namespace FMVideoManagerApp
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent(); // 
            DataContext = vm;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Title = e.NewSize.ToString();
        }

        private void StackPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}