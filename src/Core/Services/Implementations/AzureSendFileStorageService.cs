﻿using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Bit.Core.Enums;
using Bit.Core.Models.Table;
using Bit.Core.Settings;

namespace Bit.Core.Services
{
    public class AzureSendFileStorageService : ISendFileStorageService
    {
        public const string FilesContainerName = "sendfiles";
        private static readonly TimeSpan _downloadLinkLiveTime = TimeSpan.FromMinutes(1);
        private readonly BlobServiceClient _blobServiceClient;
        private BlobContainerClient _sendFilesContainerClient;

        public FileUploadType FileUploadType => FileUploadType.Azure;

        public static string SendIdFromBlobName(string blobName) => blobName.Split('/')[0];
        public static string BlobName(Send send, string fileId) => $"{send.Id}/{fileId}";

        public AzureSendFileStorageService(
            GlobalSettings globalSettings)
        {
            _blobServiceClient = new BlobServiceClient(globalSettings.Send.ConnectionString);
        }

        public async Task UploadNewFileAsync(Stream stream, Send send, string fileId)
        {
            await InitAsync();

            var blobClient = _sendFilesContainerClient.GetBlobClient(BlobName(send, fileId));
            var metadata = blobClient.GetProperties().Value.Metadata;

            if (send.UserId.HasValue)
            {
                metadata.Add("userId", send.UserId.Value.ToString());
            }
            else
            {
                metadata.Add("organizationId", send.OrganizationId.Value.ToString());
            }

            var headers = new BlobHttpHeaders
            {
                ContentDisposition = $"attachment; filename=\"{fileId}\""
            };
            
            await blobClient.UploadAsync(stream, new BlobUploadOptions { Metadata = metadata, HttpHeaders = headers });
        }

        public async Task DeleteFileAsync(Send send, string fileId) => await DeleteBlobAsync(BlobName(send, fileId));

        public async Task DeleteBlobAsync(string blobName)
        {
            await InitAsync();
            var blobClient = _sendFilesContainerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task DeleteFilesForOrganizationAsync(Guid organizationId)
        {
            await InitAsync();
        }

        public async Task DeleteFilesForUserAsync(Guid userId)
        {
            await InitAsync();
        }

        public async Task<string> GetSendFileDownloadUrlAsync(Send send, string fileId)
        {
            await InitAsync();
            var blobClient = _sendFilesContainerClient.GetBlobClient(BlobName(send, fileId));
            if (blobClient.CanGenerateSasUri)
            {
                var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTime.UtcNow.Add(_downloadLinkLiveTime));
                return sasUri.ToString();
            }
            return null;
        }

        public async Task<string> GetSendFileUploadUrlAsync(Send send, string fileId)
        {
            await InitAsync();
            var blobClient = _sendFilesContainerClient.GetBlobClient(BlobName(send, fileId));
            if (blobClient.CanGenerateSasUri)
            {
                var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Create | BlobSasPermissions.Write, DateTime.UtcNow.Add(_downloadLinkLiveTime));
                return sasUri.ToString();
            }
            return null;
        }

        public async Task<(bool, long?)> ValidateFileAsync(Send send, string fileId, long expectedFileSize, long leeway)
        {
            await InitAsync();

            var blobClient = _sendFilesContainerClient.GetBlobClient(BlobName(send, fileId));

            if (!blobClient.Exists())
            {
                return (false, null);
            }

            var blobProperties = blobClient.GetProperties().Value;
            var metadata = blobProperties.Metadata;

            if (send.UserId.HasValue)
            {
                metadata["userId"] = send.UserId.Value.ToString();
            }
            else
            {
                metadata["organizationId"] = send.OrganizationId.Value.ToString();
            }
            await blobClient.SetMetadataAsync(metadata);

            var headers = new BlobHttpHeaders {
                ContentDisposition = $"attachment; filename=\"{fileId}\""
            };
            await blobClient.SetHttpHeadersAsync(headers);

            //TODO djsmith85 Is this the correct length
            //var length = blob.Properties.Length;
            var length = blobProperties.ContentLength;
            if (length < expectedFileSize - leeway || length > expectedFileSize + leeway)
            {
                return (false, length);
            }

            return (true, length);
        }

        private async Task InitAsync()
        {
            if (_sendFilesContainerClient == null)
            {
                _sendFilesContainerClient = _blobServiceClient.GetBlobContainerClient(FilesContainerName);
                await _sendFilesContainerClient.CreateIfNotExistsAsync(PublicAccessType.None, null, null);;
            }
        }
    }
}
