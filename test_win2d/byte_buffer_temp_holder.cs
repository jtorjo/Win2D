using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace common2.util.byte_buffer
{
    public class byte_buffer_temp_holder : IDisposable{
        private byte_buffer_holder holder_;
        private int wants_size_;

        private byte_buffer_temp_holder() {
            // can't create it yourself
        }
        internal byte_buffer_temp_holder(byte_buffer_holder holder, int wants_size) {
            holder_ = holder;
            wants_size_ = wants_size;
            Debug.Assert(wants_size <= holder.size && holder.in_use());
        }

        public byte[] bytes => holder_.bytes;
        public int size => wants_size_;

        // helper
        public void read(FileStream stream) {
            stream.Read(holder_.bytes, 0, size);
        }

        public async Task write_from_stream_async(InMemoryRandomAccessStream stream) {
            var buffer = holder_.bytes.AsBuffer(0, wants_size_);
            await stream.WriteAsync(buffer);
        }

        public void Dispose() {
            holder_?.unuse();
            holder_ = null;
        }
    }
}
