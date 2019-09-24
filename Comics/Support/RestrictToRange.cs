using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comics.Support {
    public class Helper {
        public class NullableWrapper<T> {
            public readonly T Stored;

            private NullableWrapper(T item) {
                this.Stored = item;
            }

            public static implicit operator NullableWrapper<T>(T item) => new NullableWrapper<T>(item);
        }
        public static T RestrictToRange<T>(T value, NullableWrapper<T> lower, NullableWrapper<T> higher) where T : IComparable<T> {
            if (lower != null && value.CompareTo(lower.Stored) < 0) {
                return lower.Stored;
            }
            if (higher != null && value.CompareTo(higher.Stored) > 0) {
                return higher.Stored;
            }
            return value;
        }
    }
}
