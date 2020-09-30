using System;
using System.Collections.Generic;

namespace Aws.Crt
{
    public class WeakReferenceVendor<T> where T : class
    {
        private ulong NextId = 0;

        private Dictionary<ulong, WeakReference> references = new Dictionary<ulong, WeakReference>();

        public WeakReferenceVendor()
        {
        }

        public ulong AcquireWeakReference(T thing)
        {
            ulong id = 0;

            lock(this) {
                id = NextId++;
                references.Add(id, new WeakReference(thing));
            }

            return id;
        }

        public T ReleaseWeakReference(ulong id)
        {
            WeakReference reference = null;
            lock(this) {
                references.TryGetValue(id, out reference);
                references.Remove(id);
            }

            T thing = default(T);
            if (reference != null) {
                thing = (T) reference.Target;
            }

            return thing;
        }
    }

    public class StrongReferenceVendor<T> where T : class
    {
        private ulong NextId = 0;

        private Dictionary<ulong, T> references = new Dictionary<ulong, T>();

        public StrongReferenceVendor()
        {
        }

        public ulong AcquireStrongReference(T thing)
        {
            ulong id = 0;

            lock(this) {
                id = NextId++;
                references.Add(id, thing);
            }

            return id;
        }

        public T ReleaseStrongReference(ulong id)
        {
            T thing = default(T);
            lock(this) {
                references.TryGetValue(id, out thing);
                references.Remove(id);
            }

            return thing;
        }
    }
}