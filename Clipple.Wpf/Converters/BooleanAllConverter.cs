﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Clipple.Converters;

public class BooleanAllConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var value in values)
            if (value is bool boolean && boolean == false)
                return false;

        return true;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}