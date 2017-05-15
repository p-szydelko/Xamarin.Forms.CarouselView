using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Tizen;

[assembly: ExportRenderer(typeof(Xamarin.Forms.CarouselView), typeof(Xamarin.Forms.Platform.CarouselViewRenderer))]
namespace Xamarin.Forms.Platform
{
    public class CarouselViewRenderer : ViewRenderer<Xamarin.Forms.CarouselView, ElmSharp.Scroller>
    {
        /// <summary>
        /// The children views.
        /// </summary>
        protected List<View> children = new List<View>();

        /// <summary>
        /// The container for children native views.
        /// </summary>
        private ElmSharp.Box _box;

        /// <summary>
        /// The width of the single view.
        /// </summary>
        private int _pageWidth;

        /// <summary>
        /// The height of the single view.
        /// </summary>
        private int _pageHeight;

        /// <summary>
        /// The index of the currently displayed view.
        /// </summary>
        private int _pageIndex;

        IItemViewController ItemsController => Element as IItemViewController;
        ICarouselViewController Controller => Element as ICarouselViewController;

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.CarouselView> e)
        {
            if (Control == null)
            {
                _box = new ElmSharp.Box(Xamarin.Forms.Platform.Tizen.Forms.Context.MainWindow);
                _box.IsHorizontal = true;

                var _scroller = new ElmSharp.Scroller(Xamarin.Forms.Platform.Tizen.Forms.Context.MainWindow)
                {
                    HorizontalScrollBarVisiblePolicy = ElmSharp.ScrollBarVisiblePolicy.Invisible,
                    VerticalScrollBarVisiblePolicy = ElmSharp.ScrollBarVisiblePolicy.Invisible,
                    HorizontalPageScrollLimit = 1,
                };
                _scroller.SetContent(_box);
                _scroller.PageScrolled += OnScrolled;
                _scroller.Resized += OnResized;
                SetNativeControl(_scroller);
            }

            if (e.NewElement != null)
            {
                UpdateContent();
                Controller.CollectionChanged += OnCollectionChanged;
            }

            base.OnElementChanged(e);
        }

        protected override void Dispose(bool disposing)
        {
            Controller.CollectionChanged -= OnCollectionChanged;
            foreach (View child in children)
                Xamarin.Forms.Platform.Tizen.Platform.GetRenderer(child)?.Dispose();
            base.Dispose(disposing);
        }

        void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Something changed in the ItemsSource? rebuild everything
            UpdateContent();
        }

        /// <summary>
        /// Handles the size changes.
        /// </summary>
        void OnResized(object sender, EventArgs e)
        {
            var g = NativeView.Geometry;
            _pageWidth = g.Width;
            _pageHeight = g.Height;
            UpdateChildrenSize();
        }

        /// <summary>
        /// Handles the PageScrolled event of the _scroller.
        /// </summary>
        void OnScrolled(object sender, EventArgs e2)
        {
            int previousIndex = _pageIndex;
            _pageIndex = Control.HorizontalPageIndex;
            if (_pageIndex == previousIndex)
                return;
            Controller.SendSelectedPositionChanged(_pageIndex);
            Controller.SendSelectedItemChanged(Controller.GetItem(_pageIndex));
        }

        /// <summary>
        /// Updates the size of all the children.
        /// </summary>
        void UpdateChildrenSize()
        {
            ElmSharp.EvasObject nativeChild;
            Control.SetPageSize(_pageWidth, _pageHeight);
            foreach (View child in children)
            {

                nativeChild = Xamarin.Forms.Platform.Tizen.Platform.GetRenderer(child)?.NativeView;
                if (nativeChild != null)
                {
                    nativeChild.MinimumWidth = _pageWidth;
                    nativeChild.MinimumHeight = _pageHeight;
                }
            }
            Control.ScrollTo(new ElmSharp.Rect(_pageIndex * _pageWidth, 0, _pageWidth, _pageHeight), false);
        }

        /// <summary>
        /// Create renderers of the children views and arrange them.
        /// </summary>
        private void UpdateContent()
        {
            IVisualElementRenderer renderer;
            foreach (var child in children)
                Xamarin.Forms.Platform.Tizen.Platform.GetRenderer(child)?.Dispose();

            _box.UnPackAll();
            children.Clear();
            if (Element.ItemsSource != null)
            {
                foreach (var item in Element.ItemsSource)
                {
                    var view = CreateItemView(item);
                    children.Add(view);
                    renderer = Xamarin.Forms.Platform.Tizen.Platform.GetOrCreateRenderer(view);
                    _box.PackEnd(renderer?.NativeView);
                }
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == Xamarin.Forms.CarouselView.PositionProperty.PropertyName && !Controller.IgnorePositionUpdates)
            {
                if (_pageIndex != Element.Position)
                {
                    _pageIndex = Element.Position;
                    Control.ScrollTo(new ElmSharp.Rect(_pageIndex * _pageWidth, 0, _pageWidth, _pageHeight), true);
                    (Element as IElementController).SetValueFromRenderer(Xamarin.Forms.CarouselView.ItemProperty, ItemsController.GetItem(_pageIndex));
                }
            }
            else if (e.PropertyName == Xamarin.Forms.CarouselView.ItemProperty.PropertyName)
            {
                int previousIndex = _pageIndex;
                _pageIndex = 0;
                object selectedItem = Element.Item;
                if (selectedItem == null && selectedItem == ItemsController.GetItem(Element.Position))
                {
                    // selectedItem is not usable, or Element.Position already contains correct value
                    _pageIndex = Element.Position;
                }
                else
                {
                    var index = 0;
                    // try to find the position of the new selectedItem
                    foreach (object item in Element.ItemsSource)
                    {
                        if (selectedItem.Equals(item))
                        {
                            _pageIndex = index;
                            break;
                        }
                        ++index;
                    }
                }

                (Element as IElementController).SetValueFromRenderer(Xamarin.Forms.CarouselView.PositionProperty, _pageIndex);
                if (_pageIndex != previousIndex)
                    Control.ScrollTo(new ElmSharp.Rect(_pageIndex * _pageWidth, 0, _pageWidth, _pageHeight), true);
            }
            else if (e.PropertyName == Xamarin.Forms.CarouselView.ItemsSourceProperty.PropertyName)
            {
                // recreate all of the children, and update their size if it makes sense
                UpdateContent();
                if (_pageHeight > 0 && _pageWidth > 0)
                    UpdateChildrenSize();
            }
        }

        /// <summary>
        /// Creates the item's view.
        /// </summary>
        /// <returns>The View bound to the given item.</returns>
        /// <param name="item">Item of the ItemsSource.</param>
        View CreateItemView(object item)
        {
            View view = ItemsController.CreateView(ItemsController.GetItemType(item));
            view.Parent = Element;
            ItemsController.BindView(view, item);
            view.Layout(new Rectangle(0, 0, Element.Width, Element.Height));
            return view;
        }
    }
}
