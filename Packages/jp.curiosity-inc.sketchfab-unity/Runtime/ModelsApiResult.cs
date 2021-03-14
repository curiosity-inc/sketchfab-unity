using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;

namespace Curiosity.Sketchfab
{
    [Serializable]
    public class ModelsApiResult
    {
        public string next;
        public string previous;
        public Model[] results;

        public override string ToString()
        {
            return String.Join(",", results.Select((r) => r.ToString()));
        }
    }

    [Serializable]
    public class DownloadModelResult
    {
        public DownloadInfo gltf;
        public DownloadInfo usdz;
    }

    [Serializable]
    public class DownloadInfo
    {
        public string url;
        public int expires;
    }

    [Serializable]
    public class Model
    {
        public int viewCount;
        public string uid;
        public string name;
        public int animationCount;
        public string viewerUrl;
        public bool isPublished;
        public bool isDownloadable;
        public Archives archives;
        public Thumbnails thumbnails;
        public User user;

        public override string ToString()
        {
            return $"(name={name}, user={user.displayName}, url={viewerUrl})";
        }
    }

    [Serializable]
    public class Thumbnails
    {
        public Image[] images;
    }

    [Serializable]
    public class Image
    {
        public string url;
        public int width;
        public int height;
    }

    [Serializable]
    public class Archives
    {
        public Archive gltf;
        public Archive fbx;
    }

    [Serializable]
    public class Archive
    {
        public int size;
    }

    [Serializable]
    public class User
    {
        public string username;
        public string profileUrl;
        public string displayName;
        public string account;
    }
}
