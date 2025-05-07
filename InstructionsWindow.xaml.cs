using System.Windows;

namespace Gra2D
{
    public partial class MainMenu : Window
    {
        public MainMenu()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Zasady gry:\n\n" +
                "1. Poruszaj się strzałkami\n" +
                "2. Zbieraj surowce (E)\n" +
                "3. Atakuj spację, by niszczyć skały\n" +
                "4. Uzupełniaj statystyki, by przejść do kolejnego poziomu\n" +
                "5. Po ukończeniu poziomu mapa się powiększa oraz wymagania surowców powiększają się",
                "Instrukcja",
                
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}