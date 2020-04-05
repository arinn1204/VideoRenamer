﻿using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Models.Extensions;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Models.Segment.MetaSeekInformation;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Models.Segment.SegmentInformation;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Models.Specification;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Readers;
using Grains.Tests.Unit.Extensions;
using Grains.Tests.Unit.Fixtures;
using Moq;
using Xunit;

namespace Grains.Tests.Unit.Codecs
{
	public class EbmlReaderTests : IClassFixture<MatroskaFixture>
	{
#region Setup/Teardown

		public EbmlReaderTests(MatroskaFixture fixture)
		{
			_specification = fixture.Specification;
		}

#endregion

		private readonly EbmlSpecification _specification;

		[Fact]
		public void ShouldKeepReadingUntilReceivingAnAcceptableId()
		{
			var seek = new Seek
			           {
				           SeekId = new byte[]
				                    {
					                    1,
					                    2,
					                    3
				                    },
				           SeekPosition = 123432
			           };
			var stream = new MemoryStream();
			var reader = new Mock<EbmlReader>();
			reader.SetupSequence(s => s.ReadBytes(stream, 1))
			      .Returns(
				       new[]
				       {
					       Convert.ToByte("53", 16)
				       })
			      .Returns(
				       new[]
				       {
					       Convert.ToByte("AB", 16)
				       })
			      .Returns(
				       new[]
				       {
					       Convert.ToByte("53", 16)
				       })
			      .Returns(
				       new[]
				       {
					       Convert.ToByte("AC", 16)
				       });

			reader.Setup(s => s.GetSize(stream))
			      .Returns<Stream>(
				       s =>
				       {
					       s.Position++;
					       return 5;
				       });

			reader.SetupSequence(s => s.ReadBytes(stream, 5))
			      .Returns(seek.SeekId)
			      .Returns(BitConverter.GetBytes(seek.SeekPosition).Reverse().ToArray());

			var result = reader.Object.GetElement<Seek>(
				stream,
				2,
				_specification.Elements.ToDictionary(k => k.Id),
				_specification.GetSkippableElements().ToList());

			result.Should().BeEquivalentTo(seek);
			reader.Verify(v => v.ReadBytes(stream, 1), Times.Exactly(4));
			reader.Verify(v => v.ReadBytes(stream, 5), Times.Exactly(2));
			reader.Verify(v => v.GetSize(stream), Times.Exactly(2));
		}

		[Fact]
		public void ShouldSeekIfReceivingSkippedId()
		{
			var stream = new MemoryStream();
			var reader = new Mock<EbmlReader>();
			var skippedId = _specification.Elements
			                              .First(f => f.Name == "Void")
			                              .IdString
			                              .ToBytes()
			                              .ToArray();

			reader.Setup(s => s.ReadBytes(stream, 1))
			      .Returns(skippedId);

			reader.Setup(s => s.GetSize(stream))
			      .Returns<Stream>(
				       s =>
				       {
					       s.Position++;
					       return 5;
				       });

			var result = reader.Object.GetElement<Info>(
				stream,
				1,
				_specification.Elements.ToDictionary(k => k.Id),
				_specification.GetSkippableElements().ToList());

			result.Should().BeEquivalentTo(new Info());

			reader.Verify(v => v.GetSize(stream), Times.Once);
			reader.Verify(v => v.ReadBytes(stream, 1), Times.Once);
		}
	}
}