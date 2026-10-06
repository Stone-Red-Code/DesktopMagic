using DesktopMagic.DataContexts;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopMagic.Controls;

/// <summary>
/// Visual screen selector showing the connected monitors as a mini-map
/// scaled to the control's size while preserving their relative positions.
/// </summary>
public partial class ScreenSelector : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ScreenSelector),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(ScreenDisplay), typeof(ScreenSelector),
            new PropertyMetadata(null, OnSelectedItemChanged));

    public ScreenDisplay? SelectedItem
    {
        get => (ScreenDisplay?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    private INotifyCollectionChanged? notifySource;

    /// <summary>
    /// Raised when the user picks a screen by clicking it.
    /// </summary>
    public event Action? SelectionChanged;

    public ScreenSelector()
    {
        InitializeComponent();
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ScreenSelector control = (ScreenSelector)d;
        control.AttachSource();
        control.UpdateCanvas();
        control.UpdateSelection();
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ScreenSelector)d).UpdateSelection();
    }

    private void AttachSource()
    {
        if (notifySource is INotifyCollectionChanged oldSource)
        {
            oldSource.CollectionChanged -= Source_CollectionChanged;
        }

        notifySource = ItemsSource as INotifyCollectionChanged;
        if (notifySource is not null)
        {
            notifySource.CollectionChanged += Source_CollectionChanged;
        }
    }

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCanvas();
        UpdateSelection();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Re-attach in case the control was unloaded and the source instance changed.
        AttachSource();
        UpdateCanvas();
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateCanvas();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (notifySource is INotifyCollectionChanged source)
        {
            source.CollectionChanged -= Source_CollectionChanged;
        }

        notifySource = null;
    }

    /// <summary>
    /// Scales the real screen bounds so they fit the control while keeping their
    /// relative positions (including negative coordinates), then centers them.
    /// </summary>
    private void UpdateCanvas()
    {
        if (ItemsSource is null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        List<ScreenDisplay> screens = ItemsSource.Cast<ScreenDisplay>().ToList();
        if (screens.Count == 0)
        {
            return;
        }

        // Union of all monitor bounds (virtual screen space, negative coords kept).
        Rectangle totalBounds = new Rectangle();
        foreach (var item in screens)
        {
            totalBounds = Rectangle.Union(totalBounds, item.Bounds);
        }

        // Uniform scale factor + a little margin so the mini-map never touches the edge.
        double factor = Math.Max(totalBounds.Height / ActualHeight, totalBounds.Width / ActualWidth) + 2;

        foreach (var item in screens)
        {
            item.X = item.Bounds.Left / factor;
            item.Y = item.Bounds.Top / factor;
            item.Width = item.Bounds.Width / factor;
            item.Height = item.Bounds.Height / factor;
        }

        // Center the whole arrangement in the control.
        double minLeft = screens.Min(item => item.X);
        double maxRight = screens.Max(item => item.X + item.Width);
        double minTop = screens.Min(item => item.Y);
        double maxBottom = screens.Max(item => item.Y + item.Height);

        double horizontalOffset = ((maxRight + minLeft) / 2) - (ActualWidth / 2);
        double verticalOffset = ((maxBottom + minTop) / 2) - (ActualHeight / 2);

        foreach (var item in screens)
        {
            item.X -= horizontalOffset;
            item.Y -= verticalOffset;
        }
    }

    private void UpdateSelection()
    {
        if (ItemsSource is null)
        {
            return;
        }

        foreach (ScreenDisplay item in ItemsSource)
        {
            item.IsSelected = item == SelectedItem;
        }
    }

    private void Screen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ScreenDisplay screen)
        {
            SelectedItem = screen;
            SelectionChanged?.Invoke();
        }

        e.Handled = true;
    }
}
