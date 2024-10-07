namespace ScriptingSurvey.Helpers;
public static class PathHelper
{
	public static readonly string SubstUp = "##";

	public static string? FindDirectory(string basePath, string relPath)
	{
		if (!relPath.Contains(SubstUp))
			return Path.Combine(basePath, relPath);

		var subst = "";
		var aspt = "";
		do
		{
			var _rel = relPath.Replace(SubstUp, subst);
			var _aspt = Path.GetFullPath(Path.Combine(basePath, _rel));
			if (aspt == _aspt) return null;
			aspt = _aspt;
			subst += $"..{Path.DirectorySeparatorChar}";
		} while (!Directory.Exists(aspt));
		return aspt;
	}

	public static string? FindDirectory(string relPath) => FindDirectory(Directory.GetCurrentDirectory(), relPath);
}
