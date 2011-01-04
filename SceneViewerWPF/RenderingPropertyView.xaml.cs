using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SceneViewerWPF
{
	/// <summary>
	/// Interaction logic for RenderingPropertyView.xaml
	/// </summary>
	public partial class RenderingPropertyView : UserControl
	{
		public RenderingPropertyView()
		{
			this.InitializeComponent();
		}

	    public bool Wireframe
	    {
            get { return wireFrame.IsChecked == true; }
	    }

	    public KinectPointsRendererType SelectedRenderTech
	    {
	        get
	        {
	            var item = (ComboBoxItem) renderTech.SelectedItem;
                return (KinectPointsRendererType)item.Tag;
	        }
	    }

	    public event EventHandler SelectedRenderTechChanged;

	    private void renderTech_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EventHandler handler = SelectedRenderTechChanged;
            if (handler != null) handler(this, e);
        }

	    public Color FillColor
	    {
            get { return fillColor.SelectedColor; }
            set { fillColor.SelectedColor = value; }
	    }

	    public double TextureAlpha
	    {
            get { return texAlpha.Value; }
            set { texAlpha.Value = value; }
	    }

	    public double UserAlpha
	    {
            get { return userAlpha.Value; }
            set { userAlpha.Value = value; }
	    }

	    public double BackgroundAlpha
	    {
            get { return backAlpha.Value; }
            set { backAlpha.Value = value; }
	    }

	    public float? Scale
	    {
	        get
	        {
	            float value;
                if (float.TryParse(scale.Text, out value))
                    return value;
	            return null;
	        }
            set { scale.Text = value != null ? value.Value.ToString("f2") : ""; }
	    }

	    public event EventHandler RenderPropertyChanged;

	    private void InvokeRenderPropertyChanged()
	    {
	        var handler = RenderPropertyChanged;
	        if (handler != null) handler(this, EventArgs.Empty);
	    }

	    private void Scale_OnTextChanged(object sender, TextChangedEventArgs e)
	    {
	        InvokeRenderPropertyChanged();
	    }

	    private void FillColor_OnSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
	    {
            InvokeRenderPropertyChanged();
	    }

	    private void TexAlpha_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	    {
	        InvokeRenderPropertyChanged();
	    }
	}
}