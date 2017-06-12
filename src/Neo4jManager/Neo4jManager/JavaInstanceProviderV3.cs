﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Neo4jManager
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class JavaInstanceProviderV3 : INeo4jInstanceProvider
    {
        private const string defaultDataDirectory = "data/databases";
        private const string defaultActiveDatabase = "graph.db";
        private const int defaultWaitForKill = 10000;

        private readonly string neo4jHomeFolder;
        private readonly IFileCopy fileCopy;
        private readonly JavaProcessBuilderV3 javaProcessBuilder;
        private readonly ConfigEditor configEditor;

        private Process process;

        public JavaInstanceProviderV3(string javaPath, string neo4jHomeFolder, Neo4jEndpoints endpoints, IFileCopy fileCopy)
        {
            this.neo4jHomeFolder = neo4jHomeFolder;
            this.fileCopy = fileCopy;

            var configFile = Path.Combine(neo4jHomeFolder, "conf/neo4j.conf");
            configEditor = new ConfigEditor(configFile);

            javaProcessBuilder = new JavaProcessBuilderV3(javaPath, neo4jHomeFolder, configEditor);
            Endpoints = endpoints;
        }

        public async Task Start()
        {
            if (process == null)
            {
                process = javaProcessBuilder.GetProcess();
                process.Start();
                await this.WaitForReady();

                return;
            }

            if (!process.HasExited) return;
            
            process.Start();
            await this.WaitForReady();
        }

        public async Task Stop()
        {
            if (process == null || process.HasExited) return;

            await Task.Run(() =>
            {
                process.Kill();
                process.WaitForExit(defaultWaitForKill);
            });
        }

        public void Configure(string key, string value)
        {
            configEditor.SetValue(key, value);
        }

        public async Task Clear()
        {
            var dataPath = GetDataPath();

            await Stop();
            Directory.Delete(dataPath);
            await Start();
        }

        public async Task Backup(string destinationPath, bool stopInstanceBeforeBackup = true)
        {
            var dataPath = GetDataPath();

            if (stopInstanceBeforeBackup) await Stop();
            fileCopy.MirrorFolders(dataPath, destinationPath);
            if (stopInstanceBeforeBackup) await Start();
        }

        public async Task Restore(string sourcePath)
        {
            var dataPath = GetDataPath();

            await Stop();
            fileCopy.MirrorFolders(sourcePath, dataPath);
            await Start();
        }

        public Neo4jEndpoints Endpoints { get; }

        public void Dispose()
        {
            Stop().Wait();

            process?.Dispose();
        }

        private string GetDataPath()
        {
            var dataDirectory = configEditor.GetValue("dbms.directories.data");
            if (string.IsNullOrEmpty(dataDirectory))
                dataDirectory = defaultDataDirectory;

            var activeDatabase = configEditor.GetValue("dbms.active_database");
            if (string.IsNullOrEmpty(activeDatabase))
                activeDatabase = defaultActiveDatabase;

            return Path.Combine(neo4jHomeFolder, dataDirectory, activeDatabase);
        }
    }
}
