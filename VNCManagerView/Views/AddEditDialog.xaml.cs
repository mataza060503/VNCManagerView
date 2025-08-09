// AddEditDialog.xaml.cs
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Animation;

namespace VNCManagerView
{
    public partial class AddEditDialog : Window
    {
        public string ResultText { get; private set; }

        public AddEditDialog(string title, string label, string initialValue)
        {
            InitializeComponent();
            TitleLabel.Text = title;
            Label.Content = label;
            TextBox.Text = initialValue;

            // Apply entrance animation
            ApplyEntranceAnimation();

            // Focus the textbox and select all text
            Loaded += (s, e) =>
            {
                TextBox.Focus();
                TextBox.SelectAll();
            };
        }

        private void ApplyEntranceAnimation()
        {
            // Start with the dialog slightly scaled down and transparent
            this.Opacity = 0;
            DialogBorder.RenderTransform = new System.Windows.Media.ScaleTransform(0.9, 0.9);
            DialogBorder.RenderTransformOrigin = new Point(0.5, 0.5);

            // Create entrance animation
            var fadeIn = new DoubleAnimation(0, 1, System.TimeSpan.FromMilliseconds(300));
            var scaleXAnimation = new DoubleAnimation(0.9, 1, System.TimeSpan.FromMilliseconds(300));
            var scaleYAnimation = new DoubleAnimation(0.9, 1, System.TimeSpan.FromMilliseconds(300));

            // Apply easing for smooth animation
            var easingFunction = new System.Windows.Media.Animation.BackEase
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = 0.1
            };

            scaleXAnimation.EasingFunction = easingFunction;
            scaleYAnimation.EasingFunction = easingFunction;

            // Start animations
            this.BeginAnimation(Window.OpacityProperty, fadeIn);
            ((System.Windows.Media.ScaleTransform)DialogBorder.RenderTransform).BeginAnimation(
                System.Windows.Media.ScaleTransform.ScaleXProperty, scaleXAnimation);
            ((System.Windows.Media.ScaleTransform)DialogBorder.RenderTransform).BeginAnimation(
                System.Windows.Media.ScaleTransform.ScaleYProperty, scaleYAnimation);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBox.Text))
            {
                MessageBox.Show("Please enter a valid name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TextBox.Focus();
                return;
            }

            ResultText = TextBox.Text.Trim();
            ApplyExitAnimation(() => DialogResult = true);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ApplyExitAnimation(() => DialogResult = false);
        }

        private void ApplyExitAnimation(System.Action onComplete)
        {
            var fadeOut = new DoubleAnimation(1, 0, System.TimeSpan.FromMilliseconds(200));
            var scaleAnimation = new DoubleAnimation(1, 0.9, System.TimeSpan.FromMilliseconds(200));

            fadeOut.Completed += (s, e) => {
                if (this.IsLoaded && this.IsVisible)
                    onComplete?.Invoke();
            };

            this.BeginAnimation(Window.OpacityProperty, fadeOut);
            ((System.Windows.Media.ScaleTransform)DialogBorder.RenderTransform).BeginAnimation(
                System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnimation);
            ((System.Windows.Media.ScaleTransform)DialogBorder.RenderTransform).BeginAnimation(
                System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnimation);
        }
    }
}