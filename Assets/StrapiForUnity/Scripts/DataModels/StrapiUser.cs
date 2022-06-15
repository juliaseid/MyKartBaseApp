﻿using System;
 using UnityEngine;

 namespace StrapiForUnity
{
    [Serializable]
    public class StrapiUser 
    {
        public int id;
        public string username;
        public string email;
        public string provider;
        public bool confirmed;
        [SerializeField]
        private string created_at;
        private DateTime? _createdAt;
        public DateTime? CreatedAt ()
        {
            if (_createdAt == null) {
                _createdAt = Convert.ToDateTime (created_at);
            }
            return _createdAt;
        }
        [SerializeField]
        private string updated_at;
        private DateTime? _updatedAt;
        public DateTime? UpdatedAt ()
        {
            if (_updatedAt == null) {
                _updatedAt = Convert.ToDateTime (updated_at);
            }
            return _updatedAt;
        }
    }
}
