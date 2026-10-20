using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlackLauncher
{
    /// <summary>
    /// The support page: the PIX QR code on the left and the other ways to help on the right.
    /// It is the same frame as the launcher, with copy buttons so the player never has to select
    /// a long wallet address by hand.
    /// </summary>
    public partial class DonationWindow : Window
    {
        public DonationWindow()
        {
            InitializeComponent();
            Title = Loc.DonationCaption;
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { e.Handled = true; Close(); }
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CaptionText.Text = Loc.DonationCaption;

            PixLabel.Text = Loc.DonationPix;
            PixHint.Text = Loc.DonationPixHint;
            TitleText.Text = Loc.DonationTitle;
            IntroText.Text = Loc.DonationIntro;
            OtherLabel.Text = Loc.DonationOther;
            LivePixHint.Text = Loc.DonationLivePixHint;
            LivePixButton.Content = Loc.DonationLivePix;

            BitcoinAddress.Text = DonationInfo.Bitcoin;
            DogecoinAddress.Text = DonationInfo.Dogecoin;
            EthereumAddress.Text = DonationInfo.Ethereum;
            UsdtAddress.Text = DonationInfo.Usdt;

            foreach (Button b in new[] { BitcoinCopy, DogecoinCopy, EthereumCopy, UsdtCopy })
                b.Content = Loc.DonationCopy;

            ThanksText.Text = Loc.DonationThanks;
            CloseButton.Content = Loc.DonationClose;
            CloseButton.Focus();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string value = button == null ? null : button.Tag as string;
            if (string.IsNullOrEmpty(value)) return;
            try
            {
                Clipboard.SetText(value);
                ThanksText.Text = Loc.DonationCopied;
            }
            catch
            {
                ThanksText.Text = Loc.DonationCopyFailed;
            }
        }

        private void LivePix_Click(object sender, RoutedEventArgs e) => GameLauncher.OpenUrl(DonationInfo.LivePix);
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
