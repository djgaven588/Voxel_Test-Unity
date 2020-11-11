namespace Modding
{
    public struct ModVersion
    {
        public int Major;
        public int Minor;
        public int Patch;

        public ModVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public bool IsEqualOrAbove(ModVersion version)
        {
            return Major >= version.Major && Minor >= version.Minor && Patch >= version.Patch;
        }
    }
}
