using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Comics {
    /// <summary>
    /// Helper methods that obtain an object in an ItemsControl at the current mouse cursor location. 
    /// Uses arguments to a MouseButtonEvent.
    /// Code taken from answer to StackOverflow question 7001607.
    /// </summary>
    class VisualHelper {
        public static T FindParentWithType<T>(DependencyObject obj) where T : DependencyObject {
            var current = obj;
            while (current != null && !(current is T)) {
                current = VisualTreeHelper.GetParent(current);
            }

            return current as T;
        }

        /// <summary>
        /// Pass arguments from your mouse event handler. Note: may return null.
        /// </summary>
        public static Comic ComicAtMouseButtonEvent(object sender, MouseButtonEventArgs e) {
            ItemsControl itemsControl;
            if (sender is ItemsControl control) {
                itemsControl = control;
            } else if (sender is Panel panel) {
                itemsControl = FindParentWithType<ItemsControl>(panel);
            } else {
                return null;
            }


            object item;
            if (!(itemsControl.ContainerFromElement(e.OriginalSource as Visual) is FrameworkElement sourceItemsContainer)) {
                item = itemsControl.DataContext;
            } else if (sourceItemsContainer == e.Source) {
                item = e.Source;
            } else {
                item = sourceItemsContainer.DataContext;
            }

            return item as Comic;
        }
    }
}
