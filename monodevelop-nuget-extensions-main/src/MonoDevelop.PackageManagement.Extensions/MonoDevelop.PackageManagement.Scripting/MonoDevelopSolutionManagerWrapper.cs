﻿//
// MonoDevelopSolutionManagerWrapper.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	/// <summary>
	/// Wraps the IMonoDevelopSolutionManager so the SaveProject can handle saving a
	/// project used by the ConsoleHostMSBuildNuGetProjectSystem. Using the solution
	/// manager provided by the NuGet addin results in null reference exception when
	/// the PowerShell console is used to add or remove a NuGet package.
	/// </summary>
	class MonoDevelopSolutionManagerWrapper : IMonoDevelopSolutionManager
	{
		readonly IMonoDevelopSolutionManager solutionManager;

		public MonoDevelopSolutionManagerWrapper (IMonoDevelopSolutionManager solutionManager)
		{
			this.solutionManager = solutionManager;
		}

		public Task<bool> IsSolutionAvailableAsync ()
		{
			return solutionManager.IsSolutionAvailableAsync ();
		}

		public bool IsSolutionOpen {
			get { return solutionManager.IsSolutionOpen; }
		}

		public INuGetProjectContext NuGetProjectContext {
			get { return solutionManager.NuGetProjectContext; }
			set { solutionManager.NuGetProjectContext = value; }
		}

		public ISettings Settings {
			get { return solutionManager.Settings; }
		}

		public string SolutionDirectory {
			get { return solutionManager.SolutionDirectory; }
		}

		public Solution Solution {
			get { return solutionManager.Solution; }
		}

		public ConfigurationSelector Configuration {
			get { return solutionManager.Configuration; }
		}

		#pragma warning disable 67
		public event EventHandler<ActionsExecutedEventArgs> ActionsExecuted;
		public event EventHandler<NuGetProjectEventArgs> AfterNuGetProjectRenamed;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectAdded;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectRemoved;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectRenamed;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectUpdated;
		public event EventHandler<NuGetEventArgs<string>> AfterNuGetCacheUpdated;
		public event EventHandler SolutionClosed;
		public event EventHandler SolutionClosing;
		public event EventHandler SolutionOpened;
		public event EventHandler SolutionOpening;
		#pragma warning restore 67

		public ISourceRepositoryProvider CreateSourceRepositoryProvider ()
		{
			return solutionManager.CreateSourceRepositoryProvider ();
		}

		public Task<NuGetProject> GetNuGetProjectAsync (string nuGetProjectSafeName)
		{
			return solutionManager.GetNuGetProjectAsync (nuGetProjectSafeName);
		}

		public NuGetProject GetNuGetProject (IDotNetProject project)
		{
			return solutionManager.GetNuGetProject (project);
		}

		public Task<IEnumerable<NuGetProject>> GetNuGetProjectsAsync ()
		{
			return solutionManager.GetNuGetProjectsAsync ();
		}

		public Task<string> GetNuGetProjectSafeNameAsync (NuGetProject nuGetProject)
		{
			return solutionManager.GetNuGetProjectSafeNameAsync (nuGetProject);
		}

		public void OnActionsExecuted (IEnumerable<ResolvedAction> actions)
		{
			solutionManager.OnActionsExecuted (actions);
		}

		public void ReloadSettings ()
		{
			solutionManager.ReloadSettings ();
		}

		public void SaveProject (NuGetProject nuGetProject)
		{
			var hasProject = nuGetProject as IHasDotNetProject;
			if (hasProject != null) {
				hasProject.SaveProject ().Wait ();
				return;
			}

			throw new ApplicationException (string.Format ("Unsupported NuGetProject type: {0}", nuGetProject.GetType ().FullName));
		}

		public void ClearProjectCache ()
		{
			solutionManager.ClearProjectCache ();
		}

		public void EnsureSolutionIsLoaded ()
		{
			solutionManager.EnsureSolutionIsLoaded ();
		}

		public Task<bool> DoesNuGetSupportsAnyProjectAsync ()
		{
			return solutionManager.DoesNuGetSupportsAnyProjectAsync ();
		}
	}
}
