#pragma once
#include <XnOS.h>
#include <XnCppWrapper.h>

using namespace xn;

namespace ManagedNiteEx
{
	public enum class XnMPixelFormat
	{
		Rgb24 = XN_PIXEL_FORMAT_RGB24,
		Yuv422 = XN_PIXEL_FORMAT_YUV422,
		Grayscale8Bit = XN_PIXEL_FORMAT_GRAYSCALE_8_BIT,
		Grayscale16Bit = XN_PIXEL_FORMAT_GRAYSCALE_16_BIT   
	};

	public enum class XnMProductionNodeType {
		/** A device node **/
		Device = XN_NODE_TYPE_DEVICE,
		
		/** A depth generator **/
		Depth = XN_NODE_TYPE_DEPTH,
	
		/** An image generator **/
		Image = XN_NODE_TYPE_IMAGE,

		/** An audio generator **/
		Audio = XN_NODE_TYPE_AUDIO,
	
		/** An IR generator **/
		Infrared = XN_NODE_TYPE_IR,

		/** A user generator **/
		User = XN_NODE_TYPE_USER,
	
		/** A recorder **/
		Recorder = XN_NODE_TYPE_RECORDER,
	
		/** A player **/
		Player = XN_NODE_TYPE_PLAYER,
	
		/** A gesture generator **/
		Gesture = XN_NODE_TYPE_GESTURE,
	
		/** A scene analyzer **/
		Scene = XN_NODE_TYPE_SCENE,
	
		/** A hands generator **/
		Hands = XN_NODE_TYPE_HANDS,

		/** A Codec **/
		Codec = XN_NODE_TYPE_CODEC,
	};
}