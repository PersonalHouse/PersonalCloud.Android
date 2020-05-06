using System.Linq;

using Android.App;
using Android.Content.PM;

using AndroidX.AppCompat.App;
using AndroidX.Navigation;
using AndroidX.Navigation.Fragment;
using AndroidX.Navigation.UI;

using Google.Android.Material.BottomNavigation;

namespace Unishare.Apps.DevolMobile
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.MainActivity", Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity
    {
        private NavHostFragment HostFragment { get; set; }
        private NavController Controller { get; set; }
        private BottomNavigationView NavigationBar { get; set; }

        protected override void OnCreate(Android.OS.Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.main);
            HostFragment = (NavHostFragment) SupportFragmentManager.FindFragmentById(Resource.Id.main_fragment);
            Controller = HostFragment.NavController;
            NavigationBar = FindViewById<BottomNavigationView>(Resource.Id.main_navigation);
            NavigationUI.SetupWithNavController(NavigationBar, Controller);
            Controller.DestinationChanged += OnDestinationChange;
        }

        public override void OnBackPressed()
        {
            if (HostFragment.ChildFragmentManager?.Fragments?.FirstOrDefault() is IBackButtonHandler handler && handler.OnBack()) return;
            base.OnBackPressed();
        }

        private void OnDestinationChange(object sender, NavController.DestinationChangedEventArgs e)
        {
            InvalidateOptionsMenu();
            if (e.P1.Id != Resource.Id.finderFragment) SupportActionBar.Title = GetString(Resource.String.app_name);
        }

        public void SetTitle(string title)
        {
            if (SupportActionBar is null) return;
            SupportActionBar.Title = title;
        }
    }
}
