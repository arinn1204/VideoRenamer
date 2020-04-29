﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Grains.FileFormat.Models;
using GrainsInterfaces.FileFormat;
using GrainsInterfaces.FileFormat.Models;
using Newtonsoft.Json;
using Orleans;

namespace Grains.FileFormat
{
	public class FileFormatRepository : Grain, IFileFormatRepository
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IMapper _mapper;

		public FileFormatRepository(
			IHttpClientFactory httpClientFactory,
			IMapper mapper)
		{
			_httpClientFactory = httpClientFactory;
			_mapper = mapper;
		}

#region IFileFormatRepository Members

		public Task<IAsyncEnumerable<RegisteredFileFormat>> GetAcceptableFileFormats()
		{
			var responseContentTask = GetResponseContent("fileFormats");

			return Task.Factory.StartNew(
				() =>
				{
					return AsyncEnumerable.Create(
						token => EnumerateContent(
								responseContentTask,
								(FilePattern response)
									=> _mapper.Map<RegisteredFileFormat>(response))
						   .GetAsyncEnumerator(token));
				});
		}

		public Task<IAsyncEnumerable<string>> GetAllowedFileTypes()
		{
			var responseContentTask = GetResponseContent("fileTypes");

			return Task.Factory.StartNew(
				() =>
				{
					return AsyncEnumerable.Create(
						token => EnumerateContent(
								responseContentTask,
								(string response) => response)
						   .GetAsyncEnumerator(token));
				});
		}

		public Task<IAsyncEnumerable<string>> GetFilteredKeywords()
		{
			var responseContentTask = GetResponseContent("filteredKeywords");

			return Task.Factory.StartNew(
				() =>
				{
					return AsyncEnumerable.Create(
						token => EnumerateContent(
								responseContentTask,
								(string response) => response)
						   .GetAsyncEnumerator(token));
				});
		}

		public async Task<string> GetTargetTitleFormat()
			=> await GetResponseContent("targetTitleFormat").ConfigureAwait(false);

#endregion

		private async Task<string> GetResponseContent(string relativePath)
		{
			var client = _httpClientFactory.CreateClient(nameof(FileFormatRepository));
			var request = new HttpRequestMessage
			              {
				              Method = HttpMethod.Get,
				              RequestUri = new Uri(relativePath.TrimStart('/'), UriKind.Relative)
			              };

			var responseMessage = await client.SendAsync(request).ConfigureAwait(false);
			var responseContent = await responseMessage
			                           .Content
			                           .ReadAsStringAsync()
			                           .ConfigureAwait(false);
			return responseContent;
		}


		private async IAsyncEnumerable<TResult> EnumerateContent<TResponse, TResult>(
			Task<string> responseContent,
			Func<TResponse, TResult> getResult)
		{
			var content = await responseContent.ConfigureAwait(false);
			var acceptableFormats =
				JsonConvert.DeserializeObject<IEnumerable<TResponse>>(content);

			foreach (var acceptableFormat in acceptableFormats)
			{
				yield return getResult(acceptableFormat);
			}
		}
	}
}