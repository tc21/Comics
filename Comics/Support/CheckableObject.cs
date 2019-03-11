using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comics {
    public class Checkable<T> : INotifyPropertyChanged {
        private readonly T stored;

        public Checkable(T obj) {
            stored = obj;
        }

        public static implicit operator Checkable<T>(T obj) {
            return new Checkable<T>(obj);
        }

        public override string ToString() {
            return stored.ToString();
        }

        public override bool Equals(object obj) {
            if (obj is Checkable<T>) {
                return this.stored.Equals(((Checkable<T>)obj).stored);
            }

            return stored.Equals(obj);
        }

        private bool isChecked = false;
        public bool IsChecked {
            get => isChecked;
            set {
                isChecked = value;
                NotifyPropertyChanged("IsChecked");
            }
        }

        public override int GetHashCode() {
            return this.stored.GetHashCode();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
