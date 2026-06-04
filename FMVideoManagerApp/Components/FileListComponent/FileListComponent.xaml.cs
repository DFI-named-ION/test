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

        public FileListComponent()
        {
            InitializeComponent();
        }

        private void SidePanel_MouseEnter(object sender, MouseEventArgs e)
        {
            PlayAnimation(MaxWidth);
        }

        private void SidePanel_MouseLeave(object sender, MouseEventArgs e)
        {
            PlayAnimation(MinWidth);
        }

        private void PlayAnimation(double targetPanelWidth)
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

            SidePanel.BeginAnimation(FrameworkElement.WidthProperty, panelAnimation);
        }
    }
}
