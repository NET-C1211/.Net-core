﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Telemetry.Utils;
using System;
using System.IO;
using WorkspaceServer;

namespace MLS.Agent.Telemetry.Configurer
{
    public sealed class UserLevelCacheWriter : IUserLevelCacheWriter
    {
        private string _dotnetTryUserProfileFolderPath;
        private readonly Func<string, bool> _fileExists;
        private readonly Func<string, bool> _directoryExists;
        private readonly Action<string> _createDirectory;
        private readonly Action<string, string> _writeAllText;
        private readonly Func<string, string> _readAllText;

        public UserLevelCacheWriter() :
            this(
                Paths.DotnetUserProfileFolderPath,
                path => File.Exists(path),
                path => Directory.Exists(path),
                path => Directory.CreateDirectory(path),
                (path, text) => File.WriteAllText(path, text),
                path => File.ReadAllText(path))
        {
        }

        public UserLevelCacheWriter(string dotnetTryUserProfileFolderPath,
            Func<string, bool> fileExists,
            Func<string, bool> directoryExists,
            Action<string> createDirectory,
            Action<string, string> writeAllText,
            Func<string, string> readAllText)
        {
            _dotnetTryUserProfileFolderPath = dotnetTryUserProfileFolderPath;
            _fileExists = fileExists;
            _directoryExists = directoryExists;
            _createDirectory = createDirectory;
            _writeAllText = writeAllText;
            _readAllText = readAllText;
        }

        public string RunWithCache(string cacheKey, Func<string> getValueToCache)
        {
            var cacheFilepath = GetCacheFilePath(cacheKey);
            try
            {
                if (!_fileExists(cacheFilepath))
                {
                    if (!_directoryExists(_dotnetTryUserProfileFolderPath))
                    {
                        _createDirectory(_dotnetTryUserProfileFolderPath);
                    }

                    var runResult = getValueToCache();

                    _writeAllText(cacheFilepath, runResult);
                    return runResult;
                }
                else
                {
                    return _readAllText(cacheFilepath);
                }
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedAccessException
                    || ex is PathTooLongException
                    || ex is IOException)
                {
                    return getValueToCache();
                }

                throw;
            }

        }

        private string GetCacheFilePath(string cacheKey)
        {
            return Path.Combine(_dotnetTryUserProfileFolderPath, $"{Product.Version}_{cacheKey}.dotnetTryUserLevelCache");
        }
    }
}
