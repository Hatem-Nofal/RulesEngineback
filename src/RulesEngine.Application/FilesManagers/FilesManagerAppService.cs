using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace RulesEngine.FilesManagers
{
    public class FilesManagerAppService : RulesEngineAppService
    {

        private readonly string _logFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");



        [AllowAnonymous]
        [HttpGet]
        [Route("api/FilesManager/DownloadLatestLog")]
        public async Task<IRemoteStreamContent> DownloadLatestLog()
        {

            if (!Directory.Exists(_logFolder))
                throw new UserFriendlyException("Log File not found.");

            // Get the latest log file
            var latestFile = new DirectoryInfo(_logFolder)
                .GetFiles("*.txt")
                .OrderByDescending(f => f.CreationTimeUtc)
                .FirstOrDefault();

            if (latestFile == null)
                throw new UserFriendlyException("Log File not found.");

            // Return the file as download
            var fileStream = new FileStream(latestFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new RemoteStreamContent(fileStream, latestFile.Name, "text/plain");

        }
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream",
            };
        }
    }
}
