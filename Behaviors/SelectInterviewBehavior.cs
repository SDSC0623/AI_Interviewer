// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using Wpf.Ui.Controls;

namespace AI_Interviewer.Behaviors;

public class SelectInterviewBehavior : Behavior<Border> {
    private bool _selected;

    public Brush HoverBackgroundBrush {
        get => (Brush)GetValue(HoverBackgroundBrushProperty);
        set => SetValue(HoverBackgroundBrushProperty, value);
    }

    public static readonly DependencyProperty HoverBackgroundBrushProperty = DependencyProperty.Register(
        nameof(HoverBackgroundBrush),
        typeof(Brush),
        typeof(SelectInterviewBehavior),
        new UIPropertyMetadata(Brushes.Transparent)
    );

    public Brush NormalBackgroundBrush {
        get => (Brush)GetValue(NormalBackgroundBrushProperty);
        set => SetValue(NormalBackgroundBrushProperty, value);
    }

    public static readonly DependencyProperty NormalBackgroundBrushProperty = DependencyProperty.Register(
        nameof(NormalBackgroundBrush),
        typeof(Brush),
        typeof(SelectInterviewBehavior),
        new UIPropertyMetadata(Brushes.Transparent)
    );

    public Brush SelectedBackgroundBrush {
        get => (Brush)GetValue(SelectedBackgroundBrushProperty);
        set => SetValue(SelectedBackgroundBrushProperty, value);
    }

    public static readonly DependencyProperty SelectedBackgroundBrushProperty = DependencyProperty.Register(
        nameof(SelectedBackgroundBrush),
        typeof(Brush),
        typeof(SelectInterviewBehavior),
        new UIPropertyMetadata(Brushes.Transparent)
    );

    public ICommand? MouseLeftClickCommand {
        get => GetValue(MouseLeftClickCommandProperty) as ICommand;
        set => SetValue(MouseLeftClickCommandProperty, value);
    }

    public static readonly DependencyProperty MouseLeftClickCommandProperty = DependencyProperty.Register(
        nameof(MouseLeftClickCommand),
        typeof(ICommand),
        typeof(SelectInterviewBehavior),
        new PropertyMetadata(null)
    );

    public ICommand? MouseLeftDoubleClickCommand {
        get => GetValue(MouseLeftDoubleClickCommandProperty) as ICommand;
        set => SetValue(MouseLeftDoubleClickCommandProperty, value);
    }

    public static readonly DependencyProperty MouseLeftDoubleClickCommandProperty = DependencyProperty.Register(
        nameof(MouseLeftDoubleClickCommand),
        typeof(ICommand),
        typeof(SelectInterviewBehavior),
        new PropertyMetadata(null)
    );

    public object CommandParameter {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter),
        typeof(object),
        typeof(SelectInterviewBehavior),
        new PropertyMetadata(null)
    );

    public object SelectedInterview {
        get => GetValue(SelectedInterviewProperty);
        set => SetValue(SelectedInterviewProperty, value);
    }

    public static readonly DependencyProperty SelectedInterviewProperty = DependencyProperty.Register(
        nameof(SelectedInterview),
        typeof(object),
        typeof(SelectInterviewBehavior),
        new PropertyMetadata(null, OnSelectedInterviewChanged)
    );

    private static void OnSelectedInterviewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is SelectInterviewBehavior behavior) {
            behavior.UpdateBackground();
        }
    }

    private void UpdateBackground() {
        _selected = Self == SelectedInterview;
        if (!_selected) {
            AssociatedObject.Background = NormalBackgroundBrush;
        }
    }

    public object Self {
        get => GetValue(SelfProperty);
        set => SetValue(SelfProperty, value);
    }

    public static readonly DependencyProperty SelfProperty = DependencyProperty.Register(
        nameof(Self),
        typeof(object),
        typeof(SelectInterviewBehavior),
        new PropertyMetadata(null)
    );

    protected override void OnAttached() {
        AssociatedObject.MouseEnter += OnMouseEnter;
        AssociatedObject.MouseLeave += OnMouseLeave;
        AssociatedObject.MouseDown += OnMouseLeftButtonUp;
        AssociatedObject.Loaded += (_, _) => { UpdateBackground(); };
    }

    protected override void OnDetaching() {
        AssociatedObject.MouseEnter -= OnMouseEnter;
        AssociatedObject.MouseLeave -= OnMouseLeave;
        AssociatedObject.MouseDown -= OnMouseLeftButtonUp;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        if (e.ClickCount == 1) {
            if (MouseLeftClickCommand != null && MouseLeftClickCommand.CanExecute(CommandParameter)) {
                MouseLeftClickCommand.Execute(CommandParameter);
            }
        } else {
            if (MouseLeftDoubleClickCommand != null && MouseLeftDoubleClickCommand.CanExecute(CommandParameter)) {
                MouseLeftDoubleClickCommand.Execute(CommandParameter);
            }
        }
    }

    private void OnMouseEnter(object sender, MouseEventArgs e) {
        AssociatedObject.Background = HoverBackgroundBrush;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e) {
        AssociatedObject.Background = _selected ? SelectedBackgroundBrush : NormalBackgroundBrush;
    }
}