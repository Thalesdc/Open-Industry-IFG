using Godot;
using System;

[GlobalClass]
public partial class StackLightData : Resource
{
	public int segments = 0;
	StackSegmentData segmentData = (StackSegmentData)ResourceLoader.Load("res://src/StackLight/StackSegmentData.tres");
	[Export] public StackSegmentData[] segmentDatas = Array.Empty<StackSegmentData>();
	
	public void InitSegments(int count)
	{
		segments = count;
		
		if (segmentDatas.Length == 0)
		{
			segmentDatas = new StackSegmentData[segments];
			for (int i = 0; i < segments; i++)
			{
				segmentDatas[i] = segmentData.Duplicate() as StackSegmentData;
			}
		}
		else
		{
			StackSegmentData[] cache = new StackSegmentData[count];
			for (int i = 0; i < count; i++)
			{
				cache[i] = segmentDatas[i].Duplicate() as StackSegmentData;
			}
			segmentDatas = cache;
		}
	}
	
	public void SetSegments(int count)
	{
		if (count == segments) return;
		
		StackSegmentData[] cache = new StackSegmentData[count];
		
		if (count < segments)
		{
			for (int i = 0; i < count; i++)
			{
				cache[i] = segmentDatas[i];
			}
		}
		else
		{
			for (int i = 0; i < count; i++)
			{
				if (i < segments)
					cache[i] = segmentDatas[i];
				else
					cache[i] = segmentData.Duplicate() as StackSegmentData;
			}
		}
		
		segments = count;
		segmentDatas = cache;
	}
}
