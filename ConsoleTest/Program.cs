using System;
using ManagedNiteEx;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            XnMOpenNIContextEx ctx = new XnMOpenNIContextEx();
            ctx.InitFromXmlFile("openni.xml");

/*
            var depthNode = ctx.FindExistingNode(XnMProductionNodeType.Depth) as XnMDepthGenerator;
            PrintNodeInfo(depthNode);
*/

            var imageNode = ctx.FindExistingNode(XnMProductionNodeType.Image) as XnMImageGenerator;
            PrintNodeInfo(imageNode);

            Console.WriteLine("Press ESC to exit.");

            var imageMD = new XnMImageMetaData();

            while(true)
            {
                ctx.WaitAndUpdateAll();

                imageNode.GetMetaData(imageMD);
                PrintImageMetaData(imageMD);
                
                Console.WriteLine("-----------------");
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        break;
                }
            }
            ctx.Shutdown();
        }

        private static void PrintImageMetaData(XnMImageMetaData imageMD)
        {
            Console.WriteLine("FrameID: " + imageMD.FrameID);
            Console.WriteLine("FPS: " + imageMD.FPS);
            Console.WriteLine("PixelFormat: " + imageMD.PixelFormat);
            Console.WriteLine("BytesPerPixel: " + imageMD.BytesPerPixel);
            Console.WriteLine("XRes: " + imageMD.XRes);
            Console.WriteLine("YRes: " + imageMD.YRes);
            Console.WriteLine("XOffset: " + imageMD.XOffset);
            Console.WriteLine("YOffset: " + imageMD.YOffset);
        }

        private static void PrintNodeInfo(XnMProductionNode node)
        {
            var nodeInfo = node.GetNodeInfo();
            Console.WriteLine("Instance Name: " + nodeInfo.InstanceName);
            Console.WriteLine("Node Name: " + nodeInfo.Name);
            Console.WriteLine("Creation Info: " + nodeInfo.CreationInfo);
            Console.WriteLine("Vendor: " + nodeInfo.Vendor);
            Console.WriteLine("Type: " + nodeInfo.Type);
        }
    }
}
