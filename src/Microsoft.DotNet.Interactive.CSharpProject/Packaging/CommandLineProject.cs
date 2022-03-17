// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

// adapted from https://github.com/dotnet/roslyn/blob/master/src/Workspaces/Core/Desktop/Workspace/CommandLineProject.cs

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    internal static class CommandLineProject
    {
        /// <summary>
        /// Create a <see cref="ProjectInfo"/> structure initialized from a compilers command line arguments.
        /// </summary>
        public static ProjectInfo CreateProjectInfo(
            ProjectId projectId,
            string projectName,
            string language,
            CommandLineArguments commandLineArguments,
            string projectDirectory)
        {
            var languageServices = new AdhocWorkspace(MefHostServices.DefaultHost).Services.GetLanguageServices(language);

            if (languageServices == null)
            {
                throw new ArgumentException("WorkspacesResources.Unrecognized_language_name");
            }

            // construct file infos
            var docs = new List<DocumentInfo>();
            foreach (var fileArg in commandLineArguments.SourceFiles)
            {
                var absolutePath = Path.IsPathRooted(fileArg.Path) || string.IsNullOrEmpty(projectDirectory)
                                       ? Path.GetFullPath(fileArg.Path)
                                       : Path.GetFullPath(Path.Combine(projectDirectory, fileArg.Path));

                var relativePath = PathUtilities.GetRelativePath(projectDirectory, absolutePath);
                var isWithinProject = PathUtilities.IsChildPath(projectDirectory, absolutePath);

                var folders = isWithinProject ? GetFolders(relativePath) : null;
                var name = Path.GetFileName(relativePath);
                var id = DocumentId.CreateNewId(projectId, absolutePath);

                var doc = DocumentInfo.Create(
                    id: id,
                    name: name,
                    folders: folders,
                    sourceCodeKind: fileArg.IsScript ? SourceCodeKind.Script : SourceCodeKind.Regular,
                    loader: new FileTextLoader(absolutePath),
                    filePath: absolutePath);

                docs.Add(doc);
            }

            // construct file infos for additional files.
            var additionalDocs = new List<DocumentInfo>();
            foreach (var fileArg in commandLineArguments.AdditionalFiles)
            {
                var absolutePath = Path.IsPathRooted(fileArg.Path) || string.IsNullOrEmpty(projectDirectory)
                                       ? Path.GetFullPath(fileArg.Path)
                                       : Path.GetFullPath(Path.Combine(projectDirectory, fileArg.Path));

                var relativePath = PathUtilities.GetRelativePath(projectDirectory, absolutePath);
                var isWithinProject = PathUtilities.IsChildPath(projectDirectory, absolutePath);

                var folders = isWithinProject ? GetFolders(relativePath) : null;
                var name = Path.GetFileName(relativePath);
                var id = DocumentId.CreateNewId(projectId, absolutePath);

                var doc = DocumentInfo.Create(
                    id: id,
                    name: name,
                    folders: folders,
                    sourceCodeKind: SourceCodeKind.Regular,
                    loader: new FileTextLoader(absolutePath),
                    filePath: absolutePath);

                additionalDocs.Add(doc);
            }

            // If /out is not specified and the project is a console app the csc.exe finds out the Main method
            // and names the compilation after the file that contains it. We don't want to create a compilation, 
            // bind Mains etc. here. Besides the msbuild always includes /out in the command line it produces.
            // So if we don't have the /out argument we name the compilation "<anonymous>".
            string assemblyName = commandLineArguments.OutputFileName != null
                                      ? Path.GetFileNameWithoutExtension(commandLineArguments.OutputFileName)
                                      : "<anonymous>";

            var metadataReferences = commandLineArguments
                                     .MetadataReferences
                                     .Select(r => r.Reference)
                                     .GetMetadataReferences();

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                projectName,
                assemblyName,
                language: language,
                compilationOptions: commandLineArguments
                                    .CompilationOptions
                                    .WithXmlReferenceResolver(new XmlFileResolver(commandLineArguments.BaseDirectory)),
                parseOptions: commandLineArguments.ParseOptions,
                documents: docs,
                additionalDocuments: additionalDocs,
                metadataReferences: metadataReferences);

            return projectInfo;
        }

        private static readonly char[] s_folderSplitters = { Path.DirectorySeparatorChar };

        private static IList<string> GetFolders(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                return ImmutableArray.Create<string>();
            }
            else
            {
                return directory.Split(s_folderSplitters, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
            }
        }
    }
}
