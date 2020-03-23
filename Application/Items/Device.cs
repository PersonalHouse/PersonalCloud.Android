using System;
using System.Collections.Generic;

using Android.Views;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Items;

using NSPersonalCloud.Interfaces.FileSystem;

namespace Unishare.Apps.DevolMobile.Items
{
    internal class Device : AbstractFlexibleItem
    {
        private readonly FileSystemEntry device;

        public Device(FileSystemEntry item)
        {
            device = item ?? throw new ArgumentNullException(nameof(item));
        }

        public override int LayoutRes => Resource.Layout.file_cell;

        public override Java.Lang.Object CreateViewHolder(View view, FlexibleAdapter adapter) => new FileEntryViewHolder(view, adapter);

        public override void BindViewHolder(FlexibleAdapter adapter, Java.Lang.Object viewHolder, int position, IList<Java.Lang.Object> payloads)
        {
            if (!(viewHolder is FileEntryViewHolder holder)) return;

            holder.R.file_cell_icon.SetImageResource(Resource.Drawable.device_phone);
            holder.R.file_cell_title.Text = device.Name;
            holder.R.file_cell_detail.Text = null;
            holder.R.file_cell_detail.Visibility = ViewStates.Gone;
            holder.R.file_cell_size.Text = null;
            holder.R.file_cell_size.Visibility = ViewStates.Gone;
        }

        public override void UnbindViewHolder(FlexibleAdapter adapter, Java.Lang.Object holder, int position)
        {
            base.UnbindViewHolder(adapter, holder, position);
        }

        public override bool Equals(Java.Lang.Object o) => false;

        
    }
}
