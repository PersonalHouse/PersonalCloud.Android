using System;

using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using AndroidX.AppCompat.App;
using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.ViewHolders;

namespace NSPersonalCloud.DevolMobile.Items
{
    internal class FooterViewHolder : FlexibleViewHolder
    {
        public FooterViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        internal footer R { get; private set; }
        internal Context Context { get; private set; }

        public FooterViewHolder(View view, FlexibleAdapter adapter) : base(view, adapter)
        {
            R = new footer(view);
            Context = view.Context;
            R.add_more_device.Click += Add_more_device_Click;
        }

        private void Add_more_device_Click(object sender, EventArgs e)
        {
            // create a WebView
            WebView webView = new WebView(Context);

            webView.SetBackgroundColor(Color.Red);

            // populate the WebView with an HTML string
            webView.LoadUrl("file:///android_asset/NewDevicePromt.html");

            webView.SetWebViewClient(new MyWebViewClient(Context));

            // create an AlertDialog.Builder
            AlertDialog.Builder builder = new AlertDialog.Builder(Context);

            // set the WebView as the AlertDialog.Builder’s view
            //builder.SetTitle("Add More Device");
            builder.SetView(webView);

            builder.Show();
        }

        public class MyWebViewClient : WebViewClient
        {
            private readonly Context _Context;

            public MyWebViewClient(Context context)
            {
                _Context = context;
            }

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(url));

                if (intent.ResolveActivity(_Context.PackageManager) != null)
                {
                    AlertDialog.Builder builder = new AlertDialog.Builder(_Context, Resource.Style.AlertDialogTheme);
                    builder.SetTitle("Confirm");
                    builder.SetMessage($"Open '{url}' in browser?");

                    builder.SetPositiveButton("OK", (o, e) => _Context.StartActivity(intent));
                    builder.SetNegativeButton("Cancel", (o, e) => { });

                    AlertDialog dialog = builder.Create();
                    dialog.Show();
                }
                else
                {
                    // ShowAlert(_Context.GetString(Resource.String.error_web_browser), null);
                }
                _Context.StartActivity(intent);
                return true;
            }
        }
    }
}
