// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AI_Interviewer.Helpers;

namespace AI_Interviewer.Converters;

// Boolean显示转换器
public class BooleanToVisibilityConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not bool visible) {
            return Visibility.Visible;
        }

        if (parameter is "True") {
            visible = !visible;
        }

        return visible switch {
            true => Visibility.Visible,
            false => Visibility.Collapsed
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// bool取反转换器
public class BoolInverseConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is bool boolValue) {
            return !boolValue;
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is bool boolValue) {
            return !boolValue;
        }

        return false;
    }
}

public class FieldConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is string field) {
            return field switch {
                "professional_ability" => "个人能力",
                "communication" => "交流能力",
                "thinking_ability" => "思考能力",
                "attitude_and_values" => "态度与价值",
                "learning_potential" => "学习潜力",
                _ => field
            };
        }

        return value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}