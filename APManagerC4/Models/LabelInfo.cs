﻿using Uid = HM.Common.Uid;

namespace APManagerC4.Models
{
    public record class LabelInfo
    {
        public Uid Uid
        {
            get => _guid;
            init
            {
                _guid = value;
            }
        }
        public string Title
        {
            get => _title;
            init
            {
                _title = value;
            }
        }
        public string Category
        {
            get => _category;
            init
            {
                _category = value;
            }
        }

        private readonly Uid _guid;
        private readonly string _title = string.Empty;
        private readonly string _category = string.Empty;
    }
}
