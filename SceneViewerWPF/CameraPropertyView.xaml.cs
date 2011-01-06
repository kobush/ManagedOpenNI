using System.Windows.Controls;

namespace SceneViewerWPF
{
	/// <summary>
	/// Interaction logic for CameraPropertyView.xaml
	/// </summary>
	public partial class CameraPropertyView
	{
		public CameraPropertyView()
		{
			this.InitializeComponent();
		}

        public CameraPropertyViewModel ViewModel
        {
            get { return (CameraPropertyViewModel)DataContext; }
        }
	}

    public enum CameraPositionMode
    {
        Free,
        KinectIR,
        KinectRGB,
        Head
    }
}