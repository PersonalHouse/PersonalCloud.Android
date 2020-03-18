using System;

using Android.Runtime;
using Android.Views;

using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.ViewHolders;

namespace Unishare.Apps.DevolMobile.Items
{
    internal class FileEntryViewHolder : FlexibleViewHolder
    {
        public FileEntryViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        internal file_cell R { get; private set; }

        public FileEntryViewHolder(View view, FlexibleAdapter adapter) : base(view, adapter)
        {
            R = new file_cell(view);
        }

        public FileEntryViewHolder(View view, FlexibleAdapter adapter, bool stickyHeader) : base(view, adapter, stickyHeader)
        {
            R = new file_cell(view);
        }
    }
}
