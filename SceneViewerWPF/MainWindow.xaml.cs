using System.Windows;

namespace SceneViewerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectTracker _kinectTracker;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _kinectTracker = new KinectTracker();
            _kinectTracker.StartTracking(pointCloud);
        }
    }
}
