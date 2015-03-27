//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace UserControls
{
    public class ScrollBarConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object convertedValue = null;

            if (values.Count() == 2 && targetType == typeof(double))
            {
                double[] doubleValues = new double[2];
                try
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (values[i] is double)
                        {
                            doubleValues[i] = (double)values[i];
                        }
                    }
                    convertedValue = doubleValues[0] * doubleValues[1];
                }
                catch
                {
                    convertedValue = 0;
                }
            }

            return convertedValue;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new object[2];
        }
    }
}
