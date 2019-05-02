using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comics.Support
{
    public class CustomSort<T> : IComparable
    {
        private readonly T stored;
        private readonly int? sort;

        public CustomSort(T t, int s)
        {
            this.stored = t;
            this.sort = s;
        }

        public CustomSort(T t)
        {
            this.stored = t;
            this.sort = null;
        }

        public override string ToString()
        {
            return stored.ToString();
        }

        public int CompareSort(CustomSort<T> cs)
        {
            if (this.sort == null || cs.sort == null)
            {
                return 0;
            }

            return ((int)this.sort).CompareTo((int)cs.sort);
        }

        public override bool Equals(object obj)
        {
            if (obj is CustomSort<T> custom)
            {
                return CompareSort(custom) == 0 && this.stored.Equals(custom.stored);
            }

            return this.stored.Equals(obj);
        }

        public int CompareTo(object obj)
        {
            if (obj is CustomSort<T> custom)
            {
                return CompareSort(custom);
            }

            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            return this.stored.GetHashCode();
        }
    }
}
