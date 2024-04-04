﻿// 
// ItemOperations.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012-2014 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Threading.Tasks;
using EnvDTE;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement.EnvDTE
{
	public class ItemOperations : MarshalByRefObject, global::EnvDTE.ItemOperations
	{
		public ItemOperations ()
		{
		}

		public void OpenFile (string fileName)
		{
			Runtime.RunInMainThread (async () => {
				await OpenFileAsync (new FilePath (fileName));
			}).Wait ();
		}

		Task OpenFileAsync (FilePath filePath)
		{
			return IdeApp.Workbench.OpenDocument (filePath, null, true);
		}

		public void Navigate (string url)
		{
			Runtime.RunInMainThread (() => {
				IdeServices.DesktopService.ShowUrl (url);
			}).Wait ();
		}
		
		public global::EnvDTE.Window NewFile (string fileName)
		{
			throw new NotImplementedException();
		}

		public global::EnvDTE.Window OpenFile (string FileName, string ViewKind = "{00000000-0000-0000-0000-000000000000}")
		{
			throw new NotImplementedException ();
		}

		public global::EnvDTE.Window NewFile (string Item = "General\\Text File", string Name = "", string ViewKind = "{00000000-0000-0000-0000-000000000000}")
		{
			throw new NotImplementedException ();
		}

		public bool IsFileOpen (string FileName, string ViewKind = "{FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF}")
		{
			throw new NotImplementedException ();
		}

		public global::EnvDTE.ProjectItem AddExistingItem (string FileName)
		{
			throw new NotImplementedException ();
		}

		public global::EnvDTE.ProjectItem AddNewItem (string Item = "General\\Text File", string Name = "")
		{
			throw new NotImplementedException ();
		}

		public global::EnvDTE.Window Navigate (string URL = "", vsNavigateOptions Options = vsNavigateOptions.vsNavigateOptionsDefault)
		{
			throw new NotImplementedException ();
		}

		public global::EnvDTE.DTE DTE => throw new NotImplementedException ();

		public global::EnvDTE.DTE Parent => throw new NotImplementedException ();

		public vsPromptResult PromptToSave => throw new NotImplementedException ();
	}
}
