using System;
using System.Collections.Generic;
using System.IO;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Items;

using Humanizer;

using NSPersonalCloud.Interfaces.FileSystem;

namespace NSPersonalCloud.DevolMobile.Items
{
    internal class FileFolder : AbstractFlexibleItem
    {
        private readonly FileSystemEntry entry;

        public FileFolder(FileSystemEntry item)
        {
            entry = item ?? throw new ArgumentNullException(nameof(item));
        }

        public FileFolder(FileSystemInfo file)
        {
            entry = new FileSystemEntry(file);
        }

        public override int LayoutRes => Resource.Layout.file_cell;

        public override Java.Lang.Object CreateViewHolder(View view, FlexibleAdapter adapter) => new FileEntryViewHolder(view, adapter);

        public override void BindViewHolder(FlexibleAdapter adapter, Java.Lang.Object viewHolder, int position, IList<Java.Lang.Object> payloads)
        {
            if (!(viewHolder is FileEntryViewHolder holder)) return;

            holder.R.file_cell_title.Text = entry.Name;

            if (entry.IsDirectory)
            {
                holder.R.file_cell_icon.SetImageResource(Resource.Drawable.folder);
                holder.R.file_cell_detail.SetText(Resource.String.folder);
            }
            else
            {
                SetImage(holder.R.file_cell_icon, entry);
                holder.R.file_cell_detail.SetText(Resource.String.file);
            }

            if (entry.Size.HasValue)
            {
                holder.R.file_cell_size.Text = entry.Size.Value.Bytes().ToString("0.00");
                holder.R.file_cell_size.Visibility = ViewStates.Visible;
            }
            else
            {
                holder.R.file_cell_size.Text = null;
                holder.R.file_cell_size.Visibility = ViewStates.Gone;
            }
        }

        public override bool Equals(Java.Lang.Object o) => false;

        private static void SetImage(ImageView view, FileSystemEntry item)
        {
            var extension = Path.GetExtension(item.Name)?.TrimStart('.');
            if (string.IsNullOrEmpty(extension))
            {
                view.SetImageResource(Resource.Drawable.file_generic);
                return;
            }

            var mime = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
            if (mime?.StartsWith("audio/", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                view.SetImageResource(Resource.Drawable.file_audio);
                return;
            }
            if (mime?.StartsWith("video/", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                view.SetImageResource(Resource.Drawable.file_video);
                return;
            }
            if (mime?.StartsWith("text/", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                view.SetImageResource(Resource.Drawable.file_text);
                return;
            }
            if (mime?.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                view.SetImageResource(Resource.Drawable.file_image);
                return;
            }
            view.SetImageResource(Resource.Drawable.file_generic);
        }
    }
}
