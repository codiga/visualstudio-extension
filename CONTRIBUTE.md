![build and test workflow](https://github.com/codiga/visualstudio-extension/actions/workflows/main.yml/badge.svg)

# Codiga Visual Studio Extension
Codiga extension for Visual Studio for the fastest code analysis and code snippets management.

# Table of contents
1. [Environment setup](#development-setup)
2. [Getting started](#getting-started)
   1. [Debugging with the VS experimental instance](#debugging)
   2. [Build and test from CLI](#build-cli)
3. [Development](#development)

# Environment setup
To get started with VSIX development I recommend watching [this video](https://youtu.be/u0pRDM8qW04).

## Prerequisites
* Installation of [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
* Installed Visual Studio Extension development workload (via VS Installer)
* [Extensibility Essentials 2022](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.ExtensibilityEssentials2022) extension installed

# Getting started

## Debugging with the VS experimental instance 
This section describes how you can build and debug the extension with the [experimental instance](https://learn.microsoft.com/en-us/visualstudio/extensibility/the-experimental-instance?view=vs-2022) of Visual Studio.

### Start the experimental instance
1. Open `Extension.sln` with Visual Studio
2. Hit `Ctrl + F5` to bring up the experimental instance

The experimental instance will have the extension automatically installed and updated.

> Always check that you don't have a installation of the extension from the marketplace in your experimental instance. If the id changed it can happen that you have two versions of the same extension which will result in conflicts.

### Test out the extension
To see how the extension is working create or open a `.cs`-File and type `.` anywhere to bring up the shortcut completion window. The extension should also appear under *Extensions -> Manage Extensions -> Installed*.

### Run unit tests in Visual Studio
To run the unit tests with the Visual Studio Test Explorer just right click the *Test.csproj* and hit *Run Tests*.

## Build and test from CLI

This section describes how you can build and test the extension with your local Visual Studio installation. Building Visual Studio extension with the `dotnet` command is not supported yet.
### Prerequisites
* Installation of Visual Studio ([Visual Studio 2022](https://visualstudio.microsoft.com/vs/) is recommended)
* MSBuild version >= `17.3.1` (ships with Visual Studio)
* [NUnit.Console](https://github.com/nunit/nunit-console) installed

MSBuild can be found in this folder for a standard installation:

 `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin`
  
  You might want to add this folder to your PATH.
### Build the extension
To build the extension navigate to the project root and execute

`msbuild .\Extension.sln`
### Run unit tests
To run the unit tests execute

`nunit3-console .\src\Tests\Tests.csproj`
### Install the extension
Navigate to the project root and double click

`bin\Debug\Extension.vsix` .

This will install the extension to your local Visual Studio installation.


  

  
