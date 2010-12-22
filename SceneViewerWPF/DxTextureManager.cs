using System;
using System.Collections.Generic;
using SlimDX;
using SlimDX.Direct3D10;
using SlimDX.DXGI;
using Device = SlimDX.Direct3D10.Device;
using MapFlags = SlimDX.Direct3D10.MapFlags;
using Resource = SlimDX.Direct3D10.Resource;

namespace SceneViewerWPF
{
    internal class DxTextureManager : IDisposable
    {
        private readonly Device _dxDevice;
        private readonly Dictionary<string, ShaderResourceView> _textures;

        public DxTextureManager(Device dxDevice)
        {
            _textures = new Dictionary<string, ShaderResourceView>();
            _dxDevice = dxDevice;
        }

        public ShaderResourceView CreateTexArray(string arrayName, params string[] filenames)
        {
            if (_textures.ContainsKey(arrayName))
                return _textures[arrayName];

            //
	        // Load the texture elements individually from file.  These textures
	        // won't be used by the GPU (0 bind flags), they are just used to 
	        // load the image data from file.  We use the STAGING usage so the
	        // CPU can read the resource.
	        //

        	int arraySize = filenames.Length;

	        var srcTex = new Texture2D[arraySize];
	        for(int i = 0; i < arraySize; ++i)
	        {
                var loadInfo = new ImageLoadInformation
                                   {
                                       Usage = ResourceUsage.Staging,
                                       BindFlags = BindFlags.None,
                                       CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
                                       OptionFlags = ResourceOptionFlags.None,
                                       Format = Format.R8G8B8A8_UNorm,
                                       FilterFlags = FilterFlags.None,
                                       MipFilterFlags = FilterFlags.None
                                   };

	            srcTex[i] = Texture2D.FromFile(_dxDevice, filenames[i], loadInfo);
	        }

	        //
	        // Create the texture array.  Each element in the texture 
	        // array has the same format/dimensions.
	        //
            var texElementDesc = srcTex[0].Description;
            var texArrayDesc = new Texture2DDescription
                                   {
                                       Width = texElementDesc.Width,
                                       Height = texElementDesc.Height,
                                       MipLevels = texElementDesc.MipLevels,
                                       ArraySize = arraySize,
                                       Format = Format.R8G8B8A8_UNorm,
                                       SampleDescription = new SampleDescription(1, 0),
                                       Usage = ResourceUsage.Default,
                                       BindFlags = BindFlags.ShaderResource,
                                       CpuAccessFlags = CpuAccessFlags.None,
                                       OptionFlags = ResourceOptionFlags.None
                                   };

            var texArray = new Texture2D(_dxDevice, texArrayDesc);

	        //
	        // Copy individual texture elements into texture array.
	        //

	        // for each texture element...
            for (int i = 0; i < arraySize; ++i)
            {
                // for each mipmap level...
                for (int j = 0; j < texElementDesc.MipLevels; ++j)
                {
                    var mappedTex2D = srcTex[i].Map(j, MapMode.Read, MapFlags.None);

                    _dxDevice.UpdateSubresource(
                        new DataBox(mappedTex2D.Pitch, 0, mappedTex2D.Data), texArray, 
                        Resource.CalculateSubresourceIndex(j, i, texElementDesc.MipLevels));     

                    srcTex[i].Unmap(j);
                }
            }

            //
	        // Create a resource view to the texture array.
	        //
	
	        var viewDesc = new ShaderResourceViewDescription();
	        viewDesc.Format = texArrayDesc.Format;
	        viewDesc.Dimension = ShaderResourceViewDimension.Texture2DArray;
	        viewDesc.MostDetailedMip = 0;
	        viewDesc.MipLevels = texArrayDesc.MipLevels;
	        viewDesc.FirstArraySlice = 0;
	        viewDesc.ArraySize = arraySize;


            var texArrayRV = new ShaderResourceView(_dxDevice, texArray, viewDesc);

	        //
	        // Cleanup--we only need the resource view.
	        //
	        texArray.Dispose();

	        for(int i = 0; i < arraySize; ++i)
		        srcTex[i].Dispose();

            _textures.Add(arrayName, texArrayRV);

        	return texArrayRV;
        }

        public void Dispose()
        {
            foreach (var resourceView in _textures.Values)
            {
                resourceView.Dispose();
            }
            _textures.Clear();
        }
    }
}