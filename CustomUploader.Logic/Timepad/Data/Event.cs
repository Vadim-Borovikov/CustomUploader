using System;

namespace CustomUploader.Logic.Timepad.Data
{
    public class Event
    {
        public int Id { get; set; }
        public DateTime StartsAt { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public PosterImage PosterImage { get; set; }
    }
}