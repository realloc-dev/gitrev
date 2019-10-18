/*
 * GitRev - Command line tool for Windows to build revision information text file from a git repository.
 *
 * <https://github.com/realloc-dev/gitrev>
 *
 * The working is based on TortoiseSVN's SubWCRev program (https://tortoisesvn.net/docs/release/TortoiseSVN_en/tsvn-subwcrev.html)
 * Initial code stolen from <https://bitbucket.org/JPlenert/mercurialrev/wiki/Home>
 *
 * Copyright (c) 2019 realloc.dev
 *
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace GitRev
{
	public class Program
	{
		private string repositoryPath;

		private string sourceFilename;
		private string destFilename;

		private string gitRev;
		private string gitID;
		private string gitBranch;
		private string gitTag;

		public Program(string[] args)
		{
			ParseArgs(args);
		}

		public enum ErrorType
		{
			  None
			, Argument
			, Git
			, SourceFile
			, DestFile
			, HandleFile
		}

		public static int Main(string[] args)
		{
			Program prg = new Program(args);
			return prg.Run();
		}

		private int Run()
		{
			Console.WriteLine("GitRev v" + GetVersionString() + " by realloc.dev (https://github.com/realloc-dev/gitrev)");
			Console.WriteLine("");

			if ( sourceFilename == null || destFilename == null || repositoryPath == null )
			{
				return ReturnWithError(ErrorType.Argument);
			}

			// Got to the correct directory
			var prevDir = Directory.GetCurrentDirectory();
			string workingPath = repositoryPath;
			if ( !Path.IsPathRooted(repositoryPath) )
			{
				var x = Path.Combine(prevDir, repositoryPath);
				workingPath = Path.GetFullPath((new Uri(x)).LocalPath);
			}

			Directory.SetCurrentDirectory(workingPath);
			{

				// Get info from git
				gitID     = GetGitInfo("log -n1 --format=\"%h\"");
				gitRev    = GetGitInfo("rev-list --count --all");  // Of course this is not 'exact', but close enough if you always build from the same repo.
				gitBranch = GetGitInfo("rev-parse --abbrev-ref HEAD");
				gitTag    = GetGitInfo("describe --all --tags HEAD");
			}
			Directory.SetCurrentDirectory(prevDir);

			if ( gitID == null || gitRev == null || gitBranch == null || gitTag == null )
			{
				return ReturnWithError(ErrorType.Git);
			}

			var now             = DateTime.Now.ToUniversalTime();
			string nowIsoFormat = now.ToString("yyyy-MM-ddTHH:mm:ssK");
			string nowDateOnly  = now.ToString("yyyy-MM-dd");
			string nowYearOnly  = now.ToString("yyyy");

			string txt = null;
			try
			{
				txt = File.ReadAllText(sourceFilename);
			}
			catch
			{
				return ReturnWithError(ErrorType.SourceFile);
			}

			if ( txt != null )
			{
				txt = txt.Replace("$WCREVNUM$", gitRev);
				txt = txt.Replace("$WCREVID$", gitID);
				txt = txt.Replace("$WCBRANCH$", gitBranch);
				txt = txt.Replace("$WCTAG", gitTag);
				txt = txt.Replace("$WCREV$", gitRev);
				txt = txt.Replace("$WCDATE$", nowIsoFormat);
				txt = txt.Replace("$WCDATE2$", nowDateOnly);
				txt = txt.Replace("$WCYEAR$", nowYearOnly);
			}

			try
			{
				File.WriteAllText(destFilename, txt);
			}
			catch
			{
				return ReturnWithError(ErrorType.HandleFile);
			}

			return ReturnWithError(ErrorType.None);
		}

		private void ParseArgs(string[] args)
		{
			if ( args.Length == 3 )
			{
				repositoryPath = args[0];
				sourceFilename = args[1];
				destFilename = args[2];

				// Repository path should not end in "\"
				if ( repositoryPath.EndsWith("\\") )
				{
					repositoryPath = repositoryPath.Substring(0, repositoryPath.Length - 1);
				}
			}
		}

		private string GetGitInfo(string arg)
		{
			Process proc = new Process();
			proc.StartInfo.FileName = "git";
			proc.StartInfo.Arguments = string.Format(" {0}", arg);
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;

			proc.Start();

			return proc.StandardOutput.ReadLine();
		}

		private string GetErrorDescription(ErrorType error)
		{
			switch ( error )
			{
				case ErrorType.Argument: return "Incorrect number of arguments";
				case ErrorType.Git: return "Git error";
				case ErrorType.SourceFile: return "Source file not found";
				case ErrorType.DestFile: return "Destination file could not be written";
				case ErrorType.HandleFile: return "Handling files";
				default: return "Unexpected error";
			}
		}

		private string GetVersionString()
		{
			return Assembly.GetExecutingAssembly().GetName().Version.ToString();
		}
		private int ReturnWithError(ErrorType error)
		{
			if ( error != ErrorType.None )
			{
				Console.WriteLine("Replaces revision information in a tagged text file.");
				Console.WriteLine("");
				Console.WriteLine("GitRev <RepositoryPath> <SourceFile> <DestinationFile>");
				Console.WriteLine("");
				Console.WriteLine("Tags:");
				Console.WriteLine("    $WCREVNUM$        Revision number");
				Console.WriteLine("    $WCREVID$         Short revision hash");
				Console.WriteLine("    $WCBRANCH$        Current branch");
				Console.WriteLine("    $WCTAG$           Tag (version) of working copy");
				Console.WriteLine("    $WCREV$           Working copy revision");
				Console.WriteLine("    $WCDATE$          Current date in ISO format");
				Console.WriteLine("    $WCDATE2$         Current date in yyyy-MM-dd format");
				Console.WriteLine("    $WCYEAR$          Current year");
				Console.WriteLine("");

				Console.WriteLine("Error: " + GetErrorDescription(error));
			}
			else
			{
				Console.WriteLine("Done!");
			}

			return (error == ErrorType.None) ? 0 : 1;
		}
	}
}

