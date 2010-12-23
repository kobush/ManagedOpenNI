namespace SceneViewerWPF
{
	/// <summary>
	/// Interaction logic for LightPropertyView.xaml
	/// </summary>
	public partial class LightPropertyView
	{
		public LightPropertyView()
		{
			InitializeComponent();
		}

	    public LightPropertyViewModel ViewModel
	    {
	        get { return (LightPropertyViewModel) DataContext; }
	    }
	}
}