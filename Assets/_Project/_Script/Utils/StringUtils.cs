public static class StringUtils
{

    public static string GetReducedPath(string path, string extension, int separatorCount, string separator = "/")
    {
        for (int i = 0; i < separatorCount; i++)
            path = path.Remove(0, path.IndexOf(separator) + 1);

        return path.Replace(extension, "");
    }

}
