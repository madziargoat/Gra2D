using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        public const int LAS = 1, LAKA = 2, SKALA = 3, BAGNO = 4, RZEKA = 5, GORY = 6;
        private const int RozmiarSegmentu = 32;

        private int[,] mapa;
        private int szerokoscMapy = 20, wysokoscMapy = 20;
        private Image[,] tablicaTerenu;
        private ImageSource[] obrazyTerenu = new ImageSource[7];
        private Image obrazGracza;
        private int pozycjaGraczaX = 10, pozycjaGraczaY = 10;

        public int Stamina { get; set; } = 50;
        public int Drewno { get; set; } = 0;
        public int Kamien { get; set; } = 0;
        public int Zloto { get; set; } = 0;
        public int AktualnyPoziom { get; set; } = 1;

        public int WymaganeDrewno => 10 + (AktualnyPoziom * 2);
        public int WymaganyKamien => 5 + AktualnyPoziom;
        public int WymaganeZloto => 3 + AktualnyPoziom;

        private Random rand = new Random();
        private DispatcherTimer gameTimer;
        private readonly SolidColorBrush domyslnyKolor = Brushes.White;
        private readonly SolidColorBrush osiagnietyLimitKolor = Brushes.LimeGreen;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => InicjalizujGre();
        }

        private void InicjalizujGre()
        {
            WczytajObrazy();
            StworzGracza();
            WygenerujMape();
            StartTimer();
        }

        private void NowyPoziom()
        {
            AktualnyPoziom++;
            szerokoscMapy = 20 + (AktualnyPoziom * 2);
            wysokoscMapy = 20 + (AktualnyPoziom * 2);

            Stamina = 50;
            Drewno = 0;
            Kamien = 0;
            Zloto = 0;

            PokazKonfetti();
            WygenerujMape();
            AktualizujStatystyki();
        }

        private void PokazKonfetti()
        {
            var konfettiCanvas = new Canvas();
            Grid.SetRowSpan(konfettiCanvas, 3);
            MainGridmap.Children.Add(konfettiCanvas);

            for (int i = 0; i < 50; i++)
            {
                var element = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(
                        (byte)rand.Next(256),
                        (byte)rand.Next(256),
                        (byte)rand.Next(256))),
                    Opacity = 0.7
                };

                Canvas.SetLeft(element, rand.Next((int)ActualWidth));
                Canvas.SetTop(element, -10);
                konfettiCanvas.Children.Add(element);

                var animX = new DoubleAnimation
                {
                    By = rand.Next(-50, 50),
                    Duration = TimeSpan.FromSeconds(3)
                };

                var animY = new DoubleAnimation
                {
                    To = ActualHeight + 10,
                    Duration = TimeSpan.FromSeconds(3)
                };

                element.BeginAnimation(Canvas.LeftProperty, animX);
                element.BeginAnimation(Canvas.TopProperty, animY);
            }

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, e) =>
            {
                MainGridmap.Children.Remove(konfettiCanvas);
                timer.Stop();
            };
            timer.Start();
        }

        private void WczytajObrazy()
        {
            string[] pliki = { "", "las.png", "laka.png", "skala.png", "bagno.png", "rzeka.png", "gory.png" };
            for (int i = 1; i <= 6; i++)
            {
                try
                {
                    obrazyTerenu[i] = new BitmapImage(new Uri(pliki[i], UriKind.Relative));
                }
                catch
                {
                    var bmp = new WriteableBitmap(32, 32, 96, 96, PixelFormats.Bgr32, null);
                    byte[] pixels = new byte[32 * 32 * 4];
                    for (int j = 0; j < pixels.Length; j += 4)
                    {
                        pixels[j] = 0;
                        pixels[j + 1] = 0;
                        pixels[j + 2] = 255;
                    }
                    bmp.WritePixels(new Int32Rect(0, 0, 32, 32), pixels, 32 * 4, 0);
                    obrazyTerenu[i] = bmp;
                }
            }
        }

        private void StworzGracza()
        {
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu,
                Source = new BitmapImage(new Uri("gracz.png", UriKind.Relative))
            };
        }

        private void WygenerujMape()
        {
            mapa = new int[wysokoscMapy, szerokoscMapy];
            tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
            SiatkaMapy.Children.Clear();
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();

            for (int i = 0; i < wysokoscMapy; i++)
                SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });

            for (int i = 0; i < szerokoscMapy; i++)
                SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });

            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    int los = rand.Next(100);
                    if (los < 40) mapa[y, x] = LAKA;
                    else if (los < 60) mapa[y, x] = LAS;
                    else if (los < 80) mapa[y, x] = SKALA;
                    else if (los < 95) mapa[y, x] = BAGNO;
                    else mapa[y, x] = GORY;
                }
            }

            for (int i = 0; i < 15 + (AktualnyPoziom * 3); i++) UmiescSurowiec(LAS, SKALA, RZEKA);
            for (int i = 0; i < 10 + (AktualnyPoziom * 2); i++) UmiescSurowiec(SKALA, SKALA, RZEKA);
            for (int i = 0; i < 3 + AktualnyPoziom; i++) UmiescSurowiec(GORY, SKALA, RZEKA);

            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    var obraz = new Image
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

            pozycjaGraczaX = szerokoscMapy / 2;
            pozycjaGraczaY = wysokoscMapy / 2;
            SiatkaMapy.Children.Add(obrazGracza);
            AktualizujPozycjeGracza();
        }

        private void UmiescSurowiec(int surowiec, params int[] niedozwolone)
        {
            int x, y, proba = 0;
            do
            {
                x = rand.Next(szerokoscMapy);
                y = rand.Next(wysokoscMapy);
                proba++;
            } while (proba < 100 && Array.Exists(niedozwolone, t => t == mapa[y, x]));

            mapa[y, x] = surowiec;
        }

        private void AktualizujPozycjeGracza()
        {
            if (pozycjaGraczaY >= 0 && pozycjaGraczaY < wysokoscMapy &&
                pozycjaGraczaX >= 0 && pozycjaGraczaX < szerokoscMapy)
            {
                Grid.SetRow(obrazGracza, pozycjaGraczaY);
                Grid.SetColumn(obrazGracza, pozycjaGraczaX);
            }
        }

        private void StartTimer()
        {
            gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            gameTimer.Tick += (s, e) =>
            {
                if (Stamina < 50) Stamina++;
                AktualizujStatystyki();
            };
            gameTimer.Start();
        }

        private void AktualizujStatystyki()
        {
            
            EtykietaStamina.Content = $"Stamina: {Stamina}/50";

            EtykietaDrewna.Content = $"Drewno: {Drewno}/{WymaganeDrewno}";
            EtykietaDrewna.Foreground = Drewno >= WymaganeDrewno ? osiagnietyLimitKolor : domyslnyKolor;

            EtykietaKamien.Content = $"Kamień: {Kamien}/{WymaganyKamien}";
            EtykietaKamien.Foreground = Kamien >= WymaganyKamien ? osiagnietyLimitKolor : domyslnyKolor;

            EtykietaZloto.Content = $"Złoto: {Zloto}/{WymaganeZloto}";
            EtykietaZloto.Foreground = Zloto >= WymaganeZloto ? osiagnietyLimitKolor : domyslnyKolor;

            EtykietaPoziom.Content = $"Poziom: {AktualnyPoziom}";
        }

        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Application.Current.Shutdown();

            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;

            switch (e.Key)
            {
                case Key.Up: nowyY--; break;
                case Key.Down: nowyY++; break;
                case Key.Left: nowyX--; break;
                case Key.Right: nowyX++; break;
                case Key.E: ZbierzSurowiec(); return;
                case Key.Space: Atakuj(); return;
            }

            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                if (mapa[nowyY, nowyX] != SKALA && mapa[nowyY, nowyX] != RZEKA && Stamina >= 5)
                {
                    Stamina -= 5;
                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }
            }
        }

        private void ZbierzSurowiec()
        {
            if (Drewno >= WymaganeDrewno && Kamien >= WymaganyKamien && Zloto >= WymaganeZloto)
            {
                NowyPoziom();
                return;
            }

            int teren = mapa[pozycjaGraczaY, pozycjaGraczaX];

            switch (teren)
            {
                case LAS when Drewno < WymaganeDrewno:
                    Drewno++;
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    break;

                case GORY when Zloto < WymaganeZloto:
                    Zloto++;
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    break;

                case SKALA when Kamien < WymaganyKamien:
                    Kamien++;
                    break;
            }

            Stamina = Math.Min(Stamina + 3, 50);
            AktualizujStatystyki();

            if (Drewno >= WymaganeDrewno && Kamien >= WymaganyKamien && Zloto >= WymaganeZloto)
            {
                NowyPoziom();
            }
        }

        private void Atakuj()
        {
            if (Stamina < 15) return;
            Stamina -= 15;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int x = pozycjaGraczaX + dx;
                    int y = pozycjaGraczaY + dy;

                    if (x >= 0 && x < szerokoscMapy && y >= 0 && y < wysokoscMapy && mapa[y, x] == SKALA)
                    {
                        mapa[y, x] = LAKA;
                        tablicaTerenu[y, x].Source = obrazyTerenu[LAKA];
                        Kamien++;
                    }
                }
            }
            AktualizujStatystyki();
        }
    }
}