using haruna.adornments;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace haruna
{
    [Export(typeof(IWpfTextViewCreationListener))]
    //[Export(typeof(IWpfTextViewMarginProvider)), MarginContainer(PredefinedMarginNames.Right), Order(Before =PredefinedMarginNames.RightControl)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("text/asm")]
    [Name("CoreTextViewCreationListener")]
    public class CoreTextViewCreationListener : IWpfTextViewCreationListener, IWpfTextViewMarginProvider
    {
        static bool _isHooked = false;

        static void HookVisualStudioUI()
        {            
            if (_isHooked) return;

            _isHooked = true;

            var vs = Application.Current.MainWindow;
            var titlebar = vs.Template.FindName("PART_TitleBarFrameControlContainer", vs) as ItemsControl;
            var menubar = vs.Template.FindName("PART_MenuBarFrameControlContainer", vs) as ItemsControl;
            var statuspanel = vs.Template.FindName("StatusBarPanel", vs) as DockPanel;

            // var frameControl = Activator.CreateInstance(listView.GetItemAt(0).GetType());


            if (titlebar != null)
            {
                var listView = titlebar.ItemsSource as System.Windows.Data.ListCollectionView;

                if(listView != null && listView.Count > 0)
                {
                    var ty = listView.GetItemAt(0).GetType();
                    FrameControlWrapper.InitType(ty);
                    foreach (var p in ty.GetProperties().OrderBy(p=>p.Name))
                    {
                        Debug.WriteLine(string.Format("{0} {1}", p.PropertyType.Name, p.Name));
                    }

                    var list = new ObservableCollection<object>(listView.OfType<object>());
                    titlebar.ItemsSource = null;
                    titlebar.ItemsSource = list;

                    
                    var w = new FrameControlWrapper();
                    w.FrameworkElement = new Border() { Width = 180, Background = Brushes.RosyBrown,Margin=new Thickness(2.0) };
                    list.Add(w.Detach());
                    int ss = 0;
                    
                }
            }

            if(menubar != null)
            {
                var listView = menubar.ItemsSource as System.Windows.Data.ListCollectionView;

                if (listView != null && listView.Count > 0)
                {
                    var list = new ObservableCollection<object>(listView.OfType<object>());
                    menubar.ItemsSource = null;
                    menubar.ItemsSource = list;
                }
            }

            if(statuspanel != null)
            {
                var txt = new TextBox() {
                    Width = 128//, Height = 24
                };
                DockPanel.SetDock(txt, Dock.Left);
                statuspanel.Children.Insert(0, txt);
            }

            int a = 0;
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            // HookVisualStudioUI();            

            var layer = textView.GetAdornmentLayer("UiAdornmentLayer");
            if (layer == null) return;

            SimpleAdroner box;
            if(!textView.Properties.TryGetProperty(typeof(SimpleAdroner), out box))
            {
                var grid = new Grid()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = textView.ViewportWidth,
                    Height = textView.ViewportHeight,
                    Tag = "ASMTooltipBox"
                };
                box = new SimpleAdroner()
                {
                    Background = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Width = 400.0,
                    Height = 120.0
                };
                grid.Children.Add(box);

                textView.Properties.AddProperty(typeof(SimpleAdroner), box);
                layer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, textView, grid, null);
                
                textView.ViewportWidthChanged += TextView_ViewportWidthChanged;
                textView.ViewportHeightChanged += TextView_ViewportHeightChanged;
                textView.LayoutChanged += TextView_LayoutChanged;

            }
        }

        private void TextView_ViewportHeightChanged(object sender, EventArgs e)
        {
            var textView = sender as IWpfTextView;
            resize(textView);
        }

        private void TextView_ViewportWidthChanged(object sender, EventArgs e)
        {
            var textView = sender as IWpfTextView;
            resize(textView);
        }

        void resize(IWpfTextView textView)
        {
            var layer = textView.GetAdornmentLayer("UiAdornmentLayer");            
            foreach(var grid in layer.Elements.Where(e=>e.Tag == textView && e.Adornment is Grid && ((Grid)e.Adornment).Tag != null && ((Grid)e.Adornment).Tag.ToString() == "ASMTooltipBox").Select(e=>(Grid)e.Adornment))
            {
                grid.Width = textView.ViewportWidth;
                grid.Height = textView.ViewportHeight;
            }

            AsmMainMargin margin;
            if (textView.Properties.TryGetProperty<AsmMainMargin>(typeof(AsmMainMargin), out margin))
            {
                margin.VisualElement.Height = textView.ViewportHeight;
            }

        }

        private void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            var textView = sender as IWpfTextView;
           
        }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            AsmMainMargin margin;
            if(!wpfTextViewHost.TextView.Properties.TryGetProperty<AsmMainMargin>(typeof(AsmMainMargin), out margin))
            {
                margin = new AsmMainMargin(wpfTextViewHost);
                wpfTextViewHost.TextView.Properties.AddProperty(typeof(AsmMainMargin), margin);
            }
            return margin;
        }
    }

    [Name(AsmMainMargin.NAME)]
    internal class AsmMainMargin : IWpfTextViewMargin
    {
        public const string NAME = "AsmMainMargin";

        IWpfTextViewHost m_view;
        public AsmMainMargin(IWpfTextViewHost view)
        {
            m_view = view;
        }

        public bool Enabled
        {
            get
            {
                return true;
            }
        }

        public double MarginSize
        {
            get
            {
                return 1.0;
            }
        }

        FrameworkElement _visualElement;
        public FrameworkElement VisualElement
        {
            get
            {
                if(_visualElement == null)
                {
                    _visualElement = new Border()
                    {
                        Width = 256.0,
                        Height = 320.0,
                        Background = Brushes.Silver
                    };
                }
                return _visualElement; 
            }
        }

        public void Dispose()
        {
            
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            if (marginName == NAME) return this;
            return null;
        }
    }

    class FrameControlWrapper
    {
        static Type s_Type;
        public static void InitType(Type typeofFrameControl)
        {
            if (typeofFrameControl == null) throw new ArgumentNullException();

            if (s_Type != null) throw new InvalidOperationException();

            s_Type = typeofFrameControl;
            s_DisplayNameProp = s_Type.GetProperty("DisplayName");
            s_FrameworkElementProp = s_Type.GetField("_frameworkElement", BindingFlags.NonPublic|BindingFlags.Instance);
            s_AlignmentProp = s_Type.GetProperty("Alignment");
            s_SortProp = s_Type.GetProperty("Sort");
            s_FullScreenAlignmentProp = s_Type.GetProperty("FullScreenAlignment");
            s_FullScreenSortProp = s_Type.GetProperty("FullScreenSort");
        }

        object _This;
        public FrameControlWrapper()
        {
            if (s_Type == null) throw new InvalidOperationException();

            _This = Activator.CreateInstance(s_Type);            
        }

        public FrameControlWrapper(object instance)
        {
            if (instance == null) throw new ArgumentNullException();

            _This = instance;
        }

        public object Detach()
        {
            var ret = _This;
            _This = null;
            return ret;
        }

        public const int AlignmentNone = 0;
        public const int AlignmentTitleBarRight = 1;
        public const int AlignmentMenuBarRight = 2;

        // String DisplayName
        static PropertyInfo s_DisplayNameProp;
        public string DisplayName
        {
            get { return (string)s_DisplayNameProp.GetValue(_This); }
            set { s_DisplayNameProp.SetValue(_This, value); }
        }
        
        // FrameworkElement FrameworkElement
        static FieldInfo s_FrameworkElementProp;
        public FrameworkElement FrameworkElement
        {
            get { return (FrameworkElement)s_FrameworkElementProp.GetValue(_This); }
            set { s_FrameworkElementProp.SetValue(_This, value); }
        }
        
        // FrameControlAlignment Alignment
        static PropertyInfo s_AlignmentProp;
        public int Alignment
        {
            get { return (int)s_AlignmentProp.GetValue(_This); }
            set { s_AlignmentProp.SetValue(_This, value); }
        }

        // Int32 Sort
        static PropertyInfo s_SortProp;
        public int Sort
        {
            get { return (int)s_SortProp.GetValue(_This); }
            set { s_SortProp.SetValue(_This, value); }
        }

        // FrameControlAlignment FullScreenAlignment
        static PropertyInfo s_FullScreenAlignmentProp;
        public int FullScreenAlignment
        {
            get { return (int)s_FullScreenAlignmentProp.GetValue(_This); }
            set { s_FullScreenAlignmentProp.SetValue(_This, value); }
        }

        // Int32 FullScreenSort
        static PropertyInfo s_FullScreenSortProp;
        public int FullScreenSort
        {
            get { return (int)s_FullScreenSortProp.GetValue(_This); }
            set { s_FullScreenSortProp.SetValue(_This, value); }
        }

        //DependencyObjectType DependencyObjectType
        //Dispatcher Dispatcher

    }
}
