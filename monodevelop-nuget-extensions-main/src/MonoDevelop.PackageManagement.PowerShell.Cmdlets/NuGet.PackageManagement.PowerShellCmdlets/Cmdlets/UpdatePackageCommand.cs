﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using MonoDevelop.PackageManagement;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using NuGet.VisualStudio;

namespace NuGet.PackageManagement.PowerShellCmdlets
{
	[Cmdlet (VerbsData.Update, "Package", DefaultParameterSetName = "All")]
	public class UpdatePackageCommand : PackageActionBaseCommand
	{
		UninstallationContext uninstallcontext;
		string id;
		string projectName;
		bool idSpecified;
		bool projectSpecified;
		bool versionSpecifiedPrerelease;
		bool allowPrerelease;
		NuGetVersion nugetVersion;

		[Parameter (Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Project")]
		[Parameter (ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "All")]
		[Parameter (ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Reinstall")]
		public override string Id {
			get { return id; }
			set {
				id = value;
				idSpecified = true;
			}
		}

		[Parameter (Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "All")]
		[Parameter (Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "Project")]
		[Parameter (Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "Reinstall")]
		public override string ProjectName {
			get { return projectName; }
			set {
				projectName = value;
				projectSpecified = true;
			}
		}

		[Parameter (Position = 2, ParameterSetName = "Project")]
		[ValidateNotNullOrEmpty]
		public override string Version { get; set; }

		[Parameter]
		[Alias ("ToHighestPatch")]
		public SwitchParameter Safe { get; set; }

		[Parameter]
		public SwitchParameter ToHighestMinor { get; set; }

		[Parameter (Mandatory = true, ParameterSetName = "Reinstall")]
		[Parameter (ParameterSetName = "All")]
		public SwitchParameter Reinstall { get; set; }

		List<NuGetProject> Projects { get; set; }

		protected override void Preprocess ()
		{
			base.Preprocess ();
			ParseUserInputForVersion ();
			if (!projectSpecified) {
				NuGetUIThreadHelper.JoinableTaskFactory.Run (async () => {
					var projects = await SolutionManager.GetAllNuGetProjectsAsync ();
					Projects = projects.ToList ();
				});
			} else {
				Projects = new List<NuGetProject> { Project };
			}

			if (Reinstall) {
				ActionType = NuGetActionType.Reinstall;
			} else {
				ActionType = NuGetActionType.Update;
			}
		}

		protected override void ProcessRecordCore ()
		{
			Preprocess ();
			NuGetUIThreadHelper.JoinableTaskFactory.Run (() => {
				WarnIfParametersAreNotSupported ();

				// Update-Package without ID specified
				if (!idSpecified) {
					Task.Run (UpdateOrReinstallAllPackagesAsync).Forget ();
				}
				// Update-Package with Id specified
				else {
					Task.Run (UpdateOrReinstallSinglePackageAsync).Forget ();
				}

				WaitAndLogPackageActions ();

				return Task.FromResult (true);
			});
		}

		protected override void WarnIfParametersAreNotSupported ()
		{
			if (Source != null) {
				var projectNames = string.Join (",", Projects.Select (p => p.GetUniqueName ()));
				if (!string.IsNullOrEmpty (projectNames)) {
					var warning = string.Format (
						CultureInfo.CurrentUICulture,
						"The '{0}' parameter is not respected for the transitive package management based project(s) {1}. The enabled sources in your NuGet configuration will be used.",
						nameof (Source),
						projectNames);
					Log (MessageLevel.Warning, warning);
				}
			}
		}

		void WarnForReinstallOfBuildIntegratedProjects (IEnumerable<BuildIntegratedNuGetProject> projects)
		{
			if (projects.Any ()) {
				var projectNames = string.Join (",", projects.Select (p => p.GetUniqueName ()));
				var warning = string.Format (CultureInfo.CurrentCulture, "The `-Reinstall` parameter does not apply to PackageReference based projects '{0}'.", projectNames);
				Log (MessageLevel.Warning, warning);
			}
		}

		/// <summary>
		/// Update or reinstall all packages installed to a solution. For Update-Package or Update-Package -Reinstall.
		/// </summary>
		async Task UpdateOrReinstallAllPackagesAsync ()
		{
			try {
				using (var sourceCacheContext = new SourceCacheContext ()) {
					var resolutionContext = new ResolutionContext (
						GetDependencyBehavior (),
						allowPrerelease,
						ShouldAllowDelistedPackages (),
						DetermineVersionConstraints (),
						new GatherCache (),
						sourceCacheContext);

					// PackageReference projects don't support `Update-Package -Reinstall`. 
					List<NuGetProject> applicableProjects = GetApplicableProjectsAndWarnForRest (Projects);

					// if the source is explicitly specified we will use exclusively that source otherwise use ALL enabled sources
					var actions = await PackageManager.PreviewUpdatePackagesAsync (
						applicableProjects,
						resolutionContext,
						this,
						PrimarySourceRepositories,
						PrimarySourceRepositories,
						Token);

					await ExecuteActions (actions, sourceCacheContext);
				}
			} catch (SignatureException ex) {

				if (!string.IsNullOrEmpty (ex.Message)) {
					Log (ex.AsLogMessage ());
				}

				if (ex.Results != null) {
					var logMessages = ex.Results.SelectMany (p => p.Issues).ToList ();

					logMessages.ForEach (p => Log (ex.AsLogMessage ()));
				}
			} catch (Exception ex) {
				Log (MessageLevel.Error, ExceptionUtilities.DisplayMessage (ex));
			} finally {
				BlockingCollection.Add (new ExecutionCompleteMessage ());
			}
		}

		List<NuGetProject> GetApplicableProjectsAndWarnForRest (List<NuGetProject> applicableProjects)
		{
			if (Reinstall.IsPresent) {
				var buildIntegratedProjects = new List<NuGetProject> ();
				var nonBuildIntegratedProjects = new List<NuGetProject> ();

				foreach (var project in applicableProjects) {
					if (project is BuildIntegratedNuGetProject buildIntegratedNuGetProject) {
						buildIntegratedProjects.Add (buildIntegratedNuGetProject);
					} else {
						nonBuildIntegratedProjects.Add (project);
					}
				}

				if (buildIntegratedProjects != null && buildIntegratedProjects.Any ()) {
					WarnForReinstallOfBuildIntegratedProjects (buildIntegratedProjects.AsEnumerable ().Cast<BuildIntegratedNuGetProject> ());
				}

				return nonBuildIntegratedProjects;
			}

			return applicableProjects;
		}

		/// <summary>
		/// Update or reinstall a single package installed to a solution. For Update-Package -Id or Update-Package -Id
		/// -Reinstall.
		/// </summary>
		async Task UpdateOrReinstallSinglePackageAsync ()
		{
			try {
				var isPackageInstalled = await IsPackageInstalledAsync (Id);

				if (isPackageInstalled) {
					await PreviewAndExecuteUpdateActionsForSinglePackage ();
				} else {
					Log (MessageLevel.Error, "'{0}' was not installed in any project. Update failed.", Id);
				}
			} catch (SignatureException ex) {
				if (!string.IsNullOrEmpty (ex.Message)) {
					Log (ex.AsLogMessage ());
				}

				if (ex.Results != null) {
					var logMessages = ex.Results.SelectMany (p => p.Issues).ToList ();

					logMessages.ForEach (p => Log (p));
				}
			} catch (Exception ex) {
				Log (MessageLevel.Error, ExceptionUtilities.DisplayMessage (ex));
			} finally {
				BlockingCollection.Add (new ExecutionCompleteMessage ());
			}
		}

		/// <summary>
		/// Preview update actions for single package
		/// </summary>
		async Task PreviewAndExecuteUpdateActionsForSinglePackage ()
		{
			var actions = Enumerable.Empty<NuGetProjectAction> ();

			using (var sourceCacheContext = new SourceCacheContext ()) {
				var resolutionContext = new ResolutionContext (
					GetDependencyBehavior (),
					allowPrerelease,
					ShouldAllowDelistedPackages (),
					DetermineVersionConstraints (),
					new GatherCache (),
					sourceCacheContext);

				// PackageReference projects don't support `Update-Package -Reinstall`. 
				List<NuGetProject> applicableProjects = GetApplicableProjectsAndWarnForRest (Projects);

				// If -Version switch is specified
				if (!string.IsNullOrEmpty (Version)) {
					actions = await PackageManager.PreviewUpdatePackagesAsync (
						new PackageIdentity (Id, PowerShellCmdletsUtility.GetNuGetVersionFromString (Version)),
						applicableProjects,
						resolutionContext,
						this,
						PrimarySourceRepositories,
						EnabledSourceRepositories,
						Token);
				} else {
					actions = await PackageManager.PreviewUpdatePackagesAsync (
						Id,
						applicableProjects,
						resolutionContext,
						this,
						PrimarySourceRepositories,
						EnabledSourceRepositories,
						Token);
				}

				await ExecuteActions (actions, sourceCacheContext);
			}
		}

		/// <summary>
		/// Method checks if the package to be updated is installed in any package or not.
		/// </summary>
		/// <param name="packageId">Id of the package to be updated/checked</param>
		/// <returns><code>bool</code> indicating whether the package is already installed, on any project, or not</returns>
		async Task<bool> IsPackageInstalledAsync (string packageId)
		{
			foreach (var project in Projects) {
				var installedPackages = await project.GetInstalledPackagesAsync (Token);

				if (installedPackages.Select (installedPackage => installedPackage.PackageIdentity.Id)
					.Any (installedPackageId => installedPackageId.Equals (packageId, StringComparison.OrdinalIgnoreCase))) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Execute the project actions
		/// </summary>
		async Task ExecuteActions (IEnumerable<NuGetProjectAction> actions, SourceCacheContext sourceCacheContext)
		{
			if (!ShouldContinueDueToDotnetDeprecation (actions, WhatIf.IsPresent)) {
				return;
			}

			if (WhatIf.IsPresent) {
				// For -WhatIf, only preview the actions
				PreviewNuGetPackageActions (actions);
			} else {
				// Execute project actions by Package Manager
				await PackageManager.ExecuteNuGetProjectActionsAsync (Projects, actions, this, sourceCacheContext, Token);
			}
		}

		/// <summary>
		/// Parse user input for -Version switch
		/// </summary>
		void ParseUserInputForVersion ()
		{
			if (!string.IsNullOrEmpty (Version)) {
				// If Version is prerelease, automatically allow prerelease (i.e. append -Prerelease switch).
				nugetVersion = PowerShellCmdletsUtility.GetNuGetVersionFromString (Version);
				if (nugetVersion.IsPrerelease) {
					versionSpecifiedPrerelease = true;
				}
			}
			allowPrerelease = IncludePrerelease.IsPresent || versionSpecifiedPrerelease;
		}

		/// <summary>
		/// Uninstallation Context for Update-Package -Reinstall command
		/// </summary>
		public UninstallationContext UninstallContext {
			get {
				uninstallcontext = new UninstallationContext (false, Reinstall.IsPresent);
				return uninstallcontext;
			}
		}

		/// <summary>
		/// Return dependecy behavior for Update-Package command.
		/// </summary>
		protected override DependencyBehavior GetDependencyBehavior ()
		{
			// Return DependencyBehavior.Highest for Update-Package
			if (!idSpecified
				&& !Reinstall.IsPresent) {
				return DependencyBehavior.Highest;
			}

			return base.GetDependencyBehavior ();
		}

		/// <summary>
		/// Determine the UpdateConstraints based on the command line arguments
		/// </summary>
		VersionConstraints DetermineVersionConstraints ()
		{
			if (Reinstall.IsPresent) {
				return VersionConstraints.ExactMajor | VersionConstraints.ExactMinor | VersionConstraints.ExactPatch | VersionConstraints.ExactRelease;
			} else if (Safe.IsPresent) {
				return VersionConstraints.ExactMajor | VersionConstraints.ExactMinor;
			} else if (ToHighestMinor.IsPresent) {
				return VersionConstraints.ExactMajor;
			} else {
				return VersionConstraints.None;
			}
		}

		/// <summary>
		/// Determine if the update action should allow use of delisted packages
		/// </summary>
		bool ShouldAllowDelistedPackages ()
		{
			// If a delisted package is already installed, it should be reinstallable too.
			if (Reinstall.IsPresent) {
				return true;
			}

			return false;
		}
	}
}