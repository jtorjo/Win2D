
using System.Diagnostics;

namespace common2.util.byte_buffer
{
    internal class byte_buffer_holder {
        private readonly byte[] bytes_;
        private readonly int offset_;
        private readonly int size_;

        private object lock_object_;
        internal bool used_ = false;

        public byte_buffer_holder(int size, object lock_object) {
            offset_ = 0;
            size_ = size;
            lock_object_ = lock_object;
            bytes_ = new byte[size_];
        }

        public byte_buffer_holder(byte[] bytes, int offset, int size, object lock_object) {
            bytes_ = bytes;
            offset_ = offset;
            size_ = size;
            lock_object_ = lock_object;
        }

        public byte[] bytes => bytes_;
        public int size => size_;

        public int offset => offset_;

        public bool try_use() {
            lock(lock_object_)
                if (!used_) {
                    used_ = true;
                    return true;
                }

            return false;
        }

        public bool in_use() {
            lock (lock_object_)
                return used_;
        }

        public void unuse() {
            lock (lock_object_) {
                Debug.Assert(used_);
                used_ = false;
            }
        }
    }
}
