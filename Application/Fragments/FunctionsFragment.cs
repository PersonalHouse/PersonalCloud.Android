using Android.Runtime;
using Android.Views;

using AndroidX.Fragment.App;

using Binding;

namespace Unishare.Apps.DevolMobile.Fragments
{
    [Register("com.daoyehuo.UnishareLollipop.FunctionsFragment")]
    public class FunctionsFragment : Fragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Android.OS.Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_functions, container, false);
            return view;
        }
    }
}
