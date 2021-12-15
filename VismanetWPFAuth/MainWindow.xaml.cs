using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VismanetWPFAuth.Properties;

namespace VismanetWPFAuth
{
    public static class EncryptionHelper
    {
        public static string EncryptString(string input, string key)
        {
            byte[] encryptedData = System.Security.Cryptography.ProtectedData.Protect(
                System.Text.Encoding.Unicode.GetBytes(input),
                Encoding.UTF8.GetBytes(key),
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public static string DecryptString(string encryptedData, string key)
        {
            try
            {
                byte[] decryptedData = System.Security.Cryptography.ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedData),
                    Encoding.UTF8.GetBytes(key),
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return Encoding.Unicode.GetString(decryptedData);
            }
            catch
            {
                return null;
            }
        }

        public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
        {
            CancellationTokenSource? cancelTokenSource = null;

            return arg =>
            {
                cancelTokenSource?.Cancel();
                cancelTokenSource = new CancellationTokenSource();

                Task.Delay(milliseconds, cancelTokenSource.Token)
                    .ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            func(arg);
                        }
                    }, TaskScheduler.Default);
            };
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            VismaNetControl.ClientId = Settings.Default.ClientId;
            VismaNetControl.CallbackUrl = Settings.Default.CallbackUrl;
            if (!string.IsNullOrEmpty(Settings.Default.ClientSecret))
            {
                try
                {
                    VismaNetControl.ClientSecret = EncryptionHelper.DecryptString(Settings.Default.ClientSecret, Settings.Default.ClientId);
                    InputClientSecret.Password = VismaNetControl.ClientSecret;
                }
                catch { }
            }
            InputClientId.Text = VismaNetControl.ClientId;
            InputCallbackUrl.Text = VismaNetControl.CallbackUrl;
        }

        private void AuthControl_OnTokenReceived(object sender, string e)
        {
            try
            {
                Clipboard.SetText(e);
            }
            catch (Exception) { }
            MessageBox.Show($"Token: {e}");
        }

        private void ClientSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            VismaNetControl.ClientSecret = InputClientSecret.Password;
            Settings.Default.ClientSecret = EncryptionHelper.EncryptString(VismaNetControl.ClientSecret, VismaNetControl.ClientId);
            Settings.Default.Save();
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            VismaNetControl.ClientId = InputClientId.Text;
            VismaNetControl.CallbackUrl = InputCallbackUrl.Text;
            VismaNetControl.ClientSecret = InputClientSecret.Password;
            Settings.Default.ClientId = VismaNetControl.ClientId;
            Settings.Default.ClientSecret = EncryptionHelper.EncryptString(VismaNetControl.ClientSecret, VismaNetControl.ClientId);
            Settings.Default.CallbackUrl = VismaNetControl.CallbackUrl;
            Settings.Default.Save();
        }
    }
}
