using System;

namespace Extension
{
    /// <summary>
    /// An image moniker that provides the Codiga logo for e.g. shortcut completion. 
    /// </summary>
    /// <seealso cref="https://marketplace.visualstudio.com/items?itemName=MadsKristensen.ImageManifestTools64"/>
    /// <seealso cref="https://stackoverflow.com/questions/55972983/how-to-use-an-imagemoniker-from-a-imagemanifest-in-an-isuggestedaction"/>
    public readonly struct CodigaImageMoniker
    {
        public static readonly CodigaImageMoniker CodigaMoniker = new CodigaImageMoniker(new Guid("6D3D72C2-E711-4D74-A042-F2DBBA710896"), 263442);

        public CodigaImageMoniker(Guid guid, int id)
        {
            Guid = guid;
            Id = id;
        }

        public Guid Guid { get; }
        public int Id { get; }
    }
}