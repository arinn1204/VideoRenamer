﻿using Grains.Codecs.ExtensibleBinaryMetaLanguage.Attributes;

namespace Grains.Codecs.ExtensibleBinaryMetaLanguage.Models.Tracks
{
	[EbmlMaster]
	public class TrackCombinePlanes
	{
		public TrackPlane TrackPlane { get; set; }
	}
}