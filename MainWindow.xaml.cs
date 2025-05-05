using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        // Stałe reprezentujące rodzaje terenu
        public const int LAS = 1;     // las (drewno)
        public const int LAKA = 2;    // łąka (nic)
        public const int SKALA = 3;   // skały (kamień)
        public const int BAGNO = 4;   // bagno (spowalnia)
        public const int RZEKA = 5;   // rzeka (nie da się przejść)
        public const int GORY = 6;    // góry (złoto)
        public const int ILE_TERENOW = 7;   // ile terenów

        private int[,] mapa;
        private int szerokoscMapy;
        private int wysokoscMapy;
        private Image[,] tablicaTerenu;
        private const int RozmiarSegmentu = 32;

        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];

        // Pozycja gracza
        private int pozycjaGraczaX = 0;
        private int pozycjaGraczaY = 0;
        private Image obrazGracza;

        // Statystyki gracza
        public int HP { get; set; } = 100;
        public int MaxHP { get; set; } = 100;
        public int Stamina { get; set; } = 50;
        public int MaxStamina { get; set; } = 50;
        public int Drewno { get; set; } = 0;
        public int Kamien { get; set; } = 0;
        public int Zloto { get; set; } = 0;

        // Cel poziomu
        public int CelDrewna { get; set; } = 10;
        public int CelKamienia { get; set; } = 5;
        public int CelZlota { get; set; } = 3;
        public int AktualnyPoziom { get; set; } = 1;

        // Lista wrogów
        private List<Wrog> wrogowie = new List<Wrog>();

        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();
            InicjalizujGracza();
            WygenerujMape(20, 20); // Generuje mapę 20x20
        }

        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
            obrazyTerenu[BAGNO] = new BitmapImage(new Uri("bagno.png", UriKind.Relative));
            obrazyTerenu[RZEKA] = new BitmapImage(new Uri("rzeka.png", UriKind.Relative));
            obrazyTerenu[GORY] = new BitmapImage(new Uri("gory.png", UriKind.Relative));
        }

        private void InicjalizujGracza()
        {
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu,
                Source = new BitmapImage(new Uri("gracz.png", UriKind.Relative))
            };
        }

        private void WygenerujMape(int szerokosc, int wysokosc)
        {
            Random rand = new Random();
            mapa = new int[wysokosc, szerokosc];
            szerokoscMapy = szerokosc;
            wysokoscMapy = wysokosc;
            wrogowie.Clear();

            // Wypełnij mapę losowymi terenami
            for (int y = 0; y < wysokosc; y++)
            {
                for (int x = 0; x < szerokosc; x++)
                {
                    // 40% łąka, 20% las, 15% skały, 10% bagno, 10% rzeka, 5% góry
                    int los = rand.Next(100);
                    if (los < 40) mapa[y, x] = LAKA;
                    else if (los < 60) mapa[y, x] = LAS;
                    else if (los < 75) mapa[y, x] = SKALA;
                    else if (los < 85) mapa[y, x] = BAGNO;
                    else if (los < 95) mapa[y, x] = RZEKA;
                    else mapa[y, x] = GORY;

                    // 5% szans na wroga (tylko na łące, w lesie lub bagnie)
                    if (rand.Next(100) < 5 && (mapa[y, x] == LAKA || mapa[y, x] == LAS || mapa[y, x] == BAGNO))
                    {
                        wrogowie.Add(new Wrog(x, y));
                    }
                }
            }

            // Upewnij się, że gracz ma wolne pole startowe (0,0)
            mapa[0, 0] = LAKA;

            // Narysuj mapę
            RysujMape();
        }

        private void RysujMape()
        {
            SiatkaMapy.Children.Clear();
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();

            // Przygotuj siatkę
            for (int y = 0; y < wysokoscMapy; y++)
                SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });
            for (int x = 0; x < szerokoscMapy; x++)
                SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });

            // Wypełnij mapę obrazkami
            tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    Image obraz = new Image
                    {
                        Width = RozmiarSegmentu,
                        Height = RozmiarSegmentu,
                        Source = obrazyTerenu[mapa[y, x]]
                    };
                    Grid.SetRow(obraz, y);
                    Grid.SetColumn(obraz, x);
                    SiatkaMapy.Children.Add(obraz);
                    tablicaTerenu[y, x] = obraz;
                }
            }

            // Dodaj gracza i wrogów
            SiatkaMapy.Children.Add(obrazGracza);
            Panel.SetZIndex(obrazGracza, 1);
            pozycjaGraczaX = 0;
            pozycjaGraczaY = 0;
            AktualizujPozycjeGracza();

            foreach (var wrog in wrogowie)
            {
                SiatkaMapy.Children.Add(wrog.Obraz);
                Panel.SetZIndex(wrog.Obraz, 1);
            }

            AktualizujStatystyki();
        }

        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        private void AktualizujStatystyki()
        {
            EtykietaHP.Content = $"HP: {HP}/{MaxHP}";
            EtykietaStamina.Content = $"Stamina: {Stamina}/{MaxStamina}";
            EtykietaDrewna.Content = $"Drewno: {Drewno}/{CelDrewna}";
            EtykietaKamien.Content = $"Kamień: {Kamien}/{CelKamienia}";
            EtykietaZloto.Content = $"Złoto: {Zloto}/{CelZlota}";
            EtykietaPoziom.Content = $"Poziom: {AktualnyPoziom}";
        }

        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;

            if (e.Key == Key.Up) nowyY--;
            else if (e.Key == Key.Down) nowyY++;
            else if (e.Key == Key.Left) nowyX--;
            else if (e.Key == Key.Right) nowyX++;

            // Sprawdź, czy można się poruszyć
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                int teren = mapa[nowyY, nowyX];
                if (teren != SKALA && teren != RZEKA) // Nie można wejść na skały/rzekę
                {
                    if (teren == BAGNO && Stamina >= 10) // Bagno zabiera staminę
                        Stamina -= 10;
                    else if (Stamina >= 5) // Normalny ruch zabiera 5 staminy
                        Stamina -= 5;

                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }
            }

            // Zbieranie surowców (E)
            if (e.Key == Key.E)
            {
                int teren = mapa[pozycjaGraczaY, pozycjaGraczaX];
                switch (teren)
                {
                    case LAS:
                        Drewno++;
                        mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                        tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                        break;
                    case SKALA:
                        Kamien++;
                        break;
                    case GORY:
                        Zloto++;
                        break;
                }
                AktualizujStatystyki();
                SprawdzPoziom();
            }

            // Atak (Spacja)
            if (e.Key == Key.Space && Stamina >= 15)
            {
                Stamina -= 15;
                foreach (var wrog in wrogowie)
                {
                    if (Math.Abs(wrog.X - pozycjaGraczaX) <= 1 && Math.Abs(wrog.Y - pozycjaGraczaY) <= 1)
                    {
                        wrog.HP -= 20;
                        if (wrog.HP <= 0)
                        {
                            SiatkaMapy.Children.Remove(wrog.Obraz);
                            wrogowie.Remove(wrog);
                            break;
                        }
                    }
                }
                AktualizujStatystyki();
            }

            // Regeneracja staminy (co sekundę +5)
            if (Stamina < MaxStamina)
                Stamina += 5;
        }

        private void SprawdzPoziom()
        {
            if (Drewno >= CelDrewna && Kamien >= CelKamienia && Zloto >= CelZlota)
            {
                AktualnyPoziom++;
                MessageBox.Show($"Poziom {AktualnyPoziom} ukończony!");
                WygenerujMape(20, 20); // Generuje nową mapę
            }
        }
    }

    // Klasa wroga
    public class Wrog
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int HP { get; set; } = 50;
        public Image Obraz { get; set; }

        public Wrog(int x, int y)
        {
            X = x;
            Y = y;
            Obraz = new Image
            {
                Width = 32,
                Height = 32,
                Source = new BitmapImage(new Uri("wrog.png", UriKind.Relative))
            };
            Grid.SetRow(Obraz, y);
            Grid.SetColumn(Obraz, x);
        }
    }
}