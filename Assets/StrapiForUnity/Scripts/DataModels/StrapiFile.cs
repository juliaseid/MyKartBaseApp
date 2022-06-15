using System;
using System.Collections.Generic;

namespace StrapiForUnity
{
    [Serializable]
    public class StrapiFile
    {
        public int id;
        public string name;
        public string ext;
        public double size;
        public string url;
        public string created_at;
        public string updated_at;
        public string mime;
        public string previewUrl;
        public string provider;
        public int width;
        public int height;
        public StrapiFileFormat formats;
    }
}