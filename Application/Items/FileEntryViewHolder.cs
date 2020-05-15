using System;

using Android.Content;
using Android.Runtime;
using Android.Views;

using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.ViewHolders;

namespace NSPersonalCloud.DevolMobile.Items
{
    internal class FileEntryViewHolder : FlexibleViewHolder
    {
        public FileEntryViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        internal file_cell R { get; private set; }
        internal Context Context { get; private set; }

        public FileEntryViewHolder(View view, FlexibleAdapter adapter) : base(view, adapter)
        {
            R = new file_cell(view);
            Context = view.Context;
        }
    }
}
