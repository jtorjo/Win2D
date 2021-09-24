using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace common2.util.byte_buffer
{
    public class byte_buffer_holder_array {

        private object lock_object_ = new object();
        private List<byte_buffer_holder> buffers_ = new List<byte_buffer_holder>();
        private int biggest_size_ = 0;

        private byte_buffer_holder_array() {

        }

        public static byte_buffer_holder_array inst { get; } = new byte_buffer_holder_array();

        public byte_buffer_temp_holder get_temp_holder(int size) {
            // if size > biggest_size_ , allocate 1.5 * size
            lock (lock_object_) {
                if (size > biggest_size_) {
                    // the idea - the buffers were too small, I'll create new ones
                    buffers_.Clear();
                    biggest_size_ = size + size / 2;
                }

                for (int i = 0; i < buffers_.Count; ++i)
                    if (!buffers_[i].used_) {
                        buffers_[i].used_ = true;
                        return new byte_buffer_temp_holder(buffers_[i], size);
                    }

                // create now
                var new_buffer = new byte_buffer_holder(biggest_size_, lock_object_);
                buffers_.Add(new_buffer);
                new_buffer.used_ = true;
                return new byte_buffer_temp_holder(new_buffer, size);
            }
        }

        // when we have our own buffer, that does not need deletion
        public byte_buffer_temp_holder get_external_temp_holder(byte[] bytes, int offset, int size) {
            var buffer = new byte_buffer_holder(bytes, offset, size, lock_object_);
            buffer.used_ = true;
            return new byte_buffer_temp_holder(buffer, size);
        }
    }
}
