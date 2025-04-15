using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Pixelation_Game
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage _babyImage;
        private BitmapImage _adultImage;

        private int _currentPixelSize = 1;
        private int _maxPixelSize = 400;

        private DispatcherTimer _timer;
        private bool _isPaused;

        private List<int> _availableIndexes = new List<int>();
        private Random _rand = new Random();

        private ScaleTransform _babyScale;
        private TranslateTransform _babyTranslate;
        public MainWindow()
        {
            InitializeComponent();

            RegisterName("BabyScale", BabyScale);
            RegisterName("BabyTranslate", BabyTranslate);

            _babyScale = BabyScale;
            _babyTranslate = BabyTranslate;

            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Maximized;
            this.ResizeMode = ResizeMode.NoResize;

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(_babyScale);
            transformGroup.Children.Add(_babyTranslate);
            BabyImage.RenderTransform = transformGroup;

            LoadRandomPair();
        }

        private void LoadRandomPair()
        {
           

            BabyImage.RenderTransformOrigin = new Point(0, 0);
           
            _timer?.Stop();
            _isPaused = false;

            AdultImage.Source = null;
            AdultImage.Visibility = Visibility.Collapsed;

            BabyImage.Visibility = Visibility.Visible;
            BabyImage.HorizontalAlignment = HorizontalAlignment.Center;
            BabyImage.VerticalAlignment = VerticalAlignment.Center;
            BabyImage.Margin = new Thickness(0);
            BabyImage.RenderTransformOrigin = new Point(0.5, 0.5); // Center

            _babyScale.ScaleX = 1;
            _babyScale.ScaleY = 1;
            _babyTranslate.X = 0;
            _babyTranslate.Y = 0;

            if (_availableIndexes.Count == 0)
                _availableIndexes = Enumerable.Range(1, 22).ToList();
            
            int Index = _availableIndexes[_rand.Next(_availableIndexes.Count)];
            _availableIndexes.Remove(Index);

            string babyPath = System.IO.Path.GetFullPath($"Images/baby{Index}.png");
            string adultPath = System.IO.Path.GetFullPath($"Images/adult{Index}.png");

            _babyImage = new BitmapImage(new Uri(babyPath));
            _adultImage = new BitmapImage(new Uri(adultPath));

            BabyImage.Source = _babyImage;

            _babyScale = new ScaleTransform(1, 1);
            _babyTranslate = new TranslateTransform(0, 0);

            UnregisterName("BabyScale");
            UnregisterName("BabyTranslate");
            RegisterName("BabyScale", _babyScale);
            RegisterName("BabyTranslate", _babyTranslate);

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(_babyScale);
            transformGroup.Children.Add(_babyTranslate);
            BabyImage.RenderTransform = transformGroup;
           
        }

        private void ResetTransformAfterImageLoad(object sender, RoutedEventArgs e)
        {
            BabyImage.Loaded -= ResetTransformAfterImageLoad;

            _babyScale.ScaleX = 1;
            _babyScale.ScaleY = 1;
            _babyTranslate.X = 0;
            _babyTranslate.Y = 0;
        }
        private async void BabyImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);

            BabyImage.RenderTransformOrigin = new Point(0, 0); // From center

            // Animate shrink and move
            var scaleX = new DoubleAnimation(1, 0.33, TimeSpan.FromSeconds(0.5));
            var scaleY = new DoubleAnimation(1, 0.33, TimeSpan.FromSeconds(0.5));
            var moveX = new DoubleAnimation(0, -50, TimeSpan.FromSeconds(0.5));
            var moveY = new DoubleAnimation(0, 20, TimeSpan.FromSeconds(0.5));

            scaleX.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            scaleY.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            moveX.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            moveY.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };

            // Create storyboard
            var storyboard = new Storyboard();
            storyboard.Children.Add(scaleX);
            storyboard.Children.Add(scaleY);
            storyboard.Children.Add(moveX);
            storyboard.Children.Add(moveY);

            Storyboard.SetTargetName(scaleX, "BabyScale");
            Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));

            Storyboard.SetTargetName(scaleY, "BabyScale");
            Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));

            Storyboard.SetTargetName(moveX, "BabyTranslate");
            Storyboard.SetTargetProperty(moveX, new PropertyPath(TranslateTransform.XProperty));

            Storyboard.SetTargetName(moveY, "BabyTranslate");
            Storyboard.SetTargetProperty(moveY, new PropertyPath(TranslateTransform.YProperty));

            storyboard.Begin(this);

            // Start pixelation
            AdultImage.Visibility = Visibility.Visible;
            _currentPixelSize = 1;
            _maxPixelSize = Math.Min(_adultImage.PixelWidth, _adultImage.PixelHeight);
            
            AdultImage.Source = PixelateImage(_adultImage, _currentPixelSize);
            
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (TitleScreen.Visibility == Visibility.Visible && e.Key == Key.Space)
            {
                TitleScreen.Visibility = Visibility.Collapsed;
                LoadRandomPair();
                return;
            }
            base.OnKeyDown(e);
            switch (e.Key)
            {
                case Key.Enter:
                    TogglePause();
                    break;
                case Key.Escape:
                    FullReveal();
                    break;
                case Key.N:
                    LoadRandomPair();
                    break;
            }
        }
        
        private void TogglePause()
        {
            if (_timer == null) return;

            if (_isPaused)
            {
                _timer.Start();
                _isPaused = false;
            }
            else
            {
                _timer.Stop();
                _isPaused = true;
            }
        }

        private void FullReveal()
        {
            if (_adultImage == null) return;

            _timer?.Stop();
            AdultImage.Source = _adultImage;
            _isPaused = false;
        }
        private void Timer_Tick (object sender, EventArgs e)
        {
            if (_currentPixelSize >= _maxPixelSize)
            {
                _timer.Stop();
                AdultImage.Source = _adultImage;
                return;
            }

            AdultImage.Source = PixelateImage(_adultImage, _currentPixelSize);
            if (_currentPixelSize < 50)
            {
                _currentPixelSize += 1;
            }
            else if (_currentPixelSize < 100)
            {
                _currentPixelSize += 5;
            }
            else
            {
                _currentPixelSize += 20;
            }
        }

        private BitmapSource PixelateImage(BitmapImage original, int blockSize)
        {
            int width = original.PixelWidth;
            int height = original.PixelHeight;

            double scaleX = (double)blockSize / width;
            double scaleY = (double)blockSize / height;

            TransformedBitmap downscaled = new TransformedBitmap(original, new ScaleTransform(scaleX, scaleY));

            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawImage(downscaled, new Rect(0, 0, width, height));
            }

            rtb.Render(visual);
            return rtb;
        }
    }
}