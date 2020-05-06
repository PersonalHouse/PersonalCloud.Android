using System;
using System.Collections.Generic;

using Android.Views;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Items;

namespace Unishare.Apps.DevolMobile.Items
{
    internal class SimpleTitle : AbstractFlexibleItem
    {
        public string Title { get; }

        public SimpleTitle(string title)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        public override int LayoutRes => Resource.Layout.basic_cell;

        public override Java.Lang.Object CreateViewHolder(View view, FlexibleAdapter adapter) => new SimpleViewHolder(view, adapter);

        public override void BindViewHolder(FlexibleAdapter adapter, Java.Lang.Object viewHolder, int position, IList<Java.Lang.Object> payloads)
        {
            if (!(viewHolder is SimpleViewHolder holder)) return;
            holder.R.title_label.Text = Title;
        }

        public override bool Equals(Java.Lang.Object o) => false;
    }
}
