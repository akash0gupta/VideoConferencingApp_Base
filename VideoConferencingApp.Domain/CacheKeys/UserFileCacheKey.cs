using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.CacheKeys
{
    public class UserFileCacheKey
    {
        public const string PrefixRawFi = "user_files";
        public const string PrefixRawFo = "user_folders";
        public const string PrefixRawStats = "user_file_stats";

        public static string PrefixFi => PrefixRawFi;
        public static string PrefixFo => PrefixRawFo;
        public static string PrefixStats => PrefixRawStats;

        public static CacheKey UserFilesByUserId(long id) => new($"{PrefixFi}.{id}");
        public static CacheKey UserFoldersByUserId(long id) => new($"{PrefixFo}.{id}");
        public static CacheKey UserStatisticsByUserId(long id) => new($"{PrefixStats}.{id}");

        public static CacheKey UserFilesSearch(long userId, int searchHash) =>
            new($"{PrefixFi}.search.{userId}.{searchHash}");
    }
}
