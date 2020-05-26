using System;

using Android.Content;
using Android.Runtime;
using Android.Views;

using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.ViewHolders;

namespace NSPersonalCloud.DevolMobile.Items
{
    internal class SimpleViewHolder : FlexibleViewHolder
    {
        public SimpleViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        internal basic_cell R { get; private set; }
        internal Context Context { get; private set; }

        public SimpleViewHolder(View view, FlexibleAdapter adapter) : base(view, adapter)
        {
            R = new basic_cell(view);
            Context = view.Context;
        }
    }
}
