using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit;
using ManagedNiteEx;

namespace SceneViewerWPF
{
    public class PointCloud : ModelVisual3D, IPointCloudViewer
    {
        private uint _xRes;
        private uint _yRes;

        public static Geometry3D CreateUnitCubeGeometry()
        {
            var mb = new MeshBuilder();
            mb.AddBox(new Point3D(0, 0, 0), 1, 1, 1);
            var mesh = mb.ToMesh();
            mesh.Freeze();
            return mesh;
        }

        class PointModel
        {
            public GeometryModel3D Model { get; set; }
            public TranslateTransform3D TranslateTransform { get; set; }
            public ScaleTransform3D ScaleTransform { get; set; }
        }

        private PointModel[,] _pointModels;

        public void Initialize(uint xRes, uint yRes)
        {
            _xRes = xRes;
            _yRes = yRes;

            _pointModels = new PointModel[_xRes, _yRes];
            var material = MaterialHelper.CreateMaterial(Colors.MediumPurple);

            Model3DGroup group = new Model3DGroup();
            for (int y = 0; y < _yRes; y++)
            {
                var mb = new MeshBuilder();
                for (int x = 0; x < _xRes; x++)
                {
                    mb.AddBox(new Point3D(x, y, 0), 1, 1, 1);

/*
                    var model = new PointModel();
                    model.Model = new GeometryModel3D(CreateUnitCubeGeometry(), material);
                    model.TranslateTransform = new TranslateTransform3D();
                    model.ScaleTransform = new ScaleTransform3D();

                    Transform3DGroup tg = new Transform3DGroup();
                    tg.Children.Add(model.TranslateTransform);
                    tg.Children.Add(model.ScaleTransform);
                    model.Model.Transform = tg;

                    group.Children.Add(model.Model);
                    _pointModels[x, y] = model;
*/
                }
                var model = new GeometryModel3D(mb.ToMesh(), material);

                group.Children.Add(model);
            }


            this.Content = group;

            /*
                        X = (u - 320) * depth_md_[k] * pixel_size_ * 0.001 / F_;
                        Y = (v - 240) * depth_md_[k] * pixel_size_ * 0.001 / F_;
                        Z = depth_md_[k] * 0.001; // from mm in meters!
            */

        }
    }

    public interface IPointCloudViewer
    {
        void Initialize(uint xRes, uint yRes);
    }
}