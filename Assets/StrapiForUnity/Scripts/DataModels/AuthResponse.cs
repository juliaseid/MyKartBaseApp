using System;

namespace StrapiForUnity
{
    [Serializable]
    public class AuthResponse
    {
        public string jwt;
        public StrapiUser user;
    }
}