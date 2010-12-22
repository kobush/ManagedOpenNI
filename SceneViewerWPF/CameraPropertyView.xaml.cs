using System;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using HelixToolkit;

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

	    public CameraMode SelectedCameraMode
	    {
	        get
	        {
                var item = (ComboBoxItem)cameraMode.SelectedItem;
	            return (CameraMode) item.Tag;
	        }
	    }

	    public void UpdateCamera(PerspectiveCamera camera)
	    {
            cameraDir.Text = camera.LookDirection.Format("f1");
            cameraEye.Text = camera.Position.Format("f1");
            cameraUp.Text = camera.UpDirection.Format("f1");
	        nearPlane.Text = camera.NearPlaneDistance.ToString("f1");
	        farPlane.Text = camera.FarPlaneDistance.ToString("f1");
	        fov.Text = camera.FieldOfView.ToString("f1");
	    }

	    public event EventHandler SelectedCameraModeChanged;

	    private void OnCameraModeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EventHandler handler = SelectedCameraModeChanged;
            if (handler != null) handler(this, e);
        }
	}
}