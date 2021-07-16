﻿namespace AJut.Application.Controls
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using AJut.Application.Docking;
    using DPUtils = AJut.Application.DPUtils<DockZoneTabHeadersControl>;

    public class DockZoneTabHeadersControl : Control
    {
        public static RoutedUICommand SelectItemCommand = new RoutedUICommand("Select Item", nameof(SelectItemCommand), typeof(DockZoneTabHeadersControl));

        private readonly ObservableCollection<HeaderItem> m_items = new ObservableCollection<HeaderItem>();
        static DockZoneTabHeadersControl ()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DockZoneTabHeadersControl), new FrameworkPropertyMetadata(typeof(DockZoneTabHeadersControl)));
        }

        public DockZoneTabHeadersControl ()
        {
            this.Items = new ReadOnlyObservableCollection<HeaderItem>(m_items);
            this.CommandBindings.Add(new CommandBinding(SelectItemCommand, OnSelectedItem, OnCanSelectItem));
            this.CommandBindings.Add(new CommandBinding(DragDropElement.HorizontalDragInitiatedCommand, OnInitiateElementReorder, CanInitiateElementReorder));
            this.CommandBindings.Add(new CommandBinding(DragDropElement.VerticalDragInitiatedCommand, OnInitiateTearOff));
        }

        private void OnCanSelectItem (object sender, CanExecuteRoutedEventArgs e)
        {
            if ((((e.OriginalSource as FrameworkElement)?.DataContext as HeaderItem)?.IsSelected ?? true) == false)
            {
                e.CanExecute = true;
            }
        }

        private void OnSelectedItem (object sender, ExecutedRoutedEventArgs e)
        {
            this.SetSelection((HeaderItem)((FrameworkElement)e.OriginalSource).DataContext);
        }

        private void CanInitiateElementReorder (object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource is UIElement uisource && uisource.GetFirstParentOf<ItemsControl>(eTraversalTree.Both) is ItemsControl itemsControl && ItemsControl.ContainerFromElement(itemsControl, uisource) as UIElement != null)
            {
                e.CanExecute = true;
            }
        }

        private async void OnInitiateElementReorder (object sender, ExecutedRoutedEventArgs e)
        {
            var uisource = (UIElement)e.OriginalSource;
            var container = (UIElement)ItemsControl.ContainerFromElement(uisource.GetFirstParentOf<ItemsControl>(eTraversalTree.Both), uisource);

            await DragDropElement.DoDragReorder(container).ConfigureAwait(false);
        }

        private void OnInitiateTearOff (object sender, ExecutedRoutedEventArgs e)
        {
            var initial = (Point)e.Parameter;
            var castedSource = (UIElement)e.OriginalSource;

            var window = Window.GetWindow(castedSource);
            Point desktopMouseLocation = (Point)((Vector)window.PointToScreen(castedSource.TranslatePoint(initial, window)) - (Vector)initial);

            DockingContentAdapterModel target = ((HeaderItem)((FrameworkElement)castedSource).DataContext).Adapter;
            var result = target.DockingOwner.DoTearOff(target.Display, desktopMouseLocation);
            if (result)
            {
                result.Value.DragMove();
            }
        }


        public static readonly DependencyProperty HeaderBorderProperty = DPUtils.Register(_ => _.HeaderBorder);
        public Brush HeaderBorder
        {
            get => (Brush)this.GetValue(HeaderBorderProperty);
            set => this.SetValue(HeaderBorderProperty, value);
        }

        public static readonly DependencyProperty HeaderBackgroundProperty = DPUtils.Register(_ => _.HeaderBackground);
        public Brush HeaderBackground
        {
            get => (Brush)this.GetValue(HeaderBackgroundProperty);
            set => this.SetValue(HeaderBackgroundProperty, value);
        }

        public static readonly DependencyProperty HeaderHighlightBackgroundProperty = DPUtils.Register(_ => _.HeaderHighlightBackground);
        public Brush HeaderHighlightBackground
        {
            get => (Brush)this.GetValue(HeaderHighlightBackgroundProperty);
            set => this.SetValue(HeaderHighlightBackgroundProperty, value);
        }

        public static readonly DependencyProperty HeaderSelectedBackgroundProperty = DPUtils.Register(_ => _.HeaderSelectedBackground);
        public Brush HeaderSelectedBackground
        {
            get => (Brush)this.GetValue(HeaderSelectedBackgroundProperty);
            set => this.SetValue(HeaderSelectedBackgroundProperty, value);
        }

        public static readonly DependencyProperty HeaderItemsSourceProperty = DPUtils.Register(_ => _.ItemsSource, (d,e)=>d.OnItemsSourceChanged(e));
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)this.GetValue(HeaderItemsSourceProperty);
            set => this.SetValue(HeaderItemsSourceProperty, value);
        }

        private static readonly DependencyPropertyKey SelectedItemPropertyKey = DPUtils.RegisterReadOnly(_ => _.SelectedItem, (d,e)=>d.SetSelection(e.NewValue));
        public static readonly DependencyProperty SelectedItemProperty = SelectedItemPropertyKey.DependencyProperty;
        public HeaderItem SelectedItem
        {
            get => (HeaderItem)this.GetValue(SelectedItemProperty);
            protected set => this.SetValue(SelectedItemPropertyKey, value);
        }

        public ReadOnlyObservableCollection<HeaderItem> Items { get; }

        public static readonly DependencyProperty ItemsOrientationProperty = DPUtils.Register(_ => _.ItemsOrientation, Orientation.Horizontal);
        public Orientation ItemsOrientation
        {
            get => (Orientation)this.GetValue(ItemsOrientationProperty);
            set => this.SetValue(ItemsOrientationProperty, value);
        }

        private void SetSelection (HeaderItem newValue)
        {
            foreach (var item in this.Items.Where(i => i != newValue))
            {
                item.IsSelected = false;
            }

            newValue.IsSelected = true;
            this.SelectedItem = newValue;
        }

        private void OnItemsSourceChanged (DependencyPropertyChangedEventArgs<IEnumerable> e)
        {
            if (e.OldValue is INotifyCollectionChanged oldOC)
            {
                oldOC.CollectionChanged -= _OnItemsSourceCollectionChanged;
            }
            m_items.Clear();

            if (e.HasNewValue)
            {
                m_items.AddEach(e.NewValue.OfType<DockingContentAdapterModel>().Select(a => new HeaderItem(a)));
                if (e.NewValue is INotifyCollectionChanged newOC)
                {
                    newOC.CollectionChanged += _OnItemsSourceCollectionChanged;
                }

                if (this.SelectedItem == null)
                {
                    this.SetSelection(m_items.First());
                }
            }

            void _OnItemsSourceCollectionChanged (object _sender, NotifyCollectionChangedEventArgs _e)
            {
                if (_e.NewItems != null)
                {
                    m_items.InsertEach(_e.NewStartingIndex, _e.NewItems.OfType<DockingContentAdapterModel>().Select(a => new HeaderItem(a)));
                }

                int lastSelectedIndex = m_items.FirstIndexMatching(i => i.IsSelected);
                bool _didRemove = false;
                if (_e.OldItems != null)
                {
                    m_items.RemoveAll(i => _e.OldItems.Contains(i.Adapter));
                    _didRemove = true;
                }
                if (_e.Action == NotifyCollectionChangedAction.Reset)
                {
                    m_items.Clear();
                    _didRemove = true;
                }

                if (_didRemove && m_items.Count > 0 && !m_items.Any(i => i.IsSelected))
                {
                    int _newIndex = Math.Min(m_items.Count, lastSelectedIndex);
                    this.SetSelection(m_items[_newIndex]);
                }
            }
        }

        public class HeaderItem : NotifyPropertyChanged
        {
            private bool m_isSelected;
            private bool m_isDragging;

            public HeaderItem (DockingContentAdapterModel adapter)
            {
                this.Adapter = adapter;
            }

            public DockingContentAdapterModel Adapter { get; }

            public bool IsSelected
            {
                get => m_isSelected;
                set => this.SetAndRaiseIfChanged(ref m_isSelected, value);
            }

            public bool IsDragging
            {
                get => m_isDragging;
                set => this.SetAndRaiseIfChanged(ref m_isDragging, value);
            }
        }
    }
}
