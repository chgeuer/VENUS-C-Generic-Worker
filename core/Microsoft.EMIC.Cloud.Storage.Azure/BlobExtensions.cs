//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using Microsoft.WindowsAzure.StorageClient;
using System.IO;
using System.Text;
using System.Linq;
using System;

namespace Microsoft.EMIC.Cloud.Storage.Azure
{
    /// <summary>
    /// Extension methods for the <see cref="CloudBlob" /> type. 
    /// </summary>
    public static class BlobExtensions
    {
        /// <summary>
        /// Existses the specified BLOB.
        /// </summary>
        /// <param name="blob">The BLOB.</param>
        /// <returns></returns>
        public static bool Exists(this CloudBlob blob)
        {
            try
            {
                blob.FetchAttributes();

                return true;
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return false;
                }
                
                throw;
            }
        }

        /// <summary>
        /// Appends text to the block BLOB
        /// </summary>
        /// <param name="blockBlob">The block BLOB.</param>
        /// <param name="text">The text.</param>
        public static void AppendText(this CloudBlockBlob blockBlob, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            if (!blockBlob.Exists())
            {
                blockBlob.UploadText("");
            }
            var bl = blockBlob.DownloadBlockList(BlockListingFilter.Committed);
            var blockList = bl.Select(b => b.Name).ToList();
            var blockSizes = bl.Select(b => b.Size).ToList();
            var fullBlockSize = blockSizes.Sum();
            while (blockList.Count > 1 && fullBlockSize > 2000000) //Todo: make this configurable
            {
                fullBlockSize -= blockSizes.First();
                blockSizes.RemoveAt(0);
                blockList.RemoveAt(0);
            }
            using (var ms = new MemoryStream(Encoding.Default.GetBytes(text)))
            {
                var maxRetries=5;
                for (int i = 0; i <= maxRetries; i++)
                {
                    var newId = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace('+', 'A').Replace('\\', 'B').Replace('/', 'C');
                    try
                    {
                        ms.Position = 0;
                        blockBlob.PutBlock(newId, ms, null);                        
                        blockList.Add(newId);
                        blockBlob.PutBlockList(blockList);
                        break;
                    }
                    catch (StorageClientException)
                    {
                        blockList.Remove(newId);
                        if (i == maxRetries)
                        {                            
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Appends text to the block BLOB. The size of this text is limited as specified by the maxBytes parameter.
        /// If the text is bigger than maxBytes then only the last maxBytes of the text + a small prefix indicatinng that a part was truncated are transferred to the blob storage.
        /// </summary>
        /// <param name="blockBlob">The block BLOB.</param>
        /// <param name="text">The text.</param>
        /// <param name="maxBytes">The max size of the text in bytes.</param>
        public static void AppendText(this CloudBlockBlob blockBlob, string text, int maxBytes)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            var blobtext = text.limitSizeInBytesUTF16(maxBytes);
            if (blobtext.Length < text.Length)
            {
                blobtext = "<Some part of the output was truncated here>" + blobtext;
            }
            blockBlob.AppendText(blobtext);
        }
    }
}
