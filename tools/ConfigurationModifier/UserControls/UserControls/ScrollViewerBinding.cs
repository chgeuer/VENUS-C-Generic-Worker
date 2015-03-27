//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using LinqToVisualTree;

namespace UserControls
{
   
    public static class ScrollViewerBinding
    {
        #region VerticalOffset attached property

      
        public static double GetVerticalOffset(DependencyObject depObj)
        {
            return (double)depObj.GetValue(VerticalOffsetProperty);
        }

        public static void SetVerticalOffset(DependencyObject depObj, double value)
        {
            depObj.SetValue(VerticalOffsetProperty, value);
        }

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double),
            typeof(ScrollViewerBinding), new PropertyMetadata(0.0, OnVerticalOffsetPropertyChanged));

        #endregion

        #region HorizontalOffset attached property

        public static double GetHorizontalOffset(DependencyObject depObj)
        {
            return (double)depObj.GetValue(HorizontalOffsetProperty);
        }

        public static void SetHorizontalOffset(DependencyObject depObj, double value)
        {
            depObj.SetValue(HorizontalOffsetProperty, value);
        }

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.RegisterAttached("HorizontalOffset", typeof(double),
            typeof(ScrollViewerBinding), new PropertyMetadata(0.0, OnHorizontalOffsetPropertyChanged));

        #endregion

        #region VerticalScrollBar attached property

        private static readonly DependencyProperty VerticalScrollBarProperty =
            DependencyProperty.RegisterAttached("VerticalScrollBar", typeof(ScrollBar),
            typeof(ScrollViewerBinding), new PropertyMetadata(null));

        private static readonly DependencyProperty HorizontalScrollBarProperty =
            DependencyProperty.RegisterAttached("HorizontalScrollBar", typeof(ScrollBar),
            typeof(ScrollViewerBinding), new PropertyMetadata(null));

        #endregion

        private static void OnVerticalOffsetPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            ScrollViewer sv = d as ScrollViewer;
            if (sv != null)
            {
                if (sv.GetValue(VerticalScrollBarProperty) == null)
                {
                    sv.LayoutUpdated += (s, ev) =>
                    {
                        if (sv.GetValue(VerticalScrollBarProperty) == null)
                        {
                            GetScrollBarsForScrollViewer(sv);
                        }
                    };
                }
                else
                {
                    var value = (double)e.NewValue; 
                    sv.ScrollToVerticalOffset(value);
                }
            }
        }

        private static void OnHorizontalOffsetPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            ScrollViewer sv = d as ScrollViewer;
            if (sv != null)
            {
                if (sv.GetValue(HorizontalScrollBarProperty) == null)
                {
                    sv.LayoutUpdated += (s, ev) =>
                    {
                        if (sv.GetValue(HorizontalScrollBarProperty) == null)
                        {
                            GetScrollBarsForScrollViewer(sv);
                        }
                    };
                }
                else
                {
                    sv.ScrollToHorizontalOffset((double)e.NewValue);
                }
            }
        }

        private static void GetScrollBarsForScrollViewer(ScrollViewer scrollViewer)
        {
            ScrollBar scroll = GetScrollBar(scrollViewer, Orientation.Vertical);
            if (scroll != null)
            {
                scrollViewer.SetValue(VerticalScrollBarProperty, scroll);
                scrollViewer.ScrollToVerticalOffset(ScrollViewerBinding.GetVerticalOffset(scrollViewer));

                scroll.ValueChanged += (s, e) =>
                {
                    SetVerticalOffset(scrollViewer, e.NewValue);
                };
            }

            scroll = GetScrollBar(scrollViewer, Orientation.Horizontal);
            if (scroll != null)
            {
                scrollViewer.SetValue(HorizontalScrollBarProperty, scroll);

                scrollViewer.ScrollToHorizontalOffset(ScrollViewerBinding.GetHorizontalOffset(scrollViewer));

                scroll.ValueChanged += (s, e) =>
                {
                    scrollViewer.SetValue(HorizontalOffsetProperty, e.NewValue);
                };
            }
        }

        private static ScrollBar GetScrollBar(FrameworkElement fe, System.Windows.Controls.Orientation orientation)
        {
            return fe.Descendants()
                      .OfType<ScrollBar>()
                      .Where(s => s.Orientation == orientation)
                      .SingleOrDefault();

        }
    }
}
