using System.Windows;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Views
{
    /// <summary>
    /// SetupView.xaml の相互作用ロジック
    /// </summary>
    public partial class SetupView : Window
    {
        public SetupView()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "データ保存フォルダを選択してください",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (DataContext is SetupViewModel vm)
                {
                    vm.SetPath(dialog.SelectedPath);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ViewModelからのクローズリクエストを受け取るためにイベント購読
        protected override void OnContentRendered(System.EventArgs e)
        {
            base.OnContentRendered(e);
            if (DataContext is SetupViewModel vm)
            {
                vm.RequestClose += () =>
                {
                    DialogResult = true;
                    Close();
                };
            }
        }
    }
}
