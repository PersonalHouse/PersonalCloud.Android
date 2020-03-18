using Android.OS;
using Android.Runtime;
using Android.Views;

using AndroidX.AppCompat.App;

namespace Unishare.Apps.DevolMobile
{
    [Register("com.daoyehuo.UnishareLollipop.NavigableActivity")]
    public abstract class NavigableActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SupportActionBar.EnableNavigation();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                Finish();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }
    }
}
