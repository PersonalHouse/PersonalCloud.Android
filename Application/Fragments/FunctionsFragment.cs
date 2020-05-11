using System;
using System.Collections.Generic;
using System.Linq;

using Android.Content;
using Android.Runtime;
using Android.Views;

using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;

using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Common;
using DavideSteduto.FlexibleAdapter.Helpers;

using NSPersonalCloud.Interfaces.Apps;

using Unishare.Apps.DevolMobile.Items;

using static DavideSteduto.FlexibleAdapter.FlexibleAdapter;

namespace Unishare.Apps.DevolMobile.Fragments
{
    [Register("com.daoyehuo.UnishareLollipop.FunctionsFragment")]
    public class FunctionsFragment : Fragment, IOnItemClickListener
    {
        internal fragment_functions R { get; private set; }

        private FlexibleAdapter adapter;
        private RecyclerView.LayoutManager layoutManager;

        private List<AppLauncher> items;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Android.OS.Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_functions, container, false);
            R = new fragment_functions(view);

            adapter = new FlexibleAdapter(null, this);
            adapter.SetAnimationOnForwardScrolling(true);
            layoutManager = new SmoothScrollLinearLayoutManager(Context);
            R.list_recycler.SetLayoutManager(layoutManager);
            R.list_recycler.SetAdapter(adapter);
            R.list_recycler.AddItemDecoration(new FlexibleItemDecoration(Context).WithDefaultDivider());
            R.list_reloader.SetColorSchemeResources(Resource.Color.colorAccent);
            R.list_reloader.Refresh += RefreshFunctions;
            EmptyViewHelper.Create(adapter, R.list_empty);

            return view;
        }

        public override void OnStart()
        {
            base.OnStart();
            RefreshFunctions(this, EventArgs.Empty);
        }

        public bool OnItemClick(View view, int position)
        {
            var app = items[position];
            var url = Globals.CloudManager.PersonalClouds[0].GetWebAppUri(app);
            var intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(url.AbsoluteUri));
            if (intent.ResolveActivity(Context.PackageManager) != null) StartActivity(intent);
            else
            {
                Activity.ShowAlert(GetString(Resource.String.error_web_browser), null);
            }

            return false;
        }

        private void RefreshFunctions(object sender, EventArgs e)
        {
            if (!R.list_reloader.Refreshing) R.list_reloader.Refreshing = true;

            items = Globals.CloudManager.PersonalClouds[0].Apps;
            var models = items.Select(x => new WebApp(x)).ToList();
            adapter.UpdateDataSet(models, true);
            if (R.list_reloader.Refreshing) R.list_reloader.Refreshing = false;
        }


    }
}
