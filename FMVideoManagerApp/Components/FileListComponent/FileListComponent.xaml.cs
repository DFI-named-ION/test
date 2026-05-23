using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace FMVideoManagerApp.Components.FileListComponent
{
    public partial class FileListComponent : UserControl
    {
        private const double MinWidth = 350;
        private const double MaxWidth = 500;

        private const double PreviewMinWidth = 300;
        private const double PreviewMinHeight = 225;
        private const double PreviewMaxWidth = 440;
        private const double PreviewMaxHeight = 330;

        public FileListComponent()
        {
            InitializeComponent();
        }

        private void SidePanel_MouseEnter(object sender, MouseEventArgs e)
        {
            PlayAnimation(MaxWidth, PreviewMaxWidth, PreviewMaxHeight);
        }

        private void SidePanel_MouseLeave(object sender, MouseEventArgs e)
        {
            PlayAnimation(MinWidth, PreviewMinWidth, PreviewMinHeight);
        }

        private void PlayAnimation(double targetPanelWidth, double targetPreviewWidth, double targetPreviewHeight)
        {
            var duration = TimeSpan.FromMilliseconds(300);

            //var panelAnimation = new DoubleAnimation
            //{
            //    To = targetPanelWidth,
            //    Duration = duration,
            //    EasingFunction = new BounceEase
            //    {
            //        EasingMode = EasingMode.EaseOut,
            //        Bounces = 3,
            //        Bounciness = 5
            //    }
            //};

            var panelAnimation = new DoubleAnimation
            {
                To = targetPanelWidth,
                Duration = duration,
                EasingFunction = new SineEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            var previewWidthAnimation = new DoubleAnimation
            {
                To = targetPreviewWidth,
                Duration = duration,
                EasingFunction = new SineEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            var previewHeightAnimation = new DoubleAnimation
            {
                To = targetPreviewHeight,
                Duration = duration,
                EasingFunction = new SineEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            SidePanel.BeginAnimation(FrameworkElement.WidthProperty, panelAnimation);
            Preview.BeginAnimation(FrameworkElement.WidthProperty, previewWidthAnimation);
            Preview.BeginAnimation(FrameworkElement.HeightProperty, previewHeightAnimation);
        }
    }
}
