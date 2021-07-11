using System;
using System.Collections.Generic;



namespace Gabriel.Cat.S.Check
{
    public interface IFile
    {
        string Name { get; }
        Uri Picture { get; }
        IEnumerable<Link> GetLinks();
    }
}
