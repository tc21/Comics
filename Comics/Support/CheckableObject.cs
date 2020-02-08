using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comics {
    public class Checkable<T> : IComparable, INotifyPropertyChanged where T : IComparable {
        private readonly T stored;

        public Checkable(T obj) {
            this.stored = obj;
        }

        public static implicit operator Checkable<T>(T obj) {
            return new Checkable<T>(obj);
        }

        public override string ToString() {
            return this.stored.ToString();
        }

        public override bool Equals(object obj) {
            if (obj is Checkable<T> checkable) {
                return this.stored.Equals(checkable.stored);
            }

            return this.stored.Equals(obj);
        }


        public int CompareTo(object obj) {
            if (obj is Checkable<T> checkable) {
                return this.stored.CompareTo(checkable.stored);
            }

            return this.stored.CompareTo(obj);
        }

        private bool isChecked = false;
        public bool IsChecked {
            get => this.isChecked;
            set {
                this.isChecked = value;
                this.NotifyPropertyChanged("IsChecked");
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
