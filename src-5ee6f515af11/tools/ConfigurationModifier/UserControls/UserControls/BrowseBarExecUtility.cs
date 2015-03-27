//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading;

namespace UserControls
{
    public static class BrowseBarExecUtility
    {
        public static readonly DependencyProperty BindableBrowseFunction =
            DependencyProperty.RegisterAttached("BindableBrowseFunction", typeof(Func<string, CancellationTokenSource>), typeof(BrowseBarExecUtility), new PropertyMetadata(null, BindableBrowseFunctionChanged));

        public static Func<string, CancellationTokenSource> GetBindableBrowseFunction(DependencyObject obj)
        {
            return (Func<string, CancellationTokenSource>)obj.GetValue(BindableBrowseFunction);
        }

        public static void SetBindableBrowseFunction(DependencyObject obj, Func<string, CancellationTokenSource> value)
        {
            obj.SetValue(BindableBrowseFunction, value);
        }

        public static void BindableBrowseFunctionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            BrowseBar browseBar = o as BrowseBar;
            if (browseBar != null)
            {
                Func<string, CancellationTokenSource> action = e.NewValue as Func<string, CancellationTokenSource>;
                browseBar.BrowseFunction = action;
            }
        }

        public static readonly DependencyProperty BindableMultiSelect =
            DependencyProperty.RegisterAttached("BindableMultiSelect", typeof(bool), typeof(BrowseBarExecUtility), new PropertyMetadata(false, BindableMultiSelectChanged));

        public static bool GetBindableMultiSelect(DependencyObject obj)
        {
            return (bool)obj.GetValue(BindableMultiSelect);
        }

        public static void SetBindableMultiSelect(DependencyObject obj, bool value)
        {
            obj.SetValue(BindableMultiSelect, value);
        }

        public static void BindableMultiSelectChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            BrowseBar browseBar = o as BrowseBar;
            if (browseBar != null)
            {
                bool? multiSelect = e.NewValue as bool?;
                browseBar.MultiSelect = multiSelect.HasValue && multiSelect.Value;
            }
        }


        public static readonly DependencyProperty BindableFilter =
            DependencyProperty.RegisterAttached("BindableFilter", typeof(string), typeof(BrowseBarExecUtility), new PropertyMetadata("All files|*.*", BindableFilterChanged));

        public static string GetBindableFilter(DependencyObject obj)
        {
            return (string)obj.GetValue(BindableFilter);
        }

        public static void SetBindableFilter(DependencyObject obj, string value)
        {
            obj.SetValue(BindableFilter, value);
        }

        public static void BindableFilterChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            BrowseBar browseBar = o as BrowseBar;
            if (browseBar != null)
            {
                string filter = e.NewValue as string;
                browseBar.Filter = filter;
            }
        }

        public static readonly DependencyProperty BindableText =
          DependencyProperty.RegisterAttached("BindableText", typeof(string), typeof(BrowseBarExecUtility), new PropertyMetadata("Browse", BindableTextChanged));

        public static string GetBindableText(DependencyObject obj)
        {
            return (string)obj.GetValue(BindableText);
        }

        public static void SetBindableText(DependencyObject obj, string value)
        {
            obj.SetValue(BindableText, value);
        }

        public static void BindableTextChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            BrowseBar browseBar = o as BrowseBar;
            if (browseBar != null)
            {
                string text = e.NewValue as string;
                browseBar.Text = text;
            }
        }
    }
}
