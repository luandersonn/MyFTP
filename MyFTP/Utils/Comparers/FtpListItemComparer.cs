using MyFTP.ViewModels;
using System.Collections.Generic;

namespace Utils.Comparers
{
	public class FtpListItemComparer : IComparer<FtpListItemViewModel>
	{
		public int Compare(FtpListItemViewModel x, FtpListItemViewModel y)
		{
			// Directories first
			if (x.IsDirectory && !y.IsDirectory)
				return -1; 
			else if (!x.IsDirectory && y.IsDirectory)
				return 1;
			else
				return x.Name.CompareTo(y.Name);
		}
	}
}
