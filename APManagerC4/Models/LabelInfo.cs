namespace APManagerC4.Models
{
    public record class LabelInfo
    {
        public Guid Guid
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
        public string GroupName
        {
            get => _groupName;
            init
            {
                _groupName = value;
            }
        }

        private readonly Guid _guid;
        private readonly string _title = string.Empty;
        private readonly string _groupName = string.Empty;
    }
}
