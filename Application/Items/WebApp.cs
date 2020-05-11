using System.Collections.Generic;

using Android.Views;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Items;

using NSPersonalCloud.Interfaces.Apps;

namespace Unishare.Apps.DevolMobile.Items
{
    internal class WebApp : AbstractFlexibleItem
    {
        public string Title { get; }

        public WebApp(AppLauncher app)
        {
            Title = app?.Name ?? string.Empty;
        }

        public override int LayoutRes => Resource.Layout.file_cell;

        public override Java.Lang.Object CreateViewHolder(View view, FlexibleAdapter adapter) => new FileEntryViewHolder(view, adapter);

        public override void BindViewHolder(FlexibleAdapter adapter, Java.Lang.Object viewHolder, int position, IList<Java.Lang.Object> payloads)
        {
            if (!(viewHolder is FileEntryViewHolder holder)) return;
            holder.R.file_cell_title.Text = Title;
            holder.R.file_cell_icon.SetImageDrawable(UIExtensions.GetTintedDrawable(holder.Context, Resource.Drawable.extension, Resource.Color.colorAccent));
            holder.R.file_cell_detail.SetText(Resource.String.web_app);
            holder.R.file_cell_size.Visibility = ViewStates.Gone;
        }

        public override bool Equals(Java.Lang.Object o) => false;
    }
}
