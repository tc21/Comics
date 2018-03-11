using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace UI {
    // class from: https://github.com/samueldjack/VirtualCollection/blob/master/VirtualCollection/VirtualCollection/VirtualizingWrapPanel.cs
    // (Original Author: Samuel Jack)
    // MakeVisible() method from: http://www.switchonthecode.com/tutorials/wpf-tutorial-implementing-iscrollinfo
    // Further modified by Tianyi Cao
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo {
        private const double ScrollLineAmount = 16.0;

        private Size _extentSize;
        private Size _viewportSize;
        private Point _offset;
        private ItemsControl _itemsControl;
        private readonly Dictionary<UIElement, Rect> _childLayouts = new Dictionary<UIElement, Rect>();

        private static readonly DependencyProperty VirtualItemIndexProperty =
            DependencyProperty.RegisterAttached("VirtualItemIndex", typeof(int), typeof(VirtualizingWrapPanel), new PropertyMetadata(-1));
        private IRecyclingItemContainerGenerator _itemsGenerator;

        private bool _isInMeasure;

        private static int GetVirtualItemIndex(DependencyObject obj) {
            return (int)obj.GetValue(VirtualItemIndexProperty);
        }

        private static void SetVirtualItemIndex(DependencyObject obj, int value) {
            obj.SetValue(VirtualItemIndexProperty, value);
        }

        // This is the change I made to the panel, to enable the process of dynamically changing items' size
        // based on window size. An item's size is calculated by other classes and then retrieved here. Since
        // the calculation of item size requires variables that are not always available, it is "cached" in 
        // this variable. The fallback of calculating using _viewportSize.Width is a pretty good estimate, but
        // may not be totally accurate. 
        private Size? lastItemSize;

        public Size ItemSize => this.lastItemSize ?? Comics.Defaults.DynamicSize(this._viewportSize.Width);

        private Size GetItemSize(Size availableSize) {
            this.lastItemSize = Comics.Defaults.DynamicSize(availableSize.Width);
            return (Size)this.lastItemSize;
        }

        public VirtualizingWrapPanel() {
            if (!DesignerProperties.GetIsInDesignMode(this)) {
                this.Dispatcher.BeginInvoke((Action)this.Initialize);
            }
        }

        private void Initialize() {
            this._itemsControl = ItemsControl.GetItemsOwner(this);
            this._itemsGenerator = (IRecyclingItemContainerGenerator)this.ItemContainerGenerator;

            InvalidateMeasure();
        }

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args) {
            base.OnItemsChanged(sender, args);

            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize) {
            if (this._itemsControl == null) {
                return availableSize;
            }

            this._isInMeasure = true;
            this._childLayouts.Clear();

            var itemSize = GetItemSize(availableSize);

            var extentInfo = GetExtentInfo(availableSize, itemSize.Height);

            EnsureScrollOffsetIsWithinConstrains(extentInfo);

            var layoutInfo = GetLayoutInfo(availableSize, itemSize.Height, extentInfo);

            RecycleItems(layoutInfo);

            // Determine where the first item is in relation to previously realized items
            var generatorStartPosition = this._itemsGenerator.GeneratorPositionFromIndex(layoutInfo.FirstRealizedItemIndex);

            var visualIndex = 0;

            var currentX = layoutInfo.FirstRealizedItemLeft;
            var currentY = layoutInfo.FirstRealizedLineTop;

            using (this._itemsGenerator.StartAt(generatorStartPosition, GeneratorDirection.Forward, true)) {
                for (var itemIndex = layoutInfo.FirstRealizedItemIndex; itemIndex <= layoutInfo.LastRealizedItemIndex; itemIndex++, visualIndex++) {

                    var child = (UIElement)this._itemsGenerator.GenerateNext(out var newlyRealized);
                    SetVirtualItemIndex(child, itemIndex);

                    if (newlyRealized) {
                        InsertInternalChild(visualIndex, child);
                    } else {
                        // check if item needs to be moved into a new position in the Children collection
                        if (visualIndex < this.Children.Count) {
                            if (this.Children[visualIndex] != child) {
                                var childCurrentIndex = this.Children.IndexOf(child);

                                if (childCurrentIndex >= 0) {
                                    RemoveInternalChildRange(childCurrentIndex, 1);
                                }

                                InsertInternalChild(visualIndex, child);
                            }
                        } else {
                            // we know that the child can't already be in the children collection
                            // because we've been inserting children in correct visualIndex order,
                            // and this child has a visualIndex greater than the Children.Count
                            AddInternalChild(child);
                        }
                    }

                    // only prepare the item once it has been added to the visual tree
                    this._itemsGenerator.PrepareItemContainer(child);

                    child.Measure(itemSize);

                    this._childLayouts.Add(child, new Rect(currentX, currentY, itemSize.Width, itemSize.Height));

                    if (currentX + itemSize.Width * 2 >= availableSize.Width) {
                        // wrap to a new line
                        currentY += itemSize.Height;
                        currentX = 0;
                    } else {
                        currentX += itemSize.Width;
                    }
                }
            }

            RemoveRedundantChildren();
            UpdateScrollInfo(availableSize, extentInfo);

            var desiredSize = new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
                                       double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);

            this._isInMeasure = false;

            return desiredSize;
        }

        private void EnsureScrollOffsetIsWithinConstrains(ExtentInfo extentInfo) {
            this._offset.Y = Clamp(this._offset.Y, 0, extentInfo.MaxVerticalOffset);
        }

        private void RecycleItems(ItemLayoutInfo layoutInfo) {
            foreach (UIElement child in this.Children) {
                var virtualItemIndex = GetVirtualItemIndex(child);

                if (virtualItemIndex < layoutInfo.FirstRealizedItemIndex || virtualItemIndex > layoutInfo.LastRealizedItemIndex) {
                    var generatorPosition = this._itemsGenerator.GeneratorPositionFromIndex(virtualItemIndex);
                    if (generatorPosition.Index >= 0) {
                        this._itemsGenerator.Recycle(generatorPosition, 1);
                    }
                }

                SetVirtualItemIndex(child, -1);
            }
        }

        protected override Size ArrangeOverride(Size finalSize) {
            foreach (UIElement child in this.Children) {
                child.Arrange(this._childLayouts[child]);
            }

            return finalSize;
        }

        private void UpdateScrollInfo(Size availableSize, ExtentInfo extentInfo) {
            this._viewportSize = availableSize;
            this._extentSize = new Size(availableSize.Width, extentInfo.ExtentHeight);

            InvalidateScrollInfo();
        }

        private void RemoveRedundantChildren() {
            // iterate backwards through the child collection because we're going to be
            // removing items from it
            for (var i = this.Children.Count - 1; i >= 0; i--) {
                var child = this.Children[i];

                // if the virtual item index is -1, this indicates
                // it is a recycled item that hasn't been reused this time round
                if (GetVirtualItemIndex(child) == -1) {
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        private ItemLayoutInfo GetLayoutInfo(Size availableSize, double itemHeight, ExtentInfo extentInfo) {
            if (this._itemsControl == null) {
                return new ItemLayoutInfo();
            }

            // we need to ensure that there is one realized item prior to the first visible item, and one after the last visible item,
            // so that keyboard navigation works properly. For example, when focus is on the first visible item, and the user
            // navigates up, the ListBox selects the previous item, and the scrolls that into view - and this triggers the loading of the rest of the items 
            // in that row

            var itemSize = GetItemSize(availableSize);
            var firstVisibleLine = (int)Math.Floor(this.VerticalOffset / itemHeight);

            var firstRealizedIndex = Math.Max(extentInfo.ItemsPerLine * firstVisibleLine - 1, 0);
            var firstRealizedItemLeft = firstRealizedIndex % extentInfo.ItemsPerLine * itemSize.Width - this.HorizontalOffset;
            var firstRealizedItemTop = (firstRealizedIndex / extentInfo.ItemsPerLine) * itemHeight - this.VerticalOffset;

            var firstCompleteLineTop = (firstVisibleLine == 0 ? firstRealizedItemTop : firstRealizedItemTop + itemSize.Height);
            var completeRealizedLines = (int)Math.Ceiling((availableSize.Height - firstCompleteLineTop) / itemHeight);

            var lastRealizedIndex = Math.Min(firstRealizedIndex + completeRealizedLines * extentInfo.ItemsPerLine + 2, this._itemsControl.Items.Count - 1);

            return new ItemLayoutInfo {
                FirstRealizedItemIndex = firstRealizedIndex,
                FirstRealizedItemLeft = firstRealizedItemLeft,
                FirstRealizedLineTop = firstRealizedItemTop,
                LastRealizedItemIndex = lastRealizedIndex,
            };
        }

        private ExtentInfo GetExtentInfo(Size viewPortSize, double itemHeight) {
            if (this._itemsControl == null) {
                return new ExtentInfo();
            }

            var itemsPerLine = Math.Max((int)Math.Floor(viewPortSize.Width / this.ItemSize.Width), 1);
            var totalLines = (int)Math.Ceiling((double)this._itemsControl.Items.Count / itemsPerLine);
            var extentHeight = Math.Max(totalLines * this.ItemSize.Height, viewPortSize.Height);

            return new ExtentInfo {
                ItemsPerLine = itemsPerLine,
                TotalLines = totalLines,
                ExtentHeight = extentHeight,
                MaxVerticalOffset = extentHeight - viewPortSize.Height,
            };
        }

        public void LineUp() {
            SetVerticalOffset(this.VerticalOffset - ScrollLineAmount);
        }

        public void LineDown() {
            SetVerticalOffset(this.VerticalOffset + ScrollLineAmount);
        }

        public void LineLeft() {
            SetHorizontalOffset(this.HorizontalOffset + ScrollLineAmount);
        }

        public void LineRight() {
            SetHorizontalOffset(this.HorizontalOffset - ScrollLineAmount);
        }

        public void PageUp() {
            SetVerticalOffset(this.VerticalOffset - this.ViewportHeight);
        }

        public void PageDown() {
            SetVerticalOffset(this.VerticalOffset + this.ViewportHeight);
        }

        public void PageLeft() {
            SetHorizontalOffset(this.HorizontalOffset + this.ItemSize.Width);
        }

        public void PageRight() {
            SetHorizontalOffset(this.HorizontalOffset - this.ItemSize.Width);
        }

        public void MouseWheelUp() {
            SetVerticalOffset(this.VerticalOffset - ScrollLineAmount * SystemParameters.WheelScrollLines);
        }

        public void MouseWheelDown() {
            SetVerticalOffset(this.VerticalOffset + ScrollLineAmount * SystemParameters.WheelScrollLines);
        }

        public void MouseWheelLeft() {
            SetHorizontalOffset(this.HorizontalOffset - ScrollLineAmount * SystemParameters.WheelScrollLines);
        }

        public void MouseWheelRight() {
            SetHorizontalOffset(this.HorizontalOffset + ScrollLineAmount * SystemParameters.WheelScrollLines);
        }

        public void SetHorizontalOffset(double offset) {
            if (this._isInMeasure) {
                return;
            }

            offset = Clamp(offset, 0, this.ExtentWidth - this.ViewportWidth);
            this._offset = new Point(offset, this._offset.Y);

            InvalidateScrollInfo();
            InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset) {
            if (this._isInMeasure) {
                return;
            }

            offset = Clamp(offset, 0, this.ExtentHeight - this.ViewportHeight);
            this._offset = new Point(this._offset.X, offset);

            InvalidateScrollInfo();
            InvalidateMeasure();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle) {
            if (rectangle.IsEmpty ||
                visual == null ||
                visual == this ||
                !IsAncestorOf(visual)) {
                return Rect.Empty;
            }

            rectangle = visual.TransformToAncestor(this).TransformBounds(rectangle);

            var viewRect = new Rect(this.HorizontalOffset, this.VerticalOffset, this.ViewportWidth, this.ViewportHeight);
            rectangle.X += viewRect.X;
            rectangle.Y += viewRect.Y;

            viewRect.X = CalculateNewScrollOffset(viewRect.Left, viewRect.Right, rectangle.Left, rectangle.Right);
            viewRect.Y = CalculateNewScrollOffset(viewRect.Top, viewRect.Bottom, rectangle.Top, rectangle.Bottom);

            SetHorizontalOffset(viewRect.X);
            SetVerticalOffset(viewRect.Y);
            rectangle.Intersect(viewRect);

            rectangle.X -= viewRect.X;
            rectangle.Y -= viewRect.Y;

            return rectangle;
        }

        private static double CalculateNewScrollOffset(double topView, double bottomView, double topChild, double bottomChild) {
            var offBottom = topChild < topView && bottomChild < bottomView;
            var offTop = bottomChild > bottomView && topChild > topView;
            var tooLarge = (bottomChild - topChild) > (bottomView - topView);

            if (!offBottom && !offTop) {
                return topView;
            }

            if ((offBottom && !tooLarge) || (offTop && tooLarge)) {
                return topChild;
            }

            return bottomChild - (bottomView - topView);
        }


        public ItemLayoutInfo GetVisibleItemsRange() {
            return GetLayoutInfo(this._viewportSize, this.ItemSize.Height, GetExtentInfo(this._viewportSize, this.ItemSize.Height));
        }

        public bool CanVerticallyScroll {
            get;
            set;
        }

        public bool CanHorizontallyScroll {
            get;
            set;
        }

        public double ExtentWidth => this._extentSize.Width;

        public double ExtentHeight => this._extentSize.Height;

        public double ViewportWidth => this._viewportSize.Width;

        public double ViewportHeight => this._viewportSize.Height;

        public double HorizontalOffset => this._offset.X;

        public double VerticalOffset => this._offset.Y;

        public ScrollViewer ScrollOwner {
            get;
            set;
        }

        private void InvalidateScrollInfo() {
            if (this.ScrollOwner != null) {
                this.ScrollOwner.InvalidateScrollInfo();
            }
        }

        private static void HandleItemDimensionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var wrapPanel = (d as VirtualizingWrapPanel);

            if (wrapPanel != null) {
                wrapPanel.InvalidateMeasure();
            }
        }

        private double Clamp(double value, double min, double max) {
            return Math.Min(Math.Max(value, min), max);
        }

        internal class ExtentInfo {
            public int ItemsPerLine;
            public int TotalLines;
            public double ExtentHeight;
            public double MaxVerticalOffset;
        }

        public class ItemLayoutInfo {
            public int FirstRealizedItemIndex;
            public double FirstRealizedLineTop;
            public double FirstRealizedItemLeft;
            public int LastRealizedItemIndex;
        }
    }
}