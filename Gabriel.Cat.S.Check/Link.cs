using System;



namespace Gabriel.Cat.S.Check
{
    public class Link
    {
        public string Url { get; set; }
        public string TextoAntes { get; set; } = String.Empty;
        public string TextoDespues { get; set; } = String.Empty;

        public static implicit operator Link(string url)
        {
            return new Link() { Url = url };
        }
    }
}
