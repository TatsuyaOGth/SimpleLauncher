using System;
using CommandLine;

namespace SimpleLauncherCli
{
	public class Options
	{
		[Option('p', "parameter", Required = false, HelpText = "Path to parameter file")]
		public string? ParameterFilePath { get; set; } = null;

		[Option('d', "display-type", HelpText = "Display type for user selection. (FileName, DirectoryName)")]
		public DisplayType DisplayType { get; set; } = DisplayType.FileName;
	}

	public enum DisplayType
	{
		FileName,
		DirectoryName
	}
}

