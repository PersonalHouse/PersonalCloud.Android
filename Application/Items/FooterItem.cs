using System.Collections.Generic;
using Android.Views;
using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Items;

namespace NSPersonalCloud.DevolMobile.Items
{
    internal class FooterItem : AbstractFlexibleItem
    {
        public override int LayoutRes => Resource.Layout.footer;

        public override Java.Lang.Object CreateViewHolder(View view, FlexibleAdapter adapter) => new FooterViewHolder(view, adapter);

        public override void BindViewHolder(FlexibleAdapter adapter, Java.Lang.Object viewHolder, int position, IList<Java.Lang.Object> payloads)
        {
            if (!(viewHolder is FooterViewHolder holder)) return;
        }

        public override bool Equals(Java.Lang.Object obj)
        {
            if (obj is FooterItem jobj)
            {
                return jobj.LayoutRes == LayoutRes;
            }
            return false;
        }
    }
}
