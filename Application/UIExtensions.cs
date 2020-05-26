using System;
using System.Globalization;

using Android.Content;

using AndroidX.Fragment.App;

namespace NSPersonalCloud.DevolMobile
{
    internal static class UIExtensions
    {
        #region Application

        public static string GetPackageVersion(this Context application)
        {
            try
            {
                var package = application.PackageManager.GetPackageInfo(application.PackageName, 0);
                if (!string.IsNullOrEmpty(package.VersionName)) return $"{package.VersionName} ({package.VersionCode.ToString(CultureInfo.InvariantCulture)})";
                return package.VersionCode.ToString(CultureInfo.InvariantCulture);
            }
            catch
            {
                return "v0";
            }
        }

        #endregion

        #region Fragment

         public static void StartActivity(this Fragment fragment, Type activity)
        {
            var intent = new Intent(fragment.Context, activity);
            fragment.StartActivity(intent);
        }

        public static void StartActivityForResult(this Fragment fragment, Type activity, int requestCode)
        {
            var intent = new Intent(fragment.Context, activity);
            fragment.StartActivityForResult(intent, requestCode);
        }

        #endregion

        #region Activity

        public static void EnableNavigation(this AndroidX.AppCompat.App.ActionBar actionBar)
        {
            actionBar.SetHomeButtonEnabled(true);
            actionBar.SetDisplayHomeAsUpEnabled(true);
        }

        #endregion Activity

        #region Alert Dialog

        public static void ShowAlert(this Android.App.Activity activity, string title, string message,
                                     Action onDismiss = null)
        {
            new AndroidX.AppCompat.App.AlertDialog.Builder(activity)
                .SetIcon(Resource.Mipmap.ic_launcher_round)
                .SetCancelable(false)
                .SetTitle(title)
                .SetMessage(message)
                .SetPositiveButton("好", (o, e) => onDismiss?.Invoke())
                .Show();
        }

        public static void ShowAlert(this Android.App.Activity activity, string title, string message,
                                     string positiveAction, Action onPositive,
                                     string negativeAction, Action onNegative,
                                     bool negativeActionIsNeutral = true)
        {
            var dialog = new AndroidX.AppCompat.App.AlertDialog.Builder(activity)
                .SetIcon(Resource.Mipmap.ic_launcher_round)
                .SetCancelable(false)
                .SetTitle(title)
                .SetMessage(message)
                .SetPositiveButton(positiveAction, (o, e) => onPositive?.Invoke());

            if (negativeActionIsNeutral) dialog.SetNeutralButton(negativeAction, (o, e) => onNegative?.Invoke());
            else dialog.SetNegativeButton(negativeAction, (o, e) => onNegative?.Invoke());

            dialog.Show();
        }

        public static void ShowFatalAlert(this Android.App.Activity activity, string title, string message)
        {
            new AndroidX.AppCompat.App.AlertDialog.Builder(activity, Resource.Style.AlertDialogTheme)
                .SetIcon(Resource.Mipmap.ic_launcher_round)
                .SetCancelable(false)
                .SetTitle(title)
                .SetMessage(message)
                .SetPositiveButton("退出", (o, e) => activity.FinishAndRemoveTask())
                .Show();
        }

        public static void ShowEditorAlert(this Android.App.Activity activity, string title, string placeholder, string text,
                                           string positiveAction, Action<string> onConfirmation,
                                           string neutralAction, Action onDismiss)
        {
            var view = activity.LayoutInflater.Inflate(Resource.Layout.alert_editor, null);
            var editor = view.FindViewById<Android.Widget.EditText>(Resource.Id.editor);
            editor.Hint = placeholder;
            editor.Text = text;
            editor.SetSelection(0, editor.Text.Length);
            var dialog = new AndroidX.AppCompat.App.AlertDialog.Builder(activity).SetCancelable(false)
                        .SetIcon(Resource.Mipmap.ic_launcher)
                        .SetTitle(title)
                        .SetView(view)
                        .SetPositiveButton(positiveAction, (o, e) => onConfirmation?.Invoke(editor.Text))
                        .SetNeutralButton(neutralAction, (o, e) => onDismiss?.Invoke())
                        .Create();
            dialog.Window.SetSoftInputMode(Android.Views.SoftInput.StateVisible);
            dialog.Show();
            editor.RequestFocus();
        }

        #endregion Alert Dialog

        #region Vector Drawable

        public static Android.Graphics.Color ColorFromResource(Android.Content.Context context, int resourceId)
        {
            return new Android.Graphics.Color(AndroidX.Core.Content.ContextCompat.GetColor(context, resourceId));
        }

        public static Android.Graphics.Drawables.Drawable GetTintedDrawable(this Android.Content.Context context, int drawableId, int colorId)
        {
            var drawable = AndroidX.Core.Content.ContextCompat.GetDrawable(context, drawableId);
#pragma warning disable 0618 // Runtime check.
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
                drawable.SetColorFilter(new Android.Graphics.BlendModeColorFilter(ColorFromResource(context, colorId), Android.Graphics.BlendMode.SrcIn));
            else
                drawable.SetColorFilter(ColorFromResource(context, colorId), Android.Graphics.PorterDuff.Mode.SrcIn);
#pragma warning restore 0618
            return drawable;
        }

        #endregion Vector Drawable
    }
}
