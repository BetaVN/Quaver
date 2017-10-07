﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quaver.Beatmaps;
using Quaver.Config;
using Quaver.QuaFile;
using SQLite;

namespace Quaver.Database
{
    internal static class BeatmapCache
    {
        /// <summary>
        ///     The name of the module - for logging purposes.
        /// </summary>
        private static readonly string Module = "[BEATMAP CACHE]";

        /// <summary>
        ///     The path of the local database
        /// </summary>
        private static readonly string DatabasePath = Configuration.GameDirectory + "/quaver.db";

        /// <summary>
        ///     Initializes and loads the beatmap database
        /// </summary>
        /// <returns></returns>
        internal static async Task<List<Beatmap>> LoadBeatmapDatabaseAsync()
        {
            try
            {
                // Create and sync the database.
                await CreateBeatmapTableAsync();
                await SyncBeatmapDatabaseAsync();

                // Fetch all the new beatmaps after syncing and return them.
                var beatmaps = await FetchAllBeatmaps();
                Console.WriteLine($"{Module} {beatmaps.Count} beatmaps have been successfully loaded.");

                return beatmaps;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                File.Delete(DatabasePath);
                return await LoadBeatmapDatabaseAsync();
            }            
        }

        /// <summary>
        ///     Create the beatmap table. If there is an issue, we'll delete the database fully, and create it again.
        /// </summary>
        private static async Task CreateBeatmapTableAsync()
        {
            var conn = new SQLiteAsyncConnection(DatabasePath);
            await conn.CreateTableAsync<Beatmap>();
            Console.WriteLine($"{Module} Beatmap DB has been created.");
        }
        
        /// <summary>
        ///     Completely syncs our beatmap database. It will call our other helper methods, and makes sure
        ///     that maps are always up-to-date in the database.
        /// </summary>
        /// <returns></returns>
        private static async Task SyncBeatmapDatabaseAsync()
        {
            // Find all the.qua files in the directory.
            var quaFiles = Directory.GetFiles(Configuration.SongDirectory, "*.qua", SearchOption.AllDirectories);
            Console.WriteLine($"{Module} Found: {quaFiles.Length} .qua files in the /songs/ directory.");

            await CacheByFileCount(quaFiles);
            await CacheByMd5ChecksumAsync(quaFiles);
        }

        /// <summary>
        ///     Compares the amount of files on the file system vs. that of in the database.
        ///     It will add any of them that it finds aren't in there.
        /// </summary>
        /// <param name="quaFiles"></param>
        /// <returns></returns>
        private static async Task CacheByFileCount(string[] quaFiles)
        {
            var beatmapsInDb = await FetchAllBeatmaps();

            // We only want to add more beatmaps here if the counts don't add up.
            if (beatmapsInDb.Count == quaFiles.Length)
                return;

            Console.WriteLine($"{Module} Incorrect # of .qua files vs maps detected. {quaFiles.Length} vs {beatmapsInDb.Count}");

            // This'll store all the beatmaps we'll be adding into the database.
            var beatmapsToCache = new List<Beatmap>();

            // Parse & add each beatmap to the list of maps to cache if they aren't already in the database.
            foreach (var file in quaFiles)
            {
                // Run a check to see if the beatmap path already exists in the database.
                if (beatmapsInDb.Any(beatmap => beatmap.Path.Replace("\\", "/") == file.Replace("\\", "/"))) continue;

                // Try to parse the file and check if it is a legitimate .qua file.
                var qua = await Qua.Create(file);
                if (!qua.IsValidQua)
                {
                    Console.WriteLine($"{Module} Error: Qua File {file} could not be parsed.");
                    File.Delete(file);
                    continue;
                }

                // Convert the Qua into a Beatmap object and add it to our list of maps we want to cache.
                var newBeatmap = new Beatmap().ConvertQuaToBeatmap(qua, file);
                newBeatmap.Path = newBeatmap.Path.Replace("\\", "/");
                beatmapsToCache.Add(newBeatmap);
            }

            // Add beatmaps to database.
            if (beatmapsToCache.Count > 0)
                await InsertBeatmapsIntoDatabase(beatmapsToCache);
        }

        /// <summary>
        ///     Compares the beatmaps MD5 checksums in that of the database and the file system, 
        ///     and updates/adds/removes them from the database.
        /// </summary>
        /// <returns></returns>
        private static async Task CacheByMd5ChecksumAsync(IEnumerable<string> quaFiles)
        {
            // This'll hold all of the MD5 Checksums of the .qua files in the directory.
            // Since this is an updated list, we'll use these to check if they are in the database and unchanged.
            var fileChecksums = new List<string>();
            quaFiles.ToList().ForEach(qua => fileChecksums.Add(Beatmap.GetMd5Checksum(qua)));

            // Find all the beatmaps in the database
            var beatmapsInDb = await FetchAllBeatmaps();

            // Find all the mismatched beatmaps.
            var mismatchedBeatmaps = beatmapsInDb
                .Except(beatmapsInDb.Where(map => fileChecksums.Any(md5 => md5 == map.Md5Checksum)).ToList()).ToList();

            Console.WriteLine($"{Module} Found: {mismatchedBeatmaps.Count} beatmaps with unmatched checksums");
            if (mismatchedBeatmaps.Count > 0)
                await DeleteBeatmapsFromDatabase(mismatchedBeatmaps);

            // Stores the list of beatmaps that were successfully reprocessed and are ready to add into the DB.
            var reprocessedBeatmaps = new List<Beatmap>();
            foreach (var map in mismatchedBeatmaps)
            {
                if (!File.Exists(map.Path))
                    continue;

                // Parse the map again and add it to the list of maps to be added to the database.
                var processedMap = new Beatmap().ConvertQuaToBeatmap(new Qua(map.Path), map.Path);
                if (!processedMap.IsValidBeatmap)
                    return;

                reprocessedBeatmaps.Add(processedMap);
            }

            // Add new beatmaps to the database.
            if (reprocessedBeatmaps.Count > 0)
                await InsertBeatmapsIntoDatabase(reprocessedBeatmaps);
        }

        /// <summary>
        ///     Responsible for fetching all the beatmaps from the database and returning them.
        /// </summary>
        /// <returns></returns>
        private static async Task<List<Beatmap>> FetchAllBeatmaps()
        {
            return await new SQLiteAsyncConnection(DatabasePath).Table<Beatmap>().ToListAsync().ContinueWith(t => t.Result);
        }

        /// <summary>
        ///     Responsible for taking a list of beatmaps from the database and adding them to it.
        /// </summary>
        /// <param name="beatmaps"></param>
        /// <returns></returns>
        private static async Task InsertBeatmapsIntoDatabase(List<Beatmap> beatmaps)
        {
            await new SQLiteAsyncConnection(DatabasePath).InsertAllAsync(beatmaps)
                .ContinueWith(t => Console.WriteLine($"{Module} Successfully added {beatmaps.Count} beatmaps to the database"));
        }

        /// <summary>
        ///     Responsible for removing a list of beatmaps from the database.
        /// </summary>
        /// <param name="beatmaps"></param>
        /// <returns></returns>
        private static async Task DeleteBeatmapsFromDatabase(List<Beatmap> beatmaps)
        {
            try
            {
                foreach (var map in beatmaps)
                {
                    await new SQLiteAsyncConnection(DatabasePath).DeleteAsync(map);
                }

                Console.WriteLine($"{Module} Successfully deleted {beatmaps.Count} beatmaps from the database.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
    }
}