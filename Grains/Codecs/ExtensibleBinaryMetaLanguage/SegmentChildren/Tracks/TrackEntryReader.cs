﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Converter;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Interfaces;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Models;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Models.Extensions;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Models.Tracks;

namespace Grains.Codecs.ExtensibleBinaryMetaLanguage.SegmentChildren.Tracks
{
	public class TrackEntryReader : ITrackEntryReader
	{
		private readonly IReader _reader;

		public TrackEntryReader(IReader reader)
		{
			_reader = reader;
		}

#region ITrackEntryReader Members

		public TrackEntry ReadEntry(
			Stream stream,
			EbmlSpecification specification,
			long trackEntrySize)
		{
			var trackSpecs = specification.GetTrackElements();
			var skippedElements = specification.GetSkippableElements()
			                                   .ToDictionary(k => k);
			var values = GetValues(
					stream,
					trackEntrySize,
					skippedElements,
					trackSpecs)
			   .ToArray();

			return EbmlConvert.DeserializeTo<TrackEntry>(values);
		}

#endregion

		private IEnumerable<(string, object)> GetValues(
			Stream stream,
			long elementSize,
			Dictionary<uint, uint> skippedElements,
			IReadOnlyDictionary<uint, EbmlElement> trackSpecs)
		{
			var data = Array.Empty<byte>();
			var endPosition = stream.Position + elementSize;
			while (stream.Position < endPosition)
			{
				data = data.Concat(_reader.ReadBytes(stream, 1))
				           .ToArray();

				var id = data.ConvertToUint();

				if (skippedElements.ContainsKey(id))
				{
					var size = _reader.GetSize(stream);
					stream.Seek(size, SeekOrigin.Current);
					continue;
				}

				if (!trackSpecs.ContainsKey(id))
				{
					continue;
				}

				var element = trackSpecs[id];
				var value = ProcessElement(
					stream,
					element,
					trackSpecs,
					skippedElements);

				data = Array.Empty<byte>();
				yield return value;
			}
		}

		private (string name, object value) ProcessElement(
			Stream stream,
			EbmlElement element,
			IReadOnlyDictionary<uint, EbmlElement> trackSpecs,
			Dictionary<uint, uint> skippedElements)
		{
			var size = _reader.GetSize(stream);
			if (element.Type != "master")
			{
				var data = _reader.ReadBytes(stream, (int) size);
				return GetValue(element, data);
			}

			var values = GetValues(
					stream,
					size,
					skippedElements,
					trackSpecs)
			   .ToArray();

			var value = EbmlConvert.DeserializeTo(element.Name, values);
			return (element.Name, value);
		}


		private (string name, object value) GetValue(
			EbmlElement element,
			byte[] data)
		{
			var value = data.GetValue(element);
			return (element.Name, value);
		}
	}
}