using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace haruna.adornments
{
    public partial class SimpleAdroner : UserControl
    {
        public SimpleAdroner( )
        {
            this.Visibility = Visibility.Hidden;

            InitializeComponent();
            
            this.Loaded += (OO, EE) =>
            {
                Dispatcher.BeginInvoke(new Action(()=>
                {
                    togStrecth.IsEnabled = false;
                    togShow.IsChecked = true;
                    togShow.IsChecked = false;
                    this.Visibility = Visibility.Visible;
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            };
        }

        public void CleanUp()
        {
            tbText.Text = string.Empty;
        }

        public void SetText(string value)
        {
            tbText.Text = value;
        }

        double? m_orgWidth;
        private void ToggleStretch_Checked(object sender, RoutedEventArgs e)
        {
            togStrecth.RenderTransformOrigin = new Point(0.5, 0.5);
            togStrecth.RenderTransform = new RotateTransform(180);
            if (m_orgWidth.GetValueOrDefault() <= 0 && this.RenderSize.Width != 40.0)
            {
                m_orgWidth = this.RenderSize.Width;
            }
            this.Width = ((UIElement)Parent).RenderSize.Width;            
        }

        private void ToggleStretch_Unchecked(object sender, RoutedEventArgs e)
        {
            togStrecth.RenderTransform = null;
            if (m_orgWidth.GetValueOrDefault() > 0 && m_orgWidth.GetValueOrDefault() != 40)
            {
                this.Width = m_orgWidth.Value;
            }            
        }

        private void ToggleShow_Checked(object sender, RoutedEventArgs e)
        {
            togShow.RenderTransformOrigin = new Point(0.5, 0.5);
            togShow.RenderTransform = new RotateTransform(180);
            togStrecth.IsEnabled = true;
            tbText.Visibility = Visibility.Visible;
            if(m_orgHeight.GetValueOrDefault() > 0)
            {
                this.Height = m_orgHeight.Value;
            }
            if (m_orgWidth.GetValueOrDefault() > 0 && m_orgWidth.GetValueOrDefault() != 40)
            {
                this.Width = m_orgWidth.Value;
            }
        }

        double? m_orgHeight;
        private void ToggleShow_Unchecked(object sender, RoutedEventArgs e)
        {
            togShow.RenderTransform = null;
            togStrecth.IsEnabled = false;
            togStrecth.IsChecked = false;
            if (m_orgHeight.GetValueOrDefault() <= 0)
            {
                m_orgHeight = this.RenderSize.Height;
            }
            tbText.Visibility = Visibility.Collapsed;
            this.Height = 16;
            this.m_orgWidth = this.Width;
            this.Width = 40;
        }

    }
}
